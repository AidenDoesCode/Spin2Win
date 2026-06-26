using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerTowerInventory : MonoBehaviour
{
    public static PlayerTowerInventory Instance { get; private set; }

    [Tooltip("Max unplaced tower cards the player can hold at once.")]
    public int maxSlots = 4;

    public List<TowerSO> ownedTowers = new List<TowerSO>();

    public bool IsFull => ownedTowers.Count >= maxSlots;

    public event Action InventoryChanged;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public bool AddTower(TowerSO tower)
    {
        if (tower == null) return false;
        if (IsFull) return false;
        ownedTowers.Add(tower);
        InventoryChanged?.Invoke();
        Debug.Log($"PlayerTowerInventory: Added {tower.towerName}");
        return true;
    }

    public void RemoveTower(TowerSO tower)
    {
        if (ownedTowers.Remove(tower))
            InventoryChanged?.Invoke();
    }

    public bool HasTower(TowerSO tower) => ownedTowers.Contains(tower);
}