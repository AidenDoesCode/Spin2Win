using UnityEngine;
using UnityEngine.UI;

public class RoundContinueUI : MonoBehaviour
{
    [Tooltip("Panel or button GameObject to show/hide when the round has ended")]
    public GameObject panel;

    private Button continueButton;

    void Awake()
    {
        // If no panel assigned, default to this GameObject
        if (panel == null)
            panel = this.gameObject;

        // If the panel is specified, try to find the Button component on it
        if (panel != null)
        {
            continueButton = panel.GetComponent<Button>();
            panel.SetActive(false);
        }
    }

    void Start()
    {
        // Subscribe to RoundManager, try Instance first, otherwise find in scene
        if (RoundManager.Instance != null)
        {
            RoundManager.Instance.RoundUpdated += OnRoundUpdated;
            Debug.Log("RoundContinueUI: Subscribed to RoundManager.Instance.RoundUpdated");
        }
        else
        {
            var rm = FindAnyObjectByType<RoundManager>();
            if (rm != null)
            {
                rm.RoundUpdated += OnRoundUpdated;
                Debug.Log("RoundContinueUI: Subscribed to RoundManager found in scene.");
            }
            else
            {
                Debug.LogWarning("RoundContinueUI: No RoundManager found to subscribe to RoundUpdated.");
            }
        }

        if (continueButton != null)
            continueButton.onClick.AddListener(OnContinueClicked);
    }

    void OnDestroy()
    {
        if (RoundManager.Instance != null)
            RoundManager.Instance.RoundUpdated -= OnRoundUpdated;

        if (continueButton != null)
            continueButton.onClick.RemoveListener(OnContinueClicked);
    }

    private void OnRoundUpdated(int round, int enemiesRemaining)
    {
        bool roundFinished = (enemiesRemaining == 0) && (RoundManager.Instance != null && !RoundManager.Instance.IsRoundActive());
        if (panel != null)
        {
            panel.SetActive(roundFinished);
            if (continueButton != null)
                continueButton.interactable = roundFinished;
        }
    }

    private void OnContinueClicked()
    {
        RoundManager.Instance?.ContinueToNextRound();
        if (panel != null)
            panel.SetActive(false);
    }
}
