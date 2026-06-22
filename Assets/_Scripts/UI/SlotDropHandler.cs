using UnityEngine;
using UnityEngine.EventSystems;

// Attach to each slot border so it receives drag-and-drop events
public class SlotDropHandler : MonoBehaviour, IDropHandler
{
    public int slotIndex;
    public LoadoutBarUI owner;

    public void OnDrop(PointerEventData eventData)
    {
        if (owner == null) return;

        // Check if the dragged object carries a TowerSO reference
        TowerDragUI drag = eventData.pointerDrag?.GetComponent<TowerDragUI>();
        if (drag == null || drag.tower == null) return;

        owner.OnTowerDroppedIntoSlot(drag.tower, slotIndex);
    }
}