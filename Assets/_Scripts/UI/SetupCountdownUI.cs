using UnityEngine;
using TMPro;

// Attach to a TextMeshPro - Text (UI) object anywhere on screen. Shows the
// setup-phase countdown ticking down from RoundManager.timeBetweenRounds,
// and hides once the round actually starts.
public class SetupCountdownUI : MonoBehaviour
{
    public string format = "Next round in {0}s";

    private TextMeshProUGUI label;
    private RoundManager roundManager;

    private void Awake()
    {
        label = GetComponent<TextMeshProUGUI>();
        if (label == null) label = gameObject.AddComponent<TextMeshProUGUI>();
    }

    private void Start()
    {
        roundManager = RoundManager.Instance != null ? RoundManager.Instance : FindAnyObjectByType<RoundManager>();
        if (roundManager != null)
        {
            roundManager.SetupTimerTick += OnTick;
            roundManager.RoundStarted += OnRoundStarted;
        }

        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (roundManager != null)
        {
            roundManager.SetupTimerTick -= OnTick;
            roundManager.RoundStarted -= OnRoundStarted;
        }
    }

    private void OnTick(float remaining)
    {
        gameObject.SetActive(true);
        label.text = string.Format(format, Mathf.CeilToInt(remaining));
    }

    private void OnRoundStarted(int round) => gameObject.SetActive(false);
}
