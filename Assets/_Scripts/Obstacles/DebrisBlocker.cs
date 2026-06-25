using System;
using UnityEngine;

// Static map clutter present on the build grid from the start of the run.
// Blocks tower placement on its tile (via TowerPlacementManager.blockedMask)
// until the player pays to clear it through DebrisBlockerManager during a
// setup/shop phase.
public class DebrisBlocker : MonoBehaviour
{
    [Tooltip("Gold cost to clear this debris and free up the tile.")]
    [Min(0)] public int clearCost = 50;

    public event Action Cleared;

    // Attempts to spend gold and remove this blocker. Fails (no gold spent)
    // if a round is currently active or the player can't afford the cost.
    public bool TryClear()
    {
        if (RoundManager.Instance != null && RoundManager.Instance.IsRoundActive())
            return false;

        if (ScoreManager.Instance == null || !ScoreManager.Instance.TrySpendScore(clearCost))
            return false;

        Cleared?.Invoke();
        Destroy(gameObject);
        return true;
    }
}
