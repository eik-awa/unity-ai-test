using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// プレイヤーのHP・ダメージ処理。
/// Stats（PlayerStats）の所有者。
/// 無敵時間・被弾フラッシュ演出も担当。
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    // ────────────────────────────────────────────────
    //  Inspector
    // ────────────────────────────────────────────────
    [Header("初期ステータス")]
    [SerializeField] private PlayerStats initialStats = new PlayerStats();

    [Header("被弾エフェクト")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private int   flashCount    = 4;
    [SerializeField] private float flashInterval = 0.1f;

    // ────────────────────────────────────────────────
    //  イベント
    // ────────────────────────────────────────────────
    public UnityEvent<float, float> OnHPChanged = new UnityEvent<float, float>(); // (current, max)
    public UnityEvent               OnDamaged   = new UnityEvent();
    public UnityEvent               OnDied      = new UnityEvent();

    // ────────────────────────────────────────────────
    //  プロパティ
    // ────────────────────────────────────────────────
    public PlayerStats Stats       { get; private set; }
    public float       CurrentHP   { get; private set; }
    public bool        IsInvincible { get; private set; }
    public bool        IsDead      { get; private set; }

    // ────────────────────────────────────────────────
    //  Unity ライフサイクル
    // ────────────────────────────────────────────────
    private void Awake()
    {
        Stats     = initialStats.Clone();
        CurrentHP = Stats.maxHP;
    }

    private void Start()
    {
        OnHPChanged.Invoke(CurrentHP, Stats.maxHP);
    }

    // ────────────────────────────────────────────────
    //  公開 API
    // ────────────────────────────────────────────────

    /// <summary>ダメージを受ける。無敵時間中は無視。</summary>
    public void TakeDamage(float damage)
    {
        if (IsInvincible || IsDead) return;

        float reduced = damage * (1f - Mathf.Clamp01(Stats.damageReduction));
        CurrentHP = Mathf.Max(0f, CurrentHP - reduced);

        OnDamaged.Invoke();
        OnHPChanged.Invoke(CurrentHP, Stats.maxHP);
        UIManager.Instance?.RefreshHP(CurrentHP, Stats.maxHP);

        StartCoroutine(InvincibilityFrames());
        StartCoroutine(FlashEffect());

        if (CurrentHP <= 0f) Die();
    }

    /// <summary>HPを回復する</summary>
    public void Heal(float amount)
    {
        if (IsDead) return;
        CurrentHP = Mathf.Min(Stats.maxHP, CurrentHP + amount);
        OnHPChanged.Invoke(CurrentHP, Stats.maxHP);
        UIManager.Instance?.RefreshHP(CurrentHP, Stats.maxHP);
    }

    /// <summary>ローグライクアップグレード後に Stats を更新する</summary>
    public void ApplyUpgrade(System.Action<PlayerStats> modifier)
    {
        modifier(Stats);

        // 最大HPが上がった場合、その差分だけ回復
        CurrentHP = Mathf.Min(CurrentHP + Stats.hpGainOnMaxHPUp, Stats.maxHP);
        Stats.hpGainOnMaxHPUp = 0f;

        OnHPChanged.Invoke(CurrentHP, Stats.maxHP);
        UIManager.Instance?.RefreshHP(CurrentHP, Stats.maxHP);

        // PlayerController / PlayerShooter も最新 Stats を参照しているので更新
        GetComponent<PlayerController>()?.SetStats(Stats);
        GetComponent<PlayerShooter>()?.RefreshStats();
    }

    // ────────────────────────────────────────────────
    //  内部
    // ────────────────────────────────────────────────
    private void Die()
    {
        IsDead = true;
        OnDied.Invoke();
        GameManager.Instance?.NotifyGameOver();
    }

    private IEnumerator InvincibilityFrames()
    {
        IsInvincible = true;
        yield return new WaitForSeconds(Stats.invincibilityDuration);
        IsInvincible = false;
    }

    private IEnumerator FlashEffect()
    {
        if (spriteRenderer == null) yield break;
        for (int i = 0; i < flashCount; i++)
        {
            spriteRenderer.color = GameColors.DangerRed;
            yield return new WaitForSeconds(flashInterval);
            spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(flashInterval);
        }
    }
}
