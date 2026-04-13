using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ゲーム中の HUD（HP・ステージ・敵数・スコア）と
/// ゲームオーバー画面を管理する。
/// </summary>
public class UIManager : MonoBehaviour
{
    // ────────────────────────────────────────────────
    //  シングルトン
    // ────────────────────────────────────────────────
    public static UIManager Instance { get; private set; }

    // ────────────────────────────────────────────────
    //  Inspector（Unity エディタでアサイン）
    // ────────────────────────────────────────────────
    [Header("HUD")]
    [SerializeField] private Slider    hpSlider;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI stageText;
    [SerializeField] private TextMeshProUGUI enemyCountText;
    [SerializeField] private TextMeshProUGUI scoreText;

    [Header("ゲームオーバーパネル")]
    [SerializeField] private GameObject        gameOverPanel;
    [SerializeField] private TextMeshProUGUI   finalScoreText;
    [SerializeField] private Button            retryButton;

    [Header("ステージクリア演出")]
    [SerializeField] private GameObject stageClearBanner;
    [SerializeField] private float      bannerDuration = 0.8f;

    // ────────────────────────────────────────────────
    //  Unity ライフサイクル
    // ────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (gameOverPanel)    gameOverPanel.SetActive(false);
        if (stageClearBanner) stageClearBanner.SetActive(false);

        retryButton?.onClick.AddListener(() => GameManager.Instance?.RestartGame());

        GameManager.OnStateChanged.AddListener(OnGameStateChanged);
    }

    private void OnDestroy()
    {
        GameManager.OnStateChanged.RemoveListener(OnGameStateChanged);
    }

    // ────────────────────────────────────────────────
    //  公開 API（各マネージャーから呼ぶ）
    // ────────────────────────────────────────────────

    public void RefreshHP(float current, float max)
    {
        if (hpSlider)
        {
            hpSlider.value = current / max;
        }
        if (hpText)
        {
            // ライフ制：♥ で残ライフを表示
            int lives    = Mathf.CeilToInt(current);
            int maxLives = Mathf.CeilToInt(max);
            hpText.text  = new string('♥', lives) + new string('♡', maxLives - lives);
        }
    }

    public void RefreshStage(int stageNumber)
    {
        if (stageText) stageText.text = $"STAGE {stageNumber}";
    }

    public void RefreshEnemyCount(int remaining)
    {
        if (enemyCountText) enemyCountText.text = $"敵 x{remaining}";
    }

    public void RefreshScore(int score)
    {
        if (scoreText) scoreText.text = $"{score:N0}";
    }

    public void ShowGameOver(int finalScore)
    {
        if (gameOverPanel) gameOverPanel.SetActive(true);
        if (finalScoreText) finalScoreText.text = $"SCORE\n{finalScore:N0}";
    }

    public void ShowStageClearBanner()
    {
        if (stageClearBanner == null) return;
        stageClearBanner.SetActive(true);
        Invoke(nameof(HideStageClearBanner), bannerDuration);
    }

    // ────────────────────────────────────────────────
    //  内部
    // ────────────────────────────────────────────────
    private void OnGameStateChanged(GameManager.GameState state)
    {
        if (state == GameManager.GameState.StageClear)
        {
            ShowStageClearBanner();
        }
    }

    private void HideStageClearBanner()
    {
        if (stageClearBanner) stageClearBanner.SetActive(false);
    }
}
