using UnityEngine;

public class Enemy : MonoBehaviour
{
    public EnemySO data;

    private int currentHealth;
    // Multipliers applied per-round (can be set by RoundManager on spawn)
    [HideInInspector] public float healthMultiplier = 1f;
    [HideInInspector] public float speedMultiplier = 1f;
    [HideInInspector] public float damageMultiplier = 1f;

    // Expose damage from the ScriptableObject, with round-based multiplier; fallback to 1 if data is missing
    public int damage { get { return data != null ? Mathf.CeilToInt(data.damage * damageMultiplier) : 1; } }
    private Rigidbody2D rb;
    private Vector2 movementDirection;
    private Transform target;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        if (data != null)
        {
            currentHealth = Mathf.Max(1, Mathf.RoundToInt(data.maxHealth * healthMultiplier));
        }

        GameObject playerObj = GameObject.Find("Player");
        if (playerObj != null)
            target = playerObj.transform;
    }

    // Update is called once per frame
    void Update()
    {
        if (target == null || data == null) return; // Prevents errors if the player is destroyed

        // Only compute movement direction; actual movement in FixedUpdate will respect phase.
        Vector3 direction = (target.position - transform.position).normalized;
        movementDirection = direction;
    }

    private void FixedUpdate()
    {
        // Only move when TurnManager is in Enemies phase (so enemies don't act during player planning)
        if (TurnManager.Instance != null)
        {
            if (TurnManager.Instance.CurrentPhase != TurnManager.Phase.Enemies) return;
        }

        if (target && data != null)
        {
            rb.linearVelocity = new Vector2(movementDirection.x, movementDirection.y) * (data.moveSpeed * speedMultiplier);
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
        // notify round manager before destroying so it can update counts
        if (RoundManager.Instance != null)
        {
            RoundManager.Instance.OnEnemyKilled(this);
        }
        else
        {
            // fallback: try to find a RoundManager in scene (handles execution order issues)
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
