using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Full-screen modal showing a tower's icon, name, description, and stats.
// Opened by TowerCardClickHandler from the shop, loadout bar, or inventory
// cards; this script doesn't know or care which one called it.
public class TowerDetailPopupUI : MonoBehaviour
{
    public static TowerDetailPopupUI Instance { get; private set; }

    [Header("Layout")]
    public float panelWidth = 420f;
    public float panelHeight = 560f;
    public float padding = 30f;
    public float iconSize = 150f;
    public float titleHeight = 50f;
    public float closeButtonSize = 56f;

    [Header("Colors")]
    public Color backdropColor = new Color(0.078f, 0.012f, 0.012f, 0.8f); // Near-black wine red
    public Color panelColor = new Color(0.2f, 0.031f, 0.031f, 0.97f);    // Deep Maroon (casino felt)
    public Color descriptionColor = Color.white;                             // Crisp Dice White
    public Color statColor = new Color(1f, 0.8431f, 0f, 1f);                 // Jackpot Gold
    public Color closeColor = new Color(0.8157f, 0f, 0f, 1f);                // Card Suit Red

    private RectTransform innerPanel;
    private Image iconImage;
    private TextMeshProUGUI nameLabel;
    private TextMeshProUGUI descriptionLabel;
    private TextMeshProUGUI statsLabel;

    // True only when the currently-shown popup is for a placed Tower instance
    // (as opposed to a shop/inventory card) -- so Hide() knows whether it
    // also needs to clear the range ring it asked TowerPlacementManager to draw.
    private bool showingPlacedTowerRange;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        BuildBackdrop();
        BuildInnerPanel();
        BuildCloseButton();
        BuildIcon();
        BuildNameLabel();
        BuildDescriptionLabel();
        BuildStatsLabel();

