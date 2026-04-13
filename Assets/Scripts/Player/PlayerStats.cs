using UnityEngine;

/// <summary>
/// プレイヤーのステータス。
/// ローグライクのアップグレードでここの値を変えることでキャラクターが強くなる。
/// </summary>
[System.Serializable]
public class PlayerStats
{
    [Header("基本")]
    public float maxHP            = 3f;   // ライフ数（3ライフ制）
    public float moveSpeed        = 5f;

    [Header("攻撃")]
    public float bulletDamage     = 20f;   // 1発あたりのダメージ
    public float fireRate         = 0.20f; // 発射間隔（秒）
    public float bulletSpeed      = 12f;   // 弾の速度
    public int   bulletCount      = 1;     // 1回の発射で出る弾数
    public float bulletSpread     = 0f;    // 多弾時の広がり角（度）
    public float bulletRange      = 15f;   // 弾の有効射程

    [Header("防御")]
    public float invincibilityDuration = 1.5f; // 被弾後の無敵時間（秒）
    public float damageReduction       = 0f;   // ダメージ軽減率 0〜1

    /// <summary>アップグレード後に最大HPが増えた場合、現在HPも回復する量</summary>
    public float hpGainOnMaxHPUp = 0f;

    /// <summary>ディープコピー（アップグレード前にバックアップ用）</summary>
    public PlayerStats Clone()
    {
        return (PlayerStats)MemberwiseClone();
    }
}
