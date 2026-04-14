using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// メニュー「OctoShooter → シーンを自動セットアップ」を実行すると
/// プレハブ・ScriptableObject・シーンを全自動で生成します。
/// </summary>
public static class OctoShooterSetup
{
    const string PrefabDir  = "Assets/Prefabs";
    const string SpriteDir  = "Assets/Sprites";
    const string UpgradeDir = "Assets/ScriptableObjects/Upgrades";
    const string SceneDir   = "Assets/Scenes";
    const string ScenePath  = "Assets/Scenes/GameScene.unity";

    // ──────────────────────────────────────────────────
    //  エントリーポイント
    // ──────────────────────────────────────────────────
    [MenuItem("OctoShooter/シーンを自動セットアップ")]
    public static void Run()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        // フォルダ確保
        EnsureFolders();
        AssetDatabase.Refresh();

        // ── スプライト（円形プレースホルダー）──
        Sprite sPlayer = MakeCircleSprite(16, new Color(0.61f, 0.49f, 0.78f), "Player");
        Sprite sShark  = MakeCircleSprite(16, new Color(0.50f, 0.64f, 0.73f), "Shark");
        Sprite sBullet = MakeCircleSprite(16, new Color(0.96f, 0.84f, 0.20f), "Bullet", 8); // 16px / 8PPU = 2ワールド単位（見やすい黄色）
        Sprite sBoss   = MakeCircleSprite(32, new Color(0.90f, 0.30f, 0.30f), "Boss",   8); // 32px / 8PPU = 4ワールド単位（大型ボス）
        AssetDatabase.Refresh();

        // ── プレハブ ──
        GameObject bulletPrefab = MakeBulletPrefab(sBullet);
        GameObject sharkPrefab  = MakeSharkPrefab(sShark);
        GameObject bossPrefab   = MakeBossPrefab(sBoss);
        AssetDatabase.Refresh();

        // ── アップグレード SO（10種） ──
        UpgradeDefinition[] upgrades = MakeDefaultUpgrades();

        // ── 新規シーン（DefaultGameObjects でカメラ付きで作成）──
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // ── シーン構築 ──
        BuildScene(sPlayer, bulletPrefab, sharkPrefab, bossPrefab, upgrades);

        // Physics2D グラビティを 0 に
        SetGravity2DZero();

        // ── 保存 ──
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuild(ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "セットアップ完了！",
            "GameScene が作成されました。\n\n" +
            "▶ Play ボタンを押してゲームを起動できます！\n\n" +
            "【仕様】\n" +
            "・プレイヤーは ♥♥♥ 3ライフ制\n" +
            "・サメに当たると1ライフ消費（3回でゲームオーバー）\n" +
            "・サメは弾3発で倒せる\n" +
            "・弾に黄色→オレンジの軌跡エフェクトあり\n" +
            "・ステージ3・6・9...でボス出現\n\n" +
            "ドット絵に差し替えたい場合は\n" +
            "Assets/Sprites/ の PNG を置き換えてください。",
            "OK！");

