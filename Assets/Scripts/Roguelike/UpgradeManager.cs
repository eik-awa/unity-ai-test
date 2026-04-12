using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ローグライクのアップグレード選択を管理する。
/// ステージクリア後に UpgradeUI へ3択を渡す。
/// </summary>
public class UpgradeManager : MonoBehaviour
{
    // ────────────────────────────────────────────────
    //  シングルトン
    // ────────────────────────────────────────────────
    public static UpgradeManager Instance { get; private set; }

    // ────────────────────────────────────────────────
    //  Inspector
    // ────────────────────────────────────────────────
    [Header("全アップグレードのリスト")]
    [SerializeField] private UpgradeDefinition[] allUpgrades;

    [Header("1回の選択肢数")]
    [SerializeField] private int choiceCount = 3;

    [Header("UI")]
    [SerializeField] private UpgradeUI upgradeUI;

    // ────────────────────────────────────────────────
    //  Unity ライフサイクル
    // ────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ────────────────────────────────────────────────
    //  公開 API
    // ────────────────────────────────────────────────

    /// <summary>GameManager から呼ばれる。アップグレード選択 UI を表示。</summary>
    public void ShowUpgradeSelection()
    {
        if (allUpgrades == null || allUpgrades.Length == 0)
        {
            // アップグレードがなければそのまま次のステージへ
            GameManager.Instance?.NotifyUpgradeSelected();
            return;
        }

        UpgradeDefinition[] choices = PickRandom(choiceCount);
        upgradeUI?.Show(choices);
    }

    /// <summary>UpgradeUI のカードがクリックされたとき呼ぶ</summary>
    public void ApplyUpgrade(UpgradeDefinition upgrade)
    {
        PlayerHealth ph = PlayerController.Instance?.GetComponent<PlayerHealth>();
        ph?.ApplyUpgrade(stats => upgrade.Apply(stats));

        upgradeUI?.Hide();
        GameManager.Instance?.NotifyUpgradeSelected();
    }

    // ────────────────────────────────────────────────
    //  内部
    // ────────────────────────────────────────────────
    private UpgradeDefinition[] PickRandom(int count)
    {
        // Fisher-Yates シャッフルで重複なしに選ぶ
        List<UpgradeDefinition> pool = new List<UpgradeDefinition>(allUpgrades);
        int take = Mathf.Min(count, pool.Count);

        for (int i = 0; i < take; i++)
        {
            int j = Random.Range(i, pool.Count);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        UpgradeDefinition[] result = new UpgradeDefinition[take];
        for (int i = 0; i < take; i++) result[i] = pool[i];
        return result;
    }
}
