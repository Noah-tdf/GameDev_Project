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

    [Header("Behavior")]
    [SerializeField] private bool followPlayer = false;
    [SerializeField] private bool useGravity = false;
    [SerializeField] private float followSpeed = 3f;
    [SerializeField] private float detectionRange = 15f;
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Patrol")]
    [SerializeField] private bool shouldPatrol = true;
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private Transform leftPoint;
    [SerializeField] private Transform rightPoint;

    [Header("Contact Damage")]
    [SerializeField] private int contactDamage = 1;         // HP taken by player on side hit
    [SerializeField] private float stompBounceForce = 10f;  // Upward push when player stomps

    [Header("Sound")]
    [SerializeField] private AudioSource hitSound;
    [SerializeField] private AudioSource deathSound;

    private Rigidbody2D _rb;
    private SpriteRenderer _sr;
    private Collider2D _col;
    private Transform _target;
    private int _currentHealth;
    private int _moveDirection = 1;
    private float _leftBoundX;
    private float _rightBoundX;
    private bool _hasPatrolBounds;
    private bool _isDead;
    private float _lastX;
    private float _stuckTimer;

    // Flash-on-hit state
    private float _flashTimer;
    private readonly Color _hitColor = new Color(1f, 0.2f, 0.2f, 1f);
    private readonly float _flashDuration = 0.12f;

    // ────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _sr = GetComponent<SpriteRenderer>();
        _col = GetComponent<Collider2D>();
        _currentHealth = maxHealth;

        if (hitSound == null)
            hitSound = GetComponent<AudioSource>();

        if (_rb != null)
        {
            if (useGravity)
            {
                _rb.bodyType = RigidbodyType2D.Dynamic;
                _rb.gravityScale = 2f;
                _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            }
            else
            {
                _rb.bodyType = RigidbodyType2D.Kinematic;
                _rb.gravityScale = 0f;
            }
            _rb.freezeRotation = true;
        }

        if (leftPoint != null && rightPoint != null)
        {
            _leftBoundX = Mathf.Min(leftPoint.position.x, rightPoint.position.x);
            _rightBoundX = Mathf.Max(leftPoint.position.x, rightPoint.position.x);
            _hasPatrolBounds = true;
        }

        // Auto-find player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) _target = playerObj.transform;

        if (groundLayer == 0) groundLayer = LayerMask.GetMask("Ground", "Platform");
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
        if (_isDead) return;

        if (followPlayer && _target != null)
        {
            float dist = Vector2.Distance(transform.position, _target.position);
            if (dist < detectionRange)
            {
                FollowTarget();
                CheckIfStuck();
                return;
            }
        }

        if (shouldPatrol)
        {
            Patrol();
        }
        else if (_rb.bodyType != RigidbodyType2D.Dynamic)
        {
            _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
        }
    }

    private void FollowTarget()
    {
        float dirX = _target.position.x - transform.position.x;
        int moveDir = dirX > 0 ? 1 : -1;
        FaceDirection(moveDir);

        if (useGravity)
        {
            _rb.linearVelocity = new Vector2(moveDir * followSpeed, _rb.linearVelocity.y);
        }
        else
        {
            Vector2 nextPosition = _rb.position + Vector2.right * (moveDir * followSpeed * Time.fixedDeltaTime);
            _rb.MovePosition(nextPosition);
        }
    }

    private void CheckIfStuck()
    {
        if (!useGravity) return;

        // Simple check: if we are trying to move but X hasn't changed much
        if (Mathf.Abs(_rb.linearVelocity.x) > 0.1f && Mathf.Abs(transform.position.x - _lastX) < 0.01f)
        {
            _stuckTimer += Time.fixedDeltaTime;
            if (_stuckTimer > 0.5f)
            {
                Jump();
                _stuckTimer = 0;
            }
        }
        else
        {
            _stuckTimer = 0;
        }
        _lastX = transform.position.x;
    }

    private void Jump()
    {
        if (IsGrounded())
        {
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, jumpForce);
        }
    }

    private bool IsGrounded()
    {
        if (_col == null) return false;
        float extraHeight = 0.1f;
        RaycastHit2D hit = Physics2D.Raycast(_col.bounds.center, Vector2.down, _col.bounds.extents.y + extraHeight, groundLayer);
        return hit.collider != null;
    }

    // ────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Reduce health by damage. Flashes red, then destroys on death.
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (_isDead)
            return;

        _currentHealth -= damage;

        if (_currentHealth > 0 && hitSound != null)
            hitSound.Play();

        // Flash red
        if (_sr != null)
        {
            _sr.color = _hitColor;
            _flashTimer = _flashDuration;
        }

        if (_currentHealth <= 0)
        {
            _isDead = true;
            Debug.Log("Enemy defeated!");
            if (deathSound != null)
                deathSound.Play();

            PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
            if (player != null)
                player.FinishCombatTip();

            GameObject combatPopUp = GameObject.Find("CombatPopUp");
            if (combatPopUp != null)
                combatPopUp.SetActive(false);

            if (_sr != null)
                _sr.enabled = false;

            Collider2D enemyCollider = GetComponent<Collider2D>();
            if (enemyCollider != null)
                enemyCollider.enabled = false;

            shouldPatrol = false;
            Destroy(gameObject, 1f);
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
        if (!_hasPatrolBounds)
        {
            _rb.linearVelocity = Vector2.zero;
            return;
        }

        if (transform.position.x <= _leftBoundX)
        {
            _moveDirection = 1;
            FaceDirection(1);
        }
        else if (transform.position.x >= _rightBoundX)
        {
            _moveDirection = -1;
            FaceDirection(-1);
        }

        Vector2 nextPosition = _rb.position + Vector2.right * (_moveDirection * patrolSpeed * Time.fixedDeltaTime);
        nextPosition.x = Mathf.Clamp(nextPosition.x, _leftBoundX, _rightBoundX);
        _rb.MovePosition(nextPosition);
    }

    public void SetPatrolPoints(Vector3 left, Vector3 right)
    {
        _leftBoundX = Mathf.Min(left.x, right.x);
        _rightBoundX = Mathf.Max(left.x, right.x);
        _hasPatrolBounds = true;
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
