using UnityEngine;

/// <summary>
/// ローグライクアップグレード1つを表す ScriptableObject。
/// Assets/ScriptableObjects/Upgrades/ に .asset として保存する。
/// </summary>
[CreateAssetMenu(fileName = "Upgrade_New", menuName = "OctoShooter/Upgrade")]
public class UpgradeDefinition : ScriptableObject
{
    // ────────────────────────────────────────────────
    //  表示用
    // ────────────────────────────────────────────────
    [Header("表示情報")]
    public string upgradeName;
    [TextArea(2, 4)]
    public string description;
    public Sprite icon;

    // ────────────────────────────────────────────────
    //  効果の種類（Inspector でドロップダウン選択）
    // ────────────────────────────────────────────────
    public enum EffectType
    {
        // 攻撃
        DamageUp,        // ダメージ増加
        FireRateUp,      // 連射速度アップ
        BulletSpeedUp,   // 弾速アップ
        BulletCountUp,   // 多弾（+1発）
        BulletSpreadDown,// 広がり角を狭める

        // 防御
        MaxHPUp,         // 最大HP増加
        HealHP,          // HP即時回復
        DamageReductionUp, // ダメージ軽減
        InvincibilityUp, // 無敵時間延長

        // 移動
        MoveSpeedUp,
    }

    [Header("効果")]
    public EffectType effectType;
    [Tooltip("効果の量（%アップは0.1 = 10%, 絶対値アップはそのまま）")]
    public float value = 0.15f;

    // ────────────────────────────────────────────────
    //  適用ロジック
    // ────────────────────────────────────────────────

    /// <summary>プレイヤーのステータスにこのアップグレードを適用する</summary>
    public void Apply(PlayerStats stats)
    {
        switch (effectType)
        {
            case EffectType.DamageUp:
                stats.bulletDamage += stats.bulletDamage * value;
                break;

            case EffectType.FireRateUp:
                // fireRate は「秒」なので下げるほど速い
                stats.fireRate = Mathf.Max(0.05f, stats.fireRate * (1f - value));
                break;

            case EffectType.BulletSpeedUp:
                stats.bulletSpeed += stats.bulletSpeed * value;
                break;

            case EffectType.BulletCountUp:
                stats.bulletCount  += 1;
                stats.bulletSpread += 15f; // 1発増えるごとに広がりを追加
                break;

            case EffectType.BulletSpreadDown:
                stats.bulletSpread = Mathf.Max(0f, stats.bulletSpread - value);
                break;

            case EffectType.MaxHPUp:
                float hpDelta = stats.maxHP * value;
                stats.maxHP            += hpDelta;
                stats.hpGainOnMaxHPUp   = hpDelta; // 差分だけ回復させる
                break;

            case EffectType.HealHP:
                stats.hpGainOnMaxHPUp = stats.maxHP * value; // 最大HPの value% 回復
                break;

            case EffectType.DamageReductionUp:
                stats.damageReduction = Mathf.Min(0.75f, stats.damageReduction + value);
                break;

            case EffectType.InvincibilityUp:
                stats.invincibilityDuration += stats.invincibilityDuration * value;
                break;

            case EffectType.MoveSpeedUp:
                stats.moveSpeed += stats.moveSpeed * value;
                break;
        }
    }

    /// <summary>
    /// アップグレードカードに表示する説明文を動的生成。
    /// description が空のときのフォールバック。
    /// </summary>
    public string GetDescription()
    {
        if (!string.IsNullOrEmpty(description)) return description;

        return effectType switch
        {
            EffectType.DamageUp          => $"インクのダメージ +{value * 100:0}%",
            EffectType.FireRateUp        => $"連射速度 +{value * 100:0}%",
            EffectType.BulletSpeedUp     => $"弾速 +{value * 100:0}%",
            EffectType.BulletCountUp     => "弾数 +1",
            EffectType.BulletSpreadDown  => $"弾の広がりを -{value:0}°",
            EffectType.MaxHPUp           => $"最大HP +{value * 100:0}%（HP回復つき）",
            EffectType.HealHP            => $"HP {value * 100:0}% 回復",
            EffectType.DamageReductionUp => $"被ダメージ軽減 +{value * 100:0}%",
            EffectType.InvincibilityUp   => $"無敵時間 +{value * 100:0}%",
            EffectType.MoveSpeedUp       => $"移動速度 +{value * 100:0}%",
            _                            => upgradeName,
        };
    }
}
