using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class SpawnManager : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject enemyPrefab;
    public BoxCollider2D arenaCollider;

    [Tooltip("When set (e.g. by GateRouletteManager), spawning is restricted to this zone instead of arenaCollider.")]
    public Collider2D activeGateOverride;

    [Tooltip("Minimum distance from any player to spawn")]
    public float minDistanceFromPlayer = 3f;

    [Tooltip("Layers considered obstacles when spawning")]
    public LayerMask obstacleMask;

    [Tooltip("Radius used to ensure spawn area is clear")]
    public float clearRadius = 0.5f;

    [Tooltip("How many attempts to try before giving up on a single spawn")]
    public int maxAttemptsPerSpawn = 30;

    [Header("Auto Spawn")]
    public bool autoSpawn = false;
    [Tooltip("Seconds between spawns when autoSpawn is enabled")]
    public float spawnInterval = 2f;

    [Header("Players")]
    [Tooltip("If empty, the script will try to find objects tagged 'Player'")]
    public Transform[] players;

    private void Start()
    {
        if (players == null || players.Length == 0)
        {
            var playerObjs = GameObject.FindGameObjectsWithTag("Player");
            if (playerObjs != null && playerObjs.Length > 0)
            {
                players = new Transform[playerObjs.Length];
                for (int i = 0; i < playerObjs.Length; i++) players[i] = playerObjs[i].transform;
            }
        }

        if (autoSpawn)
            StartCoroutine(AutoSpawnRoutine());
    }

    public IEnumerator AutoSpawnRoutine()
    {
        while (autoSpawn)
        {
            TrySpawnRandom();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    public GameObject TrySpawnRandom()
    {
        Collider2D spawnCollider = activeGateOverride != null ? activeGateOverride : arenaCollider;
        if (enemyPrefab == null || spawnCollider == null) return null;

        var bounds = spawnCollider.bounds;

        for (int attempt = 0; attempt < maxAttemptsPerSpawn; attempt++)
        {
            float x = Random.Range(bounds.min.x, bounds.max.x);
            float y = Random.Range(bounds.min.y, bounds.max.y);
            Vector2 candidate = new Vector2(x, y);

            if (!spawnCollider.OverlapPoint(candidate)) continue;

            if (IsTooCloseToPlayers(candidate)) continue;

            if (Physics2D.OverlapCircle(candidate, clearRadius, obstacleMask) != null) continue;

            var go = Instantiate(enemyPrefab, candidate, Quaternion.identity);
            return go;
        }

        return null;
    }

    private bool IsTooCloseToPlayers(Vector2 point)
    {
        if (players == null) return false;
        foreach (var p in players)
        {
            if (p == null) continue;
            if (Vector2.Distance(point, p.position) < minDistanceFromPlayer) return true;
        }
        return false;
    }

    public int SpawnMultiple(int count)
    {
        int spawned = 0;
        for (int i = 0; i < count; i++)
        {
            if (TrySpawnRandom() != null) spawned++;
        }
        return spawned;
    }
}
