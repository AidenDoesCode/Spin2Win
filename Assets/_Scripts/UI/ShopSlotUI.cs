using UnityEngine;
using UnityEngine.UI;

public class ShopSlotUI : MonoBehaviour
{
    [SerializeField] private Image background;
    [SerializeField] private Image cardBorder;

    private static readonly Color TowerBackgroundColor = new Color32(0x0A, 0x5C, 0x36, 0xFF);
    private static readonly Color UpgradeBackgroundColor = new Color32(0x33, 0x08, 0x08, 0xFF);

    private static readonly Color CommonFrameColor = new Color32(0xF5, 0xF5, 0xF0, 0xFF);
    private static readonly Color UncommonFrameColor = new Color32(0x2E, 0x8B, 0x57, 0xFF);
    private static readonly Color RareFrameColor = new Color32(0x00, 0xF5, 0xD4, 0xFF);
    private static readonly Color EpicFrameColor = new Color32(0xFF, 0x54, 0x70, 0xFF);
    private static readonly Color LegendaryFrameColor = new Color32(0xFF, 0xD7, 0x00, 0xFF);

    public void SetupSlot(ShopCardSO data)
    {
        bool isTower = data.rewardType == SpinFortRewardType.Tower;
        background.color = isTower ? TowerBackgroundColor : UpgradeBackgroundColor;

        switch (data.rarity)
        {
            case CardRarity.Common:
                cardBorder.color = CommonFrameColor;
                break;
            case CardRarity.Uncommon:
                cardBorder.color = UncommonFrameColor;
                break;
            case CardRarity.Rare:
                cardBorder.color = RareFrameColor;
                break;
            case CardRarity.Epic:
                cardBorder.color = EpicFrameColor;
                break;
            case CardRarity.Legendary:
                cardBorder.color = LegendaryFrameColor;
                break;
        }
    }
}
