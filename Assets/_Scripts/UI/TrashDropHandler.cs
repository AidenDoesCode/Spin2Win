using UnityEngine;
using UnityEngine.EventSystems;

// Drop target next to the loadout bar. Dragging a tower card here (from the
// inventory panel or out of a loadout slot) recycles it for a partial gold
// refund -- the escape valve for a tower stuck in the locked slot.
public class TrashDropHandler : MonoBehaviour, IDropHandler
{
    [Range(0f, 1f)] public float refundPercent = 0.5f;

    public void OnDrop(PointerEventData eventData)
    {
        TowerDragUI drag = eventData.pointerDrag?.GetComponent<TowerDragUI>();
        if (drag == null || drag.tower == null) return;

        TowerSO tower = drag.tower;

        if (TowerPlacementManager.Instance != null)
        {
            var loadout = TowerPlacementManager.Instance.loadout;
            for (int i = 0; i < loadout.Length; i++)
            {
                if (loadout[i] == tower) TowerPlacementManager.Instance.ClearSlot(i);
            }
        }

        PlayerTowerInventory.Instance?.RemoveTower(tower);

        int refund = Mathf.RoundToInt(tower.cost * refundPercent);
        ScoreManager.Instance?.AddScore(refund);
    }
}
