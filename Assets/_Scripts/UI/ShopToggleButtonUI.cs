using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Small persistent button that shows only during the buy phase.
// The big ShopUI modal already auto-opens on the buy phase; this lets the
// player reopen it after closing it manually.
public class ShopToggleButtonUI : MonoBehaviour
{
    public ShopUI shopUI;

    [Header("Layout")]
    public float width = 110f;
    public float height = 40f;

    [Header("Colors")]
    public Color buttonColor = new Color(1f, 0.8431f, 0f, 1f); // Jackpot Gold
    public Color textColor   = Color.black;

    private SpinFortShopManager shop;
    private Button button;

    private void Awake()
    {
        BuildButton();
    }

    private void Start()
    {
        if (shopUI == null) shopUI = FindAnyObjectByType<ShopUI>();

        shop = SpinFortShopManager.Instance;
        if (shop == null) shop = FindAnyObjectByType<SpinFortShopManager>();

        if (shop != null)
        {
            shop.ShopOpened += OnShopOpened;
            shop.ShopClosed += OnShopClosed;
        }

        gameObject.SetActive(shop != null && shop.IsOpen);
    }

    private void OnDestroy()
    {
        if (shop != null)
        {
            shop.ShopOpened -= OnShopOpened;
            shop.ShopClosed -= OnShopClosed;
        }
    }

    private void OnShopOpened() => gameObject.SetActive(true);
    private void OnShopClosed() => gameObject.SetActive(false);

    private void OnButtonClicked()
    {
        if (shopUI == null) return;
        if (shopUI.gameObject.activeSelf) shopUI.Hide();
        else shopUI.Show();
    }

    private void BuildButton()
    {
        RectTransform rt = GetComponent<RectTransform>();
        if (rt == null) rt = gameObject.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, height);

        Image img = GetComponent<Image>();
        if (img == null) img = gameObject.AddComponent<Image>();
        img.color = buttonColor;

        button = GetComponent<Button>();
        if (button == null) button = gameObject.AddComponent<Button>();
        button.onClick.AddListener(OnButtonClicked);

        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(transform, false);
        RectTransform labelRT = labelObj.AddComponent<RectTransform>();
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = labelRT.offsetMax = Vector2.zero;
        var label = labelObj.AddComponent<TextMeshProUGUI>();
        label.text = "SHOP";
        label.fontSize = 16;
        label.alignment = TextAlignmentOptions.Center;
        label.color = textColor;
    }
}
