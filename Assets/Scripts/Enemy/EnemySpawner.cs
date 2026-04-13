using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// 敵をスポーンするクラス。
/// 画面端のランダムな位置から敵を出現させる。
/// ObjectPool で敵を再利用する。
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    // ────────────────────────────────────────────────
    //  Inspector
    // ────────────────────────────────────────────────
    [Header("敵プレハブ（増やすほど種類が増える）")]
    [SerializeField] private EnemyBase[] enemyPrefabs;

    [Header("ボスプレハブ")]
    [SerializeField] private BossEnemy bossPrefab;

    [Header("スポーン範囲（カメラ範囲より少し外側）")]
    [SerializeField] private float spawnMarginX = 10f;
    [SerializeField] private float spawnMarginY =  6f;

    // ────────────────────────────────────────────────
    //  プール（種類ごとに1つ）
    // ────────────────────────────────────────────────
    private ObjectPool<EnemyBase>[] _pools;
    private ObjectPool<BossEnemy>   _bossPool;

    // ────────────────────────────────────────────────
    //  Unity ライフサイクル
    // ────────────────────────────────────────────────
    private void Awake()
    {
        if (enemyPrefabs != null && enemyPrefabs.Length > 0)
        {
            _pools = new ObjectPool<EnemyBase>[enemyPrefabs.Length];
            for (int i = 0; i < enemyPrefabs.Length; i++)
            {
                int idx = i; // クロージャ用
                _pools[i] = new ObjectPool<EnemyBase>(
                    createFunc:      () => Instantiate(enemyPrefabs[idx]),
                    actionOnGet:     e  => e.gameObject.SetActive(true),
                    actionOnRelease: e  => e.gameObject.SetActive(false),
                    actionOnDestroy: e  => Destroy(e.gameObject),
                    collectionCheck: false,
                    defaultCapacity: 10,
                    maxSize: 30
                );
            }
        }

        if (bossPrefab != null)
        {
            _bossPool = new ObjectPool<BossEnemy>(
                createFunc:      () => Instantiate(bossPrefab),
                actionOnGet:     e  => e.gameObject.SetActive(true),
                actionOnRelease: e  => e.gameObject.SetActive(false),
                actionOnDestroy: e  => Destroy(e.gameObject),
                collectionCheck: false,
                defaultCapacity: 2,
                maxSize: 4
            );
        }
    }

    // ────────────────────────────────────────────────
    //  公開 API（StageManager から呼ぶ）
    // ────────────────────────────────────────────────

    /// <summary>通常敵を1体スポーン。stageNumber に応じてスタッツをスケール。</summary>
    public void SpawnEnemy(int stageNumber)
    {
        if (_pools == null || _pools.Length == 0) return;

        int idx = Random.Range(0, _pools.Length);
        var pool = _pools[idx];

        EnemyBase enemy = pool.Get();
        enemy.OnReturnToPool = e => pool.Release(e); // プール返却コールバックを設定
        enemy.transform.position = RandomSpawnPosition();
        enemy.ScaleToStage(stageNumber);
    }

    /// <summary>ボスをスポーン。stageNumber に応じてスタッツをスケール。</summary>
    public bool SpawnBoss(int stageNumber)
    {
        if (_bossPool == null) return false;

        BossEnemy boss = _bossPool.Get();
        boss.OnReturnToPool = e => _bossPool.Release((BossEnemy)e);

        // ボスは画面上端中央から登場
        boss.transform.position = new Vector2(0f, spawnMarginY - 1f);
        boss.ScaleToStage(stageNumber);
        return true;
    }

    // ────────────────────────────────────────────────
    //  内部
    // ────────────────────────────────────────────────
    private Vector2 RandomSpawnPosition()
    {
        int side = Random.Range(0, 4); // 0:上 1:下 2:左 3:右
        return side switch
        {
            0 => new Vector2(Random.Range(-spawnMarginX, spawnMarginX),  spawnMarginY),
            1 => new Vector2(Random.Range(-spawnMarginX, spawnMarginX), -spawnMarginY),
            2 => new Vector2(-spawnMarginX, Random.Range(-spawnMarginY,  spawnMarginY)),
            _ => new Vector2( spawnMarginX, Random.Range(-spawnMarginY,  spawnMarginY)),
        };
    }
}
