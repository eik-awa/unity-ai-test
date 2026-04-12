using System.Collections;
using UnityEngine;

/// <summary>
/// ステージ進行を管理する。
/// 各ステージはウェーブ（敵の群れ）で構成される。
/// 全ての敵を倒すとステージクリア → GameManager へ通知。
/// </summary>
public class StageManager : MonoBehaviour
{
    // ────────────────────────────────────────────────
    //  シングルトン
    // ────────────────────────────────────────────────
    public static StageManager Instance { get; private set; }

    // ────────────────────────────────────────────────
    //  Inspector
    // ────────────────────────────────────────────────
    [Header("スポーン")]
    [SerializeField] private EnemySpawner spawner;

    [Header("ステージ設定")]
    [SerializeField] private int   baseEnemyCount   = 5;   // ステージ1の敵数
    [SerializeField] private float enemyCountGrowth = 2f;  // ステージごとに増える数
    [SerializeField] private float spawnInterval    = 1.5f;// 敵スポーン間隔（秒）

    // ────────────────────────────────────────────────
    //  状態
    // ────────────────────────────────────────────────
    private int _enemiesRemaining;
    private int _currentStage = 0; // 1-indexed

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
        StartStage(1);
    }

    // ────────────────────────────────────────────────
    //  公開 API
    // ────────────────────────────────────────────────

    /// <summary>GameManager からアップグレード後に呼ばれる</summary>
    public void StartNextStage()
    {
        StartStage(_currentStage + 1);
    }

    /// <summary>敵が死んだとき EnemyBase から呼ぶ</summary>
    public void NotifyEnemyDied()
    {
        _enemiesRemaining--;
        UIManager.Instance?.RefreshEnemyCount(_enemiesRemaining);

        if (_enemiesRemaining <= 0)
        {
            GameManager.Instance?.NotifyStageClear();
        }
    }

    // ────────────────────────────────────────────────
    //  内部
    // ────────────────────────────────────────────────
    private void StartStage(int stageNumber)
    {
        _currentStage = stageNumber;
        int enemyCount = baseEnemyCount + (int)((stageNumber - 1) * enemyCountGrowth);
        _enemiesRemaining = enemyCount;

        UIManager.Instance?.RefreshStage(stageNumber);
        UIManager.Instance?.RefreshEnemyCount(enemyCount);

        StartCoroutine(SpawnWave(enemyCount));
    }

    private IEnumerator SpawnWave(int count)
    {
        // ゲームが再開されるまで待つ（アップグレード画面から戻った直後対応）
        yield return new WaitUntil(() =>
            GameManager.Instance?.CurrentState == GameManager.GameState.Playing);

        for (int i = 0; i < count; i++)
        {
            spawner.SpawnEnemy(_currentStage);
            yield return new WaitForSeconds(spawnInterval);
        }
    }
}
