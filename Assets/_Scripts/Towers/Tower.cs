using UnityEngine;

public class Tower : MonoBehaviour
{
    [Header("Runtime Config")]
    public TowerSO data;

    [Header("References")]
    [Tooltip("Assign the FirePoint child object in the Inspector. Bullet spawns here.")]
    public Transform firePoint;

    [Tooltip("Distance from sprite center to barrel tip — only used if firePoint is not assigned.")]
    public float barrelLength = 0.5f;

    private Transform visualTransform;
    private float nextShotTime;
    private float currentAngle;

    private void Awake()
    {
        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        visualTransform = sr != null ? sr.transform : transform;
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
        float closestDistSqr = float.MaxValue;
        float rangeSqr = data.range * data.range;

        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] == null) continue;
            float distSqr = (enemies[i].transform.position - transform.position).sqrMagnitude;
            if (distSqr > rangeSqr || distSqr >= closestDistSqr) continue;
            closest = enemies[i];
            closestDistSqr = distSqr;
        }

        return closest;
    }

    private bool RotateVisualTowards(Enemy target)
    {
        if (target == null || visualTransform == null) return false;

        Vector2 dir = target.transform.position - visualTransform.position;
        if (dir.sqrMagnitude < 0.0001f) return false;

        float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        currentAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, data.rotationSpeed * Time.deltaTime);
        visualTransform.rotation = Quaternion.Euler(0f, 0f, currentAngle);

        return Mathf.Abs(Mathf.DeltaAngle(currentAngle, targetAngle)) < 2f;
    }

    private void FireAt(Enemy target)
    {
        Vector3 spawnPos = firePoint != null
            ? firePoint.position
            : visualTransform.position + visualTransform.right * barrelLength;

        BulletBehavior projectile = Instantiate(data.projectilePrefab, spawnPos, Quaternion.identity);
        if (projectile == null) return;

        projectile.Speed = data.projectileSpeed;
        projectile.Damage = data.damage;
        projectile.SetDirection((target.transform.position - spawnPos).normalized);
    }
}