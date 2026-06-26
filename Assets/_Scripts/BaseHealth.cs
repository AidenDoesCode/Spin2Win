using System;
using UnityEngine;

public class BaseHealth : MonoBehaviour
{
    public static BaseHealth Instance { get; private set; }

    [Min(1)] public int maxHealth = 20;
    [Min(0f)] public float damageRadius = 0.75f;
    public int CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }

    [Header("Audio")]
    [Tooltip("Heavy alarming sound played whenever the base takes damage (an enemy breached the defense).")]
    public AudioClip damagedSound;
    [Range(0f, 3f)] public float damagedVolume = 1f;

    public event Action<int, int> HealthChanged;
    public event Action Died;

    private AudioSource audioSource;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    private void Start()
    {
        CurrentHealth = maxHealth;
        IsDead = false;
        NotifyHealthChanged();
    }

    public void TakeDamage(int amount)
    {
        if (IsDead || amount <= 0)
            return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        NotifyHealthChanged();

        if (damagedSound != null)
            audioSource.PlayOneShot(damagedSound, damagedVolume * SfxSettings.Volume);

        if (CurrentHealth <= 0)
        {
            IsDead = true;
            Died?.Invoke();
            Debug.Log("BaseHealth: base destroyed.");
        }
    }

    public void Heal(int amount)
    {
        if (IsDead || amount <= 0)
            return;

        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        NotifyHealthChanged();
    }

    // Drives both the HealthChanged UI subscribers (bar, screen tint) and
    // MusicManager's low-health tension effect from a single source of truth.
    private void NotifyHealthChanged()
    {
        HealthChanged?.Invoke(CurrentHealth, maxHealth);
        MusicManager.Instance?.SetHealthFactor(maxHealth > 0 ? (float)CurrentHealth / maxHealth : 0f);
    }
}