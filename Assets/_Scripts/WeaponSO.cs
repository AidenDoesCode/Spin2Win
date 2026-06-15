using UnityEngine;

[CreateAssetMenu(menuName = "Game/Weapon")]
public class WeaponSO : ScriptableObject
{
    public string weaponName;
    public BulletBehavior projectilePrefab;
    public float projectileSpeed = 5f;
    public float fireRadius = 0.6f;
    public float fireRate = 5f; // shots per second
    public bool isAutomatic = true;
    public Sprite icon;
    public GameObject visualPrefab;
    public int damage = 1;
}