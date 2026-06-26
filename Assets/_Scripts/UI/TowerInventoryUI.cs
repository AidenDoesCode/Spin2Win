using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TowerInventoryUI : MonoBehaviour
{
    [Header("Settings")]
    public float cardSize = 64f;
    public float cardGap  = 8f;
    public float padding  = 8f;

    [Header("Background")]
    [Tooltip("Casino-themed panel background. If assigned, replaces the flat panelColor fill.")]
    public Sprite panelSprite;

    [Header("Colors")]
    public Color panelColor      = new Color(0.2f, 0.031f, 0.031f, 0.95f);  // Deep Maroon (casino felt)
    public Color cardColor       = new Color(0.122f, 0.020f, 0.020f, 1f);   // Darker Wine
    public Color cardBorderColor = new Color(0.541f, 0.078f, 0.078f, 1f);   // Red Trim

    private readonly List<TowerSO> displayedTowers = new List<TowerSO>();
    private readonly List<GameObject> cards        = new List<GameObject>();
    private int lastKnownCount = -1;

    private void Awake()
    {
        Image panel = GetComponent<Image>();
        if (panel == null) panel = gameObject.AddComponent<Image>();
        if (panelSprite != null)
        {
            panel.sprite = panelSprite;
            panel.type = Image.Type.Simple;
            panel.color = Color.white; // sprite already carries the casino color treatment
        }
        else
        {
            panel.color = panelColor;
        }
    }

    private void Start()
    {
        if (PlayerTowerInventory.Instance != null)
            PlayerTowerInventory.Instance.InventoryChanged += OnInventoryChanged;

        RefreshCards();
    }

    private void OnDestroy()
    {
        if (PlayerTowerInventory.Instance != null)
            PlayerTowerInventory.Instance.InventoryChanged -= OnInventoryChanged;
    }

    private void Update()
    {
        if (PlayerTowerInventory.Instance == null) return;
        int count = PlayerTowerInventory.Instance.ownedTowers.Count;
        if (count != lastKnownCount)
            RefreshCards();
    }

    private void OnInventoryChanged() => RefreshCards();

    private void RefreshCards()
    {
        foreach (GameObject card in cards)
            if (card != null) Destroy(card);
        cards.Clear();
        displayedTowers.Clear();

        if (PlayerTowerInventory.Instance == null)
        {
            Debug.LogWarning("TowerInventoryUI: PlayerTowerInventory.Instance is null");
            return;
        }

        Debug.Log($"TowerInventoryUI: Refreshing cards, inventory has {PlayerTowerInventory.Instance.ownedTowers.Count} towers");

        foreach (TowerSO tower in PlayerTowerInventory.Instance.ownedTowers)
        {
            if (tower == null)
            {
                Debug.LogWarning("TowerInventoryUI: Tower in list is null");
                continue;
            }
            Debug.Log($"TowerInventoryUI: Creating card for {tower.towerName}");
            displayedTowers.Add(tower);
            cards.Add(CreateCard(tower, cards.Count));
        }

        lastKnownCount = PlayerTowerInventory.Instance.ownedTowers.Count;
        ResizePanel();
        Debug.Log($"TowerInventoryUI: Refresh complete, {cards.Count} cards created");
    }

    private GameObject CreateCard(TowerSO tower, int index)
    {
        float xPos = padding + index * (cardSize + cardGap);

        // Border
        GameObject borderObj = new GameObject($"Card_{tower.towerName}");
        borderObj.transform.SetParent(transform, false);
        RectTransform borderRT = borderObj.AddComponent<RectTransform>();
        borderRT.anchorMin        = new Vector2(0f, 0.5f);
        borderRT.anchorMax        = new Vector2(0f, 0.5f);
        borderRT.pivot            = new Vector2(0f, 0.5f);
        borderRT.anchoredPosition = new Vector2(xPos, 0f);
        borderRT.sizeDelta        = new Vector2(cardSize, cardSize);
        Image borderImg = borderObj.AddComponent<Image>();
        borderImg.color = cardBorderColor;

        // Inner card
        GameObject cardObj = new GameObject("Inner");
        cardObj.transform.SetParent(borderObj.transform, false);
        RectTransform cardRT = cardObj.AddComponent<RectTransform>();
        cardRT.anchorMin = Vector2.zero;
        cardRT.anchorMax = Vector2.one;
        cardRT.offsetMin = new Vector2(2f, 2f);
        cardRT.offsetMax = new Vector2(-2f, -2f);
        Image cardImg = cardObj.AddComponent<Image>();
        cardImg.color = cardColor;

        // Icon
        if (tower.icon != null)
        {
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(cardObj.transform, false);
            RectTransform iconRT = iconObj.AddComponent<RectTransform>();
            iconRT.anchorMin = new Vector2(0.1f, 0.1f);
            iconRT.anchorMax = new Vector2(0.9f, 0.9f);
            iconRT.offsetMin = iconRT.offsetMax = Vector2.zero;
            Image iconImg = iconObj.AddComponent<Image>();
            iconImg.sprite         = tower.icon;
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
        label.text      = tower.towerName;
        label.fontSize  = 7;
        label.alignment = TextAlignmentOptions.Bottom;
        label.color     = Color.white;

        // Drag handler
        TowerDragUI drag = borderObj.AddComponent<TowerDragUI>();
        drag.tower = tower;

        // Clicking (without dragging) opens the stat/description popup
        TowerCardClickHandler clickHandler = borderObj.AddComponent<TowerCardClickHandler>();
        clickHandler.tower = tower;

        return borderObj;
    }

    private void ResizePanel()
    {
        int count    = displayedTowers.Count;
        float width  = count > 0
            ? padding * 2 + count * cardSize + (count - 1) * cardGap
            : padding * 2 + cardSize;
        float height = padding * 2 + cardSize;

        RectTransform rt = GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, height);
    }
}