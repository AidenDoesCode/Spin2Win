using UnityEngine;

public class GameModifiers : MonoBehaviour
{
    public static GameModifiers Instance { get; private set; }

    [Header("Economy")]
    public int goldPerRoundBonus = 0;

    [Header("Riptide Counter-Current")]
    public bool explodeOnSellEnabled = false;
    public float explodeOnSellDamage = 0f;
    public float explodeOnSellStunDuration = 0f;

    [Header("New Custom Card Buffs")]
    public float luckMultiplier = 1.0f; // Per-rarity-tier weight multiplier when rolling shop offers, starts at 1.0x (no bias)
    public float towerRotationSpeedBonus = 0f;
    public float globalDamageMultiplier = 1.0f; // Starts at 1.0x (normal damage)

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void AddGoldPerRound(int amount) => goldPerRoundBonus += amount;

    // --- NEW MODIFIER METHODS ---
    public void AddLuck(float multiplier) => luckMultiplier *= multiplier;
    public void AddTowerRotationSpeed(float amount) => towerRotationSpeedBonus += amount;
    public void MultiplyGlobalDamage(float multiplier) => globalDamageMultiplier *= multiplier;

    public void EnableExplodeOnSell(float damage, float stunDuration)
    {
        explodeOnSellEnabled = true;
        explodeOnSellDamage = damage;
        explodeOnSellStunDuration = stunDuration;
    }

    public void TriggerSellExplosionIfEnabled()
    {
        if (!explodeOnSellEnabled) return;

        foreach (Enemy enemy in Object.FindObjectsByType<Enemy>(FindObjectsInactive.Exclude))
        {
            if (enemy == null) continue;
            enemy.TakeDamage(Mathf.RoundToInt(explodeOnSellDamage));
            enemy.Stun(explodeOnSellStunDuration);
        }

        CameraShake.Instance?.Shake(0.2f, 0.1f);
    }
}