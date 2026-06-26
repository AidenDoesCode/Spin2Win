using UnityEngine;
using UnityEngine.UI;

// Full-screen red tint that intensifies as the base loses health, ramping up
// tension right into the Game Over screen. Self-building -- attach to any
// empty GameObject in the Arena scene, no manual UI setup needed.
public class LowHealthScreenEffect : MonoBehaviour
{
    [Tooltip("Tint color as health approaches zero.")]
    public Color tintColor = new Color(0.8157f, 0f, 0f, 1f); // Card Suit Red
    [Tooltip("Overlay alpha right before death (0 = invisible, 1 = fully opaque).")]
    [Range(0f, 1f)] public float maxAlpha = 0.55f;
    [Tooltip("Shapes the ramp as health drops. >1 keeps it subtle until health gets low, then it ramps quickly.")]
    public float rampExponent = 2f;

    private Image overlay;
    private BaseHealth baseHealth;

    private void Awake()
    {
        BuildOverlay();
    }

    private void Start()
    {
        baseHealth = BaseHealth.Instance != null ? BaseHealth.Instance : FindAnyObjectByType<BaseHealth>();
        if (baseHealth != null)
        {
            baseHealth.HealthChanged += OnHealthChanged;
            OnHealthChanged(baseHealth.CurrentHealth, baseHealth.maxHealth);
        }
    }

    private void OnDestroy()
    {
        if (baseHealth != null) baseHealth.HealthChanged -= OnHealthChanged;
    }

    private void OnHealthChanged(int current, int max)
    {
        float percent = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
        float lost = 1f - percent;
        float eased = Mathf.Pow(lost, rampExponent);

        Color c = tintColor;
        c.a = eased * maxAlpha;
        overlay.color = c;
    }

    private void BuildOverlay()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
        Transform parent = canvas != null ? canvas.transform : transform;

        GameObject overlayObj = new GameObject("LowHealthOverlay");
        overlayObj.transform.SetParent(parent, false);
        RectTransform rt = overlayObj.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        overlay = overlayObj.AddComponent<Image>();
        overlay.raycastTarget = false;
        Color c = tintColor;
        c.a = 0f;
        overlay.color = c;

        // Sits low in the draw order so anything added after it (loadout bar,
        // shop, and especially GameOverUI) still renders on top.
        overlayObj.transform.SetAsFirstSibling();
    }
}
