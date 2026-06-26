using UnityEngine;

[CreateAssetMenu(menuName = "SpinFort/Tower", fileName = "NewTower")]
public class TowerSO : ScriptableObject
{
    public string towerName = "New Tower";
    [Tooltip("Drives buy cost: Common 15, Uncommon 35, Rare 75, Epic 130, Legendary 220.")]
    public CardRarity rarity = CardRarity.Common;
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

    // ADDED: screen-shake-on-impact settings, handed down to the projectile
    // (Tower.FireAt) and triggered there on hit/explosion.
    [Header("Polish - Screen Shake")]
    [Tooltip("Triggers a camera shake when this tower's projectile hits/explodes (Bass Cannon, Puffer Hurler, etc).")]
    public bool screenShakeOnImpact = false;
    public float shakeDuration = 0.1f;
    public float shakeMagnitude = 0.05f;

    [Header("Visual")]
    [Tooltip("Scales up the whole tower (sprite + firePoint + collider) for visibility. The placement ghost preview reads this too, so it matches the placed tower's actual size.")]
    public float visualScaleMultiplier = 3f;
    [Tooltip("Projectile visual scale, relative to this tower's own visualScaleMultiplier (0.5 = half the tower's size).")]
    public float projectileScaleRatio = 0.5f;

    [Header("Audio")]
    [Tooltip("Plays when this tower fires/attacks. For melee towers (isMelee) this doubles as the hit-impact sound since the attack lands instantly.")]
    public AudioClip fireSound;
    [Range(0f, 3f)] public float fireVolume = 1f;
    [Tooltip("Ranged towers only: an extra sound when the projectile actually hits an enemy (e.g. an explosion), played on top of fireSound.")]
    public AudioClip impactSound;
    [Range(0f, 3f)] public float impactVolume = 1f;

    [Header("Animation")]
    [Tooltip("Looping animation played while this tower is idle/aiming.")]
    public AnimationClip idleAnimation;
    [Tooltip("Plays once each time this tower fires, then returns to idle.")]
    public AnimationClip fireAnimation;
    [Tooltip("Plays once on the enemy when this melee tower hits (e.g. a slap effect). Ignored for ranged towers.")]
    public AnimationClip meleeImpactAnimation;
    [Tooltip("Looping animation played on the projectile while it flies. Ignored for melee towers.")]
    public AnimationClip projectileFlightAnimation;

    public static int CostForRarity(CardRarity rarity)
    {
        switch (rarity)
        {
            case CardRarity.Uncommon:  return 35;
            case CardRarity.Rare:      return 75;
            case CardRarity.Epic:      return 130;
            case CardRarity.Legendary: return 220;
            default:                   return 15;
        }
    }

#if UNITY_EDITOR
    // Keeps cost in lockstep with rarity in the editor so designers can't
    // accidentally drift the two apart.
    private void OnValidate()
    {
        cost = CostForRarity(rarity);
    }
#endif
}