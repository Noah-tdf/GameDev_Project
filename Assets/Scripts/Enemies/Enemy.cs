using UnityEngine;

/// <summary>
/// Base enemy used for UbuntyWalker in Level 1.
/// Patrols between two points, takes damage from bullets, and damages/bounces the player on contact.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Enemy : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private int maxHealth = 3;

    [Header("Patrol")]
    [SerializeField] private bool shouldPatrol = true;
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private Transform leftPoint;
    [SerializeField] private Transform rightPoint;

    [Header("Contact Damage")]
    [SerializeField] private int contactDamage = 1;         // HP taken by player on side hit
    [SerializeField] private float stompBounceForce = 10f;  // Upward push when player stomps

    private Rigidbody2D _rb;
    private SpriteRenderer _sr;
    private int _currentHealth;
    private int _moveDirection = 1;

    // Flash-on-hit state
    private float _flashTimer;
    private readonly Color _hitColor = new Color(1f, 0.2f, 0.2f, 1f);
    private readonly float _flashDuration = 0.12f;

    // ────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _sr = GetComponent<SpriteRenderer>();
        _currentHealth = maxHealth;
    }

    private void Update()
    {
        // Tick the damage-flash timer
        if (_flashTimer > 0f)
        {
            _flashTimer -= Time.deltaTime;
            if (_flashTimer <= 0f && _sr != null)
                _sr.color = Color.white;
        }
    }

    private void FixedUpdate()
    {
        if (!shouldPatrol)
        {
            _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
            return;
        }

        Patrol();
    }

    // ────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Reduce health by damage. Flashes red, then destroys on death.
    /// </summary>
    public void TakeDamage(int damage)
    {
        _currentHealth -= damage;

        // Flash red
        if (_sr != null)
        {
            _sr.color = _hitColor;
            _flashTimer = _flashDuration;
        }

        if (_currentHealth <= 0)
        {
            Debug.Log("Enemy defeated!");
            Destroy(gameObject);
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    private void OnCollisionEnter2D(Collision2D col)
    {
        if (!col.gameObject.CompareTag("Player")) return;

        PlayerHealth ph = col.gameObject.GetComponent<PlayerHealth>();
        Rigidbody2D playerRb = col.gameObject.GetComponent<Rigidbody2D>();

        if (ph == null || playerRb == null) return;

        // Determine if the player is above this enemy (stomp)
        bool playerAbove = col.contacts[0].normal.y < -0.5f;   // Normal points DOWN into enemy = player is on top

        if (playerAbove)
        {
            // Stomp: deal 1 damage to enemy and bounce player upward
            TakeDamage(1);
            playerRb.linearVelocity = new Vector2(playerRb.linearVelocity.x, stompBounceForce);
        }
        else
        {
            // Side/bottom hit: deal contact damage to player
            ph.TakeDamage(contactDamage);
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    private void Patrol()
    {
        if (leftPoint == null || rightPoint == null)
        {
            _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
            return;
        }

        _rb.linearVelocity = new Vector2(_moveDirection * patrolSpeed, _rb.linearVelocity.y);

        if (transform.position.x <= leftPoint.position.x)
        {
            _moveDirection = 1;
            FaceDirection(1);
        }
        else if (transform.position.x >= rightPoint.position.x)
        {
            _moveDirection = -1;
            FaceDirection(-1);
        }
    }

    private void FaceDirection(int direction)
    {
        Vector3 localScale = transform.localScale;
        localScale.x = Mathf.Abs(localScale.x) * direction;
        transform.localScale = localScale;
    }

    private void OnDrawGizmosSelected()
    {
        if (leftPoint != null && rightPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(leftPoint.position, rightPoint.position);
            Gizmos.DrawWireSphere(leftPoint.position, 0.1f);
            Gizmos.DrawWireSphere(rightPoint.position, 0.1f);
        }
    }
}
