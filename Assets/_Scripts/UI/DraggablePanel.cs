using UnityEngine;
using UnityEngine.EventSystems;

// Lets the player reposition a whole UI panel (e.g. the standalone trash can)
// anywhere on screen. Unlike TowerDragUI, this drags the panel itself rather
// than a payload, and never snaps back to its starting position.
public class DraggablePanel : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    private RectTransform rectTransform;
    private Canvas rootCanvas;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        rootCanvas    = GetComponentInParent<Canvas>();
    }

    // Required so the EventSystem recognizes this object as a drag source --
    // OnDrag is only routed to whatever began the drag, not whatever is
    // currently under the cursor.
    public void OnBeginDrag(PointerEventData eventData) { }

    public void OnDrag(PointerEventData eventData)
    {
        float scale = rootCanvas != null ? rootCanvas.scaleFactor : 1f;
        rectTransform.anchoredPosition += eventData.delta / scale;
    }
}
