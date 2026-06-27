using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuUI : MonoBehaviour
{
    [Header("Scenes")]
    [Tooltip("Build-settings scene name to load when Play is pressed")]
    public string gameplaySceneName = "Arena";

    [Header("Text")]
    public string gameTitle = "SPIN2WIN";
    public string subtitle = "Spin the shop. Place your towers. Survive the tide.";

    [Header("Colors")]
    public Color backdropColor = new Color(0.078f, 0.012f, 0.012f, 1f); 
    public Color buttonColor   = new Color(1f, 0.8431f, 0f, 1f);          
    public Color titleColor    = new Color(1f, 0.8431f, 0f, 1f);          

    private GameObject createdCanvasObj;
    private GameObject settingsPanelObj;

    private void Awake()
    {
        // --- 1. UNFREEZE THE GAME ENGINE ---
        // Crucial: Fixes the freeze carried over from the Game Over screen's Time.timeScale = 0
        Time.timeScale = 1f;

        #pragma warning disable 0618
        // 2. Clear any lingering duplicate UI layers from previous gameplay
        Canvas[] existingCanvases = Object.FindObjectsOfType<Canvas>();
        foreach (Canvas c in existingCanvases)
        {
            if (c.gameObject != gameObject && c.name != "MainMenuCanvas")
            {
                Destroy(c.gameObject);
            }
        }

        // 3. Clear out broken or duplicate event systems
        EventSystem[] existingSystems = Object.FindObjectsOfType<EventSystem>();
        foreach (EventSystem es in existingSystems)
        {
            Destroy(es.gameObject);
        }
        #pragma warning restore 0618

        // 4. Spin up a clean input architecture and dedicated canvas
        EnsureEventSystem();
        CreateDedicatedCanvas();
    }

    private void Start()
    {
        MusicManager.Instance?.PlayMainMenuMusic();
    }

    private void EnsureEventSystem()
    {
        GameObject esObj = new GameObject("EventSystem");
        EventSystem.current = esObj.AddComponent<EventSystem>();

        // Universally compatible Input handling setup
        // Tries to use the New Input system module; falls back seamlessly to Classic if needed
        System.Type inputModuleType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (inputModuleType != null)
        {
            esObj.AddComponent(inputModuleType);
        }
        else
        {
            esObj.AddComponent<StandaloneInputModule>();
        }
    }

    private void CreateDedicatedCanvas()
    {
        createdCanvasObj = new GameObject("MainMenuCanvas");
        Canvas canvas = createdCanvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999; // Ensures it draws on top of everything else

        CanvasScaler scaler = createdCanvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        createdCanvasObj.AddComponent<GraphicRaycaster>();
        
        BuildUI(createdCanvasObj.transform);
    }

    private void BuildUI(Transform canvasTransform)
    {
        GameObject backdropObj = new GameObject("Backdrop");
        backdropObj.transform.SetParent(canvasTransform, false);
        RectTransform backdropRT = backdropObj.AddComponent<RectTransform>();
        backdropRT.anchorMin = Vector2.zero;
        backdropRT.anchorMax = Vector2.one;
        backdropRT.offsetMin = backdropRT.offsetMax = Vector2.zero;
        Image backdrop = backdropObj.AddComponent<Image>();
        backdrop.color = backdropColor;

        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(backdropObj.transform, false);
        RectTransform titleRT = titleObj.AddComponent<RectTransform>();
        titleRT.anchorMin = titleRT.anchorMax = new Vector2(0.5f, 0.68f);
        titleRT.pivot = new Vector2(0.5f, 0.5f);
        titleRT.anchoredPosition = Vector2.zero;
        titleRT.sizeDelta = new Vector2(900f, 160f);
        var title = titleObj.AddComponent<TextMeshProUGUI>();
        title.text = gameTitle;
        title.fontSize = 90;
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Center;
        title.color = titleColor;

        GameObject subtitleObj = new GameObject("Subtitle");
        subtitleObj.transform.SetParent(backdropObj.transform, false);
        RectTransform subtitleRT = subtitleObj.AddComponent<RectTransform>();
        subtitleRT.anchorMin = subtitleRT.anchorMax = new Vector2(0.5f, 0.58f);
        subtitleRT.pivot = new Vector2(0.5f, 0.5f);
        subtitleRT.anchoredPosition = Vector2.zero;
        subtitleRT.sizeDelta = new Vector2(900f, 60f);
        var subtitleLabel = subtitleObj.AddComponent<TextMeshProUGUI>();
        subtitleLabel.text = subtitle;
        subtitleLabel.fontSize = 28;
        subtitleLabel.alignment = TextAlignmentOptions.Center;
        subtitleLabel.color = Color.white;

        GameObject highScoreObj = new GameObject("HighScoreLabel");
        highScoreObj.transform.SetParent(backdropObj.transform, false);
        RectTransform highScoreRT = highScoreObj.AddComponent<RectTransform>();
        highScoreRT.anchorMin = highScoreRT.anchorMax = new Vector2(0.5f, 0.49f);
        highScoreRT.pivot = new Vector2(0.5f, 0.5f);
        highScoreRT.anchoredPosition = Vector2.zero;
        highScoreRT.sizeDelta = new Vector2(900f, 40f);
        var highScoreLabel = highScoreObj.AddComponent<TextMeshProUGUI>();
        highScoreLabel.text = $"High Score: {ScoreManager.HighScore}";
        highScoreLabel.fontSize = 24;
        highScoreLabel.alignment = TextAlignmentOptions.Center;
        highScoreLabel.color = titleColor;

        BuildButton(backdropObj.transform, "PlayButton", "PLAY", new Vector2(0.5f, 0.40f), OnPlayClicked);
        BuildButton(backdropObj.transform, "SettingsButton", "SETTINGS", new Vector2(0.5f, 0.28f), OnSettingsClicked);
        BuildButton(backdropObj.transform, "QuitButton", "QUIT", new Vector2(0.5f, 0.16f), OnQuitClicked);

        settingsPanelObj = SettingsSliderUI.BuildVolumePanel(backdropObj.transform, new Vector2(0f, 40f), buttonColor);
        settingsPanelObj.SetActive(false);
    }

    private void OnSettingsClicked() => settingsPanelObj.SetActive(!settingsPanelObj.activeSelf);

    private void BuildButton(Transform parent, string name, string text, Vector2 anchor, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(parent, false);
        RectTransform buttonRT = buttonObj.AddComponent<RectTransform>();
        buttonRT.anchorMin = buttonRT.anchorMax = buttonRT.pivot = anchor;
        buttonRT.anchoredPosition = Vector2.zero;
        buttonRT.sizeDelta = new Vector2(300f, 80f);
        
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
        label.fontSize = 32;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.black;
    }

    private void OnPlayClicked()
    {
        if (createdCanvasObj != null)
        {
            Destroy(createdCanvasObj);
        }

        SceneManager.LoadScene(gameplaySceneName);
    }

    private void OnQuitClicked()
    {
        Debug.Log("Quit application triggered successfully.");
        Application.Quit();
    }
}