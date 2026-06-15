using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class SpawnManager : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject enemyPrefab;
    public BoxCollider2D arenaCollider; // defines spawn bounds

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

    void Start()
    {
        if ((players == null || players.Length == 0))
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

    // Attempts to spawn one enemy inside the arena. Returns the spawned GameObject or null if failed.
    public GameObject TrySpawnRandom()
    {
        if (enemyPrefab == null || arenaCollider == null) return null;

        var bounds = arenaCollider.bounds;

        for (int attempt = 0; attempt < maxAttemptsPerSpawn; attempt++)
        {
            float x = Random.Range(bounds.min.x, bounds.max.x);
            float y = Random.Range(bounds.min.y, bounds.max.y);
            Vector2 candidate = new Vector2(x, y);

            // ensure a margin so spawn isn't exactly on the box edge
            if (!IsPointInsideCollider(arenaCollider, candidate)) continue;

            // check distance to players
            if (IsTooCloseToPlayers(candidate)) continue;

            // check overlap with obstacles
            if (Physics2D.OverlapCircle(candidate, clearRadius, obstacleMask) != null) continue;

            // passed checks: spawn
            var go = Instantiate(enemyPrefab, candidate, Quaternion.identity);
            return go;
        }

        // failed to find valid spot
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

    // Uses Collider2D.OverlapPoint-like check for BoxCollider2D
    private bool IsPointInsideCollider(BoxCollider2D box, Vector2 point)
    {
        if (box == null) return false;
        // bounds check is sufficient for axis-aligned box
        return box.bounds.Contains(point);
    }

    // Helper to spawn multiple
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
