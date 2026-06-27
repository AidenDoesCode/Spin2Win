using UnityEngine;

// Direct-flying puffer mine that splashes damage to all enemies in a radius
// on impact -- flies straight like every other tower's projectile (inherits
// BulletBehavior's Start/FixedUpdate unmodified) instead of lobbing an arc.
public class PufferMineBulletBehavior : BulletBehavior
{
    [Tooltip("Radius of the splash damage when the mine lands or hits an enemy.")]
    public float splashRadius = 1.25f;

    [Tooltip("Seconds before the mine detonates even if nothing is hit.")]
    public float fuseTime = 4f;

    private float fuseTimer;

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
