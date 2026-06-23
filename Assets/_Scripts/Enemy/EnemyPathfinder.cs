using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// Builds a flow field over the painted path Tilemap by running a single BFS
// from the base outward. Each path cell ends up pointing at the neighbor one
// step closer to the base, so any enemy can look up "which way do I go next"
// in O(1) regardless of which gate it spawned from.
[DefaultExecutionOrder(-100)]
public class EnemyPathfinder : MonoBehaviour
{
    public static EnemyPathfinder Instance { get; private set; }

    [Header("References")]
    public Tilemap pathTilemap;
    [Tooltip("Leave empty to use BaseHealth.Instance automatically")]
    public Transform baseTarget;

    private readonly Dictionary<Vector3Int, int> distanceToBase = new Dictionary<Vector3Int, int>();
    private readonly Dictionary<Vector3Int, Vector3Int> nextStep = new Dictionary<Vector3Int, Vector3Int>();
    private readonly List<Vector3Int> pathCells = new List<Vector3Int>();
    private Vector3Int baseCell;

    private static readonly Vector3Int[] Neighbors4 =
    {
        new Vector3Int(1, 0, 0),
        new Vector3Int(-1, 0, 0),
        new Vector3Int(0, 1, 0),
        new Vector3Int(0, -1, 0),
    };

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (baseTarget == null && BaseHealth.Instance != null)
            baseTarget = BaseHealth.Instance.transform;

        BuildFlowField();
    }

    public IReadOnlyList<Vector3Int> PathCells => pathCells;

    public Vector3 GetCellWorldCenter(Vector3Int cell) => pathTilemap.GetCellCenterWorld(cell);

    public void BuildFlowField()
    {
        distanceToBase.Clear();
        nextStep.Clear();
        pathCells.Clear();

        if (pathTilemap == null || baseTarget == null)
        {
            Debug.LogWarning("EnemyPathfinder: missing pathTilemap or baseTarget; enemies will fall back to moving straight at the base.");
            return;
        }

        foreach (var cell in pathTilemap.cellBounds.allPositionsWithin)
        {
            if (pathTilemap.HasTile(cell)) pathCells.Add(cell);
        }

        baseCell = FindNearestPathCell(pathTilemap.WorldToCell(baseTarget.position));

        var queue = new Queue<Vector3Int>();
        distanceToBase[baseCell] = 0;
        queue.Enqueue(baseCell);

        while (queue.Count > 0)
        {
            var cell = queue.Dequeue();
            int dist = distanceToBase[cell];

            foreach (var offset in Neighbors4)
            {
                var neighbor = cell + offset;
                if (!pathTilemap.HasTile(neighbor)) continue;
                if (distanceToBase.ContainsKey(neighbor)) continue;

                distanceToBase[neighbor] = dist + 1;
                nextStep[neighbor] = cell;
                queue.Enqueue(neighbor);
            }
        }
    }

    // Returns the world-space center of the next cell an enemy at worldPosition
    // should walk toward. Once the enemy's cell IS the base cell, this returns
    // the base's exact position so the last stretch isn't grid-snapped.
    public Vector3 GetNextWaypoint(Vector3 worldPosition)
    {
        if (pathTilemap == null || baseTarget == null)
            return baseTarget != null ? baseTarget.position : worldPosition;

        var cell = pathTilemap.WorldToCell(worldPosition);

        if (cell == baseCell) return baseTarget.position;

        if (!nextStep.TryGetValue(cell, out var next))
        {
            cell = FindNearestPathCell(cell);
            if (cell == baseCell) return baseTarget.position;
            if (!nextStep.TryGetValue(cell, out next)) return baseTarget.position;
        }

        return pathTilemap.GetCellCenterWorld(next);
    }

    private Vector3Int FindNearestPathCell(Vector3Int from)
    {
        if (pathCells.Count == 0) return from;

        var nearest = pathCells[0];
        int bestSqrDist = int.MaxValue;
        foreach (var cell in pathCells)
        {
            int dx = cell.x - from.x;
            int dy = cell.y - from.y;
            int sqrDist = dx * dx + dy * dy;
            if (sqrDist < bestSqrDist)
            {
                bestSqrDist = sqrDist;
                nearest = cell;
            }
        }
        return nearest;
    }
}
