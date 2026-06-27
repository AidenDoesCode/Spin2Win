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

    // Captured once before anything (ReduceMaxHealth/IncreaseMaxHealth from
    // run upgrades) can mutate maxHealth, so ResetForRestart can put it back
    // to what it actually started at instead of carrying a finished run's
    // permanent buffs/penalties into the next one.
    private int startingMaxHealth;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        startingMaxHealth = maxHealth;

        // Set here rather than Start() -- Unity guarantees every object's
        // Awake() runs before any object's Start(), but doesn't guarantee
        // Start() order between different scripts. BaseHealthBarUI reads
        // CurrentHealth during its own Start() for its first paint; if that
        // happened to run before this one (most reliably reproducible right
        // after a scene reload on Restart), it would catch CurrentHealth at
        // its raw default of 0 and render a permanently empty bar.
        CurrentHealth = maxHealth;
        IsDead = false;

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    private void Start()
    {
        NotifyHealthChanged();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
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

    // All-In Multiplier's downside -- permanently shrinks the health pool,
    // clamping current health down with it if it no longer fits.
    public void ReduceMaxHealth(int amount)
    {
        if (amount <= 0) return;

        maxHealth = Mathf.Max(1, maxHealth - amount);
        CurrentHealth = Mathf.Min(CurrentHealth, maxHealth);
        NotifyHealthChanged();
    }

    // Coral Barricade and similar cards -- permanently grows the health pool,
    // carrying current health up with it so the buff is felt immediately.
    public void IncreaseMaxHealth(int amount)
    {
        if (amount <= 0) return;

        maxHealth += amount;
        CurrentHealth += amount;
        NotifyHealthChanged();
    }

    // Called right before a scene reload/transition out of Arena, mirroring
    // RoundManager.ResetManagerForRestart -- a harmless no-op if this object
    // genuinely gets destroyed and recreated by the reload, but a real fix if
    // it (or the health bar reading it) ever survives instead of resetting.
    public void ResetForRestart()
    {
        maxHealth = startingMaxHealth;
        CurrentHealth = maxHealth;
        IsDead = false;
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