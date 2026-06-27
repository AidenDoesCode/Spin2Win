using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;

// Esc-key pause menu. Built the same way GameOverUI builds itself: a
// self-attaching screen-space Canvas with programmatic buttons. Resume just
// unpauses; Restart fully reloads the scene (same path as GameOverUI's
// Restart button) so it never resumes mid-round.
public class PauseMenuUI : MonoBehaviour
{
    [Header("Colors")]
    public Color backdropColor = new Color(0.078f, 0.012f, 0.012f, 0.9f);
    public Color panelColor = new Color(0.2f, 0.031f, 0.031f, 0.97f);
    public Color buttonColor = new Color(1f, 0.8431f, 0f, 1f);
    public Color titleColor = new Color(1f, 0.8431f, 0f, 1f);

    [Header("Scenes")]
    public string mainMenuSceneName = "MainMenu";

    private bool isPaused = false;
    private GameObject panelObj;
    private GameObject settingsPanelObj;

    private void Awake()
    {
        EnsureCanvasExists();
        BuildUI();
        SetVisualsActive(false);
    }

    private void Update()
    {
        if (BaseHealth.Instance != null && BaseHealth.Instance.IsDead) return;
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            TogglePause();
    }

    private void TogglePause() => SetPaused(!isPaused);

    private void SetPaused(bool paused)
    {
        isPaused = paused;
        Time.timeScale = paused ? 0f : GameSpeedUI.CurrentSpeed;
        SetVisualsActive(paused);
        MusicManager.Instance?.SetPauseMuffled(paused);
        if (!paused) settingsPanelObj.SetActive(false);
    }

    private void EnsureCanvasExists()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 998; // just under GameOverUI
            gameObject.AddComponent<CanvasScaler>();
            gameObject.AddComponent<GraphicRaycaster>();
        }
    }

    private void SetVisualsActive(bool active)
    {
        Image img = GetComponent<Image>();
        if (img != null) img.enabled = active;
        panelObj.SetActive(active);
    }

    private void OnResumeClicked() => SetPaused(false);

    private void OnRestartClicked()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnMainMenuClicked()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void OnSettingsClicked() => settingsPanelObj.SetActive(!settingsPanelObj.activeSelf);

    private void BuildUI()
    {
        RectTransform rootRT = GetComponent<RectTransform>();
        if (rootRT == null) rootRT = gameObject.AddComponent<RectTransform>();
        rootRT.anchorMin = Vector2.zero;
        rootRT.anchorMax = Vector2.one;
        rootRT.offsetMin = rootRT.offsetMax = Vector2.zero;

        Image backdrop = GetComponent<Image>();
        if (backdrop == null) backdrop = gameObject.AddComponent<Image>();
        backdrop.color = backdropColor;

        panelObj = new GameObject("Panel");
        panelObj.transform.SetParent(transform, false);
        RectTransform panelRT = panelObj.AddComponent<RectTransform>();
        panelRT.anchorMin = panelRT.anchorMax = panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta = new Vector2(420f, 420f);
        Image panelImg = panelObj.AddComponent<Image>();
        panelImg.color = panelColor;

        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panelObj.transform, false);
        RectTransform titleRT = titleObj.AddComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0f, 1f);
        titleRT.anchorMax = new Vector2(1f, 1f);
        titleRT.pivot = new Vector2(0.5f, 1f);
        titleRT.anchoredPosition = new Vector2(0f, -30f);
        titleRT.sizeDelta = new Vector2(0f, 60f);
        var title = titleObj.AddComponent<TextMeshProUGUI>();
        title.text = "PAUSED";
        title.fontSize = 44;
        title.alignment = TextAlignmentOptions.Center;
        title.color = titleColor;
        title.fontStyle = FontStyles.Bold;
        AddShadow(title);

        BuildButton(panelObj.transform, "ResumeButton", "Resume", new Vector2(0f, 280f), OnResumeClicked);
        BuildButton(panelObj.transform, "SettingsButton", "Settings", new Vector2(0f, 210f), OnSettingsClicked);
        BuildButton(panelObj.transform, "RestartButton", "Restart", new Vector2(0f, 140f), OnRestartClicked);
        BuildButton(panelObj.transform, "MainMenuButton", "Main Menu", new Vector2(0f, 70f), OnMainMenuClicked);

        BuildSettingsPanel(panelObj.transform);
    }

    private void BuildSettingsPanel(Transform parent)
    {
        settingsPanelObj = SettingsSliderUI.BuildVolumePanel(parent, new Vector2(0f, 20f), buttonColor);
        settingsPanelObj.SetActive(false);
    }

    private void BuildButton(Transform parent, string name, string text, Vector2 anchoredPosition, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(parent, false);
        RectTransform buttonRT = buttonObj.AddComponent<RectTransform>();
        buttonRT.anchorMin = buttonRT.anchorMax = buttonRT.pivot = new Vector2(0.5f, 0f);
        buttonRT.anchoredPosition = anchoredPosition;
        buttonRT.sizeDelta = new Vector2(260f, 60f);
        Image buttonImg = buttonObj.AddComponent<Image>();
        buttonImg.color = buttonColor;
        Button button = buttonObj.AddComponent<Button>();
        button.onClick.AddListener(onClick);

        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(buttonObj.transform, false);
        RectTransform labelRT = labelObj.AddComponent<RectTransform>();
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = labelRT.offsetMax = Vector2.zero;
        var label = labelObj.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = 22;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.black;
        AddShadow(label);
    }

    private void AddShadow(TextMeshProUGUI text)
    {
        if (text == null) return;
        var shadow = text.gameObject.AddComponent<PixelTextShadow>();
        shadow.enableShadow = true;
    }
}
