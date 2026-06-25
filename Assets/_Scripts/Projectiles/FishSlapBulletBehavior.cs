using UnityEngine;

// Invisible short-range hitbox for the Salmon Slapper. The tower uses melee
// by default, but this prefab can still be assigned on the Tower SO if needed.
public class FishSlapBulletBehavior : BulletBehavior
{
    [Tooltip("Max distance this invisible slap travels before despawning.")]
    public float maxTravelDistance = 1.5f;

    private Vector3 spawnPosition;

    protected override void Awake()
    {
        base.Awake();
        foreach (var sr in GetComponentsInChildren<SpriteRenderer>(true))
            sr.enabled = false;
    }

    protected override void Start()
    {
        spawnPosition = transform.position;
        base.Start();
    }

    protected override void Update()
    {
        base.Update();

        if ((transform.position - spawnPosition).sqrMagnitude >= maxTravelDistance * maxTravelDistance)
            DestroyProjectile();
    }

    protected override void OnHitEnemy(Enemy enemy)
    {
        if (enemy == null) return;
        enemy.TakeDamage(Damage);
        DestroyProjectile();
    }
}
