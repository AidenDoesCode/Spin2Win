using UnityEngine;

public class Enemy : MonoBehaviour
{

    public float moveSpeed = 4f;
    private Rigidbody2D rb;
    private Vector2 movementDirection;
    private Transform target;

    public int damage = 1;

    private void Awake()
    { 
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
            GameObject playerObj = GameObject.Find("Player");
            if (playerObj != null)
                target = playerObj.transform;
    }

    // Update is called once per frame
    void Update()
    {
        if (target == null) return; // Prevents errors if the player is destroyed

        Vector3 direction = (target.position - transform.position).normalized;
        movementDirection = direction;

        // Optional: Rotate enemy to face the player
        //float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        //rb.rotation = angle; // Adjust for sprite orientation
    }

    private void FixedUpdate()
    {
        if (target)
        {
            rb.linearVelocity = new Vector2(movementDirection.x, movementDirection.y) * moveSpeed;
        }
    }

}
