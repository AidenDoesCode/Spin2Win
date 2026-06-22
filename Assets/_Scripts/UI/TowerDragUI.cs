using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Attach to each tower card in the inventory panel
public class TowerDragUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public TowerSO tower;

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

        // Move to root canvas so it renders on top of everything
        transform.SetParent(rootCanvas.transform, true);

        canvasGroup.alpha         = 0.75f;
        canvasGroup.blocksRaycasts = false; // lets the drop land on the slot beneath
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition += eventData.delta / rootCanvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Snap back to original position whether drop succeeded or not
        transform.SetParent(originalParent, true);
        rectTransform.anchoredPosition = originalPosition;

        canvasGroup.alpha          = 1f;
        canvasGroup.blocksRaycasts = true;
    }
}