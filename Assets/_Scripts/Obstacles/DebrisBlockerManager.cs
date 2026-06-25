using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

// Drops a handful of "Casino Blocker" debris piles onto the build grid once,
// at the start of the run. Each tile stays unbuildable until the player
// clicks the debris during a setup/shop phase and pays its clearCost --
// the "spend gold on a wheel spin, or on opening up a great firing spot" choice.
public class DebrisBlockerManager : MonoBehaviour
{
    public static DebrisBlockerManager Instance { get; private set; }

    [Serializable]
    public class DebrisOption
    {
        public string label = "Debris";
        [Min(0f)] public float weight = 1f;
        public GameObject debrisPrefab; // must have a DebrisBlocker component
    }

    [Header("References")]
    public Camera inputCamera;
    [Tooltip("Leave empty to copy TowerPlacementManager's buildArea.")]
    public Collider2D buildArea;
    [Tooltip("Same mask TowerPlacementManager uses to detect towers/base; debris must be placed on one of these layers so it actually blocks placement and is clickable.")]
    public LayerMask blockedMask;
    [Min(0f)] public float clearRadius = 0.4f;
    public float gridSize = 1f;

    [Header("Spawn Count")]
    [Min(0)] public int minDebrisCount = 3;
    [Min(0)] public int maxDebrisCount = 5;

    [Header("Pool")]
    public List<DebrisOption> debrisOptions = new List<DebrisOption>();

    public event Action<DebrisBlocker> DebrisCleared;
    public event Action<DebrisBlocker> ClearFailed;

    private readonly List<DebrisBlocker> activeDebris = new List<DebrisBlocker>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (inputCamera == null) inputCamera = Camera.main;

        if (buildArea == null && TowerPlacementManager.Instance != null)
            buildArea = TowerPlacementManager.Instance.buildArea;

        if (TowerPlacementManager.Instance != null)
            gridSize = TowerPlacementManager.Instance.gridSize;

        SpawnInitialDebris();
    }

    private void Update()
    {
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        TryClearAtMouse();
    }

    private void SpawnInitialDebris()
    {
        if (buildArea == null)
        {
            Debug.LogWarning("DebrisBlockerManager: buildArea is not assigned, skipping debris spawn.");
            return;
        }

        int count = UnityEngine.Random.Range(minDebrisCount, maxDebrisCount + 1);
        var usedPoints = new List<Vector2>();

        for (int i = 0; i < count; i++)
        {
            GameObject prefab = PickRandomPrefab();
            if (prefab == null) break;

            if (!TryFindFreeGridPoint(usedPoints, out Vector2 point))
                continue;

            usedPoints.Add(point);
            SpawnDebrisAt(prefab, point);
        }
    }

    private void SpawnDebrisAt(GameObject prefab, Vector2 point)
    {
        GameObject debrisObj = Instantiate(prefab, point, Quaternion.identity);
        DebrisBlocker blocker = debrisObj.GetComponent<DebrisBlocker>();
        if (blocker == null)
        {
            Debug.LogWarning($"DebrisBlockerManager: prefab '{prefab.name}' has no DebrisBlocker component.");
            Destroy(debrisObj);
            return;
        }

        activeDebris.Add(blocker);
    }

    private GameObject PickRandomPrefab()
    {
        var pool = new List<DebrisOption>(debrisOptions);
        pool.RemoveAll(o => o == null || o.debrisPrefab == null || o.weight <= 0f);
        if (pool.Count == 0) return null;

        float totalWeight = 0f;
        foreach (var o in pool) totalWeight += o.weight;

        float roll = UnityEngine.Random.value * totalWeight;
        float cursor = 0f;
        foreach (var o in pool)
        {
            cursor += o.weight;
            if (roll <= cursor) return o.debrisPrefab;
        }

        return pool[pool.Count - 1].debrisPrefab;
    }

    private bool TryFindFreeGridPoint(List<Vector2> usedPoints, out Vector2 result)
    {
        Bounds bounds = buildArea.bounds;

        for (int attempt = 0; attempt < 30; attempt++)
        {
            Vector2 raw = new Vector2(
                UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
                UnityEngine.Random.Range(bounds.min.y, bounds.max.y));

            Vector2 point = new Vector2(
                Mathf.Round(raw.x / gridSize) * gridSize,
                Mathf.Round(raw.y / gridSize) * gridSize);

            if (!buildArea.OverlapPoint(point)) continue;
            if (Physics2D.OverlapCircle(point, clearRadius, blockedMask) != null) continue;

            bool tooClose = false;
            foreach (Vector2 used in usedPoints)
            {
                if (Vector2.Distance(used, point) < gridSize * 0.5f) { tooClose = true; break; }
            }
            if (tooClose) continue;

            result = point;
            return true;
        }

        result = Vector2.zero;
        return false;
    }

    private void TryClearAtMouse()
    {
        if (inputCamera == null) return;

        Vector3 mousePos = Mouse.current.position.ReadValue();
        float camZ = -inputCamera.transform.position.z;
        Vector3 world = inputCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, camZ));

        Collider2D hit = Physics2D.OverlapPoint(world, blockedMask);
        if (hit == null) return;

        DebrisBlocker blocker = hit.GetComponent<DebrisBlocker>();
        if (blocker == null) return;

        if (blocker.TryClear())
        {
            activeDebris.Remove(blocker);
            DebrisCleared?.Invoke(blocker);
        }
        else
        {
            ClearFailed?.Invoke(blocker);
        }
    }
}
