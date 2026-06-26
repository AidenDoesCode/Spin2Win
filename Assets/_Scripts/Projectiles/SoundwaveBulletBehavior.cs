using UnityEngine;

// Heavy bass shockwave that pierces through multiple enemies before fading out.
public class SoundwaveBulletBehavior : BulletBehavior
{
    [Tooltip("How many enemies this shockwave can pass through. 0 = unlimited until lifetime ends.")]
    public int maxPierceCount = 0;

    [Tooltip("Seconds before the shockwave dissipates.")]
    public float lifetime = 2f;

    private float lifeTimer;
    private int pierceCount;
    private readonly System.Collections.Generic.HashSet<Enemy> hitEnemies = new();

    protected override void Start()
    {
        lifeTimer = lifetime;
        base.Start();
    }

    protected override void Update()
    {
        base.Update();

        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0f)
            DestroyProjectile();
    }

    protected override void OnHitEnemy(Enemy enemy)
    {
        if (enemy == null || hitEnemies.Contains(enemy)) return;

        hitEnemies.Add(enemy);
        enemy.TakeDamage(Damage);
        pierceCount++;

        if (pierceCount == 1) TriggerShake(); // ADDED: shake once per wave, on its first hit

        if (maxPierceCount > 0 && pierceCount >= maxPierceCount)
            DestroyProjectile();
    }
}
