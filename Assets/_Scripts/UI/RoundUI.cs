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

    void Start()
    {
        if (roundManager == null && RoundManager.Instance != null)
            roundManager = RoundManager.Instance;

        if (roundManager != null)
        {
            roundManager.RoundUpdated += OnRoundUpdated;
            text.text = FormatText(roundManager.CurrentRound, roundManager.EnemiesRemaining);
        }
        else
        {
            text.text = "Round: -";
        }
    }

    void OnDestroy()
    {
        if (roundManager != null)
            roundManager.RoundUpdated -= OnRoundUpdated;
    }

    private void OnRoundUpdated(int currentRound, int enemiesRemaining)
    {
        text.text = FormatText(currentRound, enemiesRemaining);
    }

    private string FormatText(int round, int remaining)
    {
        int pd = Mathf.Max(1, paddingDigits);
        string r = round.ToString().PadLeft(pd, ' ');
        string e = remaining.ToString().PadLeft(pd, ' ');
        return $"Round {r}  -  Enemies: {e}";
    }
}
