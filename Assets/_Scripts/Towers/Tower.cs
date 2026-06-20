using UnityEngine;

public class Tower : MonoBehaviour
{
    [Header("Runtime Config")]
    public TowerSO data;

    private float nextShotTime;

    public void Configure(TowerSO towerData)
    {
        data = towerData;
    }

    private void Update()
    {
        if (data == null || data.projectilePrefab == null)
            return;

        Enemy target = FindClosestEnemyInRange();
        if (target == null)
            return;

        if (Time.time < nextShotTime)
            return;

        nextShotTime = Time.time + (1f / Mathf.Max(0.0001f, data.fireRate));
        FireAt(target);
    }

    private Enemy FindClosestEnemyInRange()
    {
        Enemy[] enemies = Object.FindObjectsByType<Enemy>();
        Enemy closest = null;
        float closestDistanceSqr = float.MaxValue;
        float rangeSqr = data.range * data.range;

        Vector3 position = transform.position;
        for (int i = 0; i < enemies.Length; i++)
        {
            Enemy enemy = enemies[i];
            if (enemy == null)
                continue;

            float distanceSqr = (enemy.transform.position - position).sqrMagnitude;
            if (distanceSqr > rangeSqr || distanceSqr >= closestDistanceSqr)
                continue;

            closest = enemy;
            closestDistanceSqr = distanceSqr;
        }

        return closest;
    }

    private void FireAt(Enemy target)
    {
        Vector3 spawnPosition = transform.position;
        Quaternion rotation = Quaternion.identity;

        BulletBehavior projectile = Instantiate(data.projectilePrefab, spawnPosition, rotation);
        if (projectile == null)
            return;

        projectile.Speed = data.projectileSpeed;
        projectile.Damage = data.damage;
        Vector2 direction = (target.transform.position - transform.position).normalized;
        projectile.SetDirection(direction);
    }
}