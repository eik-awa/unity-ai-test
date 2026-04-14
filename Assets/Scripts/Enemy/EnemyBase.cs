using System.Collections;
using UnityEngine;

/// <summary>
/// 全敵キャラの基底クラス。
/// HP・移動速度・ダメージ・スコアポイントを持つ。
/// 継承クラスで UpdateBehavior() をオーバーライドして AI を実装する。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public abstract class EnemyBase : MonoBehaviour
{
    // ────────────────────────────────────────────────
    //  Inspector（継承先でも変更可）
    // ────────────────────────────────────────────────
    [Header("基本パラメータ")]
    [SerializeField] protected float baseHP        = 50f;
    [SerializeField] protected float baseMoveSpeed = 2.5f;
    [SerializeField] protected float contactDamage = 15f;
    [SerializeField] protected int   scorePoints   = 100;

    [Header("エフェクト")]
    [SerializeField] protected SpriteRenderer spriteRenderer;

    // ────────────────────────────────────────────────
    //  プロパティ
    // ────────────────────────────────────────────────
    public bool IsDead { get; private set; }

    /// <summary>EnemySpawner がプール返却用に設定するコールバック</summary>
    public System.Action<EnemyBase> OnReturnToPool;

    protected float        CurrentHP;
    protected float        MoveSpeed;
    protected Rigidbody2D  Rb { get; private set; }
    protected Transform    PlayerTransform;

    // ────────────────────────────────────────────────
    //  ビジュアル状態キャッシュ
    // ────────────────────────────────────────────────
    protected Color _originalColor;
    private Vector3 _originalScale;
    private float   _baseHPOriginal;
    private float   _baseMoveSpeedOriginal;
    private int     _scorePointsOriginal;
    private Coroutine _hitFlashCoroutine;

    // ────────────────────────────────────────────────
    //  Unity ライフサイクル
    // ────────────────────────────────────────────────
    protected virtual void Awake()
    {
        Rb = GetComponent<Rigidbody2D>();
        Rb.gravityScale = 0f;
        Rb.constraints  = RigidbodyConstraints2D.FreezeRotation;

        // プレハブ設定値を元値として保存（ScaleToStage で上書きされても使える）
        _baseHPOriginal        = baseHP;
        _baseMoveSpeedOriginal = baseMoveSpeed;
        _scorePointsOriginal   = scorePoints;
        _originalScale         = transform.localScale;
        if (spriteRenderer != null) _originalColor = spriteRenderer.color;
    }

    protected virtual void OnEnable()
    {
        IsDead    = false;
        CurrentHP = baseHP;
        MoveSpeed = baseMoveSpeed;

        // プール再利用時にビジュアルをリセット
        transform.localScale = _originalScale;
        if (spriteRenderer != null) spriteRenderer.color = _originalColor;

        var player = PlayerController.Instance;
        if (player != null) PlayerTransform = player.transform;
    }

    protected virtual void Update()
    {
        if (IsDead) return;
        UpdateBehavior();
    }

    // ────────────────────────────────────────────────
    //  抽象メソッド（継承先で実装）
    // ────────────────────────────────────────────────

    /// <summary>敵の AI ロジック。毎フレーム呼ばれる。</summary>
    protected abstract void UpdateBehavior();

    // ────────────────────────────────────────────────
    //  公開 API
    // ────────────────────────────────────────────────

    /// <summary>弾から呼ばれるダメージ処理</summary>
    public virtual void TakeDamage(float damage)
    {
        if (IsDead) return;
        CurrentHP -= damage;

        if (CurrentHP <= 0f)
        {
            // 死亡時はフラッシュを止めてすぐ Die へ（コルーチン競合を防ぐ）
            if (_hitFlashCoroutine != null)
            {
                StopCoroutine(_hitFlashCoroutine);
                _hitFlashCoroutine = null;
            }
            Die();
        }
        else
        {
            // 前のフラッシュを止めてから新しく開始（重複防止）
            if (_hitFlashCoroutine != null) StopCoroutine(_hitFlashCoroutine);
            _hitFlashCoroutine = StartCoroutine(HitFlash());
        }
    }

    /// <summary>ステージごとにステータスをスケールさせる</summary>
    public virtual void ScaleToStage(int stageNumber)
    {
        // 元値ベースで計算することで、プール再利用時に二重乗算されない
        float mult    = 1f + (stageNumber - 1) * 0.2f;
        baseHP        = _baseHPOriginal * mult;
        baseMoveSpeed = _baseMoveSpeedOriginal * (1f + (stageNumber - 1) * 0.05f);
        scorePoints   = (int)(_scorePointsOriginal * (1f + (stageNumber - 1) * 0.3f));

        CurrentHP = baseHP;
        MoveSpeed = baseMoveSpeed;
    }

    // ────────────────────────────────────────────────
    //  接触ダメージ
    // ────────────────────────────────────────────────
    protected virtual void OnTriggerStay2D(Collider2D other)
    {
        if (IsDead) return;
        PlayerHealth ph = other.GetComponentInParent<PlayerHealth>();
        ph?.TakeDamage(contactDamage * Time.deltaTime);
    }

    // ────────────────────────────────────────────────
    //  内部
    // ────────────────────────────────────────────────
    protected virtual void Die()
    {
        IsDead = true;
        Rb.linearVelocity = Vector2.zero;

        GameManager.Instance?.AddScore(scorePoints);
        StageManager.Instance?.NotifyEnemyDied();

        // 死亡エフェクト後にプール返却
        StartCoroutine(DeathEffect());
    }

    /// <summary>ヒット時の視覚フィードバック（赤フラッシュ＋スケールパルス）</summary>
    private IEnumerator HitFlash()
    {
        if (spriteRenderer == null) yield break;

        // ヒット瞬間：白く光らせて大きく弾く
        spriteRenderer.color = Color.white;
        transform.localScale = _originalScale * 1.7f;

        yield return new WaitForSeconds(0.05f);

        // 赤に変わり縮む
        spriteRenderer.color = new Color(1f, 0.1f, 0.1f);
        transform.localScale = _originalScale * 0.75f;

        yield return new WaitForSeconds(0.1f);

        // 元に戻す（死亡中でなければ）
        transform.localScale = _originalScale;
        if (!IsDead && spriteRenderer != null)
            spriteRenderer.color = _originalColor;
    }

    /// <summary>
    /// 死亡エフェクト：白フラッシュ → 膨らみながらフェードアウト → プール返却
    /// 明確に「倒した！」とわかるよう意図的に長めの演出にしている。
    /// </summary>
    private IEnumerator DeathEffect()
    {
        // 進行中のヒットフラッシュを止めてスケールをリセット
        if (_hitFlashCoroutine != null)
        {
            StopCoroutine(_hitFlashCoroutine);
            _hitFlashCoroutine = null;
            transform.localScale = _originalScale;
        }

        // ── フェーズ 1：眩しい白フラッシュ（0.06秒）──
        if (spriteRenderer != null) spriteRenderer.color = Color.white;
        transform.localScale = _originalScale * 2.0f;
        yield return new WaitForSecondsRealtime(0.06f);

        // ── フェーズ 2：爆発的に膨らみながら透明に消える（0.28秒）──
        float elapsed  = 0f;
        float duration = 0.28f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(elapsed / duration);

            transform.localScale = _originalScale * Mathf.Lerp(2.0f, 3.5f, p);
            if (spriteRenderer != null)
                spriteRenderer.color = new Color(1f, 1f, 1f, 1f - p);

            yield return null;
        }

        // ビジュアルをリセットしてからプール返却（次の再利用のため）
        transform.localScale = _originalScale;

        if (OnReturnToPool != null)
            OnReturnToPool(this);
        else
            gameObject.SetActive(false);
    }
}
