using System.Collections;
using UnityEngine;

/// <summary>
/// ボスエネミー（大型サメ）。
/// HP に応じて3段階のフェーズがあり、行動パターンが変化する。
///   フェーズ1 (HP 100〜50%) : 追尾 + チャージ攻撃
///   フェーズ2 (HP 50〜25%)  : 速度UP + チャージ頻度UP
///   フェーズ3 (HP 25〜0%)   : 高速 + 大チャージ（突進距離が長い）
/// </summary>
public class BossEnemy : EnemyBase
{
    // ────────────────────────────────────────────────
    //  Inspector
    // ────────────────────────────────────────────────
    [Header("フェーズ閾値")]
    [SerializeField] private float phase2Threshold = 0.50f; // HP50%以下でフェーズ2
    [SerializeField] private float phase3Threshold = 0.25f; // HP25%以下でフェーズ3

    [Header("チャージ攻撃")]
    [SerializeField] private float chargeRange       = 6f;
    [SerializeField] private float telegraphDuration = 0.8f;
    [SerializeField] private float chargeDuration    = 0.45f;
    [SerializeField] private float chargeSpeedMult   = 5f;
    [SerializeField] private float chargeCooldown    = 2.2f;

    [Header("色")]
    [SerializeField] private Color phase1Color    = new Color(0.90f, 0.30f, 0.30f); // 赤
    [SerializeField] private Color phase2Color    = new Color(1.00f, 0.50f, 0.10f); // オレンジ
    [SerializeField] private Color phase3Color    = new Color(1.00f, 0.10f, 0.10f); // 深紅
    [SerializeField] private Color telegraphColor = new Color(1.00f, 0.90f, 0.00f); // 黄（警告）

    // ────────────────────────────────────────────────
    //  状態
    // ────────────────────────────────────────────────
    private enum BossState { Chase, Telegraph, Charge, Cooldown }

    private BossState _state        = BossState.Chase;
    private float     _stateTimer;
    private Vector2   _chargeDir;
    private int       _currentPhase = 1;

    // ────────────────────────────────────────────────
    //  ライフサイクル
    // ────────────────────────────────────────────────
    protected override void OnEnable()
    {
        base.OnEnable();
        _state        = BossState.Chase;
        _currentPhase = 1;
        _stateTimer   = 0f;
        MoveSpeed     = baseMoveSpeed;
        if (spriteRenderer) spriteRenderer.color = phase1Color;
    }

    // ────────────────────────────────────────────────
    //  AI ロジック
    // ────────────────────────────────────────────────
    protected override void UpdateBehavior()
    {
        if (PlayerTransform == null) return;

        UpdatePhase();
        _stateTimer -= Time.deltaTime;

        switch (_state)
        {
            case BossState.Chase:     UpdateChase();     break;
            case BossState.Telegraph: UpdateTelegraph(); break;
            case BossState.Charge:    UpdateCharge();    break;
            case BossState.Cooldown:  UpdateCooldown();  break;
        }
    }

    private void UpdatePhase()
    {
        float ratio = baseHP > 0f ? CurrentHP / baseHP : 0f;
        int newPhase = ratio > phase2Threshold ? 1
                     : ratio > phase3Threshold ? 2 : 3;

        if (newPhase == _currentPhase) return;

        _currentPhase = newPhase;
        // フェーズが変わるたびに速度アップ
        MoveSpeed = baseMoveSpeed * (1f + (_currentPhase - 1) * 0.35f);
        StartCoroutine(PhaseTransitionFlash());
    }

    // ────────────────────────────────────────────────
    //  各状態の Update
    // ────────────────────────────────────────────────
    private void UpdateChase()
    {
        float dist       = Vector2.Distance(transform.position, PlayerTransform.position);
        float rangeScale = 1f + (_currentPhase - 1) * 0.4f; // フェーズが上がるほど遠くからチャージ

        if (dist <= chargeRange * rangeScale)
        {
            EnterTelegraph();
            return;
        }

        Vector2 dir = ((Vector2)PlayerTransform.position - (Vector2)transform.position).normalized;
        Rb.linearVelocity = dir * MoveSpeed;
        FlipToward(dir);
    }

    private void UpdateTelegraph()
    {
        if (_stateTimer <= 0f) EnterCharge();
    }

    private void UpdateCharge()
    {
        if (_stateTimer <= 0f) EnterCooldown();
    }

    private void UpdateCooldown()
    {
        Rb.linearVelocity = Vector2.zero;
        if (_stateTimer <= 0f) EnterChase();
    }

    // ────────────────────────────────────────────────
    //  状態遷移
    // ────────────────────────────────────────────────
    private void EnterTelegraph()
    {
        _state      = BossState.Telegraph;
        _stateTimer = telegraphDuration;
        Rb.linearVelocity = Vector2.zero;
        _chargeDir  = ((Vector2)PlayerTransform.position - (Vector2)transform.position).normalized;
        if (spriteRenderer) spriteRenderer.color = telegraphColor;
    }

    private void EnterCharge()
    {
        _state = BossState.Charge;
        // フェーズが上がるほど突進が長く、速くなる
        _stateTimer = chargeDuration * (1f + (_currentPhase - 1) * 0.3f);
        float speed = MoveSpeed * chargeSpeedMult * (1f + (_currentPhase - 1) * 0.2f);
        Rb.linearVelocity = _chargeDir * speed;

        if (spriteRenderer) spriteRenderer.color = PhaseColor();
    }

    private void EnterCooldown()
    {
        _state      = BossState.Cooldown;
        _stateTimer = chargeCooldown * Mathf.Max(0.4f, 1f - (_currentPhase - 1) * 0.25f);
        Rb.linearVelocity = Vector2.zero;
    }

    private void EnterChase()
    {
        _state = BossState.Chase;
    }

    // ────────────────────────────────────────────────
    //  エフェクト
    // ────────────────────────────────────────────────
    private Color PhaseColor() => _currentPhase switch
    {
        2 => phase2Color,
        3 => phase3Color,
        _ => phase1Color,
    };

    private IEnumerator PhaseTransitionFlash()
    {
        if (spriteRenderer == null) yield break;
        Color target = PhaseColor();
        for (int i = 0; i < 8; i++)
        {
            spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(0.07f);
            spriteRenderer.color = target;
            yield return new WaitForSeconds(0.07f);
        }
        spriteRenderer.color = target;
    }

    // ────────────────────────────────────────────────
    //  ヘルパー
    // ────────────────────────────────────────────────
    private void FlipToward(Vector2 dir)
    {
        if (spriteRenderer == null) return;
        if (dir.x > 0.01f)       spriteRenderer.flipX = false;
        else if (dir.x < -0.01f) spriteRenderer.flipX = true;
    }
}
