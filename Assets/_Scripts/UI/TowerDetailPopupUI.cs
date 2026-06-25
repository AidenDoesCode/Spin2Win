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
    public Color backdropColor = new Color(0f, 0f, 0f, 0.7f);
    public Color panelColor = new Color(0.102f, 0.102f, 0.180f, 0.97f);
    public Color descriptionColor = new Color(0.85f, 0.85f, 0.85f, 1f);
    public Color statColor = new Color(0.878f, 0.753f, 0.376f, 1f);
    public Color closeColor = new Color(0.5f, 0.2f, 0.2f, 1f);

    private RectTransform innerPanel;
    private Image iconImage;
    private TextMeshProUGUI nameLabel;
    private TextMeshProUGUI descriptionLabel;
    private TextMeshProUGUI statsLabel;

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

        nameLabel.text = tower.towerName;
        descriptionLabel.text = string.IsNullOrWhiteSpace(tower.description)
            ? "No description available."
            : tower.description;
        statsLabel.text = BuildStatsText(tower);

        iconImage.sprite = tower.icon;
        iconImage.enabled = tower.icon != null;

        gameObject.SetActive(true);
        transform.SetAsLastSibling();
    }

    public void Hide() => gameObject.SetActive(false);

    private string BuildStatsText(TowerSO tower)
    {
        string attackLine = tower.isMelee
            ? "Attack: Melee"
            : $"Projectile Speed: {tower.projectileSpeed:0.##}";

        return
            $"Cost: {tower.cost}\n" +
            $"Range: {tower.range:0.##}\n" +
            $"Fire Rate: {tower.fireRate:0.##}/s\n" +
            $"Damage: {tower.damage}\n" +
            $"{attackLine}\n" +
            $"Rotation Speed: {tower.rotationSpeed:0.##}°/s";
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
