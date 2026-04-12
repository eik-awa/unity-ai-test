using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>
/// ゲーム全体の状態を管理するシングルトン。
/// 他のマネージャーはここを通してゲーム状態を変更する。
/// </summary>
public class GameManager : MonoBehaviour
{
    // ────────────────────────────────────────────────
    //  シングルトン
    // ────────────────────────────────────────────────
    public static GameManager Instance { get; private set; }

    // ────────────────────────────────────────────────
    //  ゲーム状態
    // ────────────────────────────────────────────────
    public enum GameState
    {
        Playing,     // プレイ中
        StageClear,  // ステージクリア（アップグレード選択前の一瞬）
        Upgrading,   // アップグレード選択中
        GameOver,    // ゲームオーバー
    }

    public GameState CurrentState { get; private set; }

    /// <summary>状態が変わったとき通知 (新しい GameState を引数に渡す)</summary>
    public static readonly UnityEvent<GameState> OnStateChanged = new UnityEvent<GameState>();

    // ────────────────────────────────────────────────
    //  スコア / ステージ
    // ────────────────────────────────────────────────
    public int Score      { get; private set; }
    public int StageIndex { get; private set; } // 0-indexed

    // ────────────────────────────────────────────────
    //  Unity ライフサイクル
    // ────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        ChangeState(GameState.Playing);
    }

    // ────────────────────────────────────────────────
    //  公開 API
    // ────────────────────────────────────────────────

    /// <summary>敵を倒したときに呼ぶ</summary>
    public void AddScore(int points)
    {
        Score += points;
        UIManager.Instance?.RefreshScore(Score);
    }

    /// <summary>ステージクリア → アップグレード選択へ</summary>
    public void NotifyStageClear()
    {
        if (CurrentState != GameState.Playing) return;

        StageIndex++;
        ChangeState(GameState.StageClear);

        // 少し間を置いてからアップグレード選択
        Invoke(nameof(EnterUpgrading), 0.8f);
    }

    /// <summary>プレイヤーが死んだとき呼ぶ</summary>
    public void NotifyGameOver()
    {
        if (CurrentState == GameState.GameOver) return;
        ChangeState(GameState.GameOver);
    }

    /// <summary>アップグレード選択が終わったら呼ぶ</summary>
    public void NotifyUpgradeSelected()
    {
        ChangeState(GameState.Playing);
        Time.timeScale = 1f;
        StageManager.Instance?.StartNextStage();
    }

    /// <summary>リトライ（シーンを最初から）</summary>
    public void RestartGame()
    {
        Time.timeScale = 1f;
        Score = 0;
        StageIndex = 0;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ────────────────────────────────────────────────
    //  内部
    // ────────────────────────────────────────────────
    private void ChangeState(GameState newState)
    {
        CurrentState = newState;
        OnStateChanged.Invoke(newState);

        switch (newState)
        {
            case GameState.GameOver:
                Time.timeScale = 0f;
                UIManager.Instance?.ShowGameOver(Score);
                break;

            case GameState.StageClear:
                // 演出中は少しスローにする（任意）
                break;
        }
    }

    private void EnterUpgrading()
    {
        ChangeState(GameState.Upgrading);
        Time.timeScale = 0f;
        UpgradeManager.Instance?.ShowUpgradeSelection();
    }
}
