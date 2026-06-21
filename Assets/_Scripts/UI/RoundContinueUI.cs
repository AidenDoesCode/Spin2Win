using UnityEngine;
using UnityEngine.UI;

public class RoundContinueUI : MonoBehaviour
{
    [Tooltip("Panel or button GameObject to show/hide when the round has ended")]
    public GameObject panel;

    private Button continueButton;
    private RoundManager subscribedRoundManager;

    void Awake()
    {
        if (panel == null)
            panel = gameObject;

        if (panel != null)
        {
            continueButton = panel.GetComponent<Button>();
            panel.SetActive(false);
        }
    }

    void Start()
    {
        subscribedRoundManager = RoundManager.Instance;
        if (subscribedRoundManager == null)
        {
            subscribedRoundManager = FindAnyObjectByType<RoundManager>();
        }

        if (subscribedRoundManager != null)
        {
            subscribedRoundManager.RoundUpdated += OnRoundUpdated;
            Debug.Log("RoundContinueUI: Subscribed to RoundManager.RoundUpdated");
            OnRoundUpdated(subscribedRoundManager.CurrentRound, subscribedRoundManager.EnemiesRemaining);
        }
        else
        {
            Debug.LogWarning("RoundContinueUI: No RoundManager found to subscribe to RoundUpdated.");
        }

        if (continueButton != null)
            continueButton.onClick.AddListener(OnContinueClicked);
    }

    void OnDestroy()
    {
        if (subscribedRoundManager != null)
            subscribedRoundManager.RoundUpdated -= OnRoundUpdated;

        if (continueButton != null)
            continueButton.onClick.RemoveListener(OnContinueClicked);
    }

    private void OnRoundUpdated(int round, int enemiesRemaining)
    {
        bool roundFinished = enemiesRemaining == 0 && subscribedRoundManager != null && !subscribedRoundManager.IsRoundActive();
        if (panel != null)
        {
            panel.SetActive(roundFinished);
            if (continueButton != null)
                continueButton.interactable = roundFinished;
        }
    }

    private void OnContinueClicked()
    {
        subscribedRoundManager?.ContinueToNextRound();
        if (panel != null)
            panel.SetActive(false);
    }
}
