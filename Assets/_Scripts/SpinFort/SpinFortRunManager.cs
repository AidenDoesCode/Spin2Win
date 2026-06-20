using System;
using UnityEngine;

public class SpinFortRunManager : MonoBehaviour
{
    public static SpinFortRunManager Instance { get; private set; }

    [Header("References")]
    public RoundManager roundManager;
    public SpinFortWheelDefinitionSO wheelDefinition;
    public PlayerController playerController;
    public PlayerHealth playerHealth;

    [Header("Behavior")]
    public bool autoSpinOnRoundComplete = true;

    public event Action<SpinFortWheelDefinitionSO.WheelSegment> SpinCompleted;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (roundManager == null)
            roundManager = FindAnyObjectByType<RoundManager>();

        if (playerController == null)
            playerController = FindAnyObjectByType<PlayerController>();

        if (playerHealth == null)
            playerHealth = FindAnyObjectByType<PlayerHealth>();

        if (roundManager != null)
            roundManager.RoundFinished += HandleRoundFinished;
    }

    private void OnDestroy()
    {
        if (roundManager != null)
            roundManager.RoundFinished -= HandleRoundFinished;
    }

    private void HandleRoundFinished(int round)
    {
        if (autoSpinOnRoundComplete)
            SpinWheel();
    }

    public void SpinWheel()
    {
        if (wheelDefinition == null)
        {
            Debug.LogWarning("SpinFortRunManager: No wheel definition assigned.");
            return;
        }

        var result = wheelDefinition.Roll();
        if (result == null)
            return;

        ApplyResult(result);
        SpinCompleted?.Invoke(result);
        Debug.Log($"SpinFortRunManager: Wheel landed on {result.label} ({result.rewardType}).");
    }

    private void ApplyResult(SpinFortWheelDefinitionSO.WheelSegment result)
    {
        switch (result.rewardType)
        {
            case SpinFortRewardType.Points:
                if (ScoreManager.Instance != null)
                    ScoreManager.Instance.AddScore(result.intValue);
                break;

            case SpinFortRewardType.FireRateBuff:
                if (playerController != null)
                    playerController.ApplyFireRateMultiplier(result.floatValue, result.duration);
                break;

            case SpinFortRewardType.DamageBuff:
                if (playerController != null)
                    playerController.AddBonusDamage(result.intValue, result.duration);
                break;

            case SpinFortRewardType.MovementSpeedBuff:
                if (playerController != null)
                    playerController.ApplyMovementSpeedMultiplier(result.floatValue, result.duration);
                break;

            case SpinFortRewardType.HealPlayer:
                if (playerHealth != null)
                    playerHealth.Heal(result.intValue);
                break;

            case SpinFortRewardType.BonusEnemiesNextRound:
                if (roundManager != null)
                    roundManager.AddBonusEnemies(Mathf.Max(0, result.intValue));
                break;
        }
    }
}