using UnityEngine;
using UnityEngine.UI;

// Self-building health bar for the base. Attach to any empty GameObject --
// it finds (or doesn't need) a Canvas on its own, no manual UI setup needed.
public class BaseHealthBarUI : MonoBehaviour
{
    [Header("Layout (used when World Target below is left empty)")]
    public Vector2 size = new Vector2(300f, 26f);
    public Vector2 anchorMin = new Vector2(0.5f, 1f);
    public Vector2 anchorMax = new Vector2(0.5f, 1f);
    public Vector2 pivot = new Vector2(0.5f, 1f);
    public Vector2 anchoredPosition = new Vector2(0f, -16f);

    // ADDED: lets the bar track a world-space transform instead of sitting at
    // a fixed screen position -- drag the base (or any empty marker GameObject
    // you've positioned wherever you like) into this field.
    [Header("World Target (optional)")]
    [Tooltip("If assigned, the bar follows this transform's world position every frame -- e.g. drag your Base GameObject in here to anchor the bar under it, or drag in any empty GameObject you've placed wherever you want the bar. Leave empty to use the fixed Layout settings above instead.")]
    public Transform worldTarget;
    [Tooltip("World-space offset from worldTarget, applied before converting to screen position (e.g. negative Y to sit the bar below the base).")]
    public Vector3 worldOffset = new Vector3(0f, -0.6f, 0f);
    [Tooltip("Camera used to project worldTarget into screen space. Defaults to Camera.main if left empty.")]
    public Camera worldCamera;

    [Header("Colors")]
    public Color backgroundColor = new Color(0.8157f, 0f, 0f, 1f);          // Card Suit Red (revealed as damage is taken)
    public Color fillColor = new Color(1f, 0.8431f, 0f, 1f);                // Jackpot Gold (current health)
    public Color borderColor = new Color(0.541f, 0.078f, 0.078f, 1f);       // Red Trim

    private RectTransform fillRT;
    private RectTransform borderRT; // ADDED
    private Canvas canvas; // ADDED
    private BaseHealth baseHealth;
    private GameObject barObj; // ADDED
    private SpinFortShopManager shop; // ADDED

    private void Awake()
    {
        BuildBar();
    }

    // ADDED: re-projects worldTarget's position onto the canvas every frame.
    private void Update()
    {
        if (worldTarget == null || canvas == null || borderRT == null) return;

        Camera cam = worldCamera != null ? worldCamera : Camera.main;
        if (cam == null) return;

        Vector3 screenPoint = cam.WorldToScreenPoint(worldTarget.position + worldOffset);
        Camera uiCam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        RectTransform canvasRT = canvas.transform as RectTransform;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, screenPoint, uiCam, out Vector2 localPoint))
            borderRT.anchoredPosition = localPoint;
    }

    private void Start()
    {
        baseHealth = BaseHealth.Instance != null ? BaseHealth.Instance : FindAnyObjectByType<BaseHealth>();
        if (baseHealth != null)
        {
            baseHealth.HealthChanged += OnHealthChanged;
            OnHealthChanged(baseHealth.CurrentHealth, baseHealth.maxHealth);
        }

        // ADDED: hide the bar whenever the shop is open so it never overlaps
        // the shop panel's title/reroll button, which sit in the same
        // top-center screen region as this bar's default position.
        shop = SpinFortShopManager.Instance != null ? SpinFortShopManager.Instance : FindAnyObjectByType<SpinFortShopManager>();
        if (shop != null)
        {
            shop.ShopOpened += OnShopOpened;
            shop.ShopClosed += OnShopClosed;
            if (shop.IsOpen) OnShopOpened();
        }
    }

    private void OnDestroy()
    {
        if (baseHealth != null) baseHealth.HealthChanged -= OnHealthChanged;
        if (shop != null)
        {
            shop.ShopOpened -= OnShopOpened;
            shop.ShopClosed -= OnShopClosed;
        }
    }

    private void OnShopOpened() => barObj?.SetActive(false); // ADDED
    private void OnShopClosed() => barObj?.SetActive(true); // ADDED

    private void OnHealthChanged(int current, int max)
    {
        float percent = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
        fillRT.anchorMax = new Vector2(percent, 1f);
    }

    private void BuildBar()
    {
        canvas = GetComponentInParent<Canvas>(); // CHANGED: now a field, not a local, so Update() can reuse it
        if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
        Transform parent = canvas != null ? canvas.transform : transform;

        GameObject borderObj = new GameObject("BaseHealthBar");
        barObj = borderObj; // ADDED: cached so OnShopOpened/OnShopClosed can toggle it
        borderObj.transform.SetParent(parent, false);
        borderRT = borderObj.AddComponent<RectTransform>(); // CHANGED: cached on the field

        if (worldTarget != null)
        {
            // ADDED: center-pivot/anchor so anchoredPosition can be driven
            // directly from the screen-projected world point in Update().
            borderRT.anchorMin = borderRT.anchorMax = borderRT.pivot = new Vector2(0.5f, 0.5f);
        }
        else
        {
            borderRT.anchorMin = anchorMin;
            borderRT.anchorMax = anchorMax;
            borderRT.pivot = pivot;
            borderRT.anchoredPosition = anchoredPosition;
        }
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
