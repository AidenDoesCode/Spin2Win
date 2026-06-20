using UnityEngine;
using UnityEngine.InputSystem;

public class TowerPlacementManager : MonoBehaviour
{
    [System.Serializable]
    public class TowerSlot
    {
        public string label = "Tower";
        public TowerSO towerData;
    }

    [Header("Placement")]
    public Camera placementCamera;
    public Collider2D buildArea;
    public LayerMask blockedMask;
    public Transform towerParent;
    public float gridSize = 1f;
    public float blockingRadius = 0.35f;

    [Header("Available Towers")]
    public TowerSlot[] towers;
    public int selectedTowerIndex = 0;

    private void Start()
    {
        if (placementCamera == null)
            placementCamera = Camera.main;
    }

    private void Update()
    {
        HandleTowerSelection();

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryPlaceSelectedTower();
        }
    }

    public void SelectTower(int index)
    {
        if (towers == null || index < 0 || index >= towers.Length)
            return;

        selectedTowerIndex = index;
    }

    private void HandleTowerSelection()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame) SelectTower(0);
        if (Keyboard.current.digit2Key.wasPressedThisFrame) SelectTower(1);
        if (Keyboard.current.digit3Key.wasPressedThisFrame) SelectTower(2);
        if (Keyboard.current.digit4Key.wasPressedThisFrame) SelectTower(3);
        if (Keyboard.current.digit5Key.wasPressedThisFrame) SelectTower(4);
    }

    public bool TryPlaceSelectedTower()
    {
        if (placementCamera == null || towers == null || towers.Length == 0)
            return false;

        if (RoundManager.Instance != null && RoundManager.Instance.IsRoundActive())
            return false;

        if (selectedTowerIndex < 0 || selectedTowerIndex >= towers.Length)
            return false;

        TowerSO selectedTower = towers[selectedTowerIndex] != null ? towers[selectedTowerIndex].towerData : null;
        if (selectedTower == null || selectedTower.towerPrefab == null)
            return false;

        if (ScoreManager.Instance == null)
            return false;

        Vector2 worldPosition = GetSnappedMouseWorldPosition();
        if (!IsInsideBuildArea(worldPosition))
            return false;

        if (Physics2D.OverlapCircle(worldPosition, blockingRadius, blockedMask) != null)
            return false;

        if (!ScoreManager.Instance.TrySpendScore(selectedTower.cost))
            return false;

        GameObject towerObject = Instantiate(selectedTower.towerPrefab, worldPosition, Quaternion.identity, towerParent);
        Tower tower = towerObject.GetComponent<Tower>();
        if (tower == null)
        {
            Destroy(towerObject);
            ScoreManager.Instance.AddScore(selectedTower.cost);
            return false;
        }

        tower.Configure(selectedTower);
        return true;
    }

    private Vector2 GetSnappedMouseWorldPosition()
    {
        Vector3 mousePosition = Mouse.current != null ? (Vector3)Mouse.current.position.ReadValue() : Vector3.zero;
        float camZ = -placementCamera.transform.position.z;
        Vector3 world = placementCamera.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, camZ));

        float snappedX = Mathf.Round(world.x / gridSize) * gridSize;
        float snappedY = Mathf.Round(world.y / gridSize) * gridSize;
        return new Vector2(snappedX, snappedY);
    }

    private bool IsInsideBuildArea(Vector2 position)
    {
        if (buildArea == null)
            return true;

        return buildArea.OverlapPoint(position);
    }
}