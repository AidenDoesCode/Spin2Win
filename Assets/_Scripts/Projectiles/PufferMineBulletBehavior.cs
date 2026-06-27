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

        // The mine only used to detonate on a direct capsule-collider hit, so
        // a target picked up near the edge of the tower's range -- the
        // longest flight, giving it the most time to walk off the straight
        // line the mine was aimed along -- would sail right past without
        // ever overlapping anything, only exploding (far away, after the
        // fuse) once it was already long gone. Checking proximity every
        // frame lets the mine go off as soon as it's within its own splash
        // radius of any enemy, which is a much larger and more forgiving
        // margin than the bullet's actual collider.
        if (IsAnyEnemyWithinSplashRadius())
        {
            Detonate();
            return;
        }

        fuseTimer -= Time.deltaTime;
        if (fuseTimer <= 0f)
            Detonate();
    }

    private bool IsAnyEnemyWithinSplashRadius()
    {
        var hits = Physics2D.OverlapCircleAll(transform.position, splashRadius);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] != null && hits[i].GetComponent<Enemy>() != null)
                return true;
        }
        return false;
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
