using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class LoadoutBarUI : MonoBehaviour
{
    [Header("Slot Settings")]
    public float slotSize = 64f;
    public float slotGap = 8f;
    public float padding = 8f;

    [Header("Colors")]
    public Color panelColor        = new Color(0.102f, 0.102f, 0.180f, 1f);
    public Color slotNormalColor   = new Color(0.059f, 0.059f, 0.137f, 1f);
    public Color slotSelectedColor = new Color(0.878f, 0.753f, 0.376f, 1f);
    public Color borderNormalColor = new Color(0.353f, 0.353f, 0.604f, 1f);
    public Color emptyIconColor    = new Color(0.227f, 0.227f, 0.416f, 0.5f);

    private Image[] slotBorders = new Image[5];
    private Image[] slotIcons   = new Image[5];
    private int currentSelected  = 0;

    private void Awake()
    {
        BuildBar();
    }

    private void Start()
    {
        if (TowerPlacementManager.Instance != null)
            SelectSlot(TowerPlacementManager.Instance.selectedSlot);
        else
            SelectSlot(0);

        RefreshAllSlots();
    }

    private void Update()
    {
        if (TowerPlacementManager.Instance != null &&
            TowerPlacementManager.Instance.selectedSlot != currentSelected)
        {
            SelectSlot(TowerPlacementManager.Instance.selectedSlot);
        }
    }

    private void BuildBar()
    {
        RectTransform rt = GetComponent<RectTransform>();
        float totalWidth  = padding * 2 + 5 * slotSize + 4 * slotGap;
        float totalHeight = padding * 2 + slotSize;
        rt.sizeDelta = new Vector2(totalWidth, totalHeight);

        Image panel = GetComponent<Image>();
        if (panel == null) panel = gameObject.AddComponent<Image>();
        panel.color = panelColor;

        for (int i = 0; i < 5; i++)
        {
            float xPos = padding + i * (slotSize + slotGap);

            // Border
            GameObject borderObj = new GameObject($"SlotBorder_{i + 1}");
            borderObj.transform.SetParent(transform, false);
            RectTransform borderRT = borderObj.AddComponent<RectTransform>();
            borderRT.anchorMin        = new Vector2(0f, 0.5f);
            borderRT.anchorMax        = new Vector2(0f, 0.5f);
            borderRT.pivot            = new Vector2(0f, 0.5f);
            borderRT.anchoredPosition = new Vector2(xPos, 0f);
            borderRT.sizeDelta        = new Vector2(slotSize, slotSize);
            Image borderImg = borderObj.AddComponent<Image>();
            borderImg.color = borderNormalColor;
            slotBorders[i]  = borderImg;

            // Inner slot
            GameObject slotObj = new GameObject($"Slot_{i + 1}");
            slotObj.transform.SetParent(borderObj.transform, false);
            RectTransform slotRT = slotObj.AddComponent<RectTransform>();
            slotRT.anchorMin = Vector2.zero;
            slotRT.anchorMax = Vector2.one;
            slotRT.offsetMin = new Vector2(2f, 2f);
            slotRT.offsetMax = new Vector2(-2f, -2f);
            Image slotImg = slotObj.AddComponent<Image>();
            slotImg.color = slotNormalColor;

            // Icon
            GameObject iconObj = new GameObject($"Icon_{i + 1}");
            iconObj.transform.SetParent(slotObj.transform, false);
            RectTransform iconRT = iconObj.AddComponent<RectTransform>();
            iconRT.anchorMin = new Vector2(0.1f, 0.1f);
            iconRT.anchorMax = new Vector2(0.9f, 0.9f);
            iconRT.offsetMin = iconRT.offsetMax = Vector2.zero;
            Image iconImg = iconObj.AddComponent<Image>();
            iconImg.preserveAspect = true;
            iconImg.color          = emptyIconColor;
            iconImg.enabled        = false;
            slotIcons[i]           = iconImg;

            // Label
            GameObject labelObj = new GameObject($"Label_{i + 1}");
            labelObj.transform.SetParent(slotObj.transform, false);
            RectTransform labelRT = labelObj.AddComponent<RectTransform>();
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = new Vector2(1f, 0f);
            labelRT.offsetMin = new Vector2(0f, 2f);
            labelRT.offsetMax = new Vector2(0f, 14f);
            var label = labelObj.AddComponent<TextMeshProUGUI>();
            label.text      = (i + 1).ToString();
            label.fontSize  = 9;
            label.alignment = TextAlignmentOptions.Bottom;
            label.color     = new Color(0.35f, 0.35f, 0.65f, 1f);

            // Drop handler
            int slotIndex = i;
            SlotDropHandler drop = borderObj.AddComponent<SlotDropHandler>();
            drop.slotIndex = slotIndex;
            drop.owner     = this;
        }
    }

    public void SelectSlot(int index)
    {
        if (index < 0 || index >= 5) return;
        currentSelected = index;

        for (int i = 0; i < 5; i++)
        {
            if (slotBorders[i] == null) continue;
            slotBorders[i].color = (i == index) ? slotSelectedColor : borderNormalColor;
        }
    }

    public void RefreshSlot(int index)
    {
        if (index < 0 || index >= 5) return;
        if (TowerPlacementManager.Instance == null) return;

        TowerSO tower = TowerPlacementManager.Instance.loadout[index];
        Image icon    = slotIcons[index];
        if (icon == null) return;

        if (tower != null && tower.icon != null)
        {
            icon.sprite  = tower.icon;
            icon.color   = Color.white;
            icon.enabled = true;
        }
        else
        {
            icon.sprite  = null;
            icon.color   = emptyIconColor;
            icon.enabled = false;
        }
    }

    public void RefreshAllSlots()
    {
        for (int i = 0; i < 5; i++)
            RefreshSlot(i);
    }

    public void OnTowerDroppedIntoSlot(TowerSO tower, int slotIndex)
    {
        if (TowerPlacementManager.Instance == null) return;
        TowerPlacementManager.Instance.AssignTowerToSlot(tower, slotIndex);
        RefreshSlot(slotIndex);
        SelectSlot(slotIndex);
    }
}