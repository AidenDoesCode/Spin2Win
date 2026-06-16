using System;
using System.Collections;
using Unity.Burst.CompilerServices;
using UnityEngine;

public class RoundManager : MonoBehaviour
{
    public static RoundManager Instance { get; private set; }

    [Header("Round Settings")]
    public int startingRound = 1;
    public float timeBetweenRounds = 3f;
    public int enemiesBasePerRound = 3;

    [Header("Difficulty Scaling")]
    [Tooltip("Multiplier applied to enemy max health per round (exponential)")]
    public float healthMultiplierPerRound = 1.2f;
    [Tooltip("Multiplier applied to enemy speed per round (linear)")]
    public float speedMultiplierPerRound = 0.05f;
    [Tooltip("Multiplier applied to enemy damage per round (linear)")]
    public float damageMultiplierPerRound = 0.05f;

    [Header("References")]
    public SpawnManager spawner;
    [Tooltip("If true, automatically end the player turn once a round's enemies are spawned so enemies will act immediately")]
    public bool autoStartEnemyPhaseAfterSpawn = true;

    public int CurrentRound { get; private set; }
    public int EnemiesRemaining { get; private set; }

    // Event: (currentRound, enemiesRemaining)
    public event Action<int,int> RoundUpdated;

    private bool roundActive = false;
    // When a round completes, wait for player confirmation to continue
    private bool waitingForPlayerContinue = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        CurrentRound = startingRound - 1; // StartRound will increment
        StartCoroutine(RoundLoop());
    }

    // Main loop: start a round, wait for it to finish, then wait before starting next
    private IEnumerator RoundLoop()
    {
        while (true)
        {
            yield return StartCoroutine(StartRound());
            // round finished - wait for player to continue
            waitingForPlayerContinue = true;
            // Optionally show UI here via event
            while (waitingForPlayerContinue)
            {
                yield return null;
            }
            // small delay before starting next
            yield return new WaitForSeconds(timeBetweenRounds);
        }
    }

    private IEnumerator StartRound()
    {
        CurrentRound++;
        RoundUpdated?.Invoke(CurrentRound, EnemiesRemaining);
        roundActive = true;
        Debug.Log($"RoundManager: Starting round {CurrentRound}");

        int toSpawn = Mathf.CeilToInt(enemiesBasePerRound + (CurrentRound - 1) * 1.5f);
        EnemiesRemaining = 0;
        RoundUpdated?.Invoke(CurrentRound, EnemiesRemaining);

        if (spawner == null)
        {
            Debug.LogWarning("RoundManager: SpawnManager reference not set. No enemies will be spawned.");
            yield break;
        }

        // spawn enemies with scaling
        for (int i = 0; i < toSpawn; i++)
        {
            var go = spawner.TrySpawnRandom();
            if (go == null)
            {
                // failed to spawn this one; skip
                yield return new WaitForSeconds(0.05f);
                continue;
            }

            var enemy = go.GetComponent<Enemy>();
            if (enemy != null)
            {
                float roundHealthMult = Mathf.Pow(healthMultiplierPerRound, CurrentRound - 1);
                enemy.healthMultiplier = roundHealthMult;
                enemy.speedMultiplier = 1f + speedMultiplierPerRound * (CurrentRound - 1);
                enemy.damageMultiplier = 1f + damageMultiplierPerRound * (CurrentRound - 1);
            }

            EnemiesRemaining++;
            RoundUpdated?.Invoke(CurrentRound, EnemiesRemaining);
            Debug.Log($"RoundManager: Spawned enemy (round {CurrentRound}) -> EnemiesRemaining={EnemiesRemaining}");

            // small stagger between spawns so they don't all overlap
            yield return new WaitForSeconds(0.1f);
        }

        // Optionally auto-start the enemies' phase by ending the player turn so enemies will act immediately
        if (autoStartEnemyPhaseAfterSpawn)
        {
            if (TurnManager.Instance != null)
            {
                Debug.Log("RoundManager: Auto-starting enemy phase by ending player turn.");
                TurnManager.Instance.EndPlayerTurn();
            }
            else
            {
                Debug.LogWarning("RoundManager: autoStartEnemyPhaseAfterSpawn is true but no TurnManager found.");
            }
        }

        // Wait until enemies are cleared.
        // Also periodically reconcile with actual Enemy objects in the scene in case notifications were missed.
        while (EnemiesRemaining > 0)
        {
            // every 0.2s check actual enemies
            // Use the newer API overload without deprecated sort mode parameter
            int actual = UnityEngine.Object.FindObjectsByType<Enemy>().Length;
            if (actual != EnemiesRemaining)
            {
                EnemiesRemaining = actual;
                RoundUpdated?.Invoke(CurrentRound, EnemiesRemaining);
                Debug.Log($"RoundManager: Reconciled EnemiesRemaining -> {EnemiesRemaining}");
            }

            if (EnemiesRemaining <= 0) break;
            yield return new WaitForSeconds(0.2f);
        }

        roundActive = false;
        Debug.Log($"RoundManager: Round {CurrentRound} finished; waitingForPlayerContinue={waitingForPlayerContinue}");
        RoundUpdated?.Invoke(CurrentRound, EnemiesRemaining);
        yield break;
    }

    // Called by UI when player wants to start the next round
    public void ContinueToNextRound()
    {
        if (waitingForPlayerContinue)
        {
            waitingForPlayerContinue = false;
        }
    }

    // Called by Enemy when it dies
    public void OnEnemyKilled(Enemy enemy)
    {
        EnemiesRemaining = Mathf.Max(0, EnemiesRemaining - 1);
        RoundUpdated?.Invoke(CurrentRound, EnemiesRemaining);
        Debug.Log($"RoundManager: OnEnemyKilled -> EnemiesRemaining={EnemiesRemaining}");
    }

    // Query whether a round is in progress
    public bool IsRoundActive() => roundActive;
}
