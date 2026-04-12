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

    protected float        CurrentHP;
    protected float        MoveSpeed;
    protected Rigidbody2D  Rb { get; private set; }
    protected Transform    PlayerTransform;

    // ────────────────────────────────────────────────
    //  Unity ライフサイクル
    // ────────────────────────────────────────────────
    protected virtual void Awake()
    {
        Rb = GetComponent<Rigidbody2D>();
        Rb.gravityScale = 0f;
        Rb.constraints  = RigidbodyConstraints2D.FreezeRotation;
    }

    protected virtual void OnEnable()
    {
        IsDead    = false;
        CurrentHP = baseHP;
        MoveSpeed = baseMoveSpeed;

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
        StartCoroutine(HitFlash());

        if (CurrentHP <= 0f) Die();
    }

    /// <summary>ステージごとにステータスをスケールさせる</summary>
    public virtual void ScaleToStage(int stageNumber)
    {
        float mult = 1f + (stageNumber - 1) * 0.2f; // ステージごと20%強化
        baseHP        *= mult;
        baseMoveSpeed *= 1f + (stageNumber - 1) * 0.05f;
        scorePoints   =  (int)(scorePoints * (1f + (stageNumber - 1) * 0.3f));

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

        // 継承先でオーバーライドして死亡演出を追加可能
        gameObject.SetActive(false);
    }

    private IEnumerator HitFlash()
    {
        if (spriteRenderer == null) yield break;
        spriteRenderer.color = Color.white * 2f; // 白フラッシュ
        yield return new WaitForSeconds(0.05f);
        spriteRenderer.color = Color.white;
    }
}
