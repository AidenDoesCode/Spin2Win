using System;
using System.Collections.Generic;
using UnityEngine;

// Picks which gate enemies will pour out of this wave. Rolling is triggered
// externally (by RoundContinueUI, when the player presses Start Round) rather
// than automatically, so the wheel spin can gate the actual round start.
public class GateRouletteManager : MonoBehaviour
{
    [Serializable]
    public class Gate
    {
        public string label = "Gate A";
        public Collider2D zone;
        [Min(0f)] public float weight = 1f;
    }

    public static GateRouletteManager Instance { get; private set; }

    [Header("References")]
    public SpawnManager spawnManager;

    [Header("Gates")]
    public List<Gate> gates = new List<Gate>();

    public int ActiveGateIndex { get; private set; } = -1;
    public Gate ActiveGate => (ActiveGateIndex >= 0 && ActiveGateIndex < gates.Count) ? gates[ActiveGateIndex] : null;

    public event Action<int> GateSelected;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (spawnManager == null) spawnManager = FindAnyObjectByType<SpawnManager>();
    }

    public void RollGate()
    {
        var pool = new List<Gate>(gates);
        pool.RemoveAll(g => g == null || g.zone == null || g.weight <= 0f);

        if (pool.Count == 0)
        {
            Debug.LogWarning("GateRouletteManager: no valid gates configured (need a zone and weight > 0).");
            return;
        }

        float totalWeight = 0f;
        foreach (var g in pool) totalWeight += g.weight;

        float roll = UnityEngine.Random.value * totalWeight;
        float cursor = 0f;
        Gate picked = pool[pool.Count - 1];
        foreach (var g in pool)
        {
            cursor += g.weight;
            if (roll <= cursor) { picked = g; break; }
        }

        ActiveGateIndex = gates.IndexOf(picked);

        if (spawnManager != null)
            spawnManager.activeGateOverride = picked.zone;

        GateSelected?.Invoke(ActiveGateIndex);
    }
}
