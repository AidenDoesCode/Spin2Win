using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeInventoryUI : MonoBehaviour
{
    [Header("Settings")]
    public float cardSize = 64f;
    public float cardGap  = 8f;
    public float padding  = 8f;

    [Header("Background")]
    [Tooltip("Casino-themed panel background. If assigned, replaces the flat panelColor fill.")]
    public Sprite panelSprite;

    [Header("Colors (fallbacks if ShopUI / its art aren't found)")]
    public Color panelColor      = new Color(0.2f, 0.031f, 0.031f, 0.95f);  // Deep Maroon (casino felt)
    public Color cardColor       = new Color32(0x33, 0x08, 0x08, 0xFF);     // Velvet Crimson
    public Color cardBorderColor = new Color(0.6275f, 0.6275f, 0.6275f, 1f); // Common rarity gray

    private ShopUI shopUI;
    private readonly List<ShopCardSO> displayedUpgrades = new List<ShopCardSO>();
    private readonly List<GameObject> cards             = new List<GameObject>();
    private int lastKnownCount = -1;

    private void Awake()
    {
        Image panel = GetComponent<Image>();
        if (panel == null) panel = gameObject.AddComponent<Image>();
        if (panelSprite != null)
        {
            panel.sprite = panelSprite;
            panel.type = Image.Type.Simple;
            panel.color = Color.white;
        }
        else
        {
            panel.color = panelColor;
        }
    }

    private void Start()
    {
        shopUI = FindAnyObjectByType<ShopUI>();

        if (PlayerUpgradeInventory.Instance != null)
            PlayerUpgradeInventory.Instance.InventoryChanged += OnInventoryChanged;

        RefreshCards();
    }

    private void OnDestroy()
    {
        if (PlayerUpgradeInventory.Instance != null)
            PlayerUpgradeInventory.Instance.InventoryChanged -= OnInventoryChanged;
    }

    private void Update()
    {
        if (PlayerUpgradeInventory.Instance == null) return;
        int count = PlayerUpgradeInventory.Instance.ownedUpgrades.Count;
        if (count != lastKnownCount)
            RefreshCards();
    }

    private void OnInventoryChanged() => RefreshCards();

    private void RefreshCards()
    {
        foreach (GameObject card in cards)
            if (card != null) Destroy(card);
        cards.Clear();
        displayedUpgrades.Clear();

        if (PlayerUpgradeInventory.Instance == null) return;

        foreach (ShopCardSO card in PlayerUpgradeInventory.Instance.ownedUpgrades)
        {
            if (card == null) continue;
            displayedUpgrades.Add(card);
            cards.Add(CreateCard(card, cards.Count));
        }

        lastKnownCount = PlayerUpgradeInventory.Instance.ownedUpgrades.Count;
        ResizePanel();
    }

    private GameObject CreateCard(ShopCardSO card, int index)
    {
        float xPos = padding + index * (cardSize + cardGap);

        // Border -- reuses ShopUI's per-rarity border art/animation so the
        // same sparkle/ember frames a card showed in the shop keep playing
        // here, just scaled down to the inventory slot size.
        GameObject borderObj = new GameObject($"Card_{card.label}");
        borderObj.transform.SetParent(transform, false);
        RectTransform borderRT = borderObj.AddComponent<RectTransform>();
        borderRT.anchorMin        = new Vector2(0f, 0.5f);
        borderRT.anchorMax        = new Vector2(0f, 0.5f);
        borderRT.pivot            = new Vector2(0f, 0.5f);
        borderRT.anchoredPosition = new Vector2(xPos, 0f);
        borderRT.sizeDelta        = new Vector2(cardSize, cardSize);
        Image borderImg = borderObj.AddComponent<Image>();

        ShopUI.BorderArt borderArt = shopUI != null ? shopUI.GetBorderArt(card.rarity) : null;
        if (borderArt != null && borderArt.animation != null)
        {
            if (borderArt.fallbackSprite != null) borderImg.sprite = borderArt.fallbackSprite;
            borderImg.color = Color.white;
            ShopUI.PlayBorderAnimation(borderObj, borderArt.animation);
        }
        else if (borderArt != null && borderArt.fallbackSprite != null)
        {
            borderImg.sprite = borderArt.fallbackSprite;
            borderImg.color = Color.white;
        }
        else
        {
            borderImg.color = cardBorderColor;
        }

        // Inner card -- same upgrade background art/color ShopUI uses.
        GameObject cardObj = new GameObject("Inner");
        cardObj.transform.SetParent(borderObj.transform, false);
        RectTransform cardRT = cardObj.AddComponent<RectTransform>();
        cardRT.anchorMin = Vector2.zero;
        cardRT.anchorMax = Vector2.one;
        cardRT.offsetMin = new Vector2(2f, 2f);
        cardRT.offsetMax = new Vector2(-2f, -2f);
        Image cardImg = cardObj.AddComponent<Image>();
        Sprite bgArt = shopUI != null ? shopUI.upgradeCardBackgroundArt : null;
        cardImg.sprite = bgArt;
        cardImg.color = bgArt != null ? Color.white : (shopUI != null ? shopUI.upgradeCardColor : cardColor);

        // Icon
        Sprite icon = card.icon;
        if (icon != null)
        {
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(cardObj.transform, false);
            RectTransform iconRT = iconObj.AddComponent<RectTransform>();
            iconRT.anchorMin = new Vector2(0.1f, 0.3f);
            iconRT.anchorMax = new Vector2(0.9f, 0.9f);
            iconRT.offsetMin = iconRT.offsetMax = Vector2.zero;
            Image iconImg = iconObj.AddComponent<Image>();
            iconImg.sprite         = icon;
            iconImg.preserveAspect = true;
            iconImg.color          = Color.white;
        }

        // Label
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(cardObj.transform, false);
        RectTransform labelRT = labelObj.AddComponent<RectTransform>();
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = new Vector2(1f, 0f);
        labelRT.offsetMin = new Vector2(0f, 2f);
        labelRT.offsetMax = new Vector2(0f, 14f);
        var label = labelObj.AddComponent<TextMeshProUGUI>();
        label.text      = card.label;
        label.fontSize  = 7;
        label.alignment = TextAlignmentOptions.Bottom;
        label.color     = Color.white;

        // Validity overlay -- tints green/red while dragging to show whether
        // the card can be used wherever the cursor currently is. Sits above
        // everything else on the card but starts hidden/disabled.
        GameObject overlayObj = new GameObject("ValidityOverlay");
        overlayObj.transform.SetParent(cardObj.transform, false);
        RectTransform overlayRT = overlayObj.AddComponent<RectTransform>();
        overlayRT.anchorMin = Vector2.zero;
        overlayRT.anchorMax = Vector2.one;
        overlayRT.offsetMin = overlayRT.offsetMax = Vector2.zero;
        Image overlayImg = overlayObj.AddComponent<Image>();
        overlayObj.SetActive(false);

        // Drag handler -- drop onto a placed tower for targeted cards, or
        // anywhere for global cards (handled inside UpgradeDragUI).
        UpgradeDragUI drag = borderObj.AddComponent<UpgradeDragUI>();
        drag.card = card;
        drag.validityOverlay = overlayImg;

        // Clicking (without dragging) opens the stat/description popup.
        ShopCardClickHandler clickHandler = borderObj.AddComponent<ShopCardClickHandler>();
        clickHandler.card = card;

        // Use button -- only for cards that don't need a tower target.
        if (!SpinFortShopManager.IsTowerTargetedUpgrade(card.rewardType))
        {
            GameObject useObj = new GameObject("UseButton");
            useObj.transform.SetParent(cardObj.transform, false);
            RectTransform useRT = useObj.AddComponent<RectTransform>();
            useRT.anchorMin = new Vector2(0f, 0f);
            useRT.anchorMax = new Vector2(1f, 0.3f);
            useRT.offsetMin = useRT.offsetMax = Vector2.zero;
            Image useImg = useObj.AddComponent<Image>();
            useImg.color = new Color(0f, 0f, 0f, 0.55f);
            Button useButton = useObj.AddComponent<Button>();

            GameObject useLabelObj = new GameObject("Label");
            useLabelObj.transform.SetParent(useObj.transform, false);
            RectTransform useLabelRT = useLabelObj.AddComponent<RectTransform>();
            useLabelRT.anchorMin = Vector2.zero;
            useLabelRT.anchorMax = Vector2.one;
            useLabelRT.offsetMin = useLabelRT.offsetMax = Vector2.zero;
            var useLabel = useLabelObj.AddComponent<TextMeshProUGUI>();
            useLabel.text      = "USE";
            useLabel.fontSize  = 8;
            useLabel.alignment = TextAlignmentOptions.Center;
            useLabel.color     = new Color32(0xF5, 0xF5, 0xF0, 0xFF);

            useButton.onClick.AddListener(() => SpinFortShopManager.Instance?.UseUpgrade(card));
        }

        return borderObj;
    }

    private void ResizePanel()
    {
        int count    = displayedUpgrades.Count;
        float width  = count > 0
            ? padding * 2 + count * cardSize + (count - 1) * cardGap
            : padding * 2 + cardSize;
        float height = padding * 2 + cardSize;

        RectTransform rt = GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, height);
    }
}