        gameObject.SetActive(false);
    }

    public void Show(TowerSO tower)
    {
        if (tower == null) return;

        showingPlacedTowerRange = false;

        nameLabel.text = tower.towerName;
        descriptionLabel.text = string.IsNullOrWhiteSpace(tower.description)
            ? "No description available."
            : tower.description;
        statsLabel.text = BuildStatsText(tower);

        iconImage.sprite = tower.icon;
        iconImage.enabled = tower.icon != null;

        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        RoundManager.Instance?.SetTimerPaused(true);
    }

    // Placed-tower overload -- shows the tower's current (instance-bonus-
    // adjusted) stats and draws its actual range ring in the world via
    // TowerPlacementManager, reusing the same ring/label it uses for the
    // drag-placement preview.
    public void Show(Tower tower)
    {
        if (tower == null || tower.data == null) return;

        showingPlacedTowerRange = true;

        nameLabel.text = tower.data.towerName;
        descriptionLabel.text = string.IsNullOrWhiteSpace(tower.data.description)
            ? "No description available."
            : tower.data.description;
        statsLabel.text = BuildStatsText(tower);

        iconImage.sprite = tower.data.icon;
        iconImage.enabled = tower.data.icon != null;

        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        RoundManager.Instance?.SetTimerPaused(true);

        TowerPlacementManager.Instance?.ShowRangeRingAt(tower.transform.position, tower.EffectiveRange);
    }

    // Upgrade-card overload -- same popup, just a different stat block since
    // ShopCardSO doesn't share TowerSO's fields. Called by ShopCardClickHandler
    // from the shop and the upgrade inventory.
    public void Show(ShopCardSO card)
    {
        if (card == null || card.rewardType == SpinFortRewardType.Tower) return;

        showingPlacedTowerRange = false;

        nameLabel.text = card.label;
        descriptionLabel.text = string.IsNullOrWhiteSpace(card.description)
            ? "No description available."
            : card.description;
        statsLabel.text = BuildStatsText(card);

        iconImage.sprite = card.icon;
        iconImage.enabled = card.icon != null;

        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        RoundManager.Instance?.SetTimerPaused(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        RoundManager.Instance?.SetTimerPaused(false);

        if (showingPlacedTowerRange)
        {
            TowerPlacementManager.Instance?.HideRangeRing();
            showingPlacedTowerRange = false;
        }
    }

    public static string BuildStatsText(TowerSO tower)
    {
        string attackLine = tower.isMelee
            ? "Attack: Melee"
            : $"Projectile Speed: {tower.projectileSpeed:0.##}";

        return
            $"Cost: ${tower.cost}\n" +
            $"Range: {tower.range:0.##}\n" +
            $"Fire Rate: {tower.fireRate:0.##}/s\n" +
            $"Damage: {tower.damage}\n" +
            $"{attackLine}\n" +
            $"Rotation Speed: {tower.rotationSpeed:0.##}°/s";
    }

    // Placed-instance variant -- shows the tower's current (instance-bonus-
    // adjusted) Effective* values, noting the unbuffed base value alongside
    // any stat a consumable upgrade card has pushed away from it.
    public static string BuildStatsText(Tower tower)
    {
        TowerSO data = tower.data;

        string rangeLine = $"Range: {tower.EffectiveRange:0.##}";
        if (!Mathf.Approximately(tower.EffectiveRange, data.range)) rangeLine += $" (base {data.range:0.##})";

        string fireRateLine = $"Fire Rate: {tower.EffectiveFireRate:0.##}/s";
        if (!Mathf.Approximately(tower.EffectiveFireRate, data.fireRate)) fireRateLine += $" (base {data.fireRate:0.##})";

        string damageLine = $"Damage: {tower.EffectiveDamage}";
        if (tower.EffectiveDamage != data.damage) damageLine += $" (base {data.damage})";

        string attackLine = data.isMelee
            ? "Attack: Melee"
            : $"Projectile Speed: {data.projectileSpeed:0.##}";

        return
            $"{rangeLine}\n" +
            $"{fireRateLine}\n" +
            $"{damageLine}\n" +
            $"{attackLine}\n" +
            $"Rotation Speed: {data.rotationSpeed:0.##}°/s";
    }

    public static string BuildStatsText(ShopCardSO card)
    {
        string effectLine;
        switch (card.rewardType)
        {
            case SpinFortRewardType.Points:
                effectLine = $"Gold: +{card.intValue}";
                break;
            case SpinFortRewardType.FireRateBuff:
                effectLine = $"Fire Rate: x{card.floatValue:0.##} for {card.duration:0.##}s";
                break;
            case SpinFortRewardType.DamageBuff:
                effectLine = $"Damage: +{card.intValue} for {card.duration:0.##}s";
                break;
            case SpinFortRewardType.MovementSpeedBuff:
                effectLine = $"Move Speed: x{card.floatValue:0.##} for {card.duration:0.##}s";
                break;
            case SpinFortRewardType.HealPlayer:
                effectLine = $"Heals: {card.intValue} HP";
                break;
            case SpinFortRewardType.BonusEnemiesNextRound:
                effectLine = $"Next Round: +{card.intValue} enemies";
                break;
            case SpinFortRewardType.BaseHeal:
                effectLine = $"Base Heal: {card.intValue} HP";
                break;
            case SpinFortRewardType.GlobalAttackSpeed:
                effectLine = $"Tower Fire Rate: +{card.floatValue * 100f:0.##}% (drag onto a tower)";
                break;
            case SpinFortRewardType.GlobalTowerRange:
                effectLine = $"Tower Range: +{card.floatValue * 100f:0.##}% (drag onto a tower)";
                break;
            case SpinFortRewardType.RerollDiscount:
                effectLine = $"Reroll Cost: -{card.intValue}";
                break;
            case SpinFortRewardType.GlobalTowerDamage:
                effectLine = $"Tower Damage: +{card.intValue} (drag onto a tower)";
                break;
            case SpinFortRewardType.GoldPerRoundGain:
                effectLine = $"Gold/Round: +{card.intValue}";
                break;
            case SpinFortRewardType.AllInMultiplier:
                effectLine = $"Strongest Tower Damage: x{card.floatValue:0.##}\nMax Base Health: -{card.intValue}";
                break;
            case SpinFortRewardType.TowersExplodeOnDeath:
                effectLine = $"Sell Explosion: {card.floatValue:0.##} dmg, {card.duration:0.##}s stun";
                break;
            default:
                effectLine = "";
                break;
        }

        return $"Cost: ${card.cost}\nRarity: {card.rarity}\n{effectLine}";
    }

    private void BuildBackdrop()
    {
        RectTransform rt = GetComponent<RectTransform>();
        if (rt == null) rt = gameObject.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image backdrop = GetComponent<Image>();
        if (backdrop == null) backdrop = gameObject.AddComponent<Image>();
        backdrop.color = backdropColor;
    }

    private void BuildInnerPanel()
    {
        GameObject panelObj = new GameObject("InnerPanel");
        panelObj.transform.SetParent(transform, false);
        innerPanel = panelObj.AddComponent<RectTransform>();
        innerPanel.anchorMin = new Vector2(0.5f, 0.5f);
        innerPanel.anchorMax = new Vector2(0.5f, 0.5f);
        innerPanel.pivot = new Vector2(0.5f, 0.5f);
        innerPanel.sizeDelta = new Vector2(panelWidth, panelHeight);

        Image panelImg = panelObj.AddComponent<Image>();
        panelImg.color = panelColor;
    }

    private void BuildCloseButton()
    {
        GameObject closeObj = new GameObject("CloseButton");
        closeObj.transform.SetParent(innerPanel, false);
        RectTransform closeRT = closeObj.AddComponent<RectTransform>();
        closeRT.anchorMin = new Vector2(1f, 1f);
        closeRT.anchorMax = new Vector2(1f, 1f);
        closeRT.pivot = new Vector2(1f, 1f);
        closeRT.anchoredPosition = new Vector2(-padding * 0.5f, -padding * 0.5f);
        closeRT.sizeDelta = new Vector2(closeButtonSize, closeButtonSize);
        Image closeImg = closeObj.AddComponent<Image>();
        closeImg.color = closeColor;
        Button closeButton = closeObj.AddComponent<Button>();
        closeButton.onClick.AddListener(Hide);

        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(closeObj.transform, false);
        RectTransform labelRT = labelObj.AddComponent<RectTransform>();
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = labelRT.offsetMax = Vector2.zero;
        var label = labelObj.AddComponent<TextMeshProUGUI>();
        label.text = "X";
        label.fontSize = 26;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
    }

    private void BuildIcon()
    {
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(innerPanel, false);
        RectTransform iconRT = iconObj.AddComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0.5f, 1f);
        iconRT.anchorMax = new Vector2(0.5f, 1f);
        iconRT.pivot = new Vector2(0.5f, 1f);
        iconRT.anchoredPosition = new Vector2(0f, -padding);
        iconRT.sizeDelta = new Vector2(iconSize, iconSize);
        iconImage = iconObj.AddComponent<Image>();
        iconImage.preserveAspect = true;
    }

    private void BuildNameLabel()
    {
        GameObject nameObj = new GameObject("NameLabel");
        nameObj.transform.SetParent(innerPanel, false);
        RectTransform nameRT = nameObj.AddComponent<RectTransform>();
        nameRT.anchorMin = new Vector2(0f, 1f);
        nameRT.anchorMax = new Vector2(1f, 1f);
        nameRT.pivot = new Vector2(0.5f, 1f);
        nameRT.anchoredPosition = new Vector2(0f, -(padding + iconSize + 8f));
        nameRT.sizeDelta = new Vector2(0f, titleHeight);
        nameLabel = nameObj.AddComponent<TextMeshProUGUI>();
        nameLabel.fontSize = 30;
        nameLabel.alignment = TextAlignmentOptions.Center;
        nameLabel.color = Color.white;
    }

    private void BuildDescriptionLabel()
    {
        GameObject descObj = new GameObject("DescriptionLabel");
        descObj.transform.SetParent(innerPanel, false);
        RectTransform descRT = descObj.AddComponent<RectTransform>();
        descRT.anchorMin = new Vector2(0f, 0.4f);
        descRT.anchorMax = new Vector2(1f, 1f);
        descRT.offsetMin = new Vector2(padding, 0f);
        descRT.offsetMax = new Vector2(-padding, -(padding + iconSize + titleHeight + 16f));
        descriptionLabel = descObj.AddComponent<TextMeshProUGUI>();
        descriptionLabel.fontSize = 19;
        descriptionLabel.alignment = TextAlignmentOptions.Top;
        descriptionLabel.color = descriptionColor;
    }

    private void BuildStatsLabel()
    {
        GameObject statsObj = new GameObject("StatsLabel");
        statsObj.transform.SetParent(innerPanel, false);
        RectTransform statsRT = statsObj.AddComponent<RectTransform>();
        statsRT.anchorMin = new Vector2(0f, 0f);
        statsRT.anchorMax = new Vector2(1f, 0.4f);
        statsRT.offsetMin = new Vector2(padding, padding);
        statsRT.offsetMax = new Vector2(-padding, 0f);
        statsLabel = statsObj.AddComponent<TextMeshProUGUI>();
        statsLabel.fontSize = 21;
        statsLabel.alignment = TextAlignmentOptions.TopLeft;
        statsLabel.color = statColor;
        statsLabel.lineSpacing = 6f;
    }
}
