using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Attach to each tower card in the inventory panel
public class TowerDragUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public TowerSO tower;

    // Set by TrashDropHandler's OnDrop (which Unity calls before this
    // object's OnEndDrag) so OnEndDrag knows a UI target already handled the
    // drop and shouldn't also attempt a world placement.
    [HideInInspector] public bool consumedByDrop;

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
        consumedByDrop    = false;

        // Move to root canvas so it renders on top of everything
        transform.SetParent(rootCanvas.transform, true);

        canvasGroup.alpha         = 0.75f;
        canvasGroup.blocksRaycasts = false; // lets the drop land on the slot beneath
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition += eventData.delta / rootCanvas.scaleFactor;

        if (tower != null && TowerPlacementManager.Instance != null)
            TowerPlacementManager.Instance.UpdateDragPreview(tower, eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (TowerPlacementManager.Instance != null)
        {
            // Dragging straight from the inventory onto the grid places the
            // tower immediately -- a second input path alongside the
            // existing 1-5/click-to-place flow, sharing the same rules.
            if (!consumedByDrop && tower != null)
                TowerPlacementManager.Instance.TryPlaceTowerAtScreenPoint(tower, eventData.position);

            TowerPlacementManager.Instance.EndDragPreview();
        }

        // Snap back to original position whether drop succeeded or not
        transform.SetParent(originalParent, true);
        rectTransform.anchoredPosition = originalPosition;

        canvasGroup.alpha          = 1f;
        canvasGroup.blocksRaycasts = true;
    }
}