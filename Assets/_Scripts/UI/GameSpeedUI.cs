using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Persistent speed-toggle HUD (1x/2x/4x). Always visible during gameplay,
// including the buy phase -- fast-forwarding the buy-phase countdown is one
// of the main reasons to use it, unlike the health bar which still hides
// behind the shop panel.
public class GameSpeedUI : MonoBehaviour
{
    public static float CurrentSpeed { get; private set; } = 1f;

    [Header("Colors")]
    public Color buttonColor = new Color(1f, 0.8431f, 0f, 1f);       // Jackpot Gold
    public Color activeColor = new Color(0f, 0.6588f, 0.5882f, 1f);  // Seafoam Teal
    public Color textColor = Color.black;

    private static readonly float[] Speeds = { 1f, 2f, 4f };
    private readonly Image[] buttonImages = new Image[Speeds.Length];

    private void Awake()
    {
        CurrentSpeed = 1f;
        Time.timeScale = 1f;
        EnsureCanvasExists();
        BuildUI();
        RefreshHighlight();
    }

    private void EnsureCanvasExists()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500; // above the regular HUD, below PauseMenuUI (998) / GameOverUI (999)
            gameObject.AddComponent<CanvasScaler>();
            gameObject.AddComponent<GraphicRaycaster>();
        }
    }

    private void BuildUI()
    {
        RectTransform rootRT = GetComponent<RectTransform>();
        if (rootRT == null) rootRT = gameObject.AddComponent<RectTransform>();
        rootRT.anchorMin = Vector2.zero;
        rootRT.anchorMax = Vector2.one;
        rootRT.offsetMin = rootRT.offsetMax = Vector2.zero;

        const float buttonWidth = 50f;
        const float buttonHeight = 36f;
        const float gap = 6f;

        for (int i = 0; i < Speeds.Length; i++)
        {
            float speed = Speeds[i];
            int index = i;

            GameObject buttonObj = new GameObject($"Speed{speed:0}xButton");
            buttonObj.transform.SetParent(transform, false);
            RectTransform buttonRT = buttonObj.AddComponent<RectTransform>();
            buttonRT.anchorMin = buttonRT.anchorMax = buttonRT.pivot = new Vector2(1f, 1f);
            buttonRT.anchoredPosition = new Vector2(-20f - i * (buttonWidth + gap), -20f);
            buttonRT.sizeDelta = new Vector2(buttonWidth, buttonHeight);

            Image buttonImg = buttonObj.AddComponent<Image>();
            buttonImages[i] = buttonImg;
            Button button = buttonObj.AddComponent<Button>();
            button.onClick.AddListener(() => SetSpeed(speed));

            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(buttonObj.transform, false);
            RectTransform labelRT = labelObj.AddComponent<RectTransform>();
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = labelRT.offsetMax = Vector2.zero;
            var label = labelObj.AddComponent<TextMeshProUGUI>();
            label.text = $"{speed:0}x";
            label.fontSize = 18;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = textColor;
        }
    }

    private void SetSpeed(float speed)
    {
        CurrentSpeed = speed;
        if (Time.timeScale > 0f) Time.timeScale = speed; // don't fight an active pause
        RefreshHighlight();
    }

    private void RefreshHighlight()
    {
        for (int i = 0; i < Speeds.Length; i++)
        {
            if (buttonImages[i] == null) continue;
            buttonImages[i].color = Mathf.Approximately(Speeds[i], CurrentSpeed) ? activeColor : buttonColor;
        }
    }
}
