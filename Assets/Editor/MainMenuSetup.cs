using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

/// <summary>
/// メニュー「OctoShooter → メインメニューをセットアップ」を実行すると
/// ポップでかわいいメインメニューシーンを自動生成します。
///
/// 【ビジュアルコンセプト】
///   ・深海をイメージした暗いネイビー背景
///   ・下から上へ漂う半透明バブル（BubbleEffect）
///   ・アクセントイエロー + 紫シャドウの大きなタイトル
///   ・明るいミントグリーンの START ボタン
/// </summary>
public static class MainMenuSetup
{
    const string ScenePath = "Assets/Scenes/MainMenuScene.unity";

    // ── カラー定数（GameColors に合わせた値）──────────
    static readonly Color BgDeep      = new Color(0.04f, 0.07f, 0.13f); // 最暗・深海
    static readonly Color BgMid       = new Color(0.06f, 0.11f, 0.20f); // 中層
    static readonly Color BgGlow      = new Color(0.10f, 0.28f, 0.42f, 0.35f); // 上部光
    static readonly Color TitleMain   = new Color(0.96f, 0.84f, 0.20f); // AccentYellow
    static readonly Color TitleShadow = new Color(0.61f, 0.31f, 0.49f, 0.70f); // 紫影
    static readonly Color SubTitle    = new Color(0.70f, 0.88f, 0.93f); // AquaLight
    static readonly Color BtnColor    = new Color(0.20f, 0.76f, 0.70f); // ミントグリーン
    static readonly Color BtnHover    = new Color(0.30f, 0.88f, 0.82f);
    static readonly Color BtnText     = new Color(1f,    1f,    1f   );
    static readonly Color FooterText  = new Color(0.38f, 0.38f, 0.52f);
    static readonly Color WaveLine    = new Color(0.29f, 0.55f, 0.65f, 0.45f);
    static readonly Color DecoA       = new Color(0.61f, 0.49f, 0.78f, 0.18f); // 紫デコ
    static readonly Color DecoB       = new Color(0.29f, 0.48f, 0.54f, 0.22f); // ティールデコ
    static readonly Color DecoC       = new Color(0.83f, 0.47f, 0.61f, 0.15f); // ピンクデコ

    // ────────────────────────────────────────────────
    //  エントリーポイント
    // ────────────────────────────────────────────────
    [MenuItem("OctoShooter/メインメニューをセットアップ")]
    public static void Run()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        // 新規シーン（カメラ付き）
        var scene = EditorSceneManager.NewScene(
            NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // ── カメラ ──
        SetupCamera();

        // ── Canvas ──
        var canvasGO = BuildCanvas();

        // ── 背景レイヤー ──
        AddBackgroundLayers(canvasGO);

        // ── デコレーション大円（ボケ感） ──
        AddBokeCircles(canvasGO);

        // ── バブルエフェクト ──
        AddBubbleEffect(canvasGO);

        // ── タイトルグループ ──
        var titleGroup = AddTitleGroup(canvasGO);

        // ── 仕切りライン ──
        AddDividerLine(canvasGO);

        // ── スタートボタン ──
        var (btn, btnImg) = AddStartButton(canvasGO);

        // ── フッター ──
        AddFooter(canvasGO);

        // ── EventSystem ──
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

        // ── コントローラー ──
        AddController(titleGroup, btn, btnImg);

        // ── 保存 & ビルド設定 ──
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildFront(ScenePath);
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "メインメニュー完成！",
            "MainMenuScene が作成されました。\n\n" +
            "▶ Play ボタンで確認できます！\n\n" +
            "【仕様】\n" +
            "・START ボタンで GameScene に遷移\n" +
            "・タイトルがゆらゆら浮遊\n" +
            "・バブルが下から上へ漂う\n" +
            "・Build Settings の index 0 に自動登録",
            "かわいい！");

        Debug.Log("[OctoShooter] メインメニューセットアップ完了");
    }

    // ────────────────────────────────────────────────
    //  カメラ
    // ────────────────────────────────────────────────
    static void SetupCamera()
    {
        var camGO = GameObject.FindGameObjectWithTag("MainCamera")
                   ?? new GameObject("Main Camera") { tag = "MainCamera" };
        if (camGO.GetComponent<Camera>() == null) camGO.AddComponent<Camera>();

        var cam = camGO.GetComponent<Camera>();
        cam.orthographic     = true;
        cam.orthographicSize = 5f;
        cam.backgroundColor  = BgDeep;
        cam.clearFlags       = CameraClearFlags.SolidColor;
        cam.transform.position = new Vector3(0f, 0f, -10f);
    }

    // ────────────────────────────────────────────────
    //  Canvas（Screen Space Overlay, 1920×1080 基準）
    // ────────────────────────────────────────────────
    static GameObject BuildCanvas()
    {
        var go     = new GameObject("MainMenuCanvas");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight  = 0.5f;

        go.AddComponent<GraphicRaycaster>();
        return go;
    }

    // ────────────────────────────────────────────────
    //  背景レイヤー（深海グラデーション風）
    // ────────────────────────────────────────────────
    static void AddBackgroundLayers(GameObject canvas)
    {
        // 最深層：全画面ネイビー
        var bg = NewChild(canvas, "BG_Deep");
        Stretch(bg);
        bg.AddComponent<Image>().color = BgDeep;

        // 中層：下 40% をほんの少し明るく
        var mid = NewChild(canvas, "BG_Mid");
        var midRT = mid.GetComponent<RectTransform>();
        midRT.anchorMin = new Vector2(0f, 0f);
        midRT.anchorMax = new Vector2(1f, 0.40f);
        midRT.offsetMin = midRT.offsetMax = Vector2.zero;
        mid.AddComponent<Image>().color = BgMid;

        // 上部グロー：画面上端に淡いティール
        var glow = NewChild(canvas, "BG_Glow");
        var glowRT = glow.GetComponent<RectTransform>();
        glowRT.anchorMin = new Vector2(0f, 0.62f);
        glowRT.anchorMax = new Vector2(1f, 1.00f);
        glowRT.offsetMin = glowRT.offsetMax = Vector2.zero;
        glow.AddComponent<Image>().color = BgGlow;
    }

    // ────────────────────────────────────────────────
    //  背景ボケ大円（深度感・ポップ感）
    // ────────────────────────────────────────────────
    static void AddBokeCircles(GameObject canvas)
    {
        var layer = NewChild(canvas, "Deco_Boke");
        Stretch(layer);

        // (pos, size, color)
        var circles = new (Vector2 pos, float size, Color color)[]
        {
            (new Vector2(-680f,  280f), 480f, DecoA),  // 左上：紫
            (new Vector2( 750f,  320f), 360f, DecoB),  // 右上：ティール
            (new Vector2(-720f, -300f), 320f, DecoC),  // 左下：ピンク
            (new Vector2( 680f, -260f), 400f, DecoA),  // 右下：紫
            (new Vector2(   0f,  420f), 560f, DecoB),  // 上中：ティール（大）
            (new Vector2(   0f, -440f), 500f, DecoC),  // 下中：ピンク（大）
        };

        foreach (var (pos, size, color) in circles)
        {
            var go = NewChild(layer, "BokeCircle");
            AnchorAt(go, AnchorPreset.Center, pos, new Vector2(size, size));
            go.AddComponent<Image>().color = color;
        }
    }

    // ────────────────────────────────────────────────
    //  バブルエフェクト
    // ────────────────────────────────────────────────
    static void AddBubbleEffect(GameObject canvas)
    {
        var go = NewChild(canvas, "BubbleEffect");
        Stretch(go);
        go.AddComponent<BubbleEffect>();
    }

    // ────────────────────────────────────────────────
    //  タイトルグループ
    // ────────────────────────────────────────────────
    static RectTransform AddTitleGroup(GameObject canvas)
    {
        // グループ（アニメーション用の親）
        var group = NewChild(canvas, "TitleGroup");
        AnchorAt(group, AnchorPreset.Center, new Vector2(0f, 130f), new Vector2(1000f, 200f));
        var groupRT = group.GetComponent<RectTransform>();

        // ── シャドウ（少し下にずらした同じ文字）──
        var shadow = NewChild(group, "TitleShadow");
        AnchorAt(shadow, AnchorPreset.Center, new Vector2(5f, -5f), new Vector2(1000f, 140f));
        var shadowTmp = shadow.AddComponent<TextMeshProUGUI>();
        shadowTmp.text      = "OctoShooter";
        shadowTmp.fontSize  = 96f;
        shadowTmp.color     = TitleShadow;
        shadowTmp.alignment = TextAlignmentOptions.Center;
        shadowTmp.raycastTarget = false;

        // ── メインタイトル ──
        var title = NewChild(group, "TitleMain");
        AnchorAt(title, AnchorPreset.Center, Vector2.zero, new Vector2(1000f, 140f));
        var titleTmp = title.AddComponent<TextMeshProUGUI>();
        titleTmp.text      = "OctoShooter";
        titleTmp.fontSize  = 96f;
        titleTmp.color     = TitleMain;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.raycastTarget = false;

        // ── サブタイトル ──
        var sub = NewChild(group, "Subtitle");
        AnchorAt(sub, AnchorPreset.Center, new Vector2(0f, -80f), new Vector2(800f, 46f));
        var subTmp = sub.AddComponent<TextMeshProUGUI>();
        subTmp.text      = "〜 タコの大冒険シューティング 〜";
        subTmp.fontSize  = 26f;
        subTmp.color     = SubTitle;
        subTmp.alignment = TextAlignmentOptions.Center;
        subTmp.raycastTarget = false;

        return groupRT;
    }

    // ────────────────────────────────────────────────
    //  仕切りライン（タイトル〜ボタン間）
    // ────────────────────────────────────────────────
    static void AddDividerLine(GameObject canvas)
    {
        var go = NewChild(canvas, "DividerLine");
        AnchorAt(go, AnchorPreset.Center, new Vector2(0f, -18f), new Vector2(820f, 2f));
        go.AddComponent<Image>().color = WaveLine;
    }

    // ────────────────────────────────────────────────
    //  スタートボタン
    // ────────────────────────────────────────────────
    static (Button btn, Image img) AddStartButton(GameObject canvas)
    {
        var go = NewChild(canvas, "StartButton");
        AnchorAt(go, AnchorPreset.Center, new Vector2(0f, -120f), new Vector2(420f, 80f));

        var img = go.AddComponent<Image>();
        img.color = BtnColor;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        var colors = btn.colors;
        colors.normalColor      = BtnColor;
        colors.highlightedColor = BtnHover;
        colors.pressedColor     = new Color(0.14f, 0.54f, 0.50f);
        colors.selectedColor    = BtnColor;
        btn.colors = colors;

        // ボタンラベル
        var label = NewChild(go, "Label");
        AnchorAt(label, AnchorPreset.Center, Vector2.zero, new Vector2(420f, 80f));
        var lbl = label.AddComponent<TextMeshProUGUI>();
        lbl.text           = "▶   START  !";
        lbl.fontSize       = 38f;
        lbl.color          = BtnText;
        lbl.alignment      = TextAlignmentOptions.Center;
        lbl.raycastTarget  = false;
        lbl.fontStyle      = FontStyles.Bold;

        return (btn, img);
    }

    // ────────────────────────────────────────────────
    //  フッター
    // ────────────────────────────────────────────────
    static void AddFooter(GameObject canvas)
    {
        var go = NewChild(canvas, "Footer");
        AnchorAt(go, AnchorPreset.BottomCenter, new Vector2(0f, 24f), new Vector2(600f, 30f));
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = "© OctoShooter  ─  いのちの輝き";
        tmp.fontSize  = 15f;
        tmp.color     = FooterText;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
    }

    // ────────────────────────────────────────────────
    //  MainMenuController を配置してリファレンスを接続
    // ────────────────────────────────────────────────
    static void AddController(RectTransform titleGroup, Button btn, Image btnImg)
    {
        var go         = new GameObject("[MenuController]");
        var controller = go.AddComponent<MainMenuController>();

        var so = new SerializedObject(controller);

        var pTitle  = so.FindProperty("titleGroup");
        var pBtn    = so.FindProperty("startButton");
        var pBtnImg = so.FindProperty("startButtonImage");

        if (pTitle  != null) pTitle .objectReferenceValue = titleGroup;
        if (pBtn    != null) pBtn   .objectReferenceValue = btn;
        if (pBtnImg != null) pBtnImg.objectReferenceValue = btnImg;

        so.ApplyModifiedProperties();
    }

    // ────────────────────────────────────────────────
    //  Build Settings：メインメニューを index 0 に挿入
    // ────────────────────────────────────────────────
    static void AddSceneToBuildFront(string path)
    {
        var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

        // 重複除去
        list.RemoveAll(s => s.path == path);

        // 先頭に挿入（index 0 = 起動時に最初に開くシーン）
        list.Insert(0, new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = list.ToArray();
    }

    // ────────────────────────────────────────────────
    //  UI ヘルパー
    // ────────────────────────────────────────────────
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
        var r      = go.GetComponent<RectTransform>();
        r.anchorMin = r.offsetMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMax = Vector2.zero;
    }

    static void AnchorAt(GameObject go, AnchorPreset preset, Vector2 pos, Vector2 size)
    {
        var r = go.GetComponent<RectTransform>();
        r.sizeDelta        = size;
        r.anchoredPosition = pos;
        r.anchorMin = r.anchorMax = preset switch
        {
            AnchorPreset.TopLeft      => new Vector2(0f,   1f  ),
            AnchorPreset.TopCenter    => new Vector2(0.5f, 1f  ),
            AnchorPreset.TopRight     => new Vector2(1f,   1f  ),
            AnchorPreset.Center       => new Vector2(0.5f, 0.5f),
            AnchorPreset.BottomCenter => new Vector2(0.5f, 0f  ),
            _                         => new Vector2(0.5f, 0.5f),
        };
    }
}
