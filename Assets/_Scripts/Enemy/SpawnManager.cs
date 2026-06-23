using System.Collections;
using System.Collections.Generic;
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

    [Header("Enemy Pool (Point Budget)")]
    [Tooltip("Enemy types RoundManager can draw from when generating a budget-based wave.")]
    public List<EnemySO> enemyPool = new List<EnemySO>();

    [Tooltip("Chance each 0-cost enemy (e.g. Goldfish Gambler) gets added once per wave, independent of the point budget.")]
    [Range(0f, 1f)] public float bonusSpawnChance = 0.08f;

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

        var pathfinder = EnemyPathfinder.Instance;
        if (pathfinder != null) return TrySpawnOnPath(spawnCollider, pathfinder);

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

    // Only spawn enemies on path tiles that fall inside the active gate's zone,
    // so they always start lined up on the route instead of on open ground.
    private GameObject TrySpawnOnPath(Collider2D spawnCollider, EnemyPathfinder pathfinder)
    {
        var candidates = new List<Vector3Int>();
        foreach (var cell in pathfinder.PathCells)
        {
            if (spawnCollider.OverlapPoint(pathfinder.GetCellWorldCenter(cell)))
                candidates.Add(cell);
        }

        if (candidates.Count == 0) return null;

        int attempts = Mathf.Min(maxAttemptsPerSpawn, candidates.Count);
        for (int attempt = 0; attempt < attempts; attempt++)
        {
            int index = Random.Range(0, candidates.Count);
            Vector3 point = pathfinder.GetCellWorldCenter(candidates[index]);
            candidates.RemoveAt(index);

            if (IsTooCloseToPlayers(point)) continue;
            if (Physics2D.OverlapCircle(point, clearRadius, obstacleMask) != null) continue;

            return Instantiate(enemyPrefab, point, Quaternion.identity);
        }

        return null;
    }

    // Spawns the shared enemy prefab and configures it as a specific EnemySO type.
    public GameObject TrySpawnEnemy(EnemySO enemyData)
    {
        GameObject go = TrySpawnRandom();
        if (go == null) return null;

        if (enemyData != null)
        {
            Enemy enemy = go.GetComponent<Enemy>();
            if (enemy != null) enemy.Configure(enemyData);
        }

        return go;
    }

    // Spends a fixed point budget across the enemy pool, picking randomly until
    // the budget runs out. Cheap units (Minnows) fill in whatever a single
    // expensive pick (Anchor Shark) doesn't use.
    public List<EnemySO> GenerateWave(int roundBudget)
    {
        var enemiesToSpawn = new List<EnemySO>();
        var pool = enemyPool.FindAll(e => e != null && e.pointCost > 0);

        if (pool.Count > 0 && roundBudget > 0)
        {
            // Gatekeeper rule: nothing costing more than half the round's starting
            // budget is allowed in, so one round can't blow its whole budget on
            // a single heavy unit before the budget can comfortably support it.
            int gatekeeperCap = roundBudget / 2;
            var affordablePool = pool.FindAll(e => e.pointCost <= gatekeeperCap);
            if (affordablePool.Count == 0) affordablePool = pool;

            int minCost = int.MaxValue;
            foreach (var e in affordablePool) minCost = Mathf.Min(minCost, e.pointCost);

            while (roundBudget >= minCost)
            {
                EnemySO candidate = affordablePool[Random.Range(0, affordablePool.Count)];
                if (candidate.pointCost > roundBudget) continue; // too pricey for what's left; spin again

                enemiesToSpawn.Add(candidate);
                roundBudget -= candidate.pointCost;
            }
        }

        // 0-cost enemies (e.g. Goldfish Gambler) never enter the budget loop --
        // a 0-cost pick would never reduce roundBudget and could spin forever.
        // Instead each one rolls independently as a pure bonus spawn.
        var bonusPool = enemyPool.FindAll(e => e != null && e.pointCost == 0);
        foreach (var bonus in bonusPool)
        {
            if (Random.value <= bonusSpawnChance) enemiesToSpawn.Add(bonus);
        }

        return enemiesToSpawn;
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
