using System;
using System.Collections;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    public enum Phase { Player, Allies, Enemies, EndOfTurn }

    public Phase CurrentPhase { get; private set; } = Phase.Player;

    public event Action<Phase> PhaseChanged;

    [Header("Phase Durations (seconds)")]
    [Tooltip("Duration of the Allies phase. Player phase is ended by player action.")]
    public float alliesPhaseDuration = 0.6f;
    [Tooltip("Duration of the Enemies phase when enemies are allowed to act")]
    public float enemiesPhaseDuration = 1.5f;

    private Coroutine turnRoutine;

    [Header("Startup")]
    [Tooltip("If true, immediately advance one turn sequence on Start so enemies act right away (useful for testing)")]
    public bool autoAdvanceOnStart = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // start in Player phase waiting for player input
        SetPhase(Phase.Player);
        if (autoAdvanceOnStart)
        {
            // automatically progress one turn so enemies will act immediately
            EndPlayerTurn();
        }
    }

    private void SetPhase(Phase p)
    {
        CurrentPhase = p;
        PhaseChanged?.Invoke(p);
    }

    // Called by player UI when they finish their actions for the turn
    public void EndPlayerTurn()
    {
        if (turnRoutine != null) StopCoroutine(turnRoutine);
        turnRoutine = StartCoroutine(TurnSequence());
    }

    private IEnumerator TurnSequence()
    {
        // Allies phase
        SetPhase(Phase.Allies);
        yield return new WaitForSeconds(alliesPhaseDuration);

        // Enemies phase
        SetPhase(Phase.Enemies);
        yield return new WaitForSeconds(enemiesPhaseDuration);

        // End of turn, allow reward/cleanup
        SetPhase(Phase.EndOfTurn);

        // Return to player phase
        SetPhase(Phase.Player);
        turnRoutine = null;
    }
}
