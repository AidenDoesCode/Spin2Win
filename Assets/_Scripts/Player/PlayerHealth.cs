using System;
using System.Collections; // Required for Coroutines
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 3;
    private int currentHealth;

    public HealthUI healthUI;

    // Invulnerability variables
    [SerializeField] private float invulnerabilityDuration = 0.5f;
    private bool isInvulnerable = false;

    void Start()
    {
        currentHealth = maxHealth;
        healthUI.setMaxHearts(maxHealth);
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
        // Fix: Use -= instead of = so health actually decreases
        currentHealth -= damage;
        healthUI.UpdateHearts(currentHealth);

        if (currentHealth <= 0)
        {
            Debug.Log("Player has died.");
            //Die();
        }
        else
        {
            // Start the invulnerability timer if the player survived
            StartCoroutine(InvulnerabilityCoroutine());
        }
    }

    private IEnumerator InvulnerabilityCoroutine()
    {
        isInvulnerable = true;
        yield return new WaitForSeconds(invulnerabilityDuration);
        isInvulnerable = false;
    }
}