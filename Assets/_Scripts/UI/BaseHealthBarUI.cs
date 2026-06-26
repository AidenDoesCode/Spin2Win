using UnityEngine;
using UnityEngine.UI;

// Self-building health bar for the base. Attach to any empty GameObject --
// it finds (or doesn't need) a Canvas on its own, no manual UI setup needed.
// Separate from BaseHealthUI (the text readout) so it doesn't disturb
// whatever's already wired up there.
public class BaseHealthBarUI : MonoBehaviour
{
    [Header("Layout")]
    public Vector2 size = new Vector2(300f, 26f);
    public Vector2 anchorMin = new Vector2(0.5f, 1f);
    public Vector2 anchorMax = new Vector2(0.5f, 1f);
    public Vector2 pivot = new Vector2(0.5f, 1f);
    public Vector2 anchoredPosition = new Vector2(0f, -16f);

    [Header("Colors")]
    public Color backgroundColor = new Color(0.8157f, 0f, 0f, 1f);          // Card Suit Red (revealed as damage is taken)
    public Color fillColor = new Color(1f, 0.8431f, 0f, 1f);                // Jackpot Gold (current health)
    public Color borderColor = new Color(0.541f, 0.078f, 0.078f, 1f);       // Red Trim

    private RectTransform fillRT;
    private BaseHealth baseHealth;

    private void Awake()
    {
        BuildBar();
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
        fillRT.anchorMax = new Vector2(percent, 1f);
    }

    private void BuildBar()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
        Transform parent = canvas != null ? canvas.transform : transform;

        GameObject borderObj = new GameObject("BaseHealthBar");
        borderObj.transform.SetParent(parent, false);
        RectTransform borderRT = borderObj.AddComponent<RectTransform>();
        borderRT.anchorMin = anchorMin;
        borderRT.anchorMax = anchorMax;
        borderRT.pivot = pivot;
        borderRT.anchoredPosition = anchoredPosition;
        borderRT.sizeDelta = size;
        Image borderImg = borderObj.AddComponent<Image>();
        borderImg.color = borderColor;

        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(borderObj.transform, false);
        RectTransform bgRT = bgObj.AddComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = new Vector2(2f, 2f);
        bgRT.offsetMax = new Vector2(-2f, -2f);
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = backgroundColor;

        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(bgObj.transform, false);
        fillRT = fillObj.AddComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = Vector2.zero;
        fillRT.offsetMax = Vector2.zero;
        Image fillImg = fillObj.AddComponent<Image>();
        fillImg.color = fillColor;
    }
}
