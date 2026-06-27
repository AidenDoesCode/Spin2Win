using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using TMPro;

public class TowerPlacementManager : MonoBehaviour
{
    public static TowerPlacementManager Instance { get; private set; }

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

    [Header("Placement Preview")]
    [Tooltip("Alpha of the ghost sprite shown under the cursor while placing.")]
    [Range(0f, 1f)] public float ghostAlpha = 0.5f;
    public Color highlightValidColor = new Color(0f, 0.6588f, 0.5882f, 0.5f);  // Seafoam Teal
    public Color highlightInvalidColor = new Color(0.8157f, 0f, 0f, 0.5f);    // Card Suit Red
    public int previewSortingOrder = 50;

    [Header("Selling")]
    [Tooltip("Fraction of a tower's buy cost refunded when right-clicking it off the grid.")]
    [Range(0f, 1f)] public float placedTowerRefundPercent = 0.5f;

    [Header("Audio")]
    [Tooltip("Soft plop/thud played when a tower is successfully dropped onto a grid tile.")]
    public AudioClip placeSound;
    [Range(0f, 3f)] public float placeVolume = 1f;
    private AudioSource audioSource;

    private SpriteRenderer ghostRenderer;
    private Transform ghostTransform;
    private SpriteRenderer highlightRenderer;
    private SpriteRenderer rangeRenderer;
    private TextMeshPro rangeLabel;

    private GameObject sellConfirmObj;
    private TextMeshProUGUI sellConfirmMessage;
    private Tower pendingSellTower;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        BuildPreviewObjects();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        if (placementCamera == null)
            placementCamera = Camera.main;
        if (grid == null)
            grid = FindAnyObjectByType<Grid>();

