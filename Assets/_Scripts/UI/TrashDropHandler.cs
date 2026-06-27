using UnityEngine;
using UnityEngine.EventSystems;

// Drop target for the standalone trash can. Dragging a tower card here from
// the inventory panel recycles it for a partial gold refund.
public class TrashDropHandler : MonoBehaviour, IDropHandler
{
    [Range(0f, 1f)] public float refundPercent = 0.5f;

    public void OnDrop(PointerEventData eventData)
    {
        TowerDragUI drag = eventData.pointerDrag?.GetComponent<TowerDragUI>();
        if (drag == null || drag.tower == null) return;

        drag.consumedByDrop = true;
        TowerSO tower = drag.tower;

        PlayerTowerInventory.Instance?.RemoveTower(tower);

        int refund = Mathf.RoundToInt(tower.cost * refundPercent);
        ScoreManager.Instance?.AddScore(refund);

        // Riptide Counter-Current -- selling/recycling a tower card triggers
        // the map-wide shockwave, if that upgrade's been bought.
        GameModifiers.Instance?.TriggerSellExplosionIfEnabled();
    }
}
