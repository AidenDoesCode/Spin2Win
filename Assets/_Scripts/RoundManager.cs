using System;
using System.Collections;
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

    public event Action<int> RoundStarted;
    public event Action<int> RoundFinished;
    public event Action<int,int> RoundUpdated;

    private bool roundActive = false;
    private bool waitingForPlayerContinue = false;
    private int bonusEnemiesNextRound = 0;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        CurrentRound = startingRound - 1;
        StartCoroutine(RoundLoop());
    }

    private IEnumerator RoundLoop()
    {
        while (true)
        {
            if (BaseHealth.Instance != null && BaseHealth.Instance.IsDead)
                yield break;

            yield return StartCoroutine(StartRound());

            if (BaseHealth.Instance != null && BaseHealth.Instance.IsDead)
                yield break;

            waitingForPlayerContinue = true;
            while (waitingForPlayerContinue)
            {
                if (BaseHealth.Instance != null && BaseHealth.Instance.IsDead)
                    yield break;

                yield return null;
            }
            yield return new WaitForSeconds(timeBetweenRounds);
        }
    }

    private IEnumerator StartRound()
    {
        CurrentRound++;
        roundActive = true;
        RoundStarted?.Invoke(CurrentRound);
        Debug.Log($"RoundManager: Starting round {CurrentRound}");

        int toSpawn = Mathf.CeilToInt(enemiesBasePerRound + (CurrentRound - 1) * 1.5f) + bonusEnemiesNextRound;
        bonusEnemiesNextRound = 0;
        EnemiesRemaining = 0;
        RoundUpdated?.Invoke(CurrentRound, EnemiesRemaining);

        if (spawner == null)
        {
            Debug.LogWarning("RoundManager: SpawnManager reference not set. No enemies will be spawned.");
        }
        else
        {
            for (int i = 0; i < toSpawn; i++)
            {
                var go = spawner.TrySpawnRandom();
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
                Debug.Log($"RoundManager: Spawned enemy (round {CurrentRound}) -> EnemiesRemaining={EnemiesRemaining}");

                yield return new WaitForSeconds(0.1f);
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

    public void AddBonusEnemies(int amount)
    {
        bonusEnemiesNextRound = Mathf.Max(0, bonusEnemiesNextRound + amount);
    }

    public void ContinueToNextRound()
    {
        if (waitingForPlayerContinue)
        {
            waitingForPlayerContinue = false;
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