        Debug.Log("[OctoShooter] セットアップ完了");
    }

    // ──────────────────────────────────────────────────
    //  フォルダ作成
    // ──────────────────────────────────────────────────
    static void EnsureFolders()
    {
        foreach (var path in new[] { PrefabDir, SpriteDir, UpgradeDir, SceneDir,
                                      "Assets/ScriptableObjects" })
        {
            if (AssetDatabase.IsValidFolder(path)) continue;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/') ?? "Assets";
            string child  = Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, child);
        }
    }

    // ──────────────────────────────────────────────────
    //  円形スプライト生成（ドット絵プレースホルダー）
    //  ppu=0 の場合は size と同じ値を使う（1ワールド単位）
    // ──────────────────────────────────────────────────
    static Sprite MakeCircleSprite(int size, Color color, string baseName, int ppu = 0)
    {
        string path = $"{SpriteDir}/{baseName}.png";
        int sprPPU  = ppu > 0 ? ppu : size;

        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        float r = size * 0.5f - 0.5f;
        var  c  = new Vector2(size * 0.5f - 0.5f, size * 0.5f - 0.5f);

        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                tex.SetPixel(x, y, Vector2.Distance(new Vector2(x, y), c) <= r ? color : Color.clear);
        tex.Apply();

        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path);

        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp != null)
        {
            imp.textureType         = TextureImporterType.Sprite;
            imp.filterMode          = FilterMode.Point;
            imp.mipmapEnabled       = false;
            imp.spritePixelsPerUnit = sprPPU;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // ──────────────────────────────────────────────────
    //  Bullet プレハブ
    // ──────────────────────────────────────────────────
    static GameObject MakeBulletPrefab(Sprite sprite)
    {
        var go = new GameObject("Bullet");

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = sprite;
        sr.sortingOrder = 5;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale          = 0f;
        rb.constraints           = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // 高速でも抜け防止

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius    = 0.4f; // スプライト(2unit)に合わせた半径

        go.AddComponent<Bullet>();

        string path = $"{PrefabDir}/Bullet.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    // ──────────────────────────────────────────────────
    //  Shark プレハブ
    // ──────────────────────────────────────────────────
    static GameObject MakeSharkPrefab(Sprite sprite)
    {
        var go = new GameObject("Shark");

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = sprite;
        sr.sortingOrder = 3;
        sr.color        = new Color(0.50f, 0.64f, 0.73f);

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints  = RigidbodyConstraints2D.FreezeRotation;

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius    = 0.45f;

        var shark = go.AddComponent<SharkEnemy>();
        Set(shark, "spriteRenderer", sr);
        SetFloat(shark, "baseHP",        60f);  // 弾3発(20dmg×3)で倒せる
        SetFloat(shark, "baseMoveSpeed", 2.0f);
        SetFloat(shark, "contactDamage", 10f);  // 接触ダメージ（1ライフ消費）

        string path = $"{PrefabDir}/Shark.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    // ──────────────────────────────────────────────────
    //  Boss プレハブ
    // ──────────────────────────────────────────────────
    static GameObject MakeBossPrefab(Sprite sprite)
    {
        var go = new GameObject("Boss");

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = sprite;
        sr.sortingOrder = 4;
        sr.color        = new Color(0.90f, 0.30f, 0.30f);

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints  = RigidbodyConstraints2D.FreezeRotation;

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius    = 1.8f; // 4ユニットスプライトに合わせた大きめの当たり判定

        var boss = go.AddComponent<BossEnemy>();
        Set(boss,   "spriteRenderer", sr);
        // ボスのステータスを SerializedObject で設定
        SetFloat(boss, "baseHP",        500f);
        SetFloat(boss, "baseMoveSpeed", 2.0f);
        SetFloat(boss, "contactDamage", 30f);
        SetInt  (boss, "scorePoints",   1000);

        string path   = $"{PrefabDir}/Boss.prefab";
        var prefab    = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    // ──────────────────────────────────────────────────
    //  デフォルトアップグレード SO（10種）
    // ──────────────────────────────────────────────────
    static UpgradeDefinition[] MakeDefaultUpgrades()
    {
        var defs = new (string name, UpgradeDefinition.EffectType type, float val)[]
        {
            ("インクパワーUP",  UpgradeDefinition.EffectType.DamageUp,          0.20f),
            ("高速連射",        UpgradeDefinition.EffectType.FireRateUp,         0.20f),
            ("弾速UP",          UpgradeDefinition.EffectType.BulletSpeedUp,      0.20f),
            ("多弾発射",        UpgradeDefinition.EffectType.BulletCountUp,      1f),
            ("最大HP UP",       UpgradeDefinition.EffectType.MaxHPUp,            0.25f),
            ("HP 回復",         UpgradeDefinition.EffectType.HealHP,             0.30f),
            ("鎧の皮膚",        UpgradeDefinition.EffectType.DamageReductionUp,  0.10f),
            ("無敵時間延長",    UpgradeDefinition.EffectType.InvincibilityUp,    0.30f),
            ("スイム速度UP",    UpgradeDefinition.EffectType.MoveSpeedUp,        0.15f),
            ("集中弾",          UpgradeDefinition.EffectType.BulletSpreadDown,   10f),
        };

        var list = new List<UpgradeDefinition>();
        foreach (var (n, t, v) in defs)
        {
            string assetPath = $"{UpgradeDir}/Upgrade_{t}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<UpgradeDefinition>(assetPath);
            if (existing != null) { list.Add(existing); continue; }

            var so = ScriptableObject.CreateInstance<UpgradeDefinition>();
            so.upgradeName = n;
            so.effectType  = t;
            so.value       = v;
            AssetDatabase.CreateAsset(so, assetPath);
            list.Add(so);
        }
        AssetDatabase.SaveAssets();
        return list.ToArray();
    }

    // ──────────────────────────────────────────────────
    //  シーン構築
    // ──────────────────────────────────────────────────
    static void BuildScene(
        Sprite playerSprite,
        GameObject bulletPrefab,
        GameObject sharkPrefab,
        GameObject bossPrefab,
        UpgradeDefinition[] upgrades)
    {
        // ── カメラ ──
        SetupCamera();

        // ── マネージャーオブジェクト（全部1つの GO にまとめる）──
        var mGO = new GameObject("[Managers]");

        var gameMgr    = mGO.AddComponent<GameManager>();
        var stageMgr   = mGO.AddComponent<StageManager>();
        var upgradeMgr = mGO.AddComponent<UpgradeManager>();
        var spawner    = mGO.AddComponent<EnemySpawner>();
        var uiMgr      = mGO.AddComponent<UIManager>();

        // EnemySpawner に Shark・Boss をセット
        Set(spawner, "enemyPrefabs", new Object[]{ sharkPrefab.GetComponent<SharkEnemy>() });
        Set(spawner, "bossPrefab",   bossPrefab.GetComponent<BossEnemy>());

        // StageManager に Spawner をセット
        Set(stageMgr, "spawner", spawner);

        // UpgradeManager にアップグレード一覧をセット
        Set(upgradeMgr, "allUpgrades", upgrades.Cast<Object>().ToArray());

        // ── プレイヤー ──
        var player = BuildPlayer(playerSprite, bulletPrefab);

        // ── Canvas / UI ──
        var ui = BuildUI();

        // ── UIManager にアサイン ──
        Set(uiMgr, "hpSlider",       (Object)ui.hpSlider);
        Set(uiMgr, "hpText",          ui.hpText);
        Set(uiMgr, "stageText",       ui.stageText);
        Set(uiMgr, "enemyCountText",  ui.enemyCountText);
        Set(uiMgr, "scoreText",       ui.scoreText);
        Set(uiMgr, "gameOverPanel",   ui.gameOverPanel);
        Set(uiMgr, "finalScoreText",  ui.finalScoreText);
        Set(uiMgr, "retryButton",     (Object)ui.retryButton);
        Set(uiMgr, "stageClearBanner", ui.stageClearBanner);

        // ── UpgradeManager に UpgradeUI をセット ──
        Set(upgradeMgr, "upgradeUI", ui.upgradeUI);

        // ── モバイル操作UI（ジョイスティック） ──
        var (moveJoy, aimJoy) = BuildMobileControls();

        // プレイヤーにジョイスティックを接続
        player.GetComponent<PlayerController>().SetMoveJoystick(moveJoy);
        player.GetComponent<PlayerShooter>().SetAimJoystick(aimJoy);

        // ── EventSystem ──
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
    }

    // ── カメラ ──────────────────────────────────────
    static void SetupCamera()
    {
        // DefaultGameObjects で作られた Main Camera を取得（なければ自作）
        var camGO = GameObject.FindGameObjectWithTag("MainCamera");
        if (camGO == null)
        {
            camGO     = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
        }

        // Camera コンポーネントが確実にあるようにする
        if (camGO.GetComponent<Camera>() == null)
            camGO.AddComponent<Camera>();

        var cam = camGO.GetComponent<Camera>();
        cam.orthographic      = true;
        cam.orthographicSize  = 5f;
        cam.backgroundColor   = new Color(0.10f, 0.10f, 0.18f); // DarkBg
        cam.clearFlags        = CameraClearFlags.SolidColor;
        cam.transform.position = new Vector3(0f, 0f, -10f);

        // 2D なので AudioListener はそのままでOK（DefaultGameObjects で付いてくる）
    }

    // ── プレイヤー ───────────────────────────────────
    static GameObject BuildPlayer(Sprite sprite, GameObject bulletPrefab)
    {
        var go = new GameObject("Player");
        go.tag = "Player";
        go.transform.position = Vector3.zero;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = sprite;
        sr.sortingOrder = 10;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints  = RigidbodyConstraints2D.FreezeRotation;

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius    = 0.4f;

        var health     = go.AddComponent<PlayerHealth>();
        var controller = go.AddComponent<PlayerController>();
        var shooter    = go.AddComponent<PlayerShooter>();

        Set(health,     "spriteRenderer", sr);
        Set(controller, "spriteRenderer", sr);
        Set(shooter,    "bulletPrefab",   bulletPrefab.GetComponent<Bullet>());

        return go;
    }

    // ──────────────────────────────────────────────────
    //  モバイル操作 UI（仮想ジョイスティック）
    // ──────────────────────────────────────────────────
    static (VirtualJoystick move, VirtualJoystick aim) BuildMobileControls()
    {
        var canvasGO = new GameObject("MobileControls");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20; // HUD より前面

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // 左：移動ジョイスティック
        var moveJoy = MakeJoystick(canvasGO, "MoveJoystick",
            new Vector2(200, 200), new Color(1f, 1f, 1f, 0.25f),
            isLeft: true);

        // 右：照準＆発射ジョイスティック
        var aimJoy = MakeJoystick(canvasGO, "AimJoystick",
            new Vector2(200, 200), new Color(0.96f, 0.84f, 0.20f, 0.30f),
            isLeft: false);

        return (moveJoy, aimJoy);
    }

    static VirtualJoystick MakeJoystick(
        GameObject parent, string name, Vector2 size, Color bgColor, bool isLeft)
    {
        // ── ルート ──
        var root = NewChild(parent, name);
        float xPos = isLeft ? size.x * 0.5f + 40f : -(size.x * 0.5f + 40f);
        var rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = rootRect.anchorMax = isLeft
            ? new Vector2(0f, 0f) : new Vector2(1f, 0f);
        rootRect.sizeDelta        = size;
        rootRect.anchoredPosition = new Vector2(xPos, size.y * 0.5f + 40f);

        // ── バックグラウンド（半透明円） ──
        var bg = NewChild(root, "Background");
        var bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = bgRect.offsetMax = Vector2.zero;
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = bgColor;
        // ※ Unity 標準の白Spriteで円形にはならないが半透明の四角で十分動作する

        // ── ノブ（中心点） ──
        var knob = NewChild(root, "Knob");
        var knobRect = knob.GetComponent<RectTransform>();
        knobRect.anchorMin = knobRect.anchorMax = new Vector2(0.5f, 0.5f);
        knobRect.sizeDelta = new Vector2(80, 80);
        knobRect.anchoredPosition = Vector2.zero;
        var knobImg = knob.AddComponent<Image>();
        knobImg.color = new Color(1f, 1f, 1f, 0.6f);

        // ── VirtualJoystick コンポーネント ──
        var joy = root.AddComponent<VirtualJoystick>();

        // SerializedObject 経由で private フィールドにセット
        var so   = new SerializedObject(joy);
        var bgProp   = so.FindProperty("background");
        var knobProp = so.FindProperty("knob");
        if (bgProp   != null) bgProp.objectReferenceValue   = bgRect;
        if (knobProp != null) knobProp.objectReferenceValue = knobRect;
        so.ApplyModifiedProperties();

        return joy;
    }

    // ──────────────────────────────────────────────────
    //  UI 構築
    // ──────────────────────────────────────────────────
    struct UIRefs
    {
        public UpgradeUI       upgradeUI;
        public Slider          hpSlider;
        public TextMeshProUGUI hpText, stageText, enemyCountText, scoreText, finalScoreText;
        public GameObject      gameOverPanel, stageClearBanner;
        public Button          retryButton;
        public UpgradeCardUI[] cards;
    }

    static UIRefs BuildUI()
    {
        // Canvas
        var canvasGO = new GameObject("Canvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        var ui = new UIRefs();

        // ── HUD ──
        var hud = NewChild(canvasGO, "HUD");
        Stretch(hud);
        hud.AddComponent<CanvasGroup>(); // フェード用（任意）

        ui.hpSlider      = MakeHPSlider(hud);
        ui.hpText        = MakeText(hud, "HPText",      "",        16, new Color(0.95f,0.93f,0.98f),
            AnchorPreset.TopLeft,   new Vector2(170, -53), new Vector2(180, 26));

        ui.stageText     = MakeText(hud, "StageText",   "STAGE 1", 30, new Color(0.95f,0.93f,0.98f),
            AnchorPreset.TopCenter, new Vector2(0, -30),   new Vector2(320, 50));
        ui.stageText.alignment = TextAlignmentOptions.Center;

        ui.enemyCountText = MakeText(hud, "EnemyCount", "敵 x0",  20, new Color(0.83f,0.47f,0.61f),
            AnchorPreset.TopRight,  new Vector2(-120, -28), new Vector2(200, 36));
        ui.enemyCountText.alignment = TextAlignmentOptions.Right;

        ui.scoreText     = MakeText(hud, "ScoreText",   "0",       26, new Color(0.96f, 0.84f, 0.43f),
            AnchorPreset.TopRight,  new Vector2(-120, -66), new Vector2(200, 36));
        ui.scoreText.alignment = TextAlignmentOptions.Right;

        // ── ゲームオーバーパネル ──
        ui.gameOverPanel = NewChild(canvasGO, "GameOverPanel");
        ui.gameOverPanel.AddComponent<Image>().color = new Color(0, 0, 0, 0.82f);
        Stretch(ui.gameOverPanel);
        ui.gameOverPanel.SetActive(false);

        ui.finalScoreText = MakeText(ui.gameOverPanel, "FinalScore", "SCORE\n0", 52,
            new Color(0.95f,0.93f,0.98f),
            AnchorPreset.Center, new Vector2(0, 70), new Vector2(600, 220));
        ui.finalScoreText.alignment = TextAlignmentOptions.Center;

        var retryGO = NewChild(ui.gameOverPanel, "RetryButton");
        AnchorAt(retryGO, AnchorPreset.Center, new Vector2(0, -60), new Vector2(220, 64));
        var retryImg = retryGO.AddComponent<Image>();
        retryImg.color = new Color(0.61f, 0.49f, 0.78f);
        ui.retryButton = retryGO.AddComponent<Button>();
        ui.retryButton.targetGraphic = retryImg;
        var rc = ui.retryButton.colors;
        rc.highlightedColor = new Color(0.72f, 0.62f, 0.83f);
        ui.retryButton.colors = rc;
        var retryLbl = MakeText(retryGO, "Label", "RETRY", 26, new Color(0.95f,0.93f,0.98f),
            AnchorPreset.Center, Vector2.zero, new Vector2(220, 64));
        retryLbl.alignment = TextAlignmentOptions.Center;
        retryLbl.raycastTarget = false;

        // ── ステージクリアバナー ──
        ui.stageClearBanner = NewChild(canvasGO, "StageClearBanner");
        AnchorAt(ui.stageClearBanner, AnchorPreset.Center, Vector2.zero, new Vector2(600, 110));
        ui.stageClearBanner.SetActive(false);
        var clearTxt = MakeText(ui.stageClearBanner, "ClearText", "STAGE CLEAR!", 54,
            new Color(0.96f, 0.84f, 0.43f),
            AnchorPreset.Center, Vector2.zero, new Vector2(600, 110));
        clearTxt.alignment = TextAlignmentOptions.Center;

        // ── アップグレードパネル ──
        var upPanel = NewChild(canvasGO, "UpgradePanel");
        upPanel.AddComponent<Image>().color = new Color(0.10f, 0.10f, 0.18f, 0.92f);
        Stretch(upPanel);
        upPanel.SetActive(false);

        MakeText(upPanel, "Title", "パワーアップを選んでね！", 38, new Color(0.72f, 0.62f, 0.83f),
            AnchorPreset.TopCenter, new Vector2(0, -90), new Vector2(800, 64))
            .alignment = TextAlignmentOptions.Center;

        ui.cards = new UpgradeCardUI[3];
        float[] xs = { -440f, 0f, 440f };
        for (int i = 0; i < 3; i++) ui.cards[i] = MakeCard(upPanel, i, xs[i]);

        ui.upgradeUI = upPanel.AddComponent<UpgradeUI>();
        Set(ui.upgradeUI, "panel", upPanel);
        Set(ui.upgradeUI, "cards", ui.cards.Cast<Object>().ToArray());

        return ui;
    }

    // ── HP スライダー ────────────────────────────────
    static Slider MakeHPSlider(GameObject parent)
    {
        var go = NewChild(parent, "HPSlider");
        AnchorAt(go, AnchorPreset.TopLeft, new Vector2(160, -30), new Vector2(290, 26));
        go.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.3f, 0.85f);

        var slider = go.AddComponent<Slider>();
        slider.minValue  = 0f;
        slider.maxValue  = 1f;
        slider.value     = 1f;
        slider.direction = Slider.Direction.LeftToRight;
        slider.transition = Selectable.Transition.None;

        // Fill Area
        var faGO   = NewChild(go, "Fill Area");
        var faRect = faGO.GetComponent<RectTransform>();
        faRect.anchorMin = Vector2.zero; faRect.anchorMax = Vector2.one;
        faRect.offsetMin = new Vector2(2, 2); faRect.offsetMax = new Vector2(-2, -2);

        var fillGO   = NewChild(faGO, "Fill");
        var fillRect = fillGO.GetComponent<RectTransform>();
        fillRect.anchorMin = fillRect.anchorMax = new Vector2(0, 0.5f);
        fillRect.sizeDelta = Vector2.zero;
        fillGO.AddComponent<Image>().color = new Color(0.83f, 0.47f, 0.61f); // DustyPink

        slider.fillRect = fillRect;
        return slider;
    }

    // ── アップグレードカード ─────────────────────────
    static UpgradeCardUI MakeCard(GameObject parent, int idx, float x)
    {
        var go = NewChild(parent, $"Card_{idx}");
        AnchorAt(go, AnchorPreset.Center, new Vector2(x, 0), new Vector2(350, 440));

        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.18f, 0.21f, 0.38f, 0.96f);

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = bg;
        var bc = btn.colors;
        bc.highlightedColor = new Color(0.27f, 0.32f, 0.58f);
        btn.colors = bc;

        // アイコン枠
        var iconGO = NewChild(go, "Icon");
        AnchorAt(iconGO, AnchorPreset.TopCenter, new Vector2(0, -70), new Vector2(88, 88));
        var iconImg = iconGO.AddComponent<Image>();
        iconImg.color = new Color(0.61f, 0.49f, 0.78f, 0.6f);

        // 名前
        var nameGO = NewChild(go, "NameText");
        var nameRect = nameGO.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0, 0.58f); nameRect.anchorMax = new Vector2(1, 0.78f);
        nameRect.offsetMin = new Vector2(12, 0);    nameRect.offsetMax = new Vector2(-12, 0);
        var nameTmp = nameGO.AddComponent<TextMeshProUGUI>();
        nameTmp.text      = "アップグレード名";
        nameTmp.fontSize  = 22;
        nameTmp.color     = new Color(0.95f, 0.93f, 0.98f);
        nameTmp.alignment = TextAlignmentOptions.Center;

        // 説明
        var descGO = NewChild(go, "DescText");
        var descRect = descGO.GetComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0, 0.1f);  descRect.anchorMax = new Vector2(1, 0.52f);
        descRect.offsetMin = new Vector2(16, 0);    descRect.offsetMax = new Vector2(-16, 0);
        var descTmp = descGO.AddComponent<TextMeshProUGUI>();
        descTmp.text                = "説明テキスト";
        descTmp.fontSize            = 17;
        descTmp.color               = new Color(0.61f, 0.61f, 0.75f);
        descTmp.alignment           = TextAlignmentOptions.Center;
        descTmp.enableWordWrapping  = true;

        var card = go.AddComponent<UpgradeCardUI>();
        Set(card, "iconImage", iconImg);
        Set(card, "nameText",  nameTmp);
        Set(card, "descText",  descTmp);
        Set(card, "button",    btn);

        return card;
    }

    // ──────────────────────────────────────────────────
    //  Physics2D 重力 0
    // ──────────────────────────────────────────────────
    static void SetGravity2DZero()
    {
        try
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/Physics2DSettings.asset");
            if (assets == null || assets.Length == 0) return;
            var so   = new SerializedObject(assets[0]);
            var prop = so.FindProperty("m_Gravity");
            if (prop != null) { prop.vector2Value = Vector2.zero; so.ApplyModifiedProperties(); }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[OctoShooter] Physics2D 重力設定スキップ: {e.Message}");
        }
    }

    // ──────────────────────────────────────────────────
    //  Build Settings に追加
    // ──────────────────────────────────────────────────
    static void AddSceneToBuild(string path)
    {
        var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (list.Any(s => s.path == path)) return;
        list.Insert(0, new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = list.ToArray();
    }

    // ──────────────────────────────────────────────────
    //  UI ヘルパー
    // ──────────────────────────────────────────────────
    enum AnchorPreset { TopLeft, TopCenter, TopRight, Center, BottomCenter }

    static GameObject NewChild(GameObject parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    static void Stretch(GameObject go)
    {
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = r.offsetMin = Vector2.zero;
        r.anchorMax = Vector2.one; r.offsetMax = Vector2.zero;
    }

    static void AnchorAt(GameObject go, AnchorPreset preset, Vector2 pos, Vector2 size)
    {
        var r = go.GetComponent<RectTransform>();
        r.sizeDelta        = size;
        r.anchoredPosition = pos;
        r.anchorMin = r.anchorMax = preset switch
        {
            AnchorPreset.TopLeft    => new Vector2(0f,    1f),
            AnchorPreset.TopCenter  => new Vector2(0.5f,  1f),
            AnchorPreset.TopRight   => new Vector2(1f,    1f),
            AnchorPreset.Center     => new Vector2(0.5f, 0.5f),
            AnchorPreset.BottomCenter => new Vector2(0.5f, 0f),
            _                       => new Vector2(0.5f, 0.5f),
        };
    }

    static TextMeshProUGUI MakeText(
        GameObject parent, string name, string text, float size, Color color,
        AnchorPreset anchor, Vector2 pos, Vector2 sizeDelta)
    {
        var go = NewChild(parent, name);
        AnchorAt(go, anchor, pos, sizeDelta);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text     = text;
        tmp.fontSize = size;
        tmp.color    = color;
        return tmp;
    }

    // ──────────────────────────────────────────────────
    //  SerializedObject ヘルパー（private フィールドをセット）
    // ──────────────────────────────────────────────────
    static void Set(Object target, string field, Object value)
    {
        var so   = new SerializedObject(target);
        var prop = so.FindProperty(field);
        if (prop == null) { Debug.LogWarning($"[Setup] field '{field}' not found on {target.GetType().Name}"); return; }
        prop.objectReferenceValue = value;
        so.ApplyModifiedProperties();
    }

    static void Set(Object target, string field, Object[] values)
    {
        var so   = new SerializedObject(target);
        var prop = so.FindProperty(field);
        if (prop == null) { Debug.LogWarning($"[Setup] field '{field}' not found on {target.GetType().Name}"); return; }
        prop.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        so.ApplyModifiedProperties();
    }

    static void SetFloat(Object target, string field, float value)
    {
        var so   = new SerializedObject(target);
        var prop = so.FindProperty(field);
        if (prop == null) { Debug.LogWarning($"[Setup] field '{field}' not found on {target.GetType().Name}"); return; }
        prop.floatValue = value;
        so.ApplyModifiedProperties();
    }

    static void SetInt(Object target, string field, int value)
    {
        var so   = new SerializedObject(target);
        var prop = so.FindProperty(field);
        if (prop == null) { Debug.LogWarning($"[Setup] field '{field}' not found on {target.GetType().Name}"); return; }
        prop.intValue = value;
        so.ApplyModifiedProperties();
    }
}
