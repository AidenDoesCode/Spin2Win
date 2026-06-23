using UnityEngine;

[CreateAssetMenu(menuName = "Game/Enemy", fileName = "NewEnemy")]
public class EnemySO : ScriptableObject
{
    public string enemyName = "New Enemy";
    [Tooltip("Spawn weight in the round's point budget -- cheap fast units cost little, heavy units cost a lot.")]
    [Min(0)] public int pointCost = 1;
    public int maxHealth = 10;
    public int damage = 1;
    public float moveSpeed = 1f;
    public float detectionRadius = 5f;
    public float attackRange = 1f;
    public float attackCooldown = 1f;
    public GameObject deathPrefab;
    public int scoreValue = 10;
}