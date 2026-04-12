using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ステージクリア後のアップグレード選択画面。
/// UpgradeCardUI を3枚並べてプレイヤーに選ばせる。
/// </summary>
public class UpgradeUI : MonoBehaviour
{
    // ────────────────────────────────────────────────
    //  Inspector
    // ────────────────────────────────────────────────
    [Header("ルートパネル")]
    [SerializeField] private GameObject panel;

    [Header("カード（3枚）")]
    [SerializeField] private UpgradeCardUI[] cards;

    // ────────────────────────────────────────────────
    //  Unity ライフサイクル
    // ────────────────────────────────────────────────
    private void Awake()
    {
        if (panel) panel.SetActive(false);
    }

    // ────────────────────────────────────────────────
    //  公開 API
    // ────────────────────────────────────────────────

    /// <summary>UpgradeManager から呼ばれる</summary>
    public void Show(UpgradeDefinition[] choices)
    {
        if (panel) panel.SetActive(true);

        for (int i = 0; i < cards.Length; i++)
        {
            if (i < choices.Length)
            {
                cards[i].gameObject.SetActive(true);
                cards[i].Setup(choices[i]);
            }
            else
            {
                cards[i].gameObject.SetActive(false);
            }
        }
    }

    public void Hide()
    {
        if (panel) panel.SetActive(false);
    }
}

// ────────────────────────────────────────────────────────────────────────────
//  アップグレード1枚のカード UI
// ────────────────────────────────────────────────────────────────────────────

/// <summary>
/// アップグレードカード1枚。
/// Unity エディタ上でプレハブ化して UpgradeUI の cards[] にセットする。
/// </summary>
public class UpgradeCardUI : MonoBehaviour
{
    [SerializeField] private Image              iconImage;
    [SerializeField] private TextMeshProUGUI    nameText;
    [SerializeField] private TextMeshProUGUI    descText;
    [SerializeField] private Button             button;

    private UpgradeDefinition _upgrade;

    private void Awake()
    {
        button?.onClick.AddListener(OnCardClicked);
    }

    /// <summary>カードの内容をセット</summary>
    public void Setup(UpgradeDefinition upgrade)
    {
        _upgrade = upgrade;

        if (iconImage)
        {
            iconImage.sprite  = upgrade.icon;
            iconImage.enabled = upgrade.icon != null;
        }

        if (nameText) nameText.text = upgrade.upgradeName;
        if (descText) descText.text = upgrade.GetDescription();
    }

    private void OnCardClicked()
    {
        UpgradeManager.Instance?.ApplyUpgrade(_upgrade);
    }
}
