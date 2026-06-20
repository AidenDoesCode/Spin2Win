using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{

    [Header("Player Component References")]
    private Rigidbody2D rb;
    public Transform firePoint;
    [SerializeField] private string projectileLayerName = "Projectiles";
    [SerializeField] private string arenaLayerName = "Arena";

    [Header("Player Component Settings")]
    [SerializeField] private float movementSpeed = 2f;
    [SerializeField] private float fireRadius = 0.6f;

    [Header("Combat")]
    public WeaponSO currentWeapon;
    [Header("Weapon Visual")]
    public Transform weaponVisualParent;
    private GameObject weaponVisualInstance;

    [Tooltip("Shots per second. Set to 0 to disable firing.")]
    [SerializeField] private float fireRate = 5f;
    private float nextFireTime = 0f;
    private bool isHoldingAttack = false;

    private float runtimeFireRateMultiplier = 1f;
    private int runtimeBonusDamage = 0;
    private float runtimeMovementSpeedMultiplier = 1f;

    private Vector2 movementDirection;
    private Collider2D playerCollider;

    #region PLAYER CONTROLS

    public void Move(InputAction.CallbackContext context)
    {
        movementDirection = context.ReadValue<Vector2>();
    }

    public void Attack(InputAction.CallbackContext context)
    {
        if (context.started) isHoldingAttack = true;
        if (context.canceled) isHoldingAttack = false;
        if (context.performed)
            SpawnProjectileAtMouse();
    }

    #endregion

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<Collider2D>();

        // Configure physics layer collision rules so projectiles don't collide with arena or each other
        int projLayer = LayerMask.NameToLayer(projectileLayerName);
        int arenaLayer = LayerMask.NameToLayer(arenaLayerName);
        if (projLayer != -1 && arenaLayer != -1)
        {
            Physics2D.IgnoreLayerCollision(projLayer, arenaLayer, true);
        }
        if (projLayer != -1)
        {
            Physics2D.IgnoreLayerCollision(projLayer, projLayer, true);
        }

        UpdateWeaponVisual();
    }

    public void EquipWeapon(WeaponSO weapon)
    {
        currentWeapon = weapon;
        UpdateWeaponVisual();
    }

    private void UpdateWeaponVisual()
    {
        if (weaponVisualInstance != null)
        {
            Destroy(weaponVisualInstance);
            weaponVisualInstance = null;
        }

        if (currentWeapon == null || weaponVisualParent == null) return;

        if (currentWeapon.visualPrefab != null)
        {
            weaponVisualInstance = Instantiate(currentWeapon.visualPrefab, weaponVisualParent);
            weaponVisualInstance.transform.localPosition = Vector3.zero;
            weaponVisualInstance.transform.localRotation = Quaternion.identity;
            weaponVisualInstance.transform.localScale = Vector3.one;
            return;
        }

        if (currentWeapon.icon != null)
        {
            weaponVisualInstance = new GameObject("WeaponVisual", typeof(SpriteRenderer));
            weaponVisualInstance.transform.SetParent(weaponVisualParent, false);
            var sr = weaponVisualInstance.GetComponent<SpriteRenderer>();
            sr.sprite = currentWeapon.icon;
            sr.sortingOrder = 10;
        }
    }

    private void Update()
    {
        UpdateFirePointToMouse();
        if (isHoldingAttack)
        {
            SpawnProjectileAtMouse();
        }
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = movementDirection * movementSpeed * runtimeMovementSpeedMultiplier;
    }

    public void ApplyFireRateMultiplier(float multiplier, float duration)
    {
        StartCoroutine(TemporaryFloatMultiplierRoutine(multiplier, duration, value => runtimeFireRateMultiplier = value));
    }

    public void AddBonusDamage(int amount, float duration)
    {
        StartCoroutine(TemporaryIntBonusRoutine(amount, duration, value => runtimeBonusDamage = value));
    }

    public void ApplyMovementSpeedMultiplier(float multiplier, float duration)
    {
        StartCoroutine(TemporaryFloatMultiplierRoutine(multiplier, duration, value => runtimeMovementSpeedMultiplier = value));
    }

    private IEnumerator TemporaryFloatMultiplierRoutine(float multiplier, float duration, System.Action<float> applyValue)
    {
        applyValue(Mathf.Max(0.01f, multiplier));
        if (duration > 0f)
            yield return new WaitForSeconds(duration);
        applyValue(1f);
    }

    private IEnumerator TemporaryIntBonusRoutine(int amount, float duration, System.Action<int> applyValue)
    {
        applyValue(amount);
        if (duration > 0f)
            yield return new WaitForSeconds(duration);
        applyValue(0);
    }

    private void SpawnProjectileAtMouse()
    {
        float useFireRate = currentWeapon != null ? currentWeapon.fireRate : fireRate;
        useFireRate *= runtimeFireRateMultiplier;
        if (useFireRate <= 0f) return; // firing disabled
        if (Time.time < nextFireTime) return;
        nextFireTime = Time.time + (1f / Mathf.Max(0.0001f, useFireRate));

        if (currentWeapon == null || currentWeapon.projectilePrefab == null || firePoint == null)
        {
            Debug.LogWarning("Missing currentWeapon.projectilePrefab or firePoint on PlayerController");
            return;
        }

        var proj = Instantiate(currentWeapon.projectilePrefab, firePoint.position, firePoint.rotation);

        if (proj != null)
        {
            var projCollider = proj.GetComponent<Collider2D>();
            var projRb = proj.GetComponent<Rigidbody2D>();

            if (projCollider != null && playerCollider != null)
            {
                projCollider.isTrigger = true;
                Physics2D.IgnoreCollision(projCollider, playerCollider, true);
            }

            if (projRb != null)
            {
                projRb.gravityScale = 0f;
                proj.Speed = currentWeapon.projectileSpeed;
                proj.SetDirection((Vector2)firePoint.up);
                proj.Damage = currentWeapon.damage + runtimeBonusDamage;
            }
        }
    }

    private void UpdateFirePointToMouse()
    {
        if (firePoint == null || Camera.main == null) return;

        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        float camZ = -Camera.main.transform.position.z;
        Vector3 mouseWorld3 = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, camZ));
        Vector2 mouseWorld = mouseWorld3;

        Vector2 dir = (mouseWorld - (Vector2)transform.position);
        if (dir.sqrMagnitude < 0.0001f) return;
        dir.Normalize();

        Vector2 newPos = (Vector2)transform.position + dir * fireRadius;
        firePoint.position = new Vector3(newPos.x, newPos.y, firePoint.position.z);

        firePoint.up = dir;

        if (weaponVisualInstance != null)
        {
            var sr = weaponVisualInstance.GetComponent<SpriteRenderer>();
            weaponVisualInstance.transform.rotation = Quaternion.identity;
            if (sr != null)
            {
                sr.flipX = dir.x < 0f;
            }
        }
    }
}
