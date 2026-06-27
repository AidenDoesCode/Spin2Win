using System;
using System.Collections.Generic;
using UnityEngine;

public class SpinFortShopManager : MonoBehaviour
{
    public static SpinFortShopManager Instance { get; private set; }

    [Header("References")]
    public PlayerController playerController;
    public PlayerHealth playerHealth;
    public RoundManager roundManager;

    [Header("Shop Pool")]
    [Tooltip("All possible offers the shop can draw from. Each buy phase rolls a random subset.")]
    public List<ShopCardSO> possibleOffers = new List<ShopCardSO>();
    [Min(1)] public int numOffersPerRound = 3;

    [Header("Spin To Reroll")]
    [Tooltip("Score cost of the first reroll in a buy phase.")]
    [Min(0)] public int baseRerollCost = 10;
    [Tooltip("Added to the reroll cost every time the player spins during a buy phase.")]
    [Min(0)] public int rerollCostIncrement = 10;

    public int rerollCost { get; private set; }

    // The very first spin of each buy phase is free; rerollCost (and its
    // normal +rerollCostIncrement growth) only kicks in starting with the
    // second spin.
    private bool firstRerollUsedThisPhase;
    public bool NextRerollIsFree => !firstRerollUsedThisPhase;
    public int NextRerollCost => NextRerollIsFree ? 0 : rerollCost;

    public event Action ShopOpened;
    public event Action ShopClosed;
    public event Action ShopRefreshed;

