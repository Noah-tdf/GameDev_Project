using UnityEngine;

/// <summary>
/// Base enemy used for UbuntyWalker in Level 1 and various enemies in Final Level.
/// Patrols between two points, handles slope traversal, takes damage from bullets, 
/// and damages/bounces the player on contact.
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
    [SerializeField] private float maxSlopeAngle = 60f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Contact Damage")]
    [SerializeField] private int contactDamage = 1;         // HP taken by player on side hit
    [SerializeField] private float stompBounceForce = 10f;  // Upward push when player stomps

    [Header("Sound")]
    [SerializeField] private AudioSource hitSoundSource;
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioSource deathSound;

    private Rigidbody2D _rb;
    private SpriteRenderer _sr;
    private Collider2D _collider;
    private int _currentHealth;
    private int _moveDirection = 1;
    private float _leftBoundX;
    private float _rightBoundX;
    private bool _hasPatrolBounds;
    private bool _isDead;

    private Vector2 _slopeNormalPerp;
    private bool _isOnSlope;

    // Flash-on-hit state
    private float _flashTimer;
    private readonly Color _hitColor = new Color(1f, 0.2f, 0.2f, 1f);
    private readonly float _flashDuration = 0.12f;

    // ────────────────────────────────────────────────────────────────────────
    public void SetPatrolPoints(Vector3 left, Vector3 right)
    {
        _leftBoundX = Mathf.Min(left.x, right.x);
        _rightBoundX = Mathf.Max(left.x, right.x);
        _hasPatrolBounds = true;
        shouldPatrol = true;
    }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _sr = GetComponent<SpriteRenderer>();
        _collider = GetComponent<Collider2D>();
        _currentHealth = maxHealth;

        if (hitSoundSource == null)
            hitSoundSource = GetComponent<AudioSource>();
        
        if (hitSoundSource == null)
            hitSoundSource = gameObject.AddComponent<AudioSource>();

        if (_rb != null)
        {
            _rb.bodyType = RigidbodyType2D.Dynamic;
            _rb.gravityScale = 1f;
            _rb.freezeRotation = true;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        if (groundLayer.value == 0)
        {
            int layer = LayerMask.NameToLayer("Ground");
            if (layer != -1) groundLayer = 1 << layer;
        }

        if (leftPoint != null && rightPoint != null)
        {
            _leftBoundX = Mathf.Min(leftPoint.position.x, rightPoint.position.x);
            _rightBoundX = Mathf.Max(leftPoint.position.x, rightPoint.position.x);
            _hasPatrolBounds = true;
        }
    }

    private void Update()
    {
        if (_isDead) return;

        // Tick the damage-flash timer
        if (_flashTimer > 0f)
        {
            _flashTimer -= Time.deltaTime;
            if (_flashTimer <= 0f && _sr != null)
                _sr.color = Color.white;
        }

        SlopeCheck();
    }

    private void FixedUpdate()
    {
        if (_isDead) return;

        if (!shouldPatrol)
        {
            _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
            return;
        }

        Patrol();
    }

    private void SlopeCheck()
    {
        if (_collider == null) return;
        Vector2 checkPos = (Vector2)transform.position + _collider.offset;
        float castDistance = (_collider.bounds.size.y / 2f) + 0.2f;
        RaycastHit2D hit = Physics2D.Raycast(checkPos, Vector2.down, castDistance, groundLayer);

        if (hit)
        {
            _slopeNormalPerp = Vector2.Perpendicular(hit.normal).normalized;
            float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
            _isOnSlope = slopeAngle != 0 && slopeAngle <= maxSlopeAngle;
            
            // Visual debug
            Debug.DrawRay(hit.point, hit.normal, Color.green);
            Debug.DrawRay(hit.point, _slopeNormalPerp, Color.blue);
        }
        else
        {
            _isOnSlope = false;
        }
    }

    public void TakeDamage(int damage)
    {
        if (_isDead) return;

        _currentHealth -= damage;

        if (_currentHealth > 0 && hitSound != null && hitSoundSource != null)
            hitSoundSource.PlayOneShot(hitSound);

        if (_sr != null)
        {
            _sr.color = _hitColor;
            _flashTimer = _flashDuration;
        }

        if (_currentHealth <= 0)
        {
            _isDead = true;
            if (deathSound != null) deathSound.Play();

            PlayerMovement player = Object.FindFirstObjectByType<PlayerMovement>();
            if (player != null) player.FinishCombatTip();

            GameObject combatPopUp = GameObject.Find("CombatPopUp");
            if (combatPopUp != null) combatPopUp.SetActive(false);

            if (_sr != null) _sr.enabled = false;
            if (_collider != null) _collider.enabled = false;

            shouldPatrol = false;
            _rb.linearVelocity = Vector2.zero;
            _rb.isKinematic = true;
            Destroy(gameObject, 1f);
        }
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        if (_isDead || !col.gameObject.CompareTag("Player")) return;

        PlayerHealth ph = col.gameObject.GetComponent<PlayerHealth>();
        Rigidbody2D playerRb = col.gameObject.GetComponent<Rigidbody2D>();

        if (ph == null || playerRb == null) return;

        bool playerAbove = col.contacts[0].normal.y < -0.5f;

        if (playerAbove)
        {
            TakeDamage(1);
            playerRb.linearVelocity = new Vector2(playerRb.linearVelocity.x, stompBounceForce);
        }
        else
        {
            ph.TakeDamage(contactDamage);
        }
    }

    private void Patrol()
    {
        if (!_hasPatrolBounds)
        {
            _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
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

        if (_isOnSlope)
        {
            // Move along the slope. -_moveDirection because Vector2.Perpendicular usually points "left"
            _rb.linearVelocity = new Vector2(patrolSpeed * _slopeNormalPerp.x * -_moveDirection, patrolSpeed * _slopeNormalPerp.y * -_moveDirection);
        }
        else
        {
            _rb.linearVelocity = new Vector2(_moveDirection * patrolSpeed, _rb.linearVelocity.y);
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

