using UnityEngine;

// Holds permanent, shop-purchased modifiers that apply globally -- across all
// towers and the economy -- as opposed to the temporary per-run buffs on
// PlayerController. Singleton, mirrors ScoreManager/RoundManager's pattern.
public class GameModifiers : MonoBehaviour
{
    public static GameModifiers Instance { get; private set; }

    [Header("Economy")]
    public int goldPerRoundBonus = 0;

    [Header("Riptide Counter-Current")]
    public bool explodeOnSellEnabled = false;
    public float explodeOnSellDamage = 0f;
    public float explodeOnSellStunDuration = 0f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void AddGoldPerRound(int amount) => goldPerRoundBonus += amount;

    public void EnableExplodeOnSell(float damage, float stunDuration)
    {
        explodeOnSellEnabled = true;
        explodeOnSellDamage = damage;
        explodeOnSellStunDuration = stunDuration;
    }

    // Riptide Counter-Current's payoff -- called wherever a tower card is
    // sold/recycled. Hits and stuns every enemy currently on the map.
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