    // All three lists are kept at a fixed length (numOffersPerRound) so a
    // slot's index never shifts -- a null offer means that slot hasn't been
    // revealed yet (player still needs to spin).
    public List<ShopCardSO> CurrentOffers { get; } = new List<ShopCardSO>();
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
        firstRerollUsedThisPhase = false;
        CloseShop();
    }

    private void HandleRoundFinished(int round)
    {
        // The House Edge's payoff -- a flat gold trickle at the end of every round.
        if (GameModifiers.Instance != null && GameModifiers.Instance.goldPerRoundBonus > 0)
            ScoreManager.Instance?.AddScore(GameModifiers.Instance.goldPerRoundBonus);

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
        if (!ScoreManager.Instance.TrySpendScore(NextRerollCost)) return false;

        RerollOffers();

        if (firstRerollUsedThisPhase)
            rerollCost += rerollCostIncrement;
        firstRerollUsedThisPhase = true;

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

        if (offer.rewardType != SpinFortRewardType.Tower
            && PlayerUpgradeInventory.Instance != null
            && PlayerUpgradeInventory.Instance.IsFull)
            return false;

        if (!ScoreManager.Instance.TrySpendScore(offer.cost)) return false;

        ApplyOffer(offer);
        purchased[index] = true;
        // A locked slot whose card just got bought has nothing left to
        // protect -- clear the lock too, or OpenShop's "preserve locked
        // slots" rule would leave this slot stuck on SOLD forever, since a
        // purchased offer can't be unlocked manually either (see ToggleOfferLock).
        locked[index] = false;
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

        List<ShopCardSO> fresh = RollRandomOffers(rerollCount);
        int freshCursor = 0;
        for (int i = 0; i < CurrentOffers.Count; i++)
        {
            if (locked[i]) continue;
            CurrentOffers[i] = freshCursor < fresh.Count ? fresh[freshCursor++] : null;
            purchased[i] = false;
        }
    }

    private List<ShopCardSO> RollRandomOffers(int count)
    {
        var result = new List<ShopCardSO>();
        var pool = new List<ShopCardSO>(possibleOffers);
        pool.RemoveAll(o => o == null || o.weight <= 0f);

        // Fortune's Current's payoff -- luckMultiplier compounds per rarity
        // tier (Common untouched, Legendary gets it applied four times over),
        // skewing rolls toward rarer cards the same way other cards' floatValue
        // doubles as a "Xx" multiplier (see BrineInfusion/TowerDamageMultiplier).
        float luckMultiplier = GameModifiers.Instance != null ? GameModifiers.Instance.luckMultiplier : 1f;

        int target = Mathf.Min(count, pool.Count);
        for (int i = 0; i < target; i++)
        {
            float totalWeight = 0f;
            foreach (var o in pool) totalWeight += EffectiveRollWeight(o, luckMultiplier);

            float roll = UnityEngine.Random.value * totalWeight;
            float cursor = 0f;
            ShopCardSO picked = pool[pool.Count - 1];
            foreach (var o in pool)
            {
                cursor += EffectiveRollWeight(o, luckMultiplier);
                if (roll <= cursor) { picked = o; break; }
            }

            result.Add(picked);
            pool.Remove(picked);
        }

        return result;
    }

    private static float EffectiveRollWeight(ShopCardSO offer, float luckMultiplier) =>
        offer.weight * Mathf.Pow(luckMultiplier, (int)offer.rarity);

    // Buying a card no longer applies its effect on the spot -- it becomes a
    // consumable sitting in the player's inventory until they drag it onto a
    // tower (GlobalAttackSpeed/Range/Damage -- see IsTowerTargetedUpgrade)
    // or hit Use on it (everything else). Tower cards still go straight into
    // PlayerTowerInventory exactly as before.
    private void ApplyOffer(ShopCardSO offer)
    {
        if (offer.rewardType == SpinFortRewardType.Tower)
        {
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
            return;
        }

        if (PlayerUpgradeInventory.Instance == null)
            Debug.LogWarning($"SpinFortShopManager: Bought '{offer.label}' but no PlayerUpgradeInventory exists in the scene.");
        else
            PlayerUpgradeInventory.Instance.AddUpgrade(offer);
    }

    // GlobalAttackSpeed/GlobalTowerRange/GlobalTowerDamage keep their legacy
    // enum names, but now apply as a per-tower bonus to whichever tower the
    // card gets dragged onto (see Tower.AddInstance*Bonus) instead of a
    // permanent global multiplier.
    public static bool IsTowerTargetedUpgrade(SpinFortRewardType type) =>
        type == SpinFortRewardType.GlobalAttackSpeed
        || type == SpinFortRewardType.GlobalTowerRange
        || type == SpinFortRewardType.GlobalTowerDamage
        || type == SpinFortRewardType.TowerDamageMultiplier;

    // Called from the upgrade inventory UI when a card is dragged onto a
    // specific placed tower. Returns false (and leaves the card in inventory)
    // if this card type isn't tower-targeted.
    public bool UseUpgradeOnTower(ShopCardSO offer, Tower targetTower)
    {
        if (offer == null || targetTower == null) return false;

        switch (offer.rewardType)
        {
            case SpinFortRewardType.GlobalAttackSpeed:
                targetTower.AddInstanceAttackSpeedBonus(offer.floatValue);
                break;
            case SpinFortRewardType.GlobalTowerRange:
                targetTower.AddInstanceRangeBonus(offer.floatValue);
                break;
            case SpinFortRewardType.GlobalTowerDamage:
                targetTower.AddInstanceDamageBonus(offer.intValue);
                break;
            case SpinFortRewardType.TowerDamageMultiplier:
                targetTower.AddInstanceDamageMultiplier(offer.floatValue);
                break;
            default:
                return false;
        }

        PlayerUpgradeInventory.Instance?.RemoveUpgrade(offer);
        return true;
    }

    // Called from the upgrade inventory UI's Use button. Returns false (and
    // leaves the card in inventory) if this card type needs to be dragged
    // onto a tower instead.
    public bool UseUpgrade(ShopCardSO offer)
    {
        if (offer == null || IsTowerTargetedUpgrade(offer.rewardType)) return false;

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
            case SpinFortRewardType.BaseHeal:
                BaseHealth.Instance?.Heal(offer.intValue);
                break;
            case SpinFortRewardType.RerollDiscount:
                ApplyRerollDiscount(offer.intValue);
                break;
            case SpinFortRewardType.GoldPerRoundGain:
                GameModifiers.Instance?.AddGoldPerRound(offer.intValue);
                break;
            case SpinFortRewardType.AllInMultiplier:
                ApplyAllInMultiplier(offer.floatValue, offer.intValue);
                break;
            case SpinFortRewardType.TowersExplodeOnDeath:
                GameModifiers.Instance?.EnableExplodeOnSell(offer.floatValue, offer.duration);
                break;
            case SpinFortRewardType.MaxTowerHealthBuff:
                BaseHealth.Instance?.IncreaseMaxHealth(offer.intValue);
                break;
            case SpinFortRewardType.LuckBuff:
                GameModifiers.Instance?.AddLuck(offer.floatValue);
                break;
            case SpinFortRewardType.TowerRotationSpeedBuff:
                GameModifiers.Instance?.AddTowerRotationSpeed(offer.floatValue);
                break;
            case SpinFortRewardType.GlobalTowerDamageMultiplier:
                GameModifiers.Instance?.MultiplyGlobalDamage(offer.floatValue);
                break;
            default:
                return false;
        }

        PlayerUpgradeInventory.Instance?.RemoveUpgrade(offer);
        return true;
    }

    // Loaded Dice -- permanently lowers both the running cost and the base
    // it resets to every round.
    private void ApplyRerollDiscount(int amount)
    {
        if (amount <= 0) return;

        baseRerollCost = Mathf.Max(0, baseRerollCost - amount);
        rerollCost = Mathf.Max(0, rerollCost - amount);
    }

    // All-In Multiplier -- doubles (or whatever multiplier) the single
    // strongest currently-placed tower's damage, gambling away max base health.
    private void ApplyAllInMultiplier(float multiplier, int maxHealthReduction)
    {
        Tower strongest = null;
        int strongestDamage = -1;

        foreach (Tower tower in UnityEngine.Object.FindObjectsByType<Tower>(FindObjectsInactive.Exclude))
        {
            if (tower == null || tower.data == null) continue;
            int effective = tower.EffectiveDamage;
            if (effective > strongestDamage)
            {
                strongestDamage = effective;
                strongest = tower;
            }
        }

        if (strongest != null)
            strongest.AddInstanceDamageBonus(Mathf.RoundToInt(strongestDamage * Mathf.Max(0f, multiplier - 1f)));

        BaseHealth.Instance?.ReduceMaxHealth(maxHealthReduction);
    }
}
