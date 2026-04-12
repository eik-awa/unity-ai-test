using UnityEngine;

/// <summary>
/// タコが放つインク弾。
/// ObjectPool で管理されるため Instantiate / Destroy は行わない。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Bullet : MonoBehaviour
{
    // ────────────────────────────────────────────────
    //  内部
    // ────────────────────────────────────────────────
    private Rigidbody2D   _rb;
    private float         _damage;
    private float         _range;
    private float         _speed;
    private Vector2       _direction;
    private Vector3       _startPos;
    private PlayerShooter _owner;

    // ────────────────────────────────────────────────
    //  Unity ライフサイクル
    // ────────────────────────────────────────────────
    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f;
    }

    private void Update()
    {
        // 射程を超えたらプールに戻す
        if (Vector3.Distance(transform.position, _startPos) >= _range)
        {
            ReturnToPool();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        EnemyBase enemy = other.GetComponentInParent<EnemyBase>();
        if (enemy != null)
        {
            enemy.TakeDamage(_damage);
            ReturnToPool();
            return;
        }

        // 壁タグ（オプション）
        if (other.CompareTag("Wall"))
        {
            ReturnToPool();
        }
    }

    // ────────────────────────────────────────────────
    //  公開 API
    // ────────────────────────────────────────────────

    /// <summary>プールから取得直後に呼ぶ初期化</summary>
    public void Initialize(Vector2 direction, float speed, float damage, float range, PlayerShooter owner)
    {
        _direction = direction.normalized;
        _speed     = speed;
        _damage    = damage;
        _range     = range;
        _owner     = owner;
        _startPos  = transform.position;

        // 弾の向きをスプライトに反映
        float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        _rb.linearVelocity = _direction * _speed;
    }

    // ────────────────────────────────────────────────
    //  内部
    // ────────────────────────────────────────────────
    private void ReturnToPool()
    {
        _rb.linearVelocity = Vector2.zero;
        _owner?.ReturnBulletToPool(this);
    }
}
