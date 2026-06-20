using UnityEngine;

public class Enemy : MonoBehaviour
{
    public EnemySO data;

    private int currentHealth;
    [HideInInspector] public float healthMultiplier = 1f;
    [HideInInspector] public float speedMultiplier = 1f;
    [HideInInspector] public float damageMultiplier = 1f;

    public int damage => data != null ? Mathf.CeilToInt(data.damage * damageMultiplier) : 1;
    private Rigidbody2D rb;
    private Vector2 movementDirection;
    private Transform target;
    private BaseHealth targetBase;
    private float nextAttackTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
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

            if (distanceToBase <= data.attackRange)
            {
                movementDirection = Vector2.zero;
                if (Time.time >= nextAttackTime)
                {
                    targetBase.TakeDamage(damage);
                    nextAttackTime = Time.time + Mathf.Max(0.05f, data.attackCooldown);
                }
                return;
            }

            movementDirection = toBase.normalized;
            return;
        }

        if (target == null) return;
        movementDirection = (target.position - transform.position).normalized;
    }

    private void FixedUpdate()
    {
        var turnManager = TurnManager.Instance;
        if (turnManager != null)
        {
            if (turnManager.CurrentPhase != TurnManager.Phase.Enemies) return;
        }

        if (targetBase != null && targetBase.IsDead)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (target && data != null)
        {
            rb.linearVelocity = movementDirection * (data.moveSpeed * speedMultiplier);
        }
    }

    public void TakeDamage(int amount)
    {
        if (data == null) return;
        currentHealth -= amount;
        if (currentHealth <= 0) Die();
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
