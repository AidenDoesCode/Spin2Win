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
    public Color lockedSlotColor   = new Color(0.18f, 0.4f, 0.25f, 1f);

    [Header("Trash / Recycle")]
    [Tooltip("Icon shown on the recycle slot next to the loadout bar")]
    public Sprite trashIcon;
    public Color trashIconColor = Color.white;
    [Range(0f, 1f)] public float trashRefundPercent = 0.5f;

    private Image[] slotBorders   = new Image[5];
    private Image[] slotIcons     = new Image[5];
    private TextMeshProUGUI[] countLabels = new TextMeshProUGUI[5];
    private TowerDragUI[] slotDrags = new TowerDragUI[5];
    private int currentSelected  = 0;
    private int currentLocked    = -1;

    private void Awake()
    {
        BuildBar();
    }

    private void Start()
    {
        if (TowerPlacementManager.Instance != null)
        {
            SelectSlot(TowerPlacementManager.Instance.selectedSlot);
            TowerPlacementManager.Instance.SlotChanged += RefreshSlot;
        }
        else
        {
            SelectSlot(0);
        }

        if (PlayerTowerInventory.Instance != null)
            PlayerTowerInventory.Instance.InventoryChanged += RefreshAllSlots;

        RefreshAllSlots();
    }

    private void OnDestroy()
    {
        if (TowerPlacementManager.Instance != null)
            TowerPlacementManager.Instance.SlotChanged -= RefreshSlot;

        if (PlayerTowerInventory.Instance != null)
            PlayerTowerInventory.Instance.InventoryChanged -= RefreshAllSlots;
    }

    private void Update()
    {
        if (TowerPlacementManager.Instance == null) return;

        if (TowerPlacementManager.Instance.selectedSlot != currentSelected)
            SelectSlot(TowerPlacementManager.Instance.selectedSlot);

        if (TowerPlacementManager.Instance.lockedSlotIndex != currentLocked)
        {
            currentLocked = TowerPlacementManager.Instance.lockedSlotIndex;
            RefreshBorderColors();
            RefreshAllSlots();
        }
    }

    private void BuildBar()
    {
        RectTransform rt = GetComponent<RectTransform>();
        float totalWidth  = padding * 2 + 6 * slotSize + 5 * slotGap;
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

            // Lets the player drag the assigned tower out of this slot (e.g. to the trash)
            slotDrags[i] = iconObj.AddComponent<TowerDragUI>();

            // Owned-copies count badge (card consumption: shows how many placements are left)
            GameObject countObj = new GameObject($"Count_{i + 1}");
            countObj.transform.SetParent(slotObj.transform, false);
            RectTransform countRT = countObj.AddComponent<RectTransform>();
            countRT.anchorMin = new Vector2(1f, 1f);
            countRT.anchorMax = new Vector2(1f, 1f);
            countRT.pivot = new Vector2(1f, 1f);
            countRT.anchoredPosition = new Vector2(-2f, -2f);
            countRT.sizeDelta = new Vector2(20f, 14f);
            var countLabel = countObj.AddComponent<TextMeshProUGUI>();
            countLabel.fontSize = 10;
            countLabel.alignment = TextAlignmentOptions.TopRight;
            countLabel.color = Color.white;
            countLabel.enabled = false;
            countLabels[i] = countLabel;

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

        BuildTrashSlot();
    }

    private void BuildTrashSlot()
    {
        float xPos = padding + 5 * (slotSize + slotGap);

        GameObject borderObj = new GameObject("TrashSlot");
        borderObj.transform.SetParent(transform, false);
        RectTransform borderRT = borderObj.AddComponent<RectTransform>();
        borderRT.anchorMin        = new Vector2(0f, 0.5f);
        borderRT.anchorMax        = new Vector2(0f, 0.5f);
        borderRT.pivot            = new Vector2(0f, 0.5f);
        borderRT.anchoredPosition = new Vector2(xPos, 0f);
        borderRT.sizeDelta        = new Vector2(slotSize, slotSize);
        Image borderImg = borderObj.AddComponent<Image>();
        borderImg.color = borderNormalColor;

        GameObject slotObj = new GameObject("TrashSlotInner");
        slotObj.transform.SetParent(borderObj.transform, false);
        RectTransform slotRT = slotObj.AddComponent<RectTransform>();
        slotRT.anchorMin = Vector2.zero;
        slotRT.anchorMax = Vector2.one;
        slotRT.offsetMin = new Vector2(2f, 2f);
        slotRT.offsetMax = new Vector2(-2f, -2f);
        Image slotImg = slotObj.AddComponent<Image>();
        slotImg.color = slotNormalColor;

        GameObject iconObj = new GameObject("TrashIcon");
        iconObj.transform.SetParent(slotObj.transform, false);
        RectTransform iconRT = iconObj.AddComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0.1f, 0.1f);
        iconRT.anchorMax = new Vector2(0.9f, 0.9f);
        iconRT.offsetMin = iconRT.offsetMax = Vector2.zero;
        Image iconImg = iconObj.AddComponent<Image>();
        iconImg.sprite         = trashIcon;
        iconImg.preserveAspect = true;
        iconImg.color          = trashIconColor;
        iconImg.enabled        = trashIcon != null;
        iconImg.raycastTarget  = false;

        TrashDropHandler trash = borderObj.AddComponent<TrashDropHandler>();
        trash.refundPercent = trashRefundPercent;
    }

    public void SelectSlot(int index)
    {
        if (index < 0 || index >= 5) return;
        currentSelected = index;
        RefreshBorderColors();
    }

    private void RefreshBorderColors()
    {
        for (int i = 0; i < 5; i++)
        {
            if (slotBorders[i] == null) continue;
            slotBorders[i].color = (i == currentLocked) ? lockedSlotColor
                : (i == currentSelected) ? slotSelectedColor
                : borderNormalColor;
        }
    }

    public void RefreshSlot(int index)
    {
        if (index < 0 || index >= 5) return;
        if (TowerPlacementManager.Instance == null) return;

        TowerSO tower = TowerPlacementManager.Instance.loadout[index];
        Image icon    = slotIcons[index];
        if (icon == null) return;

        if (slotDrags[index] != null) slotDrags[index].tower = tower;

        if (tower != null && tower.icon != null)
        {
            icon.sprite  = tower.icon;
            icon.color   = (index == TowerPlacementManager.Instance.lockedSlotIndex) ? lockedSlotColor : Color.white;
            icon.enabled = true;
        }
        else
        {
            icon.sprite  = null;
            icon.color   = emptyIconColor;
            icon.enabled = false;
        }

        var countLabel = countLabels[index];
        if (countLabel != null)
        {
            int owned = CountOwned(tower);
            countLabel.enabled = tower != null;
            countLabel.text = $"x{owned}";
        }
    }

    private int CountOwned(TowerSO tower)
    {
        if (tower == null || PlayerTowerInventory.Instance == null) return 0;

        int count = 0;
        foreach (var owned in PlayerTowerInventory.Instance.ownedTowers)
        {
            if (owned == tower) count++;
        }
        return count;
    }

    public void RefreshAllSlots()
    {
        for (int i = 0; i < 5; i++)
            RefreshSlot(i);
    }

    public void OnTowerDroppedIntoSlot(TowerSO tower, int slotIndex)
    {
        if (TowerPlacementManager.Instance == null) return;
        if (!TowerPlacementManager.Instance.AssignTowerToSlot(tower, slotIndex)) return;
        RefreshSlot(slotIndex);
        SelectSlot(slotIndex);
    }
}