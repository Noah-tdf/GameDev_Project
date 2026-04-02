using UnityEngine;

/// <summary>
/// Moves a cartoon car across the screen. Destroys itself when off-camera.
/// The car has a trigger collider on its body (damages player) and a solid
/// collider on top (player can stand on it).
/// Attach to the Car prefab.
/// </summary>
public class CarMovement : MonoBehaviour
{
    // ── Inspector-exposed settings ───────────────────────────────────────────
    [Header("Movement")]
    [SerializeField] private float speed = 8f;              // Units per second
    [SerializeField] private float direction = -1f;         // -1 = left, 1 = right

    [Header("Bounds")]
    [SerializeField] private float destroyX = 40f;          // Absolute X at which the car is destroyed

    // ── Internal state ───────────────────────────────────────────────────────
    private float _travelSign;  // Which direction destroyX is checked

    // ────────────────────────────────────────────────────────────────────────
    private void Start()
    {
        _travelSign = Mathf.Sign(direction);

        // Flip sprite to match travel direction
        if (direction > 0f)
        {
            Vector3 s = transform.localScale;
            s.x = Mathf.Abs(s.x);
            transform.localScale = s;
        }
        else
        {
            Vector3 s = transform.localScale;
            s.x = -Mathf.Abs(s.x);
            transform.localScale = s;
        }
    }

    private void Update()
    {
        // Move in the assigned direction
        transform.Translate(Vector3.right * direction * speed * Time.deltaTime);

        // Destroy when past the level boundary in the travel direction
        bool pastBounds = (_travelSign < 0f) ? transform.position.x < -destroyX
                                              : transform.position.x >  destroyX;
        if (pastBounds)
            Destroy(gameObject);
    }

    // ────────────────────────────────────────────────────────────────────────
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Body trigger hits the player → deal damage
        if (other.CompareTag("Player"))
        {
            PlayerHealth ph = other.GetComponent<PlayerHealth>();
            ph?.TakeDamage(1);
        }
    }

    /// <summary>Called by CarSpawner to configure this car after spawning.</summary>
    public void Initialize(float carSpeed, float travelDirection)
    {
        speed     = carSpeed;
        direction = travelDirection;
    }
}
