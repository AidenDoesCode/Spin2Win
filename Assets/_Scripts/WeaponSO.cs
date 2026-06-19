using UnityEngine;

[CreateAssetMenu(menuName = "Game/Weapon")]
public class WeaponSO : ScriptableObject
{
    public string weaponName;
    public BulletBehavior projectilePrefab;
    public float projectileSpeed = 5f;
    public float fireRadius = 0.6f;
    [Tooltip("Shots per second.")]
    public float fireRate = 5f;
    public bool isAutomatic = true;
    public Sprite icon;
    public GameObject visualPrefab;
    public int damage = 1;
}