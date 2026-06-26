using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Attach to each upgrade card in the upgrade inventory panel. Dragging it
// onto a placed tower in the game world applies a per-tower buff (for
// tower-targeted cards like attack speed/range/damage); dropping anywhere
// else falls back to SpinFortShopManager.UseUpgrade for global cards (heal,
// gold/round, reroll discount, etc).
public class UpgradeDragUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public ShopCardSO card;

    [Tooltip("Full-card overlay tinted green/red while dragging to show whether dropping here would actually do anything. Assigned by UpgradeInventoryUI.")]
    public Image validityOverlay;
    public Color validColor   = new Color(0.2f, 1f, 0.3f, 0.45f);
    public Color invalidColor = new Color(1f, 0.2f, 0.2f, 0.45f);

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Transform originalParent;
    private Vector2 originalPosition;
    private Canvas rootCanvas;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup   = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        rootCanvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent   = transform.parent;
        originalPosition = rectTransform.anchoredPosition;

        transform.SetParent(rootCanvas.transform, true);

        canvasGroup.alpha         = 0.75f;
        canvasGroup.blocksRaycasts = false;

        if (validityOverlay != null)
        {
            validityOverlay.gameObject.SetActive(true);
            UpdateValidityOverlay(eventData.position);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition += eventData.delta / rootCanvas.scaleFactor;
        UpdateValidityOverlay(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        transform.SetParent(originalParent, true);
        rectTransform.anchoredPosition = originalPosition;

        canvasGroup.alpha          = 1f;
        canvasGroup.blocksRaycasts = true;

        if (validityOverlay != null) validityOverlay.gameObject.SetActive(false);

        if (card == null || SpinFortShopManager.Instance == null) return;

        Tower targetTower = FindTowerUnderScreenPoint(eventData.position);
        if (targetTower != null)
            SpinFortShopManager.Instance.UseUpgradeOnTower(card, targetTower);
        else if (!SpinFortShopManager.IsTowerTargetedUpgrade(card.rewardType))
            SpinFortShopManager.Instance.UseUpgrade(card);
    }

    // Tower-targeted cards (attack speed/range/damage) are locked -- red --
    // unless the cursor is currently over a placed tower. Global cards (heal,
    // gold/round, reroll discount, etc) can be used anywhere, so they're
    // always shown green while dragging.
    private void UpdateValidityOverlay(Vector2 screenPoint)
    {
        if (validityOverlay == null || card == null) return;

        bool isValid = !SpinFortShopManager.IsTowerTargetedUpgrade(card.rewardType)
            || FindTowerUnderScreenPoint(screenPoint) != null;

        validityOverlay.color = isValid ? validColor : invalidColor;
    }

    // Mirrors TowerPlacementManager.TrySellTowerAtMouse's approach -- towers
    // already carry the Collider2D it overlap-checks against.
    private Tower FindTowerUnderScreenPoint(Vector2 screenPoint)
    {
        Camera cam = Camera.main;
        if (cam == null) return null;

        Vector2 worldPoint = cam.ScreenToWorldPoint(screenPoint);
        Collider2D hit = Physics2D.OverlapPoint(worldPoint);
        return hit != null ? hit.GetComponent<Tower>() : null;
    }
}
