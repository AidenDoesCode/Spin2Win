using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_Text))]
public class RoundUI : MonoBehaviour
{
    private TMP_Text text;

    public RoundManager roundManager;
    [Tooltip("Number of digits to pad round and enemy counts to (e.g. 3 -> 001)")]
    public int paddingDigits = 3;

    void Awake()
    {
        text = GetComponent<TMP_Text>();
    }

    void OnEnable()
    {
        // Subscribing in OnEnable ensures this script hooks into the event 
        // BEFORE RoundManager fires its initial setup event in Start().
        if (roundManager == null)
            roundManager = RoundManager.Instance;

        if (roundManager != null)
        {
            roundManager.RoundUpdated += OnRoundUpdated;
        }
    }

    void Start()
    {
        // Double check instance assignment if it wasn't ready during OnEnable
        if (roundManager == null)
        {
            roundManager = RoundManager.Instance;
            if (roundManager != null)
            {
                roundManager.RoundUpdated -= OnRoundUpdated; // Avoid double subscription
                roundManager.RoundUpdated += OnRoundUpdated;
            }
        }

        // Force a direct check on frame one to catch any missed initial states
        RefreshUI();
    }

    void OnDisable()
    {
        // Safe unsubscription during scene destruction or disabling
        if (!ReferenceEquals(roundManager, null))
        {
            roundManager.RoundUpdated -= OnRoundUpdated;
        }
    }

    void RefreshUI()
    {
        if (roundManager != null)
        {
            text.text = FormatText(roundManager.CurrentRound, roundManager.EnemiesRemaining);
            ForceAlignerUpdate();
        }
    }

    private void OnRoundUpdated(int currentRound, int enemiesRemaining)
    {
        text.text = FormatText(currentRound, enemiesRemaining);
        ForceAlignerUpdate();
    }

    private void ForceAlignerUpdate()
    {
        HUDAligner aligner = GetComponentInParent<HUDAligner>();
        if (aligner != null)
        {
            aligner.AlignOnce();
        }
    }

    private string FormatText(int round, int remaining)
    {
        int pd = Mathf.Max(1, paddingDigits);
        
        // If round is 0 (pre-round state), display it as 1 so the player sees "Round 1" instantly on restart
        int displayRound = Mathf.Max(1, round);
        
        string r = displayRound.ToString().PadLeft(pd, ' ');
        string e = remaining.ToString().PadLeft(pd, ' ');
        
        return $"Round {r}  -  Enemies: {e}";
    }
}