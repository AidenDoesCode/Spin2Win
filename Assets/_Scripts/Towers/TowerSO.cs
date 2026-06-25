using UnityEngine;

[CreateAssetMenu(menuName = "SpinFort/Tower", fileName = "NewTower")]
public class TowerSO : ScriptableObject
{
    public string towerName = "New Tower";
    [Min(0)] public int cost = 100;
    [Min(0f)] public float range = 4f;
    public Sprite icon;

    [Header("Description")]
    [TextArea(2, 5)]
    public string description;

    [Header("Aiming")]
    [Min(0f)] public float rotationSpeed = 90f;
    [Min(0f)] public float fireRate = 1f;
    [Min(0f)] public float projectileSpeed = 7f;
    [Min(1)] public int damage = 1;

    [Header("Attack Type")]
    [Tooltip("If true, this tower hits its target directly (no projectile spawned) -- for melee towers like Salmon Slapper. projectilePrefab/projectileSpeed are ignored.")]
    public bool isMelee = false;

    public BulletBehavior projectilePrefab;
    public GameObject towerPrefab;

    [Header("Animation")]
    [Tooltip("Looping animation played while this tower is idle/aiming.")]
    public AnimationClip idleAnimation;
    [Tooltip("Plays once each time this tower fires, then returns to idle.")]
    public AnimationClip fireAnimation;
    [Tooltip("Plays once on the enemy when this melee tower hits (e.g. a slap effect). Ignored for ranged towers.")]
    public AnimationClip meleeImpactAnimation;
    [Tooltip("Looping animation played on the projectile while it flies. Ignored for melee towers.")]
    public AnimationClip projectileFlightAnimation;
}