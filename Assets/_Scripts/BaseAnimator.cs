using UnityEngine;

// New: drives the Base's 6 hand-authored sprite-swap clips
// (Assets/Animations/Base/*.anim) via AnimationClipPlayer -- the same
// lightweight runtime-playable approach Tower.cs and Enemy.cs already use,
// so no AnimatorController is needed. Attach to the same GameObject as
// BaseHealth/SpriteRenderer.
//
// Behavior:
// - Idle/Damaged/Critical are looping states picked from current health %.
// - Hit plays once on any damage taken, then resumes the current health loop.
// - Spin plays once whenever RoundManager starts a round (the "gears crank"
//   flavor beat), then resumes the current health loop.
// - Destroyed plays once on death and stays -- terminal, nothing resumes it.
public class BaseAnimator : MonoBehaviour
{
    [Header("Clips")]
    public AnimationClip idleAnimation;
    public AnimationClip spinAnimation;
    public AnimationClip hitAnimation;
    public AnimationClip damagedAnimation;
    public AnimationClip criticalAnimation;
    public AnimationClip destroyedAnimation;

    [Header("Health Thresholds")]
    [Tooltip("At or below this health fraction, the Damaged loop plays.")]
    [Range(0f, 1f)] public float damagedThreshold = 0.6f;
    [Tooltip("At or below this health fraction, the Critical loop plays.")]
    [Range(0f, 1f)] public float criticalThreshold = 0.25f;

    [Header("References")]
    public RoundManager roundManager;

    private enum Tier { Idle, Damaged, Critical }

    private AnimationClipPlayer animPlayer;
    private BaseHealth baseHealth;

    private Tier currentTier = Tier.Idle;
    private Tier appliedTier = Tier.Idle;
    private bool tierApplied;

    private int lastHealth = -1;
    private bool isDead;
    private bool playingInterrupt;

    private void Awake()
    {
        animPlayer = new AnimationClipPlayer(gameObject);
    }

    private void Start()
    {
        baseHealth = BaseHealth.Instance != null ? BaseHealth.Instance : FindAnyObjectByType<BaseHealth>();
        if (baseHealth != null)
        {
            baseHealth.HealthChanged += OnHealthChanged;
            baseHealth.Died += OnDied;
            lastHealth = baseHealth.CurrentHealth;
        }

        if (roundManager == null)
            roundManager = RoundManager.Instance != null ? RoundManager.Instance : FindAnyObjectByType<RoundManager>();
        if (roundManager != null)
            roundManager.RoundStarted += OnRoundStarted;

        PlayTierLoop(force: true);
    }

    private void OnDestroy()
    {
        if (baseHealth != null)
        {
            baseHealth.HealthChanged -= OnHealthChanged;
            baseHealth.Died -= OnDied;
        }
        if (roundManager != null)
            roundManager.RoundStarted -= OnRoundStarted;

        animPlayer?.Dispose();
    }

    private void Update()
    {
        bool finished = animPlayer.Tick(Time.deltaTime);

        if (finished && playingInterrupt && !isDead)
        {
            playingInterrupt = false;
            PlayTierLoop(force: true);
        }
    }

    private void OnHealthChanged(int current, int max)
    {
        if (isDead) return;

        bool tookDamage = lastHealth >= 0 && current < lastHealth;
        lastHealth = current;

        float percent = max > 0 ? (float)current / max : 0f;
        if (percent <= criticalThreshold) currentTier = Tier.Critical;
        else if (percent <= damagedThreshold) currentTier = Tier.Damaged;
        else currentTier = Tier.Idle;

        if (tookDamage && hitAnimation != null)
        {
            playingInterrupt = true;
            animPlayer.Play(hitAnimation, loop: false);
        }
        else if (!playingInterrupt)
        {
            PlayTierLoop(force: false);
        }
    }

    private void OnDied()
    {
        isDead = true;
        playingInterrupt = false;

        if (destroyedAnimation != null)
            animPlayer.Play(destroyedAnimation, loop: false);
    }

    private void OnRoundStarted(int round)
    {
        if (isDead || spinAnimation == null) return;

        playingInterrupt = true;
        animPlayer.Play(spinAnimation, loop: false);
    }

    private void PlayTierLoop(bool force)
    {
        if (!force && tierApplied && appliedTier == currentTier) return;

        AnimationClip clip = idleAnimation;
        if (currentTier == Tier.Critical && criticalAnimation != null) clip = criticalAnimation;
        else if (currentTier == Tier.Damaged && damagedAnimation != null) clip = damagedAnimation;

        if (clip == null) return;

        animPlayer.Play(clip, loop: true);
        appliedTier = currentTier;
        tierApplied = true;
    }
}
