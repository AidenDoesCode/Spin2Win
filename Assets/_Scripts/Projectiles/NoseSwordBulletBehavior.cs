using UnityEngine;

// High-velocity nose-sword dart — single target, destroys on first hit.
public class NoseSwordBulletBehavior : BulletBehavior
{
    protected override void OnHitEnemy(Enemy enemy)
    {
        if (enemy == null) return;
        enemy.TakeDamage(Damage);
        DestroyProjectile();
    }
}
