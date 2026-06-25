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

    private AnimationClipPlayer animPlayer;
    private bool playingFireAnimation;

    private void Awake()
    {
        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        visualTransform = sr != null ? sr.transform : transform;
        currentAngle = visualTransform.eulerAngles.z;
        animPlayer = new AnimationClipPlayer(visualTransform.gameObject);
    }

    private void Start()
    {
        PlayIdleAnimation();
    }

    private void OnDestroy()
    {
        animPlayer?.Dispose();
    }

    public void Configure(TowerSO towerData)
    {
        data = towerData;
        PlayIdleAnimation();
    }

    // Called by TowerPlacementManager right after placement so the tower
    // starts out facing whichever direction the player rotated the ghost to
    // with R, instead of always defaulting to the prefab's unrotated pose.
    public void SetInitialRotation(float degrees)
    {
        currentAngle = degrees;
        if (visualTransform != null)
            visualTransform.rotation = Quaternion.Euler(0f, 0f, currentAngle);
    }

    private void Update()
    {
        TickAnimation();

        if (data == null)
            return;

        if (!data.isMelee && data.projectilePrefab == null)
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

    private void TickAnimation()
    {
        bool finished = animPlayer.Tick(Time.deltaTime);

        if (finished && playingFireAnimation)
        {
            playingFireAnimation = false;
            PlayIdleAnimation();
        }
    }

    private void PlayIdleAnimation()
    {
        if (data == null || data.idleAnimation == null) return;
        playingFireAnimation = false;
        animPlayer.Play(data.idleAnimation, loop: true);
    }

    private void PlayFireAnimation()
    {
        if (data == null || data.fireAnimation == null) return;
        playingFireAnimation = true;
        animPlayer.Play(data.fireAnimation, loop: false);
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

    // Sprite art doesn't all face the same default direction, so "currentAngle"
    // (the rotation actually applied to visualTransform) and "where the firePoint
    // is currently pointing in world space" aren't the same thing unless the
    // firePoint happens to sit due-right of the pivot at zero rotation. We read
    // the firePoint's own local offset as the ground truth for the gun's neutral
    // facing, so aiming/firing line up correctly no matter how the art is drawn.
    private float GetFirePointLocalAngle()
    {
        if (firePoint == null) return 0f;

        Vector2 localDir = firePoint.localPosition;
        if (localDir.sqrMagnitude < 0.0001f) return 0f;

        return Mathf.Atan2(localDir.y, localDir.x) * Mathf.Rad2Deg;
    }

    private bool RotateVisualTowards(Enemy target)
    {
        if (target == null || visualTransform == null) return false;

        Vector2 dir = target.transform.position - visualTransform.position;
        if (dir.sqrMagnitude < 0.0001f) return false;

        float firePointLocalAngle = GetFirePointLocalAngle();
        float desiredWorldAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        float targetRotation = desiredWorldAngle - firePointLocalAngle;

        currentAngle = Mathf.MoveTowardsAngle(currentAngle, targetRotation, data.rotationSpeed * Time.deltaTime);
        visualTransform.rotation = Quaternion.Euler(0f, 0f, currentAngle);

        float firePointWorldAngle = currentAngle + firePointLocalAngle;
        return Mathf.Abs(Mathf.DeltaAngle(firePointWorldAngle, desiredWorldAngle)) < 2f;
    }

    private void FireAt(Enemy target)
    {
        PlayFireAnimation();

        if (data.isMelee)
        {
            if (target != null)
                target.TakeDamage(data.damage, data.meleeImpactAnimation);
            return;
        }

        if (data.projectilePrefab == null) return;

        Vector3 spawnPos = firePoint != null
            ? firePoint.position
            : visualTransform.position + visualTransform.right * barrelLength;

        BulletBehavior projectile = Instantiate(data.projectilePrefab, spawnPos, Quaternion.identity);
        if (projectile == null) return;

        projectile.Speed = data.projectileSpeed;
        projectile.Damage = data.damage;
        projectile.SetDirection((target.transform.position - spawnPos).normalized);

        if (data.projectileFlightAnimation != null)
            projectile.SetFlightAnimation(data.projectileFlightAnimation);
    }
}