using UnityEngine;
using UnityEngine.EventSystems;

// Attach to an upgrade card's outer container in the shop or the upgrade
// inventory. Mirrors TowerCardClickHandler but for non-Tower ShopCardSO
// offers, which TowerDetailPopupUI.Show(ShopCardSO) renders stats for.
public class ShopCardClickHandler : MonoBehaviour, IPointerClickHandler
{
    public ShopCardSO card;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (card == null) return;
        TowerDetailPopupUI.Instance?.Show(card);
    }
}
