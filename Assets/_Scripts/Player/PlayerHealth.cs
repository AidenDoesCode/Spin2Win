using System;
using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 3;
    private int currentHealth;

    public HealthUI healthUI;
    public event Action Died;

    public int CurrentHealth => currentHealth;
    public bool IsDead { get; private set; }

    // Invulnerability variables
    [SerializeField] private float invulnerabilityDuration = 0.5f;
    private bool isInvulnerable = false;

    void Start()
    {
        currentHealth = maxHealth;
        IsDead = false;

        if (healthUI != null)
            healthUI.SetMaxHearts(maxHealth);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // If currently invulnerable, ignore incoming damage collisions
        if (isInvulnerable) return;

        Enemy enemy = collision.GetComponent<Enemy>();
        if (enemy != null)
        {
            TakeDamage(enemy.damage);
        }
    }

    private void TakeDamage(int damage)
    {
        if (IsDead)
            return;

        currentHealth -= damage;
        if (healthUI != null)
            healthUI.UpdateHearts(currentHealth);

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            IsDead = true;
            Debug.Log("Player has died.");
            Died?.Invoke();
        }
        else
        {
            StartCoroutine(InvulnerabilityCoroutine());
        }
    }

    public void Heal(int amount)
    {
        if (amount <= 0 || IsDead)
            return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);

        if (healthUI != null)
            healthUI.UpdateHearts(currentHealth);
    }

    private IEnumerator InvulnerabilityCoroutine()
    {
        isInvulnerable = true;
        yield return new WaitForSeconds(invulnerabilityDuration);
        isInvulnerable = false;
    }
}