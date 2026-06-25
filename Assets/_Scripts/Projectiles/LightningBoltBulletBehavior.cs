using UnityEngine;

// Near-instant lightning bolt that chains to nearby enemies after the first hit.
public class LightningBoltBulletBehavior : BulletBehavior
{
    [Tooltip("How many extra enemies the bolt can jump to after the first target.")]
    public int chainCount = 2;

    [Tooltip("Max distance between chained targets.")]
    public float chainRadius = 1.5f;

    private bool hasStruck;

    protected override void OnHitEnemy(Enemy enemy)
    {
        if (enemy == null) return;

        if (!hasStruck)
        {
            hasStruck = true;
            enemy.TakeDamage(Damage);
            ChainFrom(enemy, chainCount, new System.Collections.Generic.HashSet<Enemy> { enemy });
            DestroyProjectile();
            return;
        }
    }

    private void ChainFrom(Enemy origin, int remainingChains, System.Collections.Generic.HashSet<Enemy> alreadyHit)
    {
        if (remainingChains <= 0 || origin == null) return;

        Enemy[] enemies = Object.FindObjectsByType<Enemy>();
        Enemy next = null;
        float closestDistSqr = float.MaxValue;
        float radiusSqr = chainRadius * chainRadius;

        for (int i = 0; i < enemies.Length; i++)
        {
            Enemy candidate = enemies[i];
            if (candidate == null || alreadyHit.Contains(candidate)) continue;

            float distSqr = (candidate.transform.position - origin.transform.position).sqrMagnitude;
            if (distSqr > radiusSqr || distSqr >= closestDistSqr) continue;

            next = candidate;
            closestDistSqr = distSqr;
        }

        if (next == null) return;

        alreadyHit.Add(next);
        next.TakeDamage(Damage);
        ChainFrom(next, remainingChains - 1, alreadyHit);
    }
}
