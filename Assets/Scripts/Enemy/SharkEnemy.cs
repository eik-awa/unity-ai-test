using System.Collections;
using UnityEngine;

/// <summary>
/// サメ敵。
/// 通常: プレイヤーを緩やかに追尾
/// チャージ: 一定距離に近づいたら予備動作 → 高速突進 → 停止
/// </summary>
public class SharkEnemy : EnemyBase
{
    // ────────────────────────────────────────────────
    //  Inspector
    // ────────────────────────────────────────────────
    [Header("チャージ攻撃")]
    [SerializeField] private float chargeRange        = 3f;   // この距離以下でチャージ開始
    [SerializeField] private float telegraphDuration  = 0.8f; // 予備動作の長さ（秒）長めにして対応しやすく
    [SerializeField] private float chargeDuration     = 0.30f;// 突進の長さ（秒）
    [SerializeField] private float chargeSpeedMult    = 3f;   // 突進速度倍率（4→3で少し遅く）
    [SerializeField] private float chargeCooldown     = 2.5f; // チャージ再発動までの時間

    [Header("色")]
    [SerializeField] private Color telegraphColor = new Color(1f, 0.6f, 0.2f); // オレンジ（警告）

    // ────────────────────────────────────────────────
    //  状態
    // ────────────────────────────────────────────────
    private enum SharkState { Chase, Telegraph, Charge, Cooldown }
    private SharkState _state        = SharkState.Chase;
    private float      _stateTimer   = 0f;
    private Vector2    _chargeDir;

    // ────────────────────────────────────────────────
    //  AI ロジック
    // ────────────────────────────────────────────────
    protected override void UpdateBehavior()
    {
        if (PlayerTransform == null) return;

        _stateTimer -= Time.deltaTime;

        switch (_state)
        {
            case SharkState.Chase:      UpdateChase();      break;
            case SharkState.Telegraph:  UpdateTelegraph();  break;
            case SharkState.Charge:     UpdateCharge();     break;
            case SharkState.Cooldown:   UpdateCooldown();   break;
        }
    }

    private void UpdateChase()
    {
        float dist = Vector2.Distance(transform.position, PlayerTransform.position);

        if (dist <= chargeRange)
        {
            EnterTelegraph();
            return;
        }

        // ゆるやかな追尾
        Vector2 dir = ((Vector2)PlayerTransform.position - (Vector2)transform.position).normalized;
        Rb.linearVelocity = dir * MoveSpeed;

        // スプライトをプレイヤーの方向に向ける
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
        _state     = SharkState.Telegraph;
        _stateTimer = telegraphDuration;
        Rb.linearVelocity = Vector2.zero;

        // 予備動作の色変化
        if (spriteRenderer) spriteRenderer.color = telegraphColor;

        // チャージ方向を確定
        _chargeDir = ((Vector2)PlayerTransform.position - (Vector2)transform.position).normalized;
    }

    private void EnterCharge()
    {
        _state      = SharkState.Charge;
        _stateTimer = chargeDuration;
        Rb.linearVelocity = _chargeDir * (MoveSpeed * chargeSpeedMult);

        if (spriteRenderer) spriteRenderer.color = _originalColor;
    }

    private void EnterCooldown()
    {
        _state      = SharkState.Cooldown;
        _stateTimer = chargeCooldown;
        Rb.linearVelocity = Vector2.zero;
    }

    private void EnterChase()
    {
        _state = SharkState.Chase;
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

    protected override void OnEnable()
    {
        base.OnEnable();
        _state = SharkState.Chase;
    }
}
