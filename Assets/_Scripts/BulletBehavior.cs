using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class BulletBehavior : MonoBehaviour
{
    public float Speed = 5f;
    public int Damage = 1;

    [Tooltip("Set by Tower from the firing TowerSO's impactSound. Plays once when this projectile hits an enemy.")]
    public AudioClip impactSound;
    [Range(0f, 3f)] public float impactVolume = 1f;

    [Header("Animation")]
    [Tooltip("Optional fallback looping animation on the prefab. TowerSO.projectileFlightAnimation overrides this when set.")]
    public AnimationClip flightAnimation;

    // ADDED: screen-shake-on-impact, set by Tower.FireAt() from the firing TowerSO.
    [Header("Polish - Screen Shake")]
    public bool screenShakeOnImpact = false;
    public float shakeDuration = 0.1f;
    public float shakeMagnitude = 0.05f;

    protected Rigidbody2D rb;
    private Vector2 initialDir = Vector2.zero;
    private bool hasInitialDir;
    private AnimationClipPlayer animPlayer;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void SetFlightAnimation(AnimationClip clip)
    {
        if (clip == null) return;
        flightAnimation = clip;
        EnsureAnimPlayer();
        animPlayer.Play(flightAnimation, loop: true);
    }

    private void EnsureAnimPlayer()
    {
        if (animPlayer != null || flightAnimation == null) return;

        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        GameObject visual = sr != null ? sr.gameObject : gameObject;
        animPlayer = new AnimationClipPlayer(visual);
    }

    protected virtual void Start()
    {
        if (hasInitialDir)
            rb.linearVelocity = initialDir.normalized * Speed;
        else
            rb.linearVelocity = transform.up * Speed;

        EnsureAnimPlayer();
        if (animPlayer != null)
            animPlayer.Play(flightAnimation, loop: true);
    }

    protected virtual void Update()
    {
        animPlayer?.Tick(Time.deltaTime);
    }

    protected virtual void FixedUpdate()
    {
        if (rb == null) return;

        if (rb.linearVelocity.sqrMagnitude > 0f)
            rb.linearVelocity = rb.linearVelocity.normalized * Speed;
        else if (hasInitialDir)
            rb.linearVelocity = initialDir.normalized * Speed;
        else
            rb.linearVelocity = transform.up * Speed;
    }

    protected virtual void OnDestroy()
    {
        animPlayer?.Dispose();
    }

    public virtual void SetDirection(Vector2 dir)
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        initialDir = dir;
        hasInitialDir = true;
        rb.linearVelocity = initialDir.normalized * Speed;
    }

    protected bool ShouldIgnoreCollider(Collider2D other)
    {
        if (other == null) return true;
        if (other.GetComponent<BulletBehavior>() != null) return true;

        int arenaLayer = LayerMask.NameToLayer("Arena");
        return arenaLayer >= 0 && other.gameObject.layer == arenaLayer;
    }

    protected Enemy GetEnemyFromCollider(Collider2D other)
    {
        if (other == null || ShouldIgnoreCollider(other)) return null;
        return other.GetComponent<Enemy>();
    }

    protected virtual void OnHitEnemy(Enemy enemy)
    {
        if (enemy == null) return;
        enemy.TakeDamage(Damage);

        if (impactSound != null)
            AudioSource.PlayClipAtPoint(impactSound, transform.position, impactVolume * SfxSettings.Volume);

        TriggerShake(); // ADDED
        DestroyProjectile();
    }

    // ADDED: shared by base OnHitEnemy and the AoE subclasses' own
    // detonate/pierce logic (which override OnHitEnemy without calling base).
    protected void TriggerShake()
    {
        if (screenShakeOnImpact && CameraShake.Instance != null)
            CameraShake.Instance.Shake(shakeDuration, shakeMagnitude);
    }

    protected void DestroyProjectile()
    {
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        OnHitEnemy(GetEnemyFromCollider(other));
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision == null || collision.collider == null) return;
        OnHitEnemy(GetEnemyFromCollider(collision.collider));
    }
}
