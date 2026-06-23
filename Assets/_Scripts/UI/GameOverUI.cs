using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

// Self-building full-screen overlay shown when BaseHealth.Died fires.
// Attach to an empty GameObject anywhere in the scene -- no manual UI setup needed.
public class GameOverUI : MonoBehaviour
{
    [Header("Colors")]
    public Color backdropColor = new Color(0f, 0f, 0f, 0.85f);
    public Color panelColor = new Color(0.102f, 0.102f, 0.180f, 0.97f);
    public Color buttonColor = new Color(0.878f, 0.753f, 0.376f, 1f);

    [Header("Scenes")]
    [Tooltip("Build-settings scene name to load for the Main Menu button")]
    public string mainMenuSceneName = "MainMenu";

    private BaseHealth baseHealth;
    private TextMeshProUGUI scoreLabel;

    private void Awake()
    {
        BuildUI();
        gameObject.SetActive(false);
    }

    private void Start()
    {
        baseHealth = BaseHealth.Instance != null ? BaseHealth.Instance : FindAnyObjectByType<BaseHealth>();
        if (baseHealth != null)
        {
            baseHealth.Died += ShowGameOver;
            if (baseHealth.IsDead) ShowGameOver(); // base was already dead when this loaded
        }
        else
        {
            Debug.LogWarning("GameOverUI: No BaseHealth found to listen for.");
        }
    }

    private void OnDestroy()
    {
        if (baseHealth != null) baseHealth.Died -= ShowGameOver;
    }

    private void ShowGameOver()
    {
        gameObject.SetActive(true);
        Time.timeScale = 0f;

        if (scoreLabel != null)
        {
            int score = ScoreManager.Instance != null ? ScoreManager.Instance.Score : 0;
            scoreLabel.text = $"Final Gold: {score}";
        }
    }

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

    private void BuildUI()
    {
        RectTransform rootRT = GetComponent<RectTransform>();
        if (rootRT == null) rootRT = gameObject.AddComponent<RectTransform>();
        rootRT.anchorMin = Vector2.zero;
        rootRT.anchorMax = Vector2.one;
        rootRT.offsetMin = Vector2.zero;
        rootRT.offsetMax = Vector2.zero;

        Image backdrop = GetComponent<Image>();
        if (backdrop == null) backdrop = gameObject.AddComponent<Image>();
        backdrop.color = backdropColor;

        GameObject panelObj = new GameObject("Panel");
        panelObj.transform.SetParent(transform, false);
        RectTransform panelRT = panelObj.AddComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta = new Vector2(520f, 360f);
        Image panelImg = panelObj.AddComponent<Image>();
        panelImg.color = panelColor;

        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panelObj.transform, false);
        RectTransform titleRT = titleObj.AddComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0f, 1f);
        titleRT.anchorMax = new Vector2(1f, 1f);
        titleRT.pivot = new Vector2(0.5f, 1f);
        titleRT.anchoredPosition = new Vector2(0f, -50f);
        titleRT.sizeDelta = new Vector2(0f, 80f);
        var title = titleObj.AddComponent<TextMeshProUGUI>();
        title.text = "GAME OVER";
        title.fontSize = 50;
        title.alignment = TextAlignmentOptions.Center;
        title.color = Color.white;

        GameObject scoreObj = new GameObject("Score");
        scoreObj.transform.SetParent(panelObj.transform, false);
        RectTransform scoreRT = scoreObj.AddComponent<RectTransform>();
        scoreRT.anchorMin = new Vector2(0f, 0.5f);
        scoreRT.anchorMax = new Vector2(1f, 0.5f);
        scoreRT.pivot = new Vector2(0.5f, 0.5f);
        scoreRT.sizeDelta = new Vector2(0f, 50f);
        scoreLabel = scoreObj.AddComponent<TextMeshProUGUI>();
        scoreLabel.fontSize = 30;
        scoreLabel.alignment = TextAlignmentOptions.Center;
        scoreLabel.color = Color.white;

        BuildButton(panelObj.transform, "RestartButton", "Restart", new Vector2(-140f, 60f), OnRestartClicked);
        BuildButton(panelObj.transform, "MainMenuButton", "Main Menu", new Vector2(140f, 60f), OnMainMenuClicked);
    }

    private void BuildButton(Transform parent, string name, string text, Vector2 anchoredPosition, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(parent, false);
        RectTransform buttonRT = buttonObj.AddComponent<RectTransform>();
        buttonRT.anchorMin = new Vector2(0.5f, 0f);
        buttonRT.anchorMax = new Vector2(0.5f, 0f);
        buttonRT.pivot = new Vector2(0.5f, 0f);
        buttonRT.anchoredPosition = anchoredPosition;
        buttonRT.sizeDelta = new Vector2(220f, 70f);
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
        label.fontSize = 24;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.black;
    }
}