        BuildSellConfirmUI();
    }

    private void Update()
    {
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            TrySellTowerAtMouse();
    }

    // Right-click brings up a Sell/Cancel confirmation for whatever placed
    // tower is under the cursor instead of selling it immediately -- same
    // phase lock as placement, since selling mid-wave would let players dodge
    // a hit they already paid to be ready for.
    public bool TrySellTowerAtMouse()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return false;

        if (placementCamera == null) return false;
        if (RoundManager.Instance != null && RoundManager.Instance.IsRoundActive())
            return false;

        Vector2 worldPos = GetSnappedMouseWorldPosition();
        Collider2D hit = Physics2D.OverlapCircle(worldPos, blockingRadius, blockedMask);
        if (hit == null) return false;

        Tower tower = hit.GetComponent<Tower>();
        if (tower == null) return false;

        ShowSellConfirm(tower);
        return true;
    }

    private void ShowSellConfirm(Tower tower)
    {
        if (sellConfirmObj == null) return;

        pendingSellTower = tower;
        int refund = tower.data != null ? Mathf.RoundToInt(tower.data.cost * placedTowerRefundPercent) : 0;
        string towerName = tower.data != null ? tower.data.towerName : "this tower";
        sellConfirmMessage.text = $"Sell {towerName} for ${refund}?";

        sellConfirmObj.SetActive(true);
        sellConfirmObj.transform.SetAsLastSibling();
        RoundManager.Instance?.SetTimerPaused(true);
    }

    private void OnConfirmSellClicked()
    {
        pendingSellTower?.Sell(placedTowerRefundPercent);
        HideSellConfirm();
    }

    private void OnCancelSellClicked() => HideSellConfirm();

    private void HideSellConfirm()
    {
        pendingSellTower = null;
        if (sellConfirmObj != null) sellConfirmObj.SetActive(false);
        RoundManager.Instance?.SetTimerPaused(false);
    }

    // Drag-and-drop placement: dragging a card from the inventory panel onto
    // the grid places it immediately.
    public bool TryPlaceTowerAtScreenPoint(TowerSO tower, Vector2 screenPoint)
    {
        if (placementCamera == null) return false;
        Vector2 worldPos = GetSnappedWorldPosition(screenPoint);
        return TryPlaceTowerAt(tower, worldPos);
    }

    private bool TryPlaceTowerAt(TowerSO selected, Vector2 worldPos)
    {
        if (RoundManager.Instance != null && RoundManager.Instance.IsRoundActive())
            return false;
        if (RoundContinueUI.IsTransitioning) return false;

        if (selected == null || selected.towerPrefab == null) return false;

        // Card consumption: placing a tower spends one owned copy of it. With
        // none left, it can't be placed again until another is bought.
        if (PlayerTowerInventory.Instance == null || !PlayerTowerInventory.Instance.HasTower(selected))
            return false;

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
        tower.SetInitialRotation(0f);
        tower.PlaySquash(); // ADDED: squash pop on successful placement

        PlayerTowerInventory.Instance.RemoveTower(selected);

        if (placeSound != null) audioSource.PlayOneShot(placeSound, placeVolume * SfxSettings.Volume);

        return true;
    }

    private Vector2 GetSnappedMouseWorldPosition()
    {
        Vector2 mousePos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        return GetSnappedWorldPosition(mousePos);
    }

    private Vector2 GetSnappedWorldPosition(Vector2 screenPos)
    {
        float camZ = -placementCamera.transform.position.z;
        Vector3 world = placementCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, camZ));

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

        GameObject rangeObj = new GameObject("PlacementRangeCircle");
        rangeObj.transform.SetParent(transform, false);
        rangeRenderer = rangeObj.AddComponent<SpriteRenderer>();
        rangeRenderer.sprite = CreateRingSprite();
        rangeRenderer.color = new Color(1f, 1f, 1f, 0.6f);
        rangeRenderer.sortingOrder = previewSortingOrder - 2;
        rangeObj.SetActive(false);

        GameObject rangeLabelObj = new GameObject("PlacementRangeLabel");
        rangeLabelObj.transform.SetParent(transform, false);
        rangeLabel = rangeLabelObj.AddComponent<TextMeshPro>();
        rangeLabel.alignment = TextAlignmentOptions.Center;
        rangeLabel.fontSize = 3f;
        rangeLabel.color = Color.white;
        rangeLabel.sortingOrder = previewSortingOrder;
        rangeLabelObj.SetActive(false);
    }

    // Small modal Yes/Cancel confirmation shown by ShowSellConfirm before a
    // right-clicked tower is actually sold. Self-attaches to whatever Canvas
    // is in the scene, same lookup pattern BaseHealthBarUI uses.
    private void BuildSellConfirmUI()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        sellConfirmObj = new GameObject("SellConfirmPanel");
        sellConfirmObj.transform.SetParent(canvas.transform, false);
        RectTransform panelRT = sellConfirmObj.AddComponent<RectTransform>();
        panelRT.anchorMin = panelRT.anchorMax = panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta = new Vector2(380f, 180f);
        Image panelImg = sellConfirmObj.AddComponent<Image>();
        panelImg.color = new Color(0.2f, 0.031f, 0.031f, 0.97f);

        GameObject messageObj = new GameObject("Message");
        messageObj.transform.SetParent(sellConfirmObj.transform, false);
        RectTransform messageRT = messageObj.AddComponent<RectTransform>();
        messageRT.anchorMin = new Vector2(0f, 1f);
        messageRT.anchorMax = new Vector2(1f, 1f);
        messageRT.pivot = new Vector2(0.5f, 1f);
        messageRT.anchoredPosition = new Vector2(0f, -20f);
        messageRT.sizeDelta = new Vector2(-40f, 80f);
        sellConfirmMessage = messageObj.AddComponent<TextMeshProUGUI>();
        sellConfirmMessage.fontSize = 22;
        sellConfirmMessage.alignment = TextAlignmentOptions.Center;
        sellConfirmMessage.color = Color.white;

        BuildSellConfirmButton("SellButton", "Sell", new Vector2(-95f, 30f), new Color(0.8157f, 0f, 0f, 1f), OnConfirmSellClicked);
        BuildSellConfirmButton("CancelButton", "Cancel", new Vector2(95f, 30f), new Color(1f, 0.8431f, 0f, 1f), OnCancelSellClicked);

        sellConfirmObj.SetActive(false);
    }

    private void BuildSellConfirmButton(string name, string text, Vector2 anchoredPosition, Color color, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(sellConfirmObj.transform, false);
        RectTransform buttonRT = buttonObj.AddComponent<RectTransform>();
        buttonRT.anchorMin = buttonRT.anchorMax = buttonRT.pivot = new Vector2(0.5f, 0f);
        buttonRT.anchoredPosition = anchoredPosition;
        buttonRT.sizeDelta = new Vector2(160f, 56f);
        Image buttonImg = buttonObj.AddComponent<Image>();
        buttonImg.color = color;
        Button button = buttonObj.AddComponent<Button>();
        button.onClick.AddListener(onClick);

        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(buttonObj.transform, false);
        RectTransform labelRT = labelObj.AddComponent<RectTransform>();
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = labelRT.offsetMax = Vector2.zero;
        var label = labelObj.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = 22;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.black;
    }

    private static Sprite CreateSquareSprite()
    {
        var texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
    }

    // Thin translucent ring -- used to show a tower's attack range while
    // dragging it from the inventory. localScale is later set to the range
    // diameter, so this sprite's PixelsPerUnit is fixed at 1 so it maps 1:1
    // to world units.
    private static Sprite CreateRingSprite()
    {
        const int size = 128;
        const float thickness = 0.06f; // fraction of the radius
        var texture = new Texture2D(size, size);
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float outerRadius = size / 2f;
        float innerRadius = outerRadius * (1f - thickness);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                bool onRing = dist <= outerRadius && dist >= innerRadius;
                texture.SetPixel(x, y, onRing ? Color.white : new Color(1f, 1f, 1f, 0f));
            }
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }

    // Drag preview: called by TowerDragUI each frame a tower card from the
    // inventory is being dragged, so the ghost/highlight/range visuals appear
    // under the cursor while the player decides where to drop it.
    public void UpdateDragPreview(TowerSO tower, Vector2 screenPoint)
    {
        bool roundActive = RoundManager.Instance != null && RoundManager.Instance.IsRoundActive();
        bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        if (tower == null || placementCamera == null || roundActive || overUI || RoundContinueUI.IsTransitioning)
        {
            HidePreview();
            return;
        }

        ShowPreviewAt(tower, GetSnappedWorldPosition(screenPoint));
    }

    // Called by TowerDragUI when a drag ends, regardless of whether it
    // resulted in a placement.
    public void EndDragPreview()
    {
        HidePreview();
    }

    private void HidePreview()
    {
        ghostTransform.gameObject.SetActive(false);
        highlightRenderer.gameObject.SetActive(false);
        HideRangeRing();
    }

    private void ShowPreviewAt(TowerSO selected, Vector2 point)
    {
        bool valid = IsInsideBuildArea(point) && IsTileAllowed(point) && Physics2D.OverlapCircle(point, blockingRadius, blockedMask) == null;

        ghostTransform.gameObject.SetActive(true);
        ghostTransform.position = point;
        ghostTransform.rotation = Quaternion.identity;
        ghostRenderer.sprite = selected.icon;
        ghostRenderer.color = new Color(1f, 1f, 1f, ghostAlpha);
        FitGhostToGrid(selected.icon, selected.visualScaleMultiplier);

        highlightRenderer.gameObject.SetActive(true);
        highlightRenderer.transform.position = point;
        highlightRenderer.transform.localScale = Vector3.one * EffectiveCellSize();
        highlightRenderer.color = valid ? highlightValidColor : highlightInvalidColor;

        ShowRangeRingAt(point, selected.range);
    }

    // Drives the range ring + tile-radius label -- shared by the drag preview
    // above and TowerDetailPopupUI's click-to-inspect view of a placed tower.
    public void ShowRangeRingAt(Vector2 point, float range)
    {
        rangeRenderer.gameObject.SetActive(true);
        rangeRenderer.transform.position = point;
        rangeRenderer.transform.localScale = Vector3.one * (range * 2f);

        // Range is already in world units that line up 1:1 with the grid
        // (EffectiveCellSize), so dividing by the cell size reads it off as
        // a tile-radius count rather than an abstract world-unit number.
        rangeLabel.gameObject.SetActive(true);
        rangeLabel.transform.position = point + new Vector2(0f, range + 0.4f);
        rangeLabel.text = $"{range / EffectiveCellSize():0.#} tiles";
    }

    public void HideRangeRing()
    {
        rangeRenderer.gameObject.SetActive(false);
        rangeLabel.gameObject.SetActive(false);
    }

    // Icons come from shop/UI art with all sorts of native pixel sizes, so the
    // ghost is rescaled to a consistent fraction of a grid tile, then scaled
    // up by the same visualScaleMultiplier the real placed tower uses, so the
    // preview actually matches the size the tower ends up at.
    private void FitGhostToGrid(Sprite sprite, float visualScaleMultiplier = 1f)
    {
        if (sprite == null)
        {
            ghostTransform.localScale = Vector3.one * visualScaleMultiplier;
            return;
        }

        Vector2 size = sprite.bounds.size;
        float maxDim = Mathf.Max(size.x, size.y, 0.0001f);
        float scale = (EffectiveCellSize() * 0.8f) / maxDim * visualScaleMultiplier;
        ghostTransform.localScale = new Vector3(scale, scale, scale);
    }
}
