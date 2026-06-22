using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerTowerInventory : MonoBehaviour
{
    public static PlayerTowerInventory Instance { get; private set; }

    public List<TowerSO> ownedTowers = new List<TowerSO>();

    public event Action InventoryChanged;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void AddTower(TowerSO tower)
    {
        if (tower == null) return;
        ownedTowers.Add(tower);
        InventoryChanged?.Invoke();
        Debug.Log($"PlayerTowerInventory: Added {tower.towerName}");
    }

    public void RemoveTower(TowerSO tower)
    {
        if (ownedTowers.Remove(tower))
            InventoryChanged?.Invoke();
    }

    public bool HasTower(TowerSO tower) => ownedTowers.Contains(tower);
}