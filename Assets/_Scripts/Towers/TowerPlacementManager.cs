using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;

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
    [Tooltip("Optional -- if assigned (e.g. the map's tilemap Grid), snapping uses this Grid's actual cell layout so placement lines up exactly with the tile art. Falls back to gridSize math if left empty.")]
    public Grid grid;
    public float gridSize = 1f;
    public float blockingRadius = 0.35f;

    [Header("Tile Restrictions")]
    [Tooltip("If assigned, a tile must exist here (e.g. your Ground tilemap) for a cell to be placeable.")]
    public Tilemap groundTilemap;
    [Tooltip("If assigned, any cell with a tile here (e.g. your Path tilemap) can never be built on, even if it also has a ground tile.")]
    public Tilemap pathTilemap;

    [Header("Manual Rotation")]
    [Tooltip("Degrees the placement preview rotates by each time R is pressed.")]
    public float manualRotationStep = 20f;

    [Header("Placement Preview")]
    [Tooltip("Alpha of the ghost sprite shown under the cursor while placing.")]
    [Range(0f, 1f)] public float ghostAlpha = 0.5f;
    public Color highlightValidColor = new Color(0.3f, 1f, 0.3f, 0.5f);
    public Color highlightInvalidColor = new Color(1f, 0.3f, 0.3f, 0.5f);
    public int previewSortingOrder = 50;

    [Header("Loadout Slots (1-5)")]
    public TowerSO[] loadout = new TowerSO[5];
    public int selectedSlot = 0;

    [Tooltip("-1 = no slot locked. Set by the 'Locked Slot' shop modifier; that slot can't be selected, assigned, or placed from.")]
    public int lockedSlotIndex = -1;

    // Rotation (in degrees) the player has dialed in for the tower about to be
    // placed; reset to 0 whenever a different slot is selected.
    private float pendingRotation = 0f;

    private SpriteRenderer ghostRenderer;
    private Transform ghostTransform;
    private SpriteRenderer highlightRenderer;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        BuildPreviewObjects();
    }
    private void Start()
    {
        if (placementCamera == null)
            placementCamera = Camera.main;
        if (grid == null)
            grid = FindAnyObjectByType<Grid>();
    }

    private void Update()
    {
        HandleSlotSelection();
        HandleManualRotation();
        UpdatePlacementPreview();

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            TryPlaceSelectedTower();
    }

    public void SelectSlot(int index)
    {
        if (index < 0 || index >= loadout.Length) return;
        if (index == lockedSlotIndex) return;
        if (selectedSlot != index) pendingRotation = 0f;
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

    // Lets the player dial in which way the tower's FirePoint faces before it's
    // committed to the grid, since not every tower's art faces the same default
    // direction. The chosen angle is handed to the Tower on placement and then
    // becomes its starting aim -- it'll still swivel from there to track enemies.
    private void HandleManualRotation()
    {
        if (Keyboard.current == null) return;
        if (!Keyboard.current.rKey.wasPressedThisFrame) return;

        pendingRotation = (pendingRotation + manualRotationStep) % 360f;
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
        if (!IsTileAllowed(worldPos)) return false;

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
        tower.SetInitialRotation(pendingRotation);

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

        // Prefer asking the actual tilemap Grid for the cell center -- this is
        // exactly what the tile art is drawn against, so it can't drift out of
        // sync with gridSize.
        if (grid != null)
        {
            Vector3Int cell = grid.WorldToCell(world);
            Vector3 center = grid.GetCellCenterWorld(cell);
            return new Vector2(center.x, center.y);
        }

        // Fallback: snap to the center of a gridSize x gridSize cell, with
        // cell 0 spanning [0, gridSize) centered at gridSize/2 -- matching
        // Unity's own Tilemap convention. Rounding to the nearest whole
        // multiple of gridSize (the old behavior) centers cells on whole-number
        // coordinates instead, which is a half-cell off from how tiles are
        // actually drawn and is why the ghost/highlight straddled two tiles.
        return new Vector2(
            SnapToCellCenter(world.x, gridSize),
            SnapToCellCenter(world.y, gridSize)
        );
    }

    private static float SnapToCellCenter(float value, float cellSize)
    {
        float cellIndex = Mathf.Floor(value / cellSize);
        return (cellIndex + 0.5f) * cellSize;
    }

    private float EffectiveCellSize()
    {
        if (grid != null) return Mathf.Max(grid.cellSize.x, 0.0001f);
        return gridSize;
    }

    private bool IsInsideBuildArea(Vector2 position)
    {
        if (buildArea == null) return true;
        return buildArea.OverlapPoint(position);
    }

    // Gates placement by tile content rather than just the buildArea collider:
    // a cell needs a tile on groundTilemap (if assigned) and must have no tile
    // on pathTilemap (if assigned), so towers can't land on the enemy lane.
    private bool IsTileAllowed(Vector2 worldPos)
    {
        if (groundTilemap == null && pathTilemap == null) return true;

        Vector3Int cell = WorldToCell(worldPos);

        if (groundTilemap != null && !groundTilemap.HasTile(cell)) return false;
        if (pathTilemap != null && pathTilemap.HasTile(cell)) return false;

        return true;
    }

    private Vector3Int WorldToCell(Vector2 worldPos)
    {
        if (grid != null) return grid.WorldToCell(worldPos);
        if (groundTilemap != null) return groundTilemap.WorldToCell(worldPos);
        if (pathTilemap != null) return pathTilemap.WorldToCell(worldPos);
        return Vector3Int.zero;
    }

    private void BuildPreviewObjects()
    {
        GameObject ghostObj = new GameObject("PlacementGhost");
        ghostObj.transform.SetParent(transform, false);
        ghostRenderer = ghostObj.AddComponent<SpriteRenderer>();
        ghostRenderer.sortingOrder = previewSortingOrder;
        ghostTransform = ghostObj.transform;
        ghostObj.SetActive(false);

        GameObject highlightObj = new GameObject("PlacementHighlight");
        highlightObj.transform.SetParent(transform, false);
        highlightRenderer = highlightObj.AddComponent<SpriteRenderer>();
        highlightRenderer.sprite = CreateSquareSprite();
        highlightRenderer.sortingOrder = previewSortingOrder - 1;
        highlightObj.SetActive(false);
    }

    private static Sprite CreateSquareSprite()
    {
        var texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
    }

    private void UpdatePlacementPreview()
    {
        if (!CanShowPreview(out TowerSO selected))
        {
            ghostTransform.gameObject.SetActive(false);
            highlightRenderer.gameObject.SetActive(false);
            return;
        }

        Vector2 point = GetSnappedMouseWorldPosition();
        bool valid = IsInsideBuildArea(point) && IsTileAllowed(point) && Physics2D.OverlapCircle(point, blockingRadius, blockedMask) == null;

        ghostTransform.gameObject.SetActive(true);
        ghostTransform.position = point;
        ghostTransform.rotation = Quaternion.Euler(0f, 0f, pendingRotation);
        ghostRenderer.sprite = selected.icon;
        ghostRenderer.color = new Color(1f, 1f, 1f, ghostAlpha);
        FitGhostToGrid(selected.icon);

        highlightRenderer.gameObject.SetActive(true);
        highlightRenderer.transform.position = point;
        highlightRenderer.transform.localScale = Vector3.one * EffectiveCellSize();
        highlightRenderer.color = valid ? highlightValidColor : highlightInvalidColor;
    }

    // Icons come from shop/UI art with all sorts of native pixel sizes, so the
    // ghost is rescaled to a consistent fraction of a grid tile instead of
    // rendering at whatever raw size the sprite's import settings happen to give it.
    private void FitGhostToGrid(Sprite sprite)
    {
        if (sprite == null)
        {
            ghostTransform.localScale = Vector3.one;
            return;
        }

        Vector2 size = sprite.bounds.size;
        float maxDim = Mathf.Max(size.x, size.y, 0.0001f);
        float scale = (EffectiveCellSize() * 0.8f) / maxDim;
        ghostTransform.localScale = Vector3.one * scale;
    }

    private bool CanShowPreview(out TowerSO selected)
    {
        selected = null;

        if (placementCamera == null) return false;
        if (RoundManager.Instance != null && RoundManager.Instance.IsRoundActive()) return false;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return false;
        if (selectedSlot == lockedSlotIndex) return false;

        TowerSO tower = loadout[selectedSlot];
        if (tower == null) return false;
        if (PlayerTowerInventory.Instance == null || !PlayerTowerInventory.Instance.HasTower(tower)) return false;

        selected = tower;
        return true;
    }
}
