using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoundManager : MonoBehaviour
{
    public static RoundManager Instance { get; private set; }

    [Header("Round Settings")]
    public int startingRound = 1;
    [Tooltip("Setup phase length: how long the player has to spend gold/place towers before the round auto-starts.")]
    public float timeBetweenRounds = 30f;

    [Header("Point Budget")]
    [Tooltip("Flat point budget added to every round, before scaling")]
    public float baseRoundBudget = 1f;
    [Tooltip("Points added to the budget per round (the 'Difficulty Scaling Constant')")]
    public float difficultyScalingConstant = 5f;

    [Header("Spawn Pacing")]
    [Tooltip("Minimum delay between two enemies spawning within the same round")]
    public float minSpawnDelay = 0.3f;
    [Tooltip("Maximum delay between two enemies spawning within the same round")]
    public float maxSpawnDelay = 1.2f;

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

    public event Action<int> RoundStarted;
    public event Action<int> RoundFinished;
    public event Action<int,int> RoundUpdated;

    [Tooltip("Fires every frame during the setup phase with seconds remaining, for a UI countdown.")]
    public event Action<float> SetupTimerTick;
    [Tooltip("Fires once when the setup countdown reaches zero, so a hard auto-start can trigger (e.g. via RoundContinueUI).")]
    public event Action SetupTimerExpired;

    private bool roundActive = false;
    private bool waitingForPlayerContinue = false;
    private int bonusBudgetNextRound = 0; // extra point budget granted by shop/wheel rewards

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        CurrentRound = startingRound - 1;
        waitingForPlayerContinue = true;
        RoundUpdated?.Invoke(CurrentRound, EnemiesRemaining);
        StartCoroutine(RoundLoop());
    }

    private IEnumerator RoundLoop()
    {
        while (true)
        {
            if (BaseHealth.Instance != null && BaseHealth.Instance.IsDead)
                yield break;

            yield return StartCoroutine(WaitForContinueOrTimeout());
            if (BaseHealth.Instance != null && BaseHealth.Instance.IsDead)
                yield break;

            yield return StartCoroutine(StartRound());

            if (BaseHealth.Instance != null && BaseHealth.Instance.IsDead)
                yield break;

            waitingForPlayerContinue = true;
            RoundUpdated?.Invoke(CurrentRound, EnemiesRemaining);
        }
    }

    // Setup phase: waits for either a manual ContinueToNextRound() call (e.g.
    // the player pressing "Start Round" early) or the countdown hitting zero,
    // at which point SetupTimerExpired fires so a listener (RoundContinueUI)
    // can run the same continue sequence (gate roll, wheel spin, etc.) automatically.
    private IEnumerator WaitForContinueOrTimeout()
    {
        float remaining = timeBetweenRounds;
        bool expiredFired = false;
        SetupTimerTick?.Invoke(remaining);

        while (waitingForPlayerContinue)
        {
            if (BaseHealth.Instance != null && BaseHealth.Instance.IsDead)
                yield break;

            remaining = Mathf.Max(0f, remaining - Time.deltaTime);
            SetupTimerTick?.Invoke(remaining);

            if (remaining <= 0f && !expiredFired)
            {
                expiredFired = true;
                SetupTimerExpired?.Invoke();
            }

            yield return null;
        }
    }

    private IEnumerator StartRound()
    {
        CurrentRound++;
        roundActive = true;
        RoundStarted?.Invoke(CurrentRound);
        Debug.Log($"RoundManager: Starting round {CurrentRound}");

        int roundBudget = Mathf.RoundToInt(baseRoundBudget + CurrentRound * difficultyScalingConstant) + bonusBudgetNextRound;
        bonusBudgetNextRound = 0;
        EnemiesRemaining = 0;
        RoundUpdated?.Invoke(CurrentRound, EnemiesRemaining);

        if (spawner == null)
        {
            Debug.LogWarning("RoundManager: SpawnManager reference not set. No enemies will be spawned.");
        }
        else
        {
            List<EnemySO> wave = spawner.GenerateWave(roundBudget);
            Debug.Log($"RoundManager: Round {CurrentRound} budget={roundBudget}, wave size={wave.Count}");

            foreach (EnemySO enemyData in wave)
            {
                var go = spawner.TrySpawnEnemy(enemyData);
                if (go == null)
                {
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
                Debug.Log($"RoundManager: Spawned {enemyData.enemyName} (round {CurrentRound}) -> EnemiesRemaining={EnemiesRemaining}");

                yield return new WaitForSeconds(UnityEngine.Random.Range(minSpawnDelay, maxSpawnDelay));
            }
        }

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

        while (EnemiesRemaining > 0)
        {
            if (BaseHealth.Instance != null && BaseHealth.Instance.IsDead)
                yield break;

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
        RoundFinished?.Invoke(CurrentRound);
        Debug.Log($"RoundManager: Round {CurrentRound} finished; waitingForPlayerContinue={waitingForPlayerContinue}");
        RoundUpdated?.Invoke(CurrentRound, EnemiesRemaining);
    }

    // Name kept for compatibility with existing shop/wheel reward call sites;
    // "amount" is now extra point budget rather than a raw enemy count.
    public void AddBonusEnemies(int amount)
    {
        bonusBudgetNextRound = Mathf.Max(0, bonusBudgetNextRound + amount);
    }

    public void ContinueToNextRound()
    {
        if (waitingForPlayerContinue)
        {
            waitingForPlayerContinue = false;
            RoundUpdated?.Invoke(CurrentRound, EnemiesRemaining);
        }
    }

    public void OnEnemyKilled(Enemy enemy)
    {
        EnemiesRemaining = Mathf.Max(0, EnemiesRemaining - 1);
        RoundUpdated?.Invoke(CurrentRound, EnemiesRemaining);
        Debug.Log($"RoundManager: OnEnemyKilled -> EnemiesRemaining={EnemiesRemaining}");
    }

    public bool IsRoundActive() => roundActive;
}
