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
    }

    private void OnDestroy()
    {
        if (col != null) activeColliders.Remove(col);
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
    }

    private void Update()
    {
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

    public void ApplyObstacleSlow(float multiplier) => obstacleSlowMultiplier = Mathf.Clamp(multiplier, 0f, 1f);
    public void ClearObstacleSlow() => obstacleSlowMultiplier = 1f;

    public void TakeDamage(int amount)
    {
        if (data == null) return;
        currentHealth -= amount;
        if (currentHealth <= 0) Die();
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
        Destroy(gameObject);
    }
}
