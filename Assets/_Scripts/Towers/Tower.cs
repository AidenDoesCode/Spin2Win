using UnityEngine;

public class Tower : MonoBehaviour
{
    [Header("Runtime Config")]
    public TowerSO data;

    [Header("References")]
    [Tooltip("Assign the FirePoint child object in the Inspector. Bullet spawns here.")]
    public Transform firePoint;

    private Transform visualTransform;
    private float nextShotTime;
    private float currentAngle;

    private void Awake()
    {
        SpriteRenderer spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        visualTransform = spriteRenderer != null ? spriteRenderer.transform : transform;
        currentAngle = visualTransform.eulerAngles.z;
    }

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

        bool isAimed = RotateVisualTowards(target);

        if (!isAimed || Time.time < nextShotTime)
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

    private bool RotateVisualTowards(Enemy target)
    {
        if (target == null || visualTransform == null)
            return false;

        Vector2 direction = (target.transform.position - visualTransform.position);
        if (direction.sqrMagnitude < 0.0001f)
            return false;

        // Barrel faces RIGHT in the sprite, so no offset needed
        float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        currentAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, data.rotationSpeed * Time.deltaTime);
        visualTransform.rotation = Quaternion.Euler(0f, 0f, currentAngle);

        float angleDiff = Mathf.Abs(Mathf.DeltaAngle(currentAngle, targetAngle));
        return angleDiff < 2f;
    }

    private void FireAt(Enemy target)
    {
        // Use firePoint if assigned, otherwise fall back to tower's own position
        Vector3 spawnPosition = firePoint != null ? firePoint.position : transform.position;

        BulletBehavior projectile = Instantiate(data.projectilePrefab, spawnPosition, Quaternion.identity);
        if (projectile == null)
            return;

        projectile.Speed = data.projectileSpeed;
        projectile.Damage = data.damage;
        Vector2 direction = (target.transform.position - spawnPosition).normalized;
        projectile.SetDirection(direction);
    }
}