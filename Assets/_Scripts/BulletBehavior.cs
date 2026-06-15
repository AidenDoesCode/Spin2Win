using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class BulletBehavior : MonoBehaviour
{
    public float Speed = 5f;
    public int Damage = 1;
    private Rigidbody2D rb;
    private Vector2 initialDir = Vector2.zero;
    private bool hasInitialDir = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        // Use local up as forward for top-down sprites. If a spawn-set initial
        // direction was provided, use that instead.
        if (hasInitialDir)
            rb.linearVelocity = initialDir.normalized * Speed;
        else
            rb.linearVelocity = transform.up * Speed;
    }

    private void FixedUpdate()
    {
        if (rb == null) return;

        // Maintain consistent speed in case physics changes velocity
        if (rb.linearVelocity.sqrMagnitude > 0f)
            rb.linearVelocity = rb.linearVelocity.normalized * Speed;
        else if (hasInitialDir)
            rb.linearVelocity = initialDir.normalized * Speed;
        else
            rb.linearVelocity = transform.up * Speed;
    }

    // Called by spawner to set an explicit direction (world-space)
    public void SetDirection(Vector2 dir)
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        initialDir = dir;
        hasInitialDir = true;
        rb.linearVelocity = initialDir.normalized * Speed;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;
        // Ignore other projectiles
        if (other.GetComponent<BulletBehavior>() != null) return;
        // Ignore arena collisions: by layer name or by tag if you prefer
        int arenaLayer = LayerMask.NameToLayer("Arena");
        if (arenaLayer >= 0 && other.gameObject.layer == arenaLayer) return;
        //if (other.CompareTag("Arena")) return;

        // Damage enemies
        var enemy = other.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.TakeDamage(Damage);
            Destroy(gameObject);
            return;
        }

        Destroy(gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision == null || collision.collider == null) return;
        // Ignore other projectiles
        if (collision.collider.GetComponent<BulletBehavior>() != null) return;
        // Ignore arena collisions: by layer name or by tag if you prefer
        int arenaLayer = LayerMask.NameToLayer("Arena");
        if (arenaLayer >= 0 && collision.collider.gameObject.layer == arenaLayer) return;
        //if (collision.collider.CompareTag("Arena")) return;

        var enemy = collision.collider.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.TakeDamage(Damage);
            Destroy(gameObject);
            return;
        }

        Destroy(gameObject);
    }
}
