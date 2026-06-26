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
    private Vector3 baseVisualScale;

    private AnimationClipPlayer animPlayer;
    private bool playingFireAnimation;
    private AudioSource audioSource;
    private SpriteSquash squash;

    // Flat per-instance damage bonus -- separate from TowerSO.damage (a
    // shared asset) so a one-off effect like All-In Multiplier can buff a
    // single placed tower without affecting every other tower of that type.
    private int instanceDamageBonus;

    public int EffectiveDamage => (data != null ? data.damage : 0) + instanceDamageBonus +
        (GameModifiers.Instance != null ? GameModifiers.Instance.globalDamageBonus : 0);
    public float EffectiveRange => data != null
        ? data.range * (GameModifiers.Instance != null ? GameModifiers.Instance.globalRangeMultiplier : 1f)
        : 0f;
    public float EffectiveFireRate => data != null
        ? data.fireRate * (GameModifiers.Instance != null ? GameModifiers.Instance.globalAttackSpeedMultiplier : 1f)
        : 0f;

    public void AddInstanceDamageBonus(int amount) => instanceDamageBonus += amount;

    // Sells this placed tower for a partial refund and removes it from the
    // grid -- triggers Riptide Counter-Current's sell explosion if bought.
    public void Sell(float refundPercent)
    {
        if (data != null)
        {
            int refund = Mathf.RoundToInt(data.cost * refundPercent);
            ScoreManager.Instance?.AddScore(refund);
            FloatingText.Spawn(transform.position, $"+{refund}g", new Color(1f, 0.8431f, 0f));
        }

        GameModifiers.Instance?.TriggerSellExplosionIfEnabled();
        Destroy(gameObject);
    }

    private void Awake()
    {
        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        visualTransform = sr != null ? sr.transform : transform;
        currentAngle = visualTransform.eulerAngles.z;
        baseVisualScale = visualTransform.localScale;
        animPlayer = new AnimationClipPlayer(visualTransform.gameObject);

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
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

        // visualScaleMultiplier lives on TowerSO (not known until now), so
        // the scale-up and the flip-trick's baseline both happen here rather
        // than in Awake -- re-capturing baseVisualScale after scaling keeps
        // ApplyVisualRotation's mirror flip using the correct magnitude.
        if (data != null)
        {
            transform.localScale *= data.visualScaleMultiplier;
            baseVisualScale = visualTransform.localScale;
        }

        PlayIdleAnimation();
    }

    // Called by TowerPlacementManager right after placement so the tower
    // starts out facing whichever direction the player rotated the ghost to
    // with R, instead of always defaulting to the prefab's unrotated pose.
    public void SetInitialRotation(float degrees)
    {
        currentAngle = degrees;
        ApplyVisualRotation(currentAngle);
    }

    // ADDED: squash-and-stretch pop, played on the sprite's own visual
    // transform so it doesn't fight the flip-mirror trick in
    // ApplyVisualRotation. Called on successful placement and on melee hits.
    public void PlaySquash()
    {
        if (visualTransform == null) return;

        if (squash == null) squash = visualTransform.gameObject.AddComponent<SpriteSquash>();
        squash.Play();
    }

    // Renders currentAngle without ever rotating the sprite past vertical --
    // folds the angle into the right-facing half and mirrors (localScale.x)
    // for the left half instead, so the single top-down sprite never looks
    // upside-down. Passing firePointLocalAngle in lets the fold correctly
    // compensate for art whose neutral facing isn't exactly local +x --
    // without it, anything that isn't due-left/due-right ends up pointing
    // the wrong way once mirrored, which is what was sending firePoint (and
    // therefore spawned projectiles) to the wrong position/angle.
    private void ApplyVisualRotation(float angle)
    {
        if (visualTransform == null) return;

        float displayAngle = TopDownAim.Fold(angle, GetFirePointLocalAngle(), out bool flipped);

        visualTransform.localScale = new Vector3(
            flipped ? -Mathf.Abs(baseVisualScale.x) : Mathf.Abs(baseVisualScale.x),
            baseVisualScale.y,
            baseVisualScale.z);

        visualTransform.rotation = Quaternion.Euler(0f, 0f, displayAngle);
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

        nextShotTime = Time.time + (1f / Mathf.Max(0.0001f, EffectiveFireRate));
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
        float rangeSqr = EffectiveRange * EffectiveRange;

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
        ApplyVisualRotation(currentAngle);

        // The aiming-accuracy check below is purely logical (based on the
        // true continuous currentAngle), independent of how ApplyVisualRotation
        // chooses to render it -- so it's unaffected by the fold/flip trick.
        float firePointWorldAngle = currentAngle + firePointLocalAngle;
        return Mathf.Abs(Mathf.DeltaAngle(firePointWorldAngle, desiredWorldAngle)) < 2f;
    }

    private void FireAt(Enemy target)
    {
        PlayFireAnimation();

        if (data.fireSound != null)
            audioSource.PlayOneShot(data.fireSound, data.fireVolume * SfxSettings.Volume);

        if (data.isMelee)
        {
            if (target != null)
            {
                target.TakeDamage(EffectiveDamage, data.meleeImpactAnimation);
                PlaySquash(); // ADDED: squash pop when the melee hit lands
            }
            return;
        }

        if (data.projectilePrefab == null) return;

        // Transform.right only reflects rotation, not the mirror flip applied
        // in ApplyVisualRotation, so it can't be used here once flipped --
        // derive the world direction straight from the true currentAngle instead.
        Vector3 spawnPos = firePoint != null
            ? firePoint.position
            : visualTransform.position + (Quaternion.Euler(0f, 0f, currentAngle) * Vector3.right) * barrelLength;

        BulletBehavior projectile = Instantiate(data.projectilePrefab, spawnPos, Quaternion.identity);
        if (projectile == null) return;

        projectile.transform.localScale *= data.visualScaleMultiplier * data.projectileScaleRatio;
        projectile.Speed = data.projectileSpeed;
        projectile.Damage = EffectiveDamage;
        projectile.impactSound = data.impactSound;
        projectile.impactVolume = data.impactVolume;
        // ADDED: hands the screen-shake-on-impact settings down from the TowerSO.
        projectile.screenShakeOnImpact = data.screenShakeOnImpact;
        projectile.shakeDuration = data.shakeDuration;
        projectile.shakeMagnitude = data.shakeMagnitude;
        projectile.SetDirection((target.transform.position - spawnPos).normalized);

        if (data.projectileFlightAnimation != null)
            projectile.SetFlightAnimation(data.projectileFlightAnimation);
    }
}