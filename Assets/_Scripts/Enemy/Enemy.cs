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
    private bool isDead;
    private float stunnedUntil;

    private AnimationClipPlayer animPlayer;
    private bool playingImpactAnimation;

    [Tooltip("Distance to a waypoint before the enemy advances to the next one along the path")]
    public float waypointReachedDistance = 0.15f;

    [Header("Visual")]
    [Tooltip("Scales up the whole enemy (sprite + collider) for visibility -- gameplay values like moveSpeed/range/damage are unaffected since those are independent of transform scale.")]
    public float visualScaleMultiplier = 3f;

    [Header("Audio")]
    [Tooltip("Coin-clink sound played when this enemy is killed (not played on a leak reaching the base).")]
    public AudioClip deathSound;
    [Range(0f, 3f)] public float deathVolume = 1f;

    public int damage => data != null ? Mathf.CeilToInt(data.damage * damageMultiplier) : 1;
    private Rigidbody2D rb;
    private Vector2 movementDirection;
    private Transform target;
    private BaseHealth targetBase;
    private Vector3 currentWaypoint;

    private void Awake()
    {
        transform.localScale *= visualScaleMultiplier;

        rb = GetComponent<Rigidbody2D>();

        col = GetComponent<Collider2D>();
        if (col != null)
        {
            // Collider shapes are defined in local space, so the scale-up above
            // also inflates the collider's world-space footprint -- on a 1-unit
            // path grid that made enemies physically too wide to round corners
            // or squeeze past obstacles. Shrink the local size/offset by the same
            // factor so the world-space hitbox stays exactly what it was before
            // the visual got bigger.
            if (col is CapsuleCollider2D capsule)
            {
                capsule.size /= visualScaleMultiplier;
                capsule.offset /= visualScaleMultiplier;
            }
            else if (col is CircleCollider2D circle)
            {
                circle.radius /= visualScaleMultiplier;
                circle.offset /= visualScaleMultiplier;
            }
            else if (col is BoxCollider2D box)
            {
                box.size /= visualScaleMultiplier;
                box.offset /= visualScaleMultiplier;
            }

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

        if (Time.time < stunnedUntil)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (target && data != null)
        {
            rb.linearVelocity = movementDirection * (data.moveSpeed * speedMultiplier);
        }
    }

    // Called by SpawnManager right after Instantiate, before Start runs, so a
    // shared enemy prefab can be spawned as any EnemySO type from the budget pool.
    public void Configure(EnemySO enemyData) => data = enemyData;

    // Riptide Counter-Current's stun. Freezes movement (but not death/damage)
    // for the given duration; stacking calls only extend, never shorten it.
    public void Stun(float duration)
    {
        if (duration <= 0f || isDead) return;
        stunnedUntil = Mathf.Max(stunnedUntil, Time.time + duration);
    }

    public void TakeDamage(int amount, AnimationClip overlayImpact = null)
    {
        if (data == null || isDead) return;
        currentHealth -= amount;

        FloatingText.Spawn(transform.position, $"-{amount}", Color.white, floatSpeed: 1.2f, duration: 0.6f);

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

            // ADDED: floating "+Ng" gold popup over the kill spot.
            FloatingText.Spawn(transform.position, $"+{data.scoreValue}g", new Color(1f, 0.8431f, 0f));
        }
        if (deathSound != null)
        {
            AudioSource.PlayClipAtPoint(deathSound, transform.position, deathVolume * SfxSettings.Volume);
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
