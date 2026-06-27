using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class GameOverUI : MonoBehaviour
{
    [Header("Colors")]
    public Color backdropColor = new Color(0.078f, 0.012f, 0.012f, 0.9f); 
    public Color panelColor = new Color(0.2f, 0.031f, 0.031f, 0.97f);     
    public Color buttonColor = new Color(1f, 0.8431f, 0f, 1f);              
    public Color titleColor = new Color(0.8157f, 0f, 0f, 1f);               

    [Header("Scenes")]
    public string mainMenuSceneName = "MainMenu";

    private BaseHealth baseHealth;
    private TextMeshProUGUI scoreLabel;
    private TextMeshProUGUI highScoreLabel;
    private bool uiBuilt = false;

    private void Awake()
    {
        EnsureCanvasExists();
        if (!uiBuilt) BuildUI();
    }

    private void OnEnable()
    {
        baseHealth = BaseHealth.Instance != null ? BaseHealth.Instance : FindAnyObjectByType<BaseHealth>();
        if (baseHealth != null)
        {
            baseHealth.Died += ShowGameOver;
            Debug.Log($"[GameOverUI] Successfully subscribed to {baseHealth.gameObject.name}'s Died event.");
            
            if (baseHealth.IsDead) 
            {
                Debug.Log("[GameOverUI] Base was already dead on enable. Triggering immediately.");
                ShowGameOver();
                return;
            }
        }
        else
        {
            Debug.LogError("[GameOverUI] CRITICAL: Could not find any BaseHealth script in the scene! UI will never pop up.");
        }

        SetVisualsActive(false);
    }

    private void Start()
    {
        if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
    }

    private void OnDisable()
    {
        if (baseHealth != null) baseHealth.Died -= ShowGameOver;
    }

    private void ShowGameOver()
    {
        Debug.Log("[GameOverUI] ShowGameOver() has been triggered! Popping up UI screen now.");
        SetVisualsActive(true);
        Time.timeScale = 0f;
        MusicManager.Instance?.PlayGameOverMusic();

        if (scoreLabel != null)
        {
            int score = ScoreManager.Instance != null ? ScoreManager.Instance.Score : 0;
            scoreLabel.text = $"Final Score: {score}";
        }

        if (highScoreLabel != null)
            highScoreLabel.text = $"High Score: {ScoreManager.HighScore}";
    }

    private void EnsureCanvasExists()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999; 
            gameObject.AddComponent<CanvasScaler>();
            gameObject.AddComponent<GraphicRaycaster>();
        }
    }

    private void SetVisualsActive(bool active)
    {
        var img = GetComponent<Image>();
        if (img != null) img.enabled = active;

        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(active);
        }
    }

    private void OnRestartClicked()
    {
        Debug.Log("[GameOverUI] Restart Button Clicked! Reloading scene...");
        Time.timeScale = 1f;
        
        // Clear old Singleton states manually before engine reload to prevent scene ghost references
        if (RoundManager.Instance != null)
        {
            RoundManager.Instance.ResetManagerForRestart();
        }
        if (BaseHealth.Instance != null)
        {
            BaseHealth.Instance.ResetForRestart();
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnMainMenuClicked()
    {
        Time.timeScale = 1f;

        // Same "ghost reference" cleanup as Restart above -- without it the
        // round counter carries the just-finished run's number into the next
        // one instead of resetting to 1.
        if (RoundManager.Instance != null)
        {
            RoundManager.Instance.ResetManagerForRestart();
        }
        if (BaseHealth.Instance != null)
        {
            BaseHealth.Instance.ResetForRestart();
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }

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

        GameObject panelObj = new GameObject("Panel");
        panelObj.transform.SetParent(transform, false);
        RectTransform panelRT = panelObj.AddComponent<RectTransform>();
        panelRT.anchorMin = panelRT.anchorMax = panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta = new Vector2(540f, 380f); 
        Image panelImg = panelObj.AddComponent<Image>();
        panelImg.color = panelColor;

        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panelObj.transform, false);
        RectTransform titleRT = titleObj.AddComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0f, 1f); 
        titleRT.anchorMax = new Vector2(1f, 1f);
        titleRT.pivot = new Vector2(0.5f, 1f);
        titleRT.anchoredPosition = new Vector2(0f, -40f); 
        titleRT.sizeDelta = new Vector2(0f, 70f);
        var title = titleObj.AddComponent<TextMeshProUGUI>();
        title.text = "GAME OVER";
        title.fontSize = 52;
        title.alignment = TextAlignmentOptions.Center;
        title.color = titleColor;
        title.fontStyle = FontStyles.Bold;
        AddShadow(title); 

        GameObject scoreObj = new GameObject("Score");
        scoreObj.transform.SetParent(panelObj.transform, false);
        RectTransform scoreRT = scoreObj.AddComponent<RectTransform>();
        scoreRT.anchorMin = new Vector2(0f, 0.5f); 
        scoreRT.anchorMax = new Vector2(1f, 0.5f);
        scoreRT.pivot = new Vector2(0.5f, 0.5f);
        scoreRT.anchoredPosition = new Vector2(0f, 10f); 
        scoreRT.sizeDelta = new Vector2(0f, 50f);
        scoreLabel = scoreObj.AddComponent<TextMeshProUGUI>();
        scoreLabel.fontSize = 28;
        scoreLabel.alignment = TextAlignmentOptions.Center;
        scoreLabel.color = Color.white;
        AddShadow(scoreLabel); 

        GameObject highScoreObj = new GameObject("HighScore");
        highScoreObj.transform.SetParent(panelObj.transform, false);
        RectTransform highScoreRT = highScoreObj.AddComponent<RectTransform>();
        highScoreRT.anchorMin = new Vector2(0f, 0.5f);
        highScoreRT.anchorMax = new Vector2(1f, 0.5f);
        highScoreRT.pivot = new Vector2(0.5f, 0.5f);
        highScoreRT.anchoredPosition = new Vector2(0f, -30f);
        highScoreRT.sizeDelta = new Vector2(0f, 30f);
        highScoreLabel = highScoreObj.AddComponent<TextMeshProUGUI>();
        highScoreLabel.fontSize = 20;
        highScoreLabel.alignment = TextAlignmentOptions.Center;
        highScoreLabel.color = buttonColor;
        AddShadow(highScoreLabel);

        // FIX: Rebuilt buttons explicitly passing references down seamlessly
        BuildButton(panelObj.transform, "RestartButton", "Restart", new Vector2(-130f, 50f), OnRestartClicked);
        BuildButton(panelObj.transform, "MainMenuButton", "Main Menu", new Vector2(130f, 50f), OnMainMenuClicked);
        
        uiBuilt = true;
    }

    private void BuildButton(Transform parent, string name, string text, Vector2 anchoredPosition, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(parent, false);
        RectTransform buttonRT = buttonObj.AddComponent<RectTransform>();
        buttonRT.anchorMin = buttonRT.anchorMax = buttonRT.pivot = new Vector2(0.5f, 0f);
        buttonRT.anchoredPosition = anchoredPosition;
        buttonRT.sizeDelta = new Vector2(220f, 70f);
        Image buttonImg = buttonObj.AddComponent<Image>();
        buttonImg.color = buttonColor;
        
        Button button = buttonObj.AddComponent<Button>();
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(onClick);

        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(buttonObj.transform, false);
        RectTransform labelRT = labelObj.AddComponent<RectTransform>();
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = labelRT.offsetMax = Vector2.zero;
        var label = labelObj.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = 24;
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