using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// メインメニューを彩る泡エフェクト。
/// Canvas の子 GameObject に AddComponent して使う。
/// 下から上へふわふわ漂う円を自動生成・アニメーションする。
/// </summary>
public class BubbleEffect : MonoBehaviour
{
    // Canvas Scaler の reference resolution に合わせた定数
    private const float CanvasW = 1920f;
    private const float CanvasH = 1080f;

    // 泡の色バリエーション（白・水色・薄紫・ピンクを低アルファで）
    private static readonly Color[] BubbleColors =
    {
        new Color(1.00f, 1.00f, 1.00f, 0.40f),
        new Color(0.55f, 0.88f, 0.95f, 0.35f),
        new Color(0.72f, 0.62f, 0.95f, 0.30f),
        new Color(0.95f, 0.72f, 0.85f, 0.28f),
        new Color(0.50f, 0.85f, 0.82f, 0.32f),
    };

    // ── 1 枚だけ作るソフトサークルスプライト ──
    private Sprite    _circleSprite;
    private Texture2D _circleTex;

    // ────────────────────────────────────────────────
    //  Unity ライフサイクル
    // ────────────────────────────────────────────────
    private void Awake()
    {
        _circleSprite = BuildCircleSprite();
    }

    private void OnDestroy()
    {
        // ランタイム生成テクスチャを手動破棄してメモリリーク防止
        if (_circleTex != null) Destroy(_circleTex);
    }

    private void Start()
    {
        // 起動時に画面全体へランダムに散らす（最初から賑やか）
        for (int i = 0; i < 12; i++)
        {
            float startY = Random.Range(-CanvasH * 0.5f, CanvasH * 0.45f);
            StartCoroutine(SpawnAndAnimate(startY));
        }

        StartCoroutine(SpawnLoop());
    }

    // ────────────────────────────────────────────────
    //  定期スポーン
    // ────────────────────────────────────────────────
    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(0.5f, 1.4f));
            StartCoroutine(SpawnAndAnimate(-CanvasH * 0.5f - 70f));
        }
    }

    // ────────────────────────────────────────────────
    //  泡 1 個の生成 + アニメーション
    // ────────────────────────────────────────────────
    private IEnumerator SpawnAndAnimate(float startY)
    {
        // ── 生成 ──
        var go = new GameObject("Bubble");
        go.transform.SetParent(transform, false);

        var rt   = go.AddComponent<RectTransform>();
        float sz = Random.Range(12f, 56f);
        rt.sizeDelta        = new Vector2(sz, sz);
        rt.anchoredPosition = new Vector2(
            Random.Range(-CanvasW * 0.5f, CanvasW * 0.5f), startY);

        var img = go.AddComponent<Image>();
        img.sprite        = _circleSprite;
        img.raycastTarget = false;
        Color col         = BubbleColors[Random.Range(0, BubbleColors.Length)];
        img.color         = col;

        // ── アニメーションパラメータ ──
        float speed     = Random.Range(65f, 155f);   // 上昇速度
        float swaySpd   = Random.Range(0.35f, 1.2f); // 左右ゆれ周期
        float swayAmt   = Random.Range(12f, 60f);    // 左右ゆれ幅
        float startX    = rt.anchoredPosition.x;
        float phase     = Random.Range(0f, Mathf.PI * 2f);
        float elapsed   = 0f;

        // ── 毎フレーム更新 ──
        while (go != null)
        {
            elapsed += Time.deltaTime;

            float y = startY + speed * elapsed;
            float x = startX + Mathf.Sin(phase + elapsed * swaySpd) * swayAmt;
            rt.anchoredPosition = new Vector2(x, y);

            // 上端に近づくと徐々に透明化
            float fade = 1f - Mathf.Clamp01(
                (y - CanvasH * 0.20f) / (CanvasH * 0.32f));
            img.color = new Color(col.r, col.g, col.b, col.a * fade);

            if (y > CanvasH * 0.52f + 80f)
            {
                Destroy(go);
                yield break;
            }
            yield return null;
        }
    }

    // ────────────────────────────────────────────────
    //  ソフトエッジ付き円スプライトをランタイム生成
    // ────────────────────────────────────────────────
    private Sprite BuildCircleSprite()
    {
        const int size = 64;
        _circleTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        _circleTex.wrapMode   = TextureWrapMode.Clamp;
        _circleTex.filterMode = FilterMode.Bilinear;

        float r   = size * 0.5f - 1.5f;
        var   ctr = new Vector2(size * 0.5f - 0.5f, size * 0.5f - 0.5f);

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float d = Vector2.Distance(new Vector2(x, y), ctr);
            // エッジを 2px ぼかして柔らかい円に
            float a = d < r - 2f ? 1f : Mathf.Clamp01(1f - (d - (r - 2f)) * 0.5f);
            _circleTex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        _circleTex.Apply();

        return Sprite.Create(
            _circleTex,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f),
            size);
    }
}
