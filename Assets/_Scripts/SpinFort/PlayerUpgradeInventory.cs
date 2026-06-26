using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerUpgradeInventory : MonoBehaviour
{
    public static PlayerUpgradeInventory Instance { get; private set; }

    [Tooltip("Max unused upgrade cards the player can hold at once.")]
    public int maxSlots = 6;

    public List<ShopCardSO> ownedUpgrades = new List<ShopCardSO>();

    public bool IsFull => ownedUpgrades.Count >= maxSlots;

    public event Action InventoryChanged;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public bool AddUpgrade(ShopCardSO card)
    {
        if (card == null) return false;
        if (IsFull) return false;
        ownedUpgrades.Add(card);
        InventoryChanged?.Invoke();
        return true;
    }

    public void RemoveUpgrade(ShopCardSO card)
    {
        if (ownedUpgrades.Remove(card))
            InventoryChanged?.Invoke();
    }
}
