using System.Collections;
using UnityEngine;

/// <summary>
/// ステージ進行を管理する。
/// 各ステージはウェーブ（敵の群れ）で構成される。
/// 全ての敵を倒すとステージクリア → GameManager へ通知。
/// bossStageInterval ごとにボスが追加で出現する。
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
    [SerializeField] private int   baseEnemyCount    = 5;   // ステージ1の敵数
    [SerializeField] private float enemyCountGrowth  = 2f;  // ステージごとに増える数
    [SerializeField] private float spawnInterval     = 1.5f;// 敵スポーン間隔（秒）

    [Header("ボス設定")]
    [SerializeField] private int bossStageInterval = 3; // 3ステージごとにボス出現

    // ────────────────────────────────────────────────
    //  状態
    // ────────────────────────────────────────────────
    private int  _enemiesRemaining;
    private int  _currentStage = 0; // 1-indexed
    private bool _bossAlive    = false;

    public bool IsBossStage => _currentStage > 0 && _currentStage % bossStageInterval == 0;

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
            _bossAlive = false;
            GameManager.Instance?.NotifyStageClear();
        }
    }

    // ────────────────────────────────────────────────
    //  内部
    // ────────────────────────────────────────────────
    private void StartStage(int stageNumber)
    {
        _currentStage    = stageNumber;
        _bossAlive       = false;

        int enemyCount   = baseEnemyCount + (int)((stageNumber - 1) * enemyCountGrowth);
        int totalCount   = enemyCount + (IsBossStage ? 1 : 0);
        _enemiesRemaining = totalCount;

        UIManager.Instance?.RefreshStage(stageNumber);
        UIManager.Instance?.RefreshEnemyCount(totalCount);

        StartCoroutine(SpawnWave(enemyCount));
    }

    private IEnumerator SpawnWave(int count)
    {
        yield return new WaitUntil(() =>
            GameManager.Instance?.CurrentState == GameManager.GameState.Playing);

        for (int i = 0; i < count; i++)
        {
            spawner.SpawnEnemy(_currentStage);
            yield return new WaitForSeconds(spawnInterval);
        }

        // ボスステージの場合：通常ウェーブ後にボスが登場
        if (IsBossStage)
        {
            yield return new WaitForSeconds(spawnInterval);
            bool bossSpawned = spawner.SpawnBoss(_currentStage);
            if (bossSpawned)
            {
                _bossAlive = true;
                // ボス登場をUIに通知（後でバナー追加可能）
                Debug.Log($"[StageManager] ボス登場！ Stage {_currentStage}");
            }
        }
    }
}
