using System;
using System.Collections.Generic;
using UnityEngine;

public class SpinFortShopManager : MonoBehaviour
{
    [Serializable]
    public class ShopOffer
    {
        public string label = "Upgrade";
        [Min(0)] public int cost = 100;
        public SpinFortRewardType rewardType = SpinFortRewardType.FireRateBuff;
        public int intValue = 1;
        public float floatValue = 1.15f;
        [Min(0f)] public float duration = 9999f;
    }

    [Header("References")]
    public PlayerController playerController;
    public PlayerHealth playerHealth;
    public RoundManager roundManager;

    [Header("Offers")]
    public List<ShopOffer> offers = new List<ShopOffer>();

    private void Start()
    {
        if (playerController == null)
            playerController = FindAnyObjectByType<PlayerController>();

        if (playerHealth == null)
            playerHealth = FindAnyObjectByType<PlayerHealth>();

        if (roundManager == null)
            roundManager = FindAnyObjectByType<RoundManager>();
    }

    public bool TryBuyOffer(int index)
    {
        if (index < 0 || index >= offers.Count)
            return false;

        if (ScoreManager.Instance == null)
            return false;

        var offer = offers[index];
        if (!ScoreManager.Instance.TrySpendScore(offer.cost))
            return false;

        ApplyOffer(offer);
        return true;
    }

    private void ApplyOffer(ShopOffer offer)
    {
        switch (offer.rewardType)
        {
            case SpinFortRewardType.Points:
                ScoreManager.Instance?.AddScore(offer.intValue);
                break;

            case SpinFortRewardType.FireRateBuff:
                playerController?.ApplyFireRateMultiplier(offer.floatValue, offer.duration);
                break;

            case SpinFortRewardType.DamageBuff:
                playerController?.AddBonusDamage(offer.intValue, offer.duration);
                break;

            case SpinFortRewardType.MovementSpeedBuff:
                playerController?.ApplyMovementSpeedMultiplier(offer.floatValue, offer.duration);
                break;

            case SpinFortRewardType.HealPlayer:
                playerHealth?.Heal(offer.intValue);
                break;

            case SpinFortRewardType.BonusEnemiesNextRound:
                roundManager?.AddBonusEnemies(Mathf.Max(0, offer.intValue));
                break;
        }
    }
}