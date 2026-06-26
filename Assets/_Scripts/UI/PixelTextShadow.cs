using UnityEngine;
using TMPro;

// New: optional 1-2px drop shadow for crisp pixel-art readability on any
// TextMeshProUGUI. Disabled by default -- flip enableShadow on per-text where
// you want it (shop card labels, gold readout, etc). The shadow auto-mirrors
// the main text's content/font/size every frame, so it stays correct even if
// the label text changes at runtime without any extra wiring.
[RequireComponent(typeof(TextMeshProUGUI))]
public class PixelTextShadow : MonoBehaviour
{
    [Header("Pixel Drop Shadow")]
    public bool enableShadow = false;
    public Color shadowColor = new Color(0.0431f, 0.0745f, 0.1686f, 1f); // #0B132B
    [Tooltip("Pixel offset from the main text (UI space: +X right, -Y down).")]
    public Vector2 pixelOffset = new Vector2(1f, -1f);

    private TextMeshProUGUI mainText;
    private TextMeshProUGUI shadowText;
    private RectTransform shadowRT;

    private void Awake()
    {
        mainText = GetComponent<TextMeshProUGUI>();
    }

    private void LateUpdate()
    {
        if (!enableShadow)
        {
            DestroyShadow();
            return;
        }

        EnsureShadow();
        SyncShadow();
    }

    private void OnDisable() => DestroyShadow();

    private void EnsureShadow()
    {
        if (shadowText != null) return;

        GameObject shadowObj = new GameObject("PixelShadow");
        shadowObj.transform.SetParent(transform.parent, false);
        shadowObj.transform.SetSiblingIndex(transform.GetSiblingIndex()); // sits behind the main text
        shadowRT = shadowObj.AddComponent<RectTransform>();
        shadowText = shadowObj.AddComponent<TextMeshProUGUI>();
        shadowText.raycastTarget = false;
    }

    private void SyncShadow()
    {
        if (shadowText == null || mainText == null) return;

        RectTransform mainRT = (RectTransform)transform;
        shadowRT.anchorMin = mainRT.anchorMin;
        shadowRT.anchorMax = mainRT.anchorMax;
        shadowRT.pivot = mainRT.pivot;
        shadowRT.sizeDelta = mainRT.sizeDelta;
        shadowRT.anchoredPosition = mainRT.anchoredPosition + pixelOffset;

        shadowText.text = mainText.text;
        shadowText.font = mainText.font;
        shadowText.fontSize = mainText.fontSize;
        shadowText.fontStyle = mainText.fontStyle;
        shadowText.alignment = mainText.alignment;
        shadowText.color = shadowColor;
    }

    private void DestroyShadow()
    {
        if (shadowText == null) return;
        Destroy(shadowText.gameObject);
        shadowText = null;
        shadowRT = null;
    }
}
