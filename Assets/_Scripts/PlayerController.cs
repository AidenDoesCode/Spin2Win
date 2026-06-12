using UnityEngine;
using UnityEngine.InputSystem;


public class PlayerController : MonoBehaviour
{

    [Header("Player Component References")]
    private Rigidbody2D rb;
    public BulletBehavior projectilePrefab;
    public Transform firePoint;
    [SerializeField] private string projectileLayerName = "Projectiles";
    [SerializeField] private string arenaLayerName = "Arena";
    

    [Header("Player Componenent Settings")]
    [SerializeField] private float movementSpeed = 2f;
    [SerializeField] private float fireRadius = 0.6f;

    [Header("Combat")]
    [Tooltip("Shots per second. Set to 0 to disable firing.")]
    [SerializeField] private float fireRate = 5f;
    private float nextFireTime = 0f;
    private bool isHoldingAttack = false;

    private Vector2 movementDirection;
    private Collider2D playerCollider;

    #region PLAYER CONTROLS

    //Movement
    public void Move(InputAction.CallbackContext context)
    {
        movementDirection = context.ReadValue<Vector2>();
    }

    
    public void Attack(InputAction.CallbackContext context)
    {
        // track hold state: started = pressed, canceled = released
        if (context.started) isHoldingAttack = true;
        if (context.canceled) isHoldingAttack = false;
        // optional: perform action immediately on performed as well
        if (context.performed)
            SpawnProjectileAtMouse();
    }

    #endregion

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
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
            // Prevent projectiles from colliding with other projectiles
            Physics2D.IgnoreLayerCollision(projLayer, projLayer, true);
        }
    }

    // Update is called once per frame
    void Update()
    {
        UpdateFirePointToMouse();
        // If the attack button is held, try to fire (SpawnProjectileAtMouse will respect cooldown)
        if (isHoldingAttack)
        {
            SpawnProjectileAtMouse();
        }
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = movementDirection * movementSpeed;
    }

    private void SpawnProjectileAtMouse()
    {

        if (fireRate <= 0f) return; // firing disabled
        if (Time.time < nextFireTime) return;
        nextFireTime = Time.time + (1f / Mathf.Max(0.0001f, fireRate));

        // Get the current screen position of the mouse
        Vector2 mousePosition = Mouse.current.position.ReadValue();

        if (projectilePrefab == null || firePoint == null)
        {
            Debug.LogWarning("Missing projectilePrefab or firePoint on PlayerController");
            return;
        }

        // Use the firePoint's orientation for spawn direction and rotation
        Quaternion rot = firePoint.rotation;
        var proj = Instantiate(projectilePrefab, firePoint.position, rot);

        // Prevent the projectile from colliding with the player (avoids recoil and instant destroy)
        if (proj != null)
        {
            var projCollider = proj.GetComponent<Collider2D>();
            var projRb = proj.GetComponent<Rigidbody2D>();

            if (projCollider != null && playerCollider != null)
            {
                // Make the projectile a trigger so it won't apply physics forces to the player
                projCollider.isTrigger = true;
                // Ignore collisions between the projectile and the player collider
                Physics2D.IgnoreCollision(projCollider, playerCollider, true);
            }

                if (projRb != null)
                {
                    // Ensure no gravity on the projectile's Rigidbody2D
                    projRb.gravityScale = 0f;
                    // Use the firePoint's up vector for direction so projectile follows the firePoint
                    proj.SetDirection((Vector2)firePoint.up);
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

        // Allow full 360° around the player; position firePoint at the given radius
        Vector2 newPos = (Vector2)transform.position + dir * fireRadius;
        firePoint.position = new Vector3(newPos.x, newPos.y, firePoint.position.z);

        // Orient the firePoint so its up vector points toward the mouse
        firePoint.up = dir;
    }
}
