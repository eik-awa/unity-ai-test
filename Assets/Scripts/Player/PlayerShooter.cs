using System.Collections;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// タコの射撃管理。
/// マウスカーソルの方向に自動連射する。
/// ObjectPool でインク弾を再利用し、パフォーマンスを確保。
/// </summary>
public class PlayerShooter : MonoBehaviour
{
    // ────────────────────────────────────────────────
    //  Inspector
    // ────────────────────────────────────────────────
    [Header("弾のプレハブ")]
    [SerializeField] private Bullet bulletPrefab;

    [Header("発射位置（タコの先端など）")]
    [SerializeField] private Transform muzzle;

    // ────────────────────────────────────────────────
    //  内部
    // ────────────────────────────────────────────────
    private PlayerStats      _stats;
    private float            _nextFireTime;
    private bool             _canShoot = true;
    private Camera           _cam;
    private ObjectPool<Bullet> _pool;
    private VirtualJoystick  _aimJoystick; // モバイル用照準ジョイスティック

    // ────────────────────────────────────────────────
    //  Unity ライフサイクル
    // ────────────────────────────────────────────────
    private void Awake()
    {
        _cam   = Camera.main;
        _stats = GetComponent<PlayerHealth>()?.Stats ?? new PlayerStats();

        _pool = new ObjectPool<Bullet>(
            createFunc:    () => Instantiate(bulletPrefab),
            actionOnGet:   b  => b.gameObject.SetActive(true),
            actionOnRelease: b => b.gameObject.SetActive(false),
            actionOnDestroy: b => Destroy(b.gameObject),
            collectionCheck: false,
            defaultCapacity: 20,
            maxSize: 50
        );
    }

    private void OnEnable()
    {
        GameManager.OnStateChanged.AddListener(OnGameStateChanged);
    }

    private void OnDisable()
    {
        GameManager.OnStateChanged.RemoveListener(OnGameStateChanged);
    }

    private void Update()
    {
        if (!_canShoot) return;
        if (Time.time < _nextFireTime) return;

        // 常時オート射撃（スマホ：右ジョイスティックで方向指定）
        Fire();
    }

    // ────────────────────────────────────────────────
    //  公開 API
    // ────────────────────────────────────────────────

    /// <summary>PlayerHealth からアップグレード後に呼ぶ</summary>
    public void RefreshStats()
    {
        _stats = GetComponent<PlayerHealth>()?.Stats ?? _stats;
    }

    /// <summary>モバイル用照準ジョイスティックを接続する（OctoShooterSetup から呼ぶ）</summary>
    public void SetAimJoystick(VirtualJoystick joystick)
    {
        _aimJoystick = joystick;
    }

    /// <summary>Bullet が範囲外に出たときプールに返す</summary>
    public void ReturnBulletToPool(Bullet bullet)
    {
        _pool.Release(bullet);
    }

    // ────────────────────────────────────────────────
    //  内部
    // ────────────────────────────────────────────────
    private void Fire()
    {
        _nextFireTime = Time.time + _stats.fireRate;

        Vector2 aimDir = GetAimDirection();

        if (_stats.bulletCount == 1)
        {
            SpawnBullet(aimDir, 0f);
        }
        else
        {
            // 複数弾: 均等に広げる
            float halfSpread = _stats.bulletSpread * 0.5f;
            float step       = _stats.bulletCount > 1
                ? _stats.bulletSpread / (_stats.bulletCount - 1)
                : 0f;

            for (int i = 0; i < _stats.bulletCount; i++)
            {
                float angleOffset = -halfSpread + step * i;
                SpawnBullet(aimDir, angleOffset);
            }
        }
    }

    private void SpawnBullet(Vector2 baseDir, float angleOffsetDeg)
    {
        Vector2 dir = Rotate(baseDir, angleOffsetDeg);
        Transform origin = muzzle != null ? muzzle : transform;

        Bullet b = _pool.Get();
        b.transform.position = origin.position;
        b.Initialize(dir, _stats.bulletSpeed, _stats.bulletDamage, _stats.bulletRange, this);
    }

    /// <summary>照準方向：右ジョイスティック優先、なければマウスカーソル方向</summary>
    private Vector2 GetAimDirection()
    {
        // スマホ：右ジョイスティックの方向
        if (_aimJoystick != null && _aimJoystick.Direction.sqrMagnitude > 0.01f)
            return _aimJoystick.Direction;

        // PC：マウスカーソルへの方向
        Vector3 mouseWorld = _cam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;
        Vector2 dir = (mouseWorld - transform.position).normalized;
        return dir == Vector2.zero ? Vector2.right : dir;
    }

    private static Vector2 Rotate(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(cos * v.x - sin * v.y, sin * v.x + cos * v.y);
    }

    private void OnGameStateChanged(GameManager.GameState state)
    {
        _canShoot = (state == GameManager.GameState.Playing);
    }
}
