using UnityEngine;

[CreateAssetMenu(fileName = "NewShopCard", menuName = "Spin2Win/Shop Card")]
public class ShopCardSO : ScriptableObject
{
    public string label = "Upgrade";
    [Tooltip("Drives the card's border color and sparkle intensity in the shop UI.")]
    public CardRarity rarity = CardRarity.Common;
    [Tooltip("Relative odds this offer gets rolled into the shop each buy phase.")]
    [Min(0f)] public float weight = 1f;
    [Min(0)] public int cost = 100;
    public SpinFortRewardType rewardType = SpinFortRewardType.FireRateBuff;
    public int intValue = 1;
    public float floatValue = 1.15f;
    [Min(0f)] public float duration = 9999f;
    public TowerSO towerReward; // only used when rewardType == Tower
    [Tooltip("Card art. Falls back to towerReward's icon (if any) when left empty.")]
    public Sprite icon;
    [TextArea(2, 4)] public string description;
}
