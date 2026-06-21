using UnityEngine;

[CreateAssetMenu(menuName = "SpinFort/Tower", fileName = "NewTower")]
public class TowerSO : ScriptableObject
{
    public string towerName = "New Tower";
    [Min(0)] public int cost = 100;
    [Min(0f)] public float range = 4f;
    [Header("Aiming")]
    [Min(1)] public int aimSnapDirections = 8;
    [Min(0f)] public float fireRate = 1f;
    [Min(0f)] public float projectileSpeed = 7f;
    [Min(1)] public int damage = 1;
    [Min(0f)] public float rotationSpeed = 90f; // degrees per second

    public BulletBehavior projectilePrefab;
    public GameObject towerPrefab;
}