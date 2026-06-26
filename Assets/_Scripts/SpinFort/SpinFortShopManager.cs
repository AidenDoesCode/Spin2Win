using System;
using System.Collections.Generic;
using UnityEngine;

public class SpinFortShopManager : MonoBehaviour
{
    [Serializable]
    public class ShopOffer
    {
        public string label = "Upgrade";
        [Tooltip("Drives the card's border color and sparkle intensity in the shop UI.")]
        public CardRarity rarity = CardRarity.Common;
        [Tooltip("Relative odds this offer gets rolled into the shop each buy phase.")]
        [Min(0f)] public float weight = 1f;
        [Min(0)] public int cost = 100;
        public SpinFortRewardType rewardType = SpinFortRewardType.FireRateBuff;
        public int intValue = 1;
        public float floatValue = 1.15f;
        [Min(0f)] public float duration = 9999f;
        public TowerSO towerReward; // only used when rewardType == Tower
    }

    public static SpinFortShopManager Instance { get; private set; }

    [Header("References")]
    public PlayerController playerController;
    public PlayerHealth playerHealth;
    public RoundManager roundManager;

    [Header("Shop Pool")]
    [Tooltip("All possible offers the shop can draw from. Each buy phase rolls a random subset.")]
    public List<ShopOffer> possibleOffers = new List<ShopOffer>();
    [Min(1)] public int numOffersPerRound = 3;

    [Header("Spin To Reroll")]
    [Tooltip("Score cost of the first reroll in a buy phase.")]
    [Min(0)] public int baseRerollCost = 10;
    [Tooltip("Added to the reroll cost every time the player spins during a buy phase.")]
    [Min(0)] public int rerollCostIncrement = 10;

    public int rerollCost { get; private set; }

    public event Action ShopOpened;
    public event Action ShopClosed;
    public event Action ShopRefreshed;

    // All three lists are kept at a fixed length (numOffersPerRound) so a
    // slot's index never shifts -- a null offer means that slot hasn't been
    // revealed yet (player still needs to spin).
    public List<ShopOffer> CurrentOffers { get; } = new List<ShopOffer>();
    public bool IsOpen { get; private set; }

    private readonly List<bool> purchased = new List<bool>();
    private readonly List<bool> locked = new List<bool>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        rerollCost = baseRerollCost;

        for (int i = 0; i < numOffersPerRound; i++)
        {
            CurrentOffers.Add(null);
            purchased.Add(false);
            locked.Add(false);
        }
    }

    private void Start()
    {
        if (playerController == null) playerController = FindAnyObjectByType<PlayerController>();
        if (playerHealth == null) playerHealth = FindAnyObjectByType<PlayerHealth>();
        if (roundManager == null) roundManager = FindAnyObjectByType<RoundManager>();

        if (roundManager != null)
        {
            roundManager.RoundStarted += HandleRoundStarted;
            roundManager.RoundFinished += HandleRoundFinished;
        }

        OpenShop();
    }

    private void OnDestroy()
    {
        if (roundManager != null)
        {
            roundManager.RoundStarted -= HandleRoundStarted;
            roundManager.RoundFinished -= HandleRoundFinished;
        }
    }

    private void HandleRoundStarted(int round)
    {
        rerollCost = baseRerollCost;
        CloseShop();
    }

    private void HandleRoundFinished(int round)
    {
        TowerPlacementManager.Instance?.LockRandomSlot();
        OpenShop();
    }

    // Opens with locked slots preserved from the previous buy phase --
    // everything else (and the first-ever open) starts empty, requiring the
    // player to spend gold via TryReroll (the "SPIN FOR TOWERS!" prompt) to
    // reveal offers in those slots.
    public void OpenShop()
    {
        for (int i = 0; i < CurrentOffers.Count; i++)
        {
            if (!locked[i])
            {
                CurrentOffers[i] = null;
                purchased[i] = false;
            }
        }

        IsOpen = true;
        ShopOpened?.Invoke();
    }

    public void CloseShop()
    {
        IsOpen = false;
        ShopClosed?.Invoke();
    }

    public bool TryReroll()
    {
        if (!IsOpen) return false;
        if (ScoreManager.Instance == null) return false;
        if (!ScoreManager.Instance.TrySpendScore(rerollCost)) return false;

        RerollOffers();
        rerollCost += rerollCostIncrement;
        return true;
    }

    public bool IsOfferPurchased(int index)
    {
        if (index < 0 || index >= purchased.Count) return true;
        return purchased[index];
    }

    public bool IsOfferLocked(int index)
    {
        if (index < 0 || index >= locked.Count) return false;
        return locked[index];
    }

    // Toggling a lock only makes sense on a revealed, unpurchased offer --
    // locking saves it (skipped on reroll) into the next round's buy phase.
    public bool ToggleOfferLock(int index)
    {
        if (index < 0 || index >= locked.Count) return false;
        if (CurrentOffers[index] == null || purchased[index]) return false;

        locked[index] = !locked[index];
        ShopRefreshed?.Invoke();
        return true;
    }

    public bool TryBuyOffer(int index)
    {
        if (!IsOpen) return false;
        if (index < 0 || index >= CurrentOffers.Count) return false;
        if (purchased[index]) return false;
        if (ScoreManager.Instance == null) return false;

        var offer = CurrentOffers[index];
        if (offer == null) return false;

        if (offer.rewardType == SpinFortRewardType.Tower
            && PlayerTowerInventory.Instance != null
            && PlayerTowerInventory.Instance.IsFull)
            return false;

        if (!ScoreManager.Instance.TrySpendScore(offer.cost)) return false;

        ApplyOffer(offer);
        purchased[index] = true;
        ShopRefreshed?.Invoke();
        return true;
    }

    // Only rerolls slots that aren't locked; locked slots keep their current
    // offer (and purchased state) untouched.
    private void RerollOffers()
    {
        int rerollCount = 0;
        for (int i = 0; i < CurrentOffers.Count; i++)
            if (!locked[i]) rerollCount++;

        List<ShopOffer> fresh = RollRandomOffers(rerollCount);
        int freshCursor = 0;
        for (int i = 0; i < CurrentOffers.Count; i++)
        {
            if (locked[i]) continue;
            CurrentOffers[i] = freshCursor < fresh.Count ? fresh[freshCursor++] : null;
            purchased[i] = false;
        }
    }

    private List<ShopOffer> RollRandomOffers(int count)
    {
        var result = new List<ShopOffer>();
        var pool = new List<ShopOffer>(possibleOffers);
        pool.RemoveAll(o => o == null || o.weight <= 0f);

        int target = Mathf.Min(count, pool.Count);
        for (int i = 0; i < target; i++)
        {
            float totalWeight = 0f;
            foreach (var o in pool) totalWeight += o.weight;

            float roll = UnityEngine.Random.value * totalWeight;
            float cursor = 0f;
            ShopOffer picked = pool[pool.Count - 1];
            foreach (var o in pool)
            {
                cursor += o.weight;
                if (roll <= cursor) { picked = o; break; }
            }

            result.Add(picked);
            pool.Remove(picked);
        }

        return result;
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
            case SpinFortRewardType.Tower:
                if (offer.towerReward != null)
                {
                    if (PlayerTowerInventory.Instance == null)
                        Debug.LogWarning($"SpinFortShopManager: Bought '{offer.label}' but no PlayerTowerInventory exists in the scene.");
                    else
                        PlayerTowerInventory.Instance.AddTower(offer.towerReward);
                }
                else
                {
                    Debug.LogWarning($"SpinFortShopManager: Offer '{offer.label}' is rewardType Tower but has no towerReward assigned.");
                }
                break;
        }
    }
}
