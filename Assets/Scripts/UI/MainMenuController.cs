using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// メインメニューの制御。
/// タイトルのぷかぷかアニメーションと、ゲームシーンへの遷移を担当する。
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [SerializeField] private RectTransform titleGroup;
    [SerializeField] private Button        startButton;
    [SerializeField] private Image         startButtonImage;

    private static readonly Color BtnNormal  = new Color(0.20f, 0.76f, 0.70f);
    private static readonly Color BtnFlash   = new Color(1f,    1f,    1f   );
    private static readonly Color BtnPressed = new Color(0.14f, 0.54f, 0.50f);

    // ────────────────────────────────────────────────
    //  Unity ライフサイクル
    // ────────────────────────────────────────────────
    private void Start()
    {
        // ボタンのクリックをスクリプトで登録（シーン保存時にも安定）
        if (startButton != null)
            startButton.onClick.AddListener(OnStartButtonClick);

        if (titleGroup != null)
            StartCoroutine(TitleFloat());
    }

    // ────────────────────────────────────────────────
    //  ボタンイベント
    // ────────────────────────────────────────────────
    public void OnStartButtonClick()
    {
        if (startButton == null || !startButton.interactable) return;
        startButton.interactable = false;
        StartCoroutine(StartGameRoutine());
    }

    // ────────────────────────────────────────────────
    //  タイトル：ゆらゆら浮遊アニメーション
    // ────────────────────────────────────────────────
    private IEnumerator TitleFloat()
    {
        float t = 0f;
        while (true)
        {
            t += Time.deltaTime;
            // 上下にゆっくり揺れる
            float offsetY = Mathf.Sin(t * 1.4f) * 12f;
            // 少しだけスケールも脈打つ
            float s = 1f + Mathf.Sin(t * 2.1f) * 0.015f;
            titleGroup.localScale          = new Vector3(s, s, 1f);
            titleGroup.anchoredPosition    = new Vector2(0f, TitleBaseY + offsetY);
            yield return null;
        }
    }

    // タイトルの基準 Y 座標（Setup で anchoredPosition.y を読んで使う）
    private float TitleBaseY = 130f;

    // ────────────────────────────────────────────────
    //  シーン遷移：ボタン点滅 → ロード
    // ────────────────────────────────────────────────
    private IEnumerator StartGameRoutine()
    {
        // 3 回点滅してからロード
        for (int i = 0; i < 3; i++)
        {
            if (startButtonImage) startButtonImage.color = BtnFlash;
            yield return new WaitForSecondsRealtime(0.07f);
            if (startButtonImage) startButtonImage.color = BtnPressed;
            yield return new WaitForSecondsRealtime(0.07f);
        }
        if (startButtonImage) startButtonImage.color = BtnNormal;

        SceneManager.LoadScene("GameScene");
    }
}
