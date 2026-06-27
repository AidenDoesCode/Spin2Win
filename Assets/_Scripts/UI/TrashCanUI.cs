using UnityEngine;
using UnityEngine.UI;

// Standalone recycle-bin panel that can be repositioned independently (drag
// its border via DraggablePanel). Dropping a tower card on it recycles the
// card via TrashDropHandler.
public class TrashCanUI : MonoBehaviour
{
    [Header("Size")]
    public float size = 64f;

    [Header("Colors")]
    public Color borderColor = new Color(0.541f, 0.078f, 0.078f, 1f);
    public Color slotColor   = new Color(0.122f, 0.020f, 0.020f, 1f);

    [Header("Icon")]
    public Sprite trashIcon;
    public Color trashIconColor = Color.white;

    [Range(0f, 1f)] public float refundPercent = 0.5f;

    private void Awake()
    {
        Build();
    }

    private void Build()
    {
        RectTransform rt = GetComponent<RectTransform>();
        if (rt == null) rt = gameObject.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(size, size);

        Image borderImg = GetComponent<Image>();
        if (borderImg == null) borderImg = gameObject.AddComponent<Image>();
        borderImg.color = borderColor;

        GameObject slotObj = new GameObject("Inner");
        slotObj.transform.SetParent(transform, false);
        RectTransform slotRT = slotObj.AddComponent<RectTransform>();
        slotRT.anchorMin = Vector2.zero;
        slotRT.anchorMax = Vector2.one;
        slotRT.offsetMin = new Vector2(2f, 2f);
        slotRT.offsetMax = new Vector2(-2f, -2f);
        Image slotImg = slotObj.AddComponent<Image>();
        slotImg.color = slotColor;
        slotImg.raycastTarget = false;

        GameObject iconObj = new GameObject("Icon");
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

        TrashDropHandler trash = gameObject.AddComponent<TrashDropHandler>();
        trash.refundPercent = refundPercent;

        gameObject.AddComponent<DraggablePanel>();
    }
}
