using UnityEngine;
using UnityEngine.EventSystems;

// Attach to a tower card's outer container in the shop, loadout bar, or
// inventory. Clicks that land on a Button or a drag handle elsewhere on the
// card are claimed by that component first (the click event bubbles up only
// when nothing closer to the cursor already handled it), so this only fires
// for plain clicks on the card itself.
public class TowerCardClickHandler : MonoBehaviour, IPointerClickHandler
{
    public TowerSO tower;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (tower == null) return;
        TowerDetailPopupUI.Instance?.Show(tower);
    }
}
