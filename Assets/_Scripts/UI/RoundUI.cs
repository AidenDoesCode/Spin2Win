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

    // Change Start to OnEnable in RoundUI to ensure subscriptions happen early,
// but handle the initial UI refresh *after* the manager initializes.
void Start()
{
    if (roundManager == null)
        roundManager = RoundManager.Instance;

    if (roundManager != null)
    {
        roundManager.RoundUpdated += OnRoundUpdated;
        
        // Use Invoke to wait one frame, letting RoundManager reset its numbers first
        Invoke(nameof(RefreshUI), 0.01f); 
    }
}

void RefreshUI()
{
    if (roundManager != null)
    {
        text.text = FormatText(roundManager.CurrentRound, roundManager.EnemiesRemaining);
    }
}

    void OnDestroy()
    {
        // CRITICAL FIX: Explicitly unsubscribe using the underlying system object 
        // to bypass Unity's custom null check during scene destruction
        if (!ReferenceEquals(roundManager, null))
        {
            roundManager.RoundUpdated -= OnRoundUpdated;
        }
    }

    private void OnRoundUpdated(int currentRound, int enemiesRemaining)
{
    text.text = FormatText(currentRound, enemiesRemaining);

    // Tell the aligner on our parent (or object) to recalculate immediately
    HUDAligner aligner = GetComponentInParent<HUDAligner>();
    if (aligner != null)
    {
        aligner.AlignOnce();
    }
}

    private string FormatText(int round, int remaining)
{
    int pd = Mathf.Max(1, paddingDigits);
    
    // Change '0' back to ' ' (spaces) so it pads cleanly without extra zeros
    string r = Mathf.Max(1, round).ToString().PadLeft(pd, ' ');
    string e = remaining.ToString().PadLeft(pd, ' ');
    
    return $"Round {r}  -  Enemies: {e}";
}
}