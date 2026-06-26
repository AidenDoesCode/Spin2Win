using UnityEngine;

// Slow arcing puffer clone that splashes damage to all enemies in a radius on impact.
public class PufferMineBulletBehavior : BulletBehavior
{
    [Tooltip("Radius of the splash damage when the mine lands or hits an enemy.")]
    public float splashRadius = 1.25f;

    [Tooltip("Downward pull applied for a lobbed arc.")]
    public float arcGravity = 2f;

    [Tooltip("Seconds before the mine detonates even if nothing is hit.")]
    public float fuseTime = 4f;

    private float fuseTimer;

    protected override void Awake()
    {
        base.Awake();
        rb.gravityScale = arcGravity;
    }

    protected override void Start()
    {
        fuseTimer = fuseTime;
        base.Start();
    }

    protected override void Update()
    {
        base.Update();

        fuseTimer -= Time.deltaTime;
        if (fuseTimer <= 0f)
            Detonate();
    }

    protected override void FixedUpdate()
    {
        if (rb == null) return;

        Vector2 velocity = rb.linearVelocity;
        if (velocity.sqrMagnitude > 0f)
            rb.linearVelocity = velocity.normalized * Speed;
        else if (velocity.sqrMagnitude <= 0f && rb.gravityScale > 0f)
            rb.linearVelocity = Vector2.down * Speed * 0.25f;
    }

    protected override void OnHitEnemy(Enemy enemy)
    {
        Detonate();
    }

    private void Detonate()
    {
        var hits = Physics2D.OverlapCircleAll(transform.position, splashRadius);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null) continue;
            var hitEnemy = hits[i].GetComponent<Enemy>();
            if (hitEnemy != null)
                hitEnemy.TakeDamage(Damage);
        }

        TriggerShake(); // ADDED
        DestroyProjectile();
    }
}
