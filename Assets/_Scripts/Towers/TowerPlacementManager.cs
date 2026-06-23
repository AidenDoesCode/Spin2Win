using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class TowerPlacementManager : MonoBehaviour
{
public static TowerPlacementManager Instance { get; private set; }

    // Fired whenever a slot's TowerSO reference changes, so LoadoutBarUI can
    // refresh that one slot's icon without polling the whole array every frame.
    public event Action<int> SlotChanged;


    [Header("Placement")]
    public Camera placementCamera;
    public Collider2D buildArea;
    public LayerMask blockedMask;
    public Transform towerParent;
    public float gridSize = 1f;
    public float blockingRadius = 0.35f;

    [Header("Loadout Slots (1-5)")]
    public TowerSO[] loadout = new TowerSO[5];
    public int selectedSlot = 0;

    [Tooltip("-1 = no slot locked. Set by the 'Locked Slot' shop modifier; that slot can't be selected, assigned, or placed from.")]
    public int lockedSlotIndex = -1;


    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    private void Start()
    {
        if (placementCamera == null)
            placementCamera = Camera.main;
    }

    private void Update()
    {
        HandleSlotSelection();

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            TryPlaceSelectedTower();
    }

    public void SelectSlot(int index)
    {
        if (index < 0 || index >= loadout.Length) return;
        if (index == lockedSlotIndex) return;
        selectedSlot = index;
    }

    // Called by the UI when the player drags a tower into a slot
    public bool AssignTowerToSlot(TowerSO tower, int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= loadout.Length) return false;
        if (slotIndex == lockedSlotIndex) return false;
        loadout[slotIndex] = tower;
        Debug.Log($"TowerPlacementManager: Slot {slotIndex + 1} assigned {(tower != null ? tower.towerName : "empty")}");
        SlotChanged?.Invoke(slotIndex);
        return true;
    }

    // Called by SpinFortShopManager whenever the shop spins, to freeze a
    // random hand slot for the upcoming wave ("Locked Slot" modifier).
    public void LockRandomSlot()
    {
        lockedSlotIndex = UnityEngine.Random.Range(0, loadout.Length);

        if (selectedSlot == lockedSlotIndex)
        {
            for (int i = 0; i < loadout.Length; i++)
            {
                if (i == lockedSlotIndex) continue;
                selectedSlot = i;
                break;
            }
        }
    }

    public void ClearLockedSlot() => lockedSlotIndex = -1;

    // Removes whatever tower occupies a slot, bypassing the lock check.
    // Used when recycling a tower (including one stuck in the locked slot).
    public void ClearSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= loadout.Length) return;
        loadout[slotIndex] = null;
        SlotChanged?.Invoke(slotIndex);
    }

    private void HandleSlotSelection()
    {
        if (Keyboard.current == null) return;
        if (Keyboard.current.digit1Key.wasPressedThisFrame) SelectSlot(0);
        if (Keyboard.current.digit2Key.wasPressedThisFrame) SelectSlot(1);
        if (Keyboard.current.digit3Key.wasPressedThisFrame) SelectSlot(2);
        if (Keyboard.current.digit4Key.wasPressedThisFrame) SelectSlot(3);
        if (Keyboard.current.digit5Key.wasPressedThisFrame) SelectSlot(4);
    }

    public bool TryPlaceSelectedTower()
    {
        // Block if mouse is over UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return false;

        if (placementCamera == null) return false;

        if (RoundManager.Instance != null && RoundManager.Instance.IsRoundActive())
            return false;

        if (selectedSlot == lockedSlotIndex) return false;

        TowerSO selected = loadout[selectedSlot];
        if (selected == null || selected.towerPrefab == null) return false;

        // Card consumption: placing a tower spends one owned copy of it. With
        // none left, the slot can't place again until another is bought.
        if (PlayerTowerInventory.Instance == null || !PlayerTowerInventory.Instance.HasTower(selected))
            return false;

        Vector2 worldPos = GetSnappedMouseWorldPosition();

        if (!IsInsideBuildArea(worldPos)) return false;

        Collider2D hit = Physics2D.OverlapCircle(worldPos, blockingRadius, blockedMask);
        if (hit != null) return false;

        // Placing from inventory is free -- the gold was already spent buying
        // this tower card from the shop, per the game's "buy the gamble, not the placement" rule.
        GameObject towerObj = Instantiate(selected.towerPrefab, worldPos, Quaternion.identity, towerParent);
        Tower tower = towerObj.GetComponent<Tower>();
        if (tower == null)
        {
            Destroy(towerObj);
            return false;
        }

        tower.Configure(selected);

        PlayerTowerInventory.Instance.RemoveTower(selected);
        if (!PlayerTowerInventory.Instance.HasTower(selected))
        {
            loadout[selectedSlot] = null;
            SlotChanged?.Invoke(selectedSlot);
        }

        return true;
    }

    private Vector2 GetSnappedMouseWorldPosition()
    {
        Vector3 mousePos = Mouse.current != null ? (Vector3)Mouse.current.position.ReadValue() : Vector3.zero;
        float camZ = -placementCamera.transform.position.z;
        Vector3 world = placementCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, camZ));
        return new Vector2(
            Mathf.Round(world.x / gridSize) * gridSize,
            Mathf.Round(world.y / gridSize) * gridSize
        );
    }

    private bool IsInsideBuildArea(Vector2 position)
    {
        if (buildArea == null) return true;
        return buildArea.OverlapPoint(position);
    }
}