using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public EnemySO data;

    // Enemies spawn stacked on the same path tile; without this, their solid
    // colliders shove each other apart on spawn and knock them off the path.
    private static readonly List<Collider2D> activeColliders = new List<Collider2D>();
    private Collider2D col;

    private int currentHealth;
    [HideInInspector] public float healthMultiplier = 1f;
    [HideInInspector] public float speedMultiplier = 1f;
    [HideInInspector] public float damageMultiplier = 1f;
    private float obstacleSlowMultiplier = 1f;
    private bool isDead;

    private AnimationClipPlayer animPlayer;
    private bool playingImpactAnimation;

    [Tooltip("Distance to a waypoint before the enemy advances to the next one along the path")]
    public float waypointReachedDistance = 0.15f;

    public int damage => data != null ? Mathf.CeilToInt(data.damage * damageMultiplier) : 1;
    private Rigidbody2D rb;
    private Vector2 movementDirection;
    private Transform target;
    private BaseHealth targetBase;
    private Vector3 currentWaypoint;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        col = GetComponent<Collider2D>();
        if (col != null)
        {
            foreach (var other in activeColliders)
            {
                if (other != null) Physics2D.IgnoreCollision(col, other);
            }
            activeColliders.Add(col);
        }

        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        GameObject visual = sr != null ? sr.gameObject : gameObject;
        animPlayer = new AnimationClipPlayer(visual);
    }

    private void OnDestroy()
    {
        if (col != null) activeColliders.Remove(col);
        animPlayer?.Dispose();
    }

    private void Start()
    {
        if (data != null)
        {
            currentHealth = Mathf.Max(1, Mathf.RoundToInt(data.maxHealth * healthMultiplier));
        }

        targetBase = BaseHealth.Instance != null ? BaseHealth.Instance : FindAnyObjectByType<BaseHealth>();

        if (targetBase != null)
        {
            target = targetBase.transform;
        }
        else
        {
            GameObject playerObj = GameObject.Find("Player");
            target = playerObj != null ? playerObj.transform : null;
        }

        if (targetBase != null && EnemyPathfinder.Instance != null)
        {
            currentWaypoint = EnemyPathfinder.Instance.GetNextWaypoint(transform.position);
        }

        if (data != null && data.walkAnimation != null)
            animPlayer.Play(data.walkAnimation, loop: true);
    }

    private void Update()
    {
        bool finished = animPlayer.Tick(Time.deltaTime);
        if (finished && playingImpactAnimation)
        {
            playingImpactAnimation = false;
            if (!isDead && data != null && data.walkAnimation != null)
                animPlayer.Play(data.walkAnimation, loop: true);
        }

        if (isDead) return;
        if (data == null) return;

        if (targetBase != null)
        {
            if (targetBase.IsDead)
            {
                movementDirection = Vector2.zero;
                return;
            }

            Vector2 toBase = (targetBase.transform.position - transform.position);
            float distanceToBase = toBase.magnitude;
            float damageRadius = Mathf.Max(0f, targetBase.damageRadius);

            if (distanceToBase <= damageRadius)
            {
                movementDirection = Vector2.zero;
                targetBase.TakeDamage(damage);
                ReachBase();
                return;
            }

            if (EnemyPathfinder.Instance == null)
            {
                movementDirection = toBase.normalized;
                return;
            }

            if (Vector2.Distance(transform.position, currentWaypoint) <= waypointReachedDistance)
            {
                currentWaypoint = EnemyPathfinder.Instance.GetNextWaypoint(transform.position);
            }

            movementDirection = ((Vector2)currentWaypoint - (Vector2)transform.position).normalized;
            return;
        }

        if (target == null) return;
        movementDirection = (target.position - transform.position).normalized;
    }

    private void FixedUpdate()
    {
        if (isDead)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (targetBase != null && targetBase.IsDead)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (target && data != null)
        {
            rb.linearVelocity = movementDirection * (data.moveSpeed * speedMultiplier * obstacleSlowMultiplier);
        }
    }

    // Called by SpawnManager right after Instantiate, before Start runs, so a
    // shared enemy prefab can be spawned as any EnemySO type from the budget pool.
    public void Configure(EnemySO enemyData) => data = enemyData;

    public void ApplyObstacleSlow(float multiplier) => obstacleSlowMultiplier = Mathf.Clamp(multiplier, 0f, 1f);
    public void ClearObstacleSlow() => obstacleSlowMultiplier = 1f;

    public void TakeDamage(int amount, AnimationClip overlayImpact = null)
    {
        if (data == null || isDead) return;
        currentHealth -= amount;

        if (overlayImpact != null)
            OverlayAnimationEffect.PlayAt(transform, overlayImpact);

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        if (overlayImpact == null)
            PlayImpactAnimation();
    }

    private void PlayImpactAnimation()
    {
        if (data == null || data.impactAnimation == null) return;
        playingImpactAnimation = true;
        animPlayer.Play(data.impactAnimation, loop: false);
    }

    // Reaching the base is a leak, not a kill: no score, no death prefab,
    // just stop counting it toward the round so the wave can still end.
    private void ReachBase()
    {
        if (RoundManager.Instance != null)
        {
            RoundManager.Instance.OnEnemyKilled(this);
        }
        else
        {
            var rm = Object.FindAnyObjectByType<RoundManager>();
            if (rm != null) rm.OnEnemyKilled(this);
        }
        Destroy(gameObject);
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;
        playingImpactAnimation = false;

        if (data != null && data.deathPrefab != null)
        {
            Instantiate(data.deathPrefab, transform.position, Quaternion.identity);
        }
        if (ScoreManager.Instance != null && data != null)
        {
            ScoreManager.Instance.AddScore(data.scoreValue);
        }
        if (RoundManager.Instance != null)
        {
            RoundManager.Instance.OnEnemyKilled(this);
        }
        else
        {
            var rm = Object.FindAnyObjectByType<RoundManager>();
            if (rm != null)
            {
                rm.OnEnemyKilled(this);
            }
            else
            {
                Debug.LogWarning("Enemy died but RoundManager not found to notify.");
            }
        }

        // Stop colliding immediately so a "dead" enemy can't still block or
        // hurt anything while its death animation plays out.
        if (col != null) col.enabled = false;

        if (data != null && data.deathAnimation != null)
        {
            animPlayer.Play(data.deathAnimation, loop: false);
            Destroy(gameObject, data.deathAnimation.length);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
