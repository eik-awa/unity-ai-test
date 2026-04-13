using UnityEngine;

/// <summary>
/// タコが放つインク弾。
/// ObjectPool で管理されるため Instantiate / Destroy は行わない。
///
/// 【弾道の見せ方】
/// TrailRenderer は URP 2D Sprite-Lit シェーダーと相性が悪いため使用しない。
/// 代わりにスプライト自体をコメット（彗星）形状にすることで、
/// 追加コンポーネント不要で弾道（進行方向と軌跡）を視覚的に表現する。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
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
    private Texture2D     _bulletTex; // ランタイム生成テクスチャ（手動破棄）

    // ────────────────────────────────────────────────
    //  Unity ライフサイクル
    // ────────────────────────────────────────────────
    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f;

        BuildSprite();
    }

    private void OnDestroy()
    {
        if (_bulletTex != null) Destroy(_bulletTex);
    }

    private void Update()
    {
        if (Vector3.Distance(transform.position, _startPos) >= _range)
            ReturnToPool();
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

        if (other.CompareTag("Wall"))
            ReturnToPool();
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

    /// <summary>
    /// コメット（彗星）型スプライトをランタイムで生成する。
    ///
    /// テクスチャ構造（32px × 8px）:
    ///   右端    ：明るい黄色の弾頭（円形）
    ///   中～左端：黄色→オレンジ→透明のグラデーション尾
    ///
    /// ピボットを弾頭の中心に設定することで、
    /// transform.rotation と一致した方向に尾が伸び、弾道が視覚化される。
    /// TrailRenderer を使わないため URP / Built-in どちらでも確実に表示される。
    /// </summary>
    private void BuildSprite()
    {
        const int w   = 32;   // テクスチャ横幅（コメット全体）
        const int h   = 8;    // テクスチャ縦幅（弾の太さ）
        const int ppu = 16;   // Pixels Per Unit（16px = 1 world unit）

        _bulletTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        _bulletTex.filterMode = FilterMode.Bilinear;
        _bulletTex.wrapMode   = TextureWrapMode.Clamp;

        // 弾頭の円（テクスチャ右端寄り）
        const float headCx = w - 4.5f; // 円中心 X ≈ 27.5
        const float headCy = (h - 1) * 0.5f; // 円中心 Y = 3.5
        const float headR  = 3.5f;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx   = x - headCx;
                float dy   = y - headCy;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                Color col;

                if (dist <= headR)
                {
                    // 弾頭：明るい黄色（不透明）
                    col = new Color(1f, 0.95f, 0.25f, 1f);
                }
                else
                {
                    // 尾：弾頭から離れるほど透明、中心ほど太い
                    float xFactor = Mathf.Clamp01((float)x / (headCx - headR - 1f));
                    float yFactor = Mathf.Clamp01(1f - Mathf.Abs(y - headCy) / (headCy * 0.75f + 1f));
                    float alpha   = xFactor * yFactor * 0.9f;

                    // xFactor 1.0 = 弾頭側（黄色）、0.0 = 末尾（オレンジ）
                    float g = Mathf.Lerp(0.3f, 0.88f, xFactor);
                    col = new Color(1f, g, 0.05f, alpha);
                }

                _bulletTex.SetPixel(x, y, col);
            }
        }

        _bulletTex.Apply();

        // ピボットを弾頭の円中心（x=headCx, y=headCy）に設定
        // → transform.position が弾頭中心 = CircleCollider2D の中心と一致
        float pivotX = headCx / (w - 1f); // ≈ 0.888

        var sr = GetComponent<SpriteRenderer>();
        sr.sprite = Sprite.Create(
            _bulletTex,
            new Rect(0, 0, w, h),
            new Vector2(pivotX, 0.5f),
            pixelsPerUnit: ppu
        );
        sr.color            = Color.white;
        sr.sortingLayerName = "Default";
        sr.sortingOrder     = 10;
    }
}
