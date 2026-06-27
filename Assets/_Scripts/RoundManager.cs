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

    [Header("Audio")]
    [Tooltip("Little stinger played the instant a round actually starts (enemies about to spawn).")]
    public AudioClip roundStartSound;
    [Range(0f, 3f)] public float roundStartVolume = 1f;
    private AudioSource audioSource;

    private bool roundActive = false;
    private bool waitingForPlayerContinue = false;
    private bool timerPaused = false;
    private int bonusBudgetNextRound = 0;

    // Lets a UI element (e.g. the card detail popup) hold the buy-phase
    // countdown steady while it's open, instead of it ticking down unseen.
    public void SetTimerPaused(bool paused) => timerPaused = paused;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        // --- BULLETPROOF VISUAL RESET FIX FOR MAIN MENU GHOSTING ---
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            // Standard canvas layer fallback reset
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = Color.black;

            // Deep URP buffer flush (handles template version conflicts seamlessly)
            Component additionalData = mainCam.GetComponent("UniversalAdditionalCameraData");
            if (additionalData != null)
            {
                try
                {
                    var clearFlagsField = additionalData.GetType().GetProperty("backgroundClearFlags");
                    if (clearFlagsField != null)
                    {
                        clearFlagsField.SetValue(additionalData, CameraClearFlags.Color);
                        Debug.Log("[CameraFix] URP Background buffer wiped clean successfully.");
                    }
                }
                catch (Exception)
                {
                    // Fallback block if pipeline namespaces shift inside older editor configurations
                }
            }
        }

        // --- EXISTING GAME ENGINE INITIALIZATION ---
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

private IEnumerator WaitForContinueOrTimeout()
{
    float remaining = timeBetweenRounds;
    bool expiredFired = false;
    SetupTimerTick?.Invoke(remaining);

    while (waitingForPlayerContinue)
    {
        if (BaseHealth.Instance != null && BaseHealth.Instance.IsDead)
            yield break;

        if (!timerPaused)
        {
            // FIX: Use Time.unscaledDeltaTime instead of Time.deltaTime
            // so the buy-phase timer ignores the GameSpeedUI multiplier.
            remaining = Mathf.Max(0f, remaining - Time.unscaledDeltaTime);

            if (remaining <= 0f && !expiredFired)
            {
                expiredFired = true;
                SetupTimerExpired?.Invoke();
            }
        }
        SetupTimerTick?.Invoke(remaining);

        yield return null;
    }
}

    private IEnumerator StartRound()
    {
        CurrentRound++;
        roundActive = true;
        RoundStarted?.Invoke(CurrentRound);
        
        // Safety check for SfxSettings dependency compatibility
        if (roundStartSound != null) 
        {
            float targetVolume = roundStartVolume;
            audioSource.PlayOneShot(roundStartSound, targetVolume);
        }
        
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
                if (BaseHealth.Instance != null && BaseHealth.Instance.IsDead)
                    yield break;

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

                yield return new WaitForSeconds(UnityEngine.Random.Range(minSpawnDelay, maxSpawnDelay));
            }
        }

        if (autoStartEnemyPhaseAfterSpawn)
        {
            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.EndPlayerTurn();
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
            }

            if (EnemiesRemaining <= 0) break;
            yield return new WaitForSeconds(0.2f);
        }

        roundActive = false;
        RoundFinished?.Invoke(CurrentRound);
        RoundUpdated?.Invoke(CurrentRound, EnemiesRemaining);
    }

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
    }

    public void ResetManagerForRestart()
{
    CurrentRound = 0; // or 1, depending on your setup
    EnemiesRemaining = 0; 
    
    // Fire the event immediately so the UI clears out the old text
    RoundUpdated?.Invoke(CurrentRound, EnemiesRemaining);
}
    public bool IsRoundActive() => roundActive;
}