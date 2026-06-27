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
        if (roundManager == null) roundManager = FindAnyObjectByType<RoundManager>();
        if (playerController == null) playerController = FindAnyObjectByType<PlayerController>();
        if (playerHealth == null) playerHealth = FindAnyObjectByType<PlayerHealth>();
        if (roundManager != null) roundManager.RoundFinished += HandleRoundFinished;
    }

    private void OnDestroy()
    {
        if (roundManager != null) roundManager.RoundFinished -= HandleRoundFinished;
    }

    private void HandleRoundFinished(int round)
    {
        if (autoSpinOnRoundComplete) SpinWheel();
    }

    public void SpinWheel()
    {
        if (wheelDefinition == null)
        {
            Debug.LogWarning("SpinFortRunManager: No wheel definition assigned.");
            return;
        }

        var result = wheelDefinition.Roll();
        if (result == null) return;

        ApplyResult(result);
        SpinCompleted?.Invoke(result);
        Debug.Log($"SpinFortRunManager: Wheel landed on {result.label} ({result.rewardType}).");
    }

private void ApplyResult(SpinFortWheelDefinitionSO.WheelSegment result)
    {
        switch (result.rewardType)
        {
            case SpinFortRewardType.Points:
                ScoreManager.Instance?.AddScore(result.intValue);
                break;
            case SpinFortRewardType.FireRateBuff:
                playerController?.ApplyFireRateMultiplier(result.floatValue, result.duration);
                break;
            case SpinFortRewardType.DamageBuff:
                playerController?.AddBonusDamage(result.intValue, result.duration);
                break;
            case SpinFortRewardType.MovementSpeedBuff:
                playerController?.ApplyMovementSpeedMultiplier(result.floatValue, result.duration);
                break;
            case SpinFortRewardType.HealPlayer:
                playerHealth?.Heal(result.intValue);
                break;
            case SpinFortRewardType.BonusEnemiesNextRound:
                roundManager?.AddBonusEnemies(Mathf.Max(0, result.intValue));
                break;
            case SpinFortRewardType.Tower:
                if (result.towerReward != null)
                    PlayerTowerInventory.Instance?.AddTower(result.towerReward);
                break;
                
            // --- NEW REWARD HANDLERS ---
            case SpinFortRewardType.MaxTowerHealthBuff:
                BaseHealth.Instance?.IncreaseMaxHealth(result.intValue);
                break;
            case SpinFortRewardType.LuckBuff:
                GameModifiers.Instance?.AddLuck(result.floatValue);
                break;
            case SpinFortRewardType.TowerRotationSpeedBuff:
                GameModifiers.Instance?.AddTowerRotationSpeed(result.floatValue);
                break;
            case SpinFortRewardType.GlobalTowerDamageMultiplier:
                GameModifiers.Instance?.MultiplyGlobalDamage(result.floatValue);
                break;
        }
    }
}