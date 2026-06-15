using UnityEngine;

public class Enemy : MonoBehaviour
{
    public EnemySO data;

    private int currentHealth;
    // Expose damage from the ScriptableObject; fallback to 1 if data is missing
    public int damage { get { return data != null ? data.damage : 1; } }
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
            currentHealth = data.maxHealth;
        }

        GameObject playerObj = GameObject.Find("Player");
        if (playerObj != null)
            target = playerObj.transform;
    }

    // Update is called once per frame
    void Update()
    {
        if (target == null || data == null) return; // Prevents errors if the player is destroyed

        Vector3 direction = (target.position - transform.position).normalized;
        movementDirection = direction;
    }

    private void FixedUpdate()
    {
        if (target && data != null)
        {
            rb.linearVelocity = new Vector2(movementDirection.x, movementDirection.y) * data.moveSpeed;
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
        Destroy(gameObject);
    }

}
