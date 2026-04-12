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

    [Header("スポーン範囲（カメラ範囲より少し外側）")]
    [SerializeField] private float spawnMarginX = 10f;
    [SerializeField] private float spawnMarginY =  6f;

    // ────────────────────────────────────────────────
    //  プール（種類ごとに1つ）
    // ────────────────────────────────────────────────
    private ObjectPool<EnemyBase>[] _pools;

    // ────────────────────────────────────────────────
    //  Unity ライフサイクル
    // ────────────────────────────────────────────────
    private void Awake()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0) return;

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

    // ────────────────────────────────────────────────
    //  公開 API（StageManager から呼ぶ）
    // ────────────────────────────────────────────────

    /// <summary>1体スポーン。stageNumber に応じてスタッツをスケール。</summary>
    public void SpawnEnemy(int stageNumber)
    {
        if (_pools == null || _pools.Length == 0) return;

        // プレハブをランダム選択
        int idx   = Random.Range(0, _pools.Length);
        EnemyBase enemy = _pools[idx].Get();

        // スポーン位置（画面の端から）
        enemy.transform.position = RandomSpawnPosition();

        // ステージに合わせてスタッツを強化
        enemy.ScaleToStage(stageNumber);
    }

    // ────────────────────────────────────────────────
    //  内部
    // ────────────────────────────────────────────────
    private Vector2 RandomSpawnPosition()
    {
        // 上下左右の辺からランダムに選ぶ
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
