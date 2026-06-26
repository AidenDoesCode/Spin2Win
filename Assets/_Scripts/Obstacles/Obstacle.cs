using System.Collections.Generic;
using UnityEngine;

public class Obstacle : MonoBehaviour
{
    public ObstacleEffectType effectType = ObstacleEffectType.PhysicalBlock;

    [Tooltip("Seconds before this obstacle removes itself. 0 = lasts until the round ends.")]
    [Min(0f)] public float lifetime = 8f;

    [Header("Slow Zone")]
    [Range(0f, 1f)] public float slowMultiplier = 0.4f;

    [Header("Damage Zone")]
    [Min(0)] public int damagePerTick = 1;
    [Min(0.05f)] public float damageTickInterval = 1f;

    [Header("Audio")]
    [Tooltip("Chest-crack / glass-shatter sound played whenever this obstacle clears (lifetime expiry or round-end cleanup).")]
    public AudioClip destroyedSound;
    [Range(0f, 3f)] public float destroyedVolume = 1f;

    private readonly HashSet<Enemy> affectedEnemies = new HashSet<Enemy>();
    private readonly Dictionary<Enemy, float> nextDamageTime = new Dictionary<Enemy, float>();

    private void Awake()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.isTrigger = effectType != ObstacleEffectType.PhysicalBlock;
    }

    private void Start()
    {
        if (lifetime > 0f) Destroy(gameObject, lifetime);
    }

    private void OnDestroy()
    {
        foreach (Enemy enemy in affectedEnemies)
            if (enemy != null) enemy.ClearObstacleSlow();

        if (destroyedSound != null)
            AudioSource.PlayClipAtPoint(destroyedSound, transform.position, destroyedVolume * SfxSettings.Volume);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (effectType != ObstacleEffectType.SlowZone) return;

        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy == null) return;

        enemy.ApplyObstacleSlow(slowMultiplier);
        affectedEnemies.Add(enemy);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (effectType != ObstacleEffectType.SlowZone) return;

        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy == null) return;

        enemy.ClearObstacleSlow();
        affectedEnemies.Remove(enemy);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (effectType != ObstacleEffectType.DamageZone) return;

        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy == null) return;

        if (nextDamageTime.TryGetValue(enemy, out float next) && Time.time < next) return;

        enemy.TakeDamage(damagePerTick);
        nextDamageTime[enemy] = Time.time + damageTickInterval;
    }
}
