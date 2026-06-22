using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Drops random obstacles into the arena while a wave is active (Defend & Survive),
// forcing the player to adapt mid-combat. Stops and clears between rounds.
public class ObstacleSpawner : MonoBehaviour
{
    [Serializable]
    public class ObstacleOption
    {
        public string label = "Obstacle";
        [Min(0f)] public float weight = 1f;
        public GameObject obstaclePrefab; // must have an Obstacle component
    }

    [Header("References")]
    public RoundManager roundManager;
    public Collider2D spawnArea;
    [Tooltip("Avoid spawning on top of towers, the base, or other obstacles.")]
    public LayerMask blockedMask;
    [Min(0f)] public float clearRadius = 0.5f;

    [Header("Timing")]
    [Min(0.1f)] public float minInterval = 4f;
    [Min(0.1f)] public float maxInterval = 9f;

    [Header("Pool")]
    public List<ObstacleOption> obstacleOptions = new List<ObstacleOption>();

    public event Action<GameObject> ObstacleSpawned;

    private readonly List<GameObject> activeObstacles = new List<GameObject>();
    private Coroutine spawnRoutine;

    private void Start()
    {
        if (roundManager == null) roundManager = FindAnyObjectByType<RoundManager>();

        if (roundManager != null)
        {
            roundManager.RoundStarted += HandleRoundStarted;
            roundManager.RoundFinished += HandleRoundFinished;
        }
    }

    private void OnDestroy()
    {
        if (roundManager != null)
        {
            roundManager.RoundStarted -= HandleRoundStarted;
            roundManager.RoundFinished -= HandleRoundFinished;
        }
    }

    private void HandleRoundStarted(int round)
    {
        if (spawnRoutine != null) StopCoroutine(spawnRoutine);
        spawnRoutine = StartCoroutine(SpawnLoop());
    }

    private void HandleRoundFinished(int round)
    {
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }

        foreach (GameObject obstacle in activeObstacles)
            if (obstacle != null) Destroy(obstacle);
        activeObstacles.Clear();
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            float wait = UnityEngine.Random.Range(minInterval, maxInterval);
            yield return new WaitForSeconds(wait);

            if (roundManager != null && !roundManager.IsRoundActive())
                yield break;

            TrySpawnObstacle();
        }
    }

    private void TrySpawnObstacle()
    {
        if (spawnArea == null)
        {
            Debug.LogWarning("ObstacleSpawner: spawnArea is not assigned, skipping spawn.");
            return;
        }

        GameObject prefab = PickRandomPrefab();
        if (prefab == null)
        {
            Debug.LogWarning("ObstacleSpawner: no obstacleOptions with weight > 0 and a valid prefab, skipping spawn.");
            return;
        }

        Vector2 point = GetRandomPointInArea();
        if (Physics2D.OverlapCircle(point, clearRadius, blockedMask) != null)
            return; // crowded spot, just skip this tick and try again next interval

        GameObject obstacle = Instantiate(prefab, point, Quaternion.identity);
        activeObstacles.Add(obstacle);
        ObstacleSpawned?.Invoke(obstacle);
    }

    private GameObject PickRandomPrefab()
    {
        var pool = new List<ObstacleOption>(obstacleOptions);
        pool.RemoveAll(o => o == null || o.obstaclePrefab == null || o.weight <= 0f);
        if (pool.Count == 0) return null;

        float totalWeight = 0f;
        foreach (var o in pool) totalWeight += o.weight;

        float roll = UnityEngine.Random.value * totalWeight;
        float cursor = 0f;
        foreach (var o in pool)
        {
            cursor += o.weight;
            if (roll <= cursor) return o.obstaclePrefab;
        }

        return pool[pool.Count - 1].obstaclePrefab;
    }

    private Vector2 GetRandomPointInArea()
    {
        Bounds bounds = spawnArea.bounds;
        for (int attempt = 0; attempt < 10; attempt++)
        {
            Vector2 point = new Vector2(
                UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
                UnityEngine.Random.Range(bounds.min.y, bounds.max.y));
            if (spawnArea.OverlapPoint(point)) return point;
        }

        return bounds.center;
    }
}
