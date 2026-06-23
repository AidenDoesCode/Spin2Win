using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class RoundContinueUI : MonoBehaviour
{
    [Tooltip("Panel or button GameObject to show/hide when the round has ended")]
    public GameObject panel;

    [Tooltip("If assigned, pressing Continue rolls the gate wheel and waits for it to finish spinning before the round actually starts.")]
    public GateRouletteManager gateRouletteManager;
    public GateRouletteUI gateRouletteUI;

    [Tooltip("Minimum seconds to wait after the wheel finishes spinning before enemies start spawning")]
    public float minDelayAfterSpin = 1f;

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

        if (gateRouletteManager == null) gateRouletteManager = FindAnyObjectByType<GateRouletteManager>();
        if (gateRouletteUI == null) gateRouletteUI = FindAnyObjectByType<GateRouletteUI>();

        if (subscribedRoundManager != null)
        {
            subscribedRoundManager.RoundUpdated += OnRoundUpdated;
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

        if (gateRouletteUI != null)
            gateRouletteUI.SpinCompleted -= HandleSpinCompleted;
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
        if (continueButton != null)
            continueButton.interactable = false;
        if (panel != null)
            panel.SetActive(false);

        if (gateRouletteManager == null)
        {
            subscribedRoundManager?.ContinueToNextRound();
            return;
        }

        if (gateRouletteUI != null)
        {
            gateRouletteUI.SpinCompleted += HandleSpinCompleted;
            gateRouletteManager.RollGate();
        }
        else
        {
            gateRouletteManager.RollGate();
            subscribedRoundManager?.ContinueToNextRound();
        }
    }

    private void HandleSpinCompleted()
    {
        if (gateRouletteUI != null)
            gateRouletteUI.SpinCompleted -= HandleSpinCompleted;

        StartCoroutine(ContinueAfterDelay());
    }

    private IEnumerator ContinueAfterDelay()
    {
        if (minDelayAfterSpin > 0f)
            yield return new WaitForSeconds(minDelayAfterSpin);

        subscribedRoundManager?.ContinueToNextRound();
    }
}
