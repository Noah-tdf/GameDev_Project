using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class GroundRobotEnemy : MonoBehaviour
{
    [Header("Stats")]
    public int health = 3;
    public float patrolRadius = 5f;
    public float moveSpeed = 2f;
    
    [Header("Combat")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float fireRate = 1.2f;
    public float attackRange = 10f;
    public float bulletSpeed = 12f;
    public float bulletRange = 6f;
    public int contactDamage = 1;
public float stompBounceForce = 12f;

    [Header("Animation/Feedback")]
    public Color hitColor = new Color(1f, 0.2f, 0.2f, 1f);
    public float flashDuration = 0.15f;
    public Transform gunTransform;

    private Vector3 _startPosition;
    private Transform _player;
    private float _fireTimer;
    private Rigidbody2D _rb;
    private SpriteRenderer _sr;
    private Animator _anim;
    private float _flashTimer;
    private bool _isDead;

    private static readonly int IsRunningHash = Animator.StringToHash("IsRunning");
    private static readonly int IsShootingHash = Animator.StringToHash("IsShooting");

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _sr = GetComponent<SpriteRenderer>();
        _anim = GetComponent<Animator>();
        
        if (gunTransform == null)
        {
            gunTransform = transform.Find("RobotGun");
        }

        _rb.bodyType = RigidbodyType2D.Dynamic;
        _rb.freezeRotation = true;
        _rb.gravityScale = 1f;
        
        _startPosition = transform.position;
    }

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) _player = playerObj.transform;
    }

    private void Update()
    {
        if (_isDead || _player == null) return;

        // Feedback timer
        if (_flashTimer > 0)
        {
            _flashTimer -= Time.deltaTime;
            if (_flashTimer <= 0) _sr.color = Color.white;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, _player.position);
        bool playerInRange = distanceToPlayer <= attackRange;

        // Movement logic
        float moveInput = 0f;

        if (playerInRange)
        {
            // Turn towards player
            bool shouldFlip = _player.position.x < transform.position.x;
            _sr.flipX = shouldFlip;

            if (gunTransform != null)
            {
                // Mirror gun position and flip its sprite if it has one
                Vector3 gunPos = gunTransform.localPosition;
                gunPos.x = Mathf.Abs(gunPos.x) * (shouldFlip ? -1 : 1);
                gunTransform.localPosition = gunPos;
                
                var gunSR = gunTransform.GetComponent<SpriteRenderer>();
                if (gunSR != null) gunSR.flipX = shouldFlip;
            }

            // Combat
            _fireTimer += Time.deltaTime;
            if (_fireTimer >= fireRate)
            {
                Shoot();
                _fireTimer = 0f;
            }

            // Move towards player if not too close, but stay in patrol radius
            float xDiff = _player.position.x - transform.position.x;
            if (Mathf.Abs(xDiff) > 2f) 
            {
                float desiredX = transform.position.x + Mathf.Sign(xDiff) * moveSpeed * Time.deltaTime;
                if (Mathf.Abs(desiredX - _startPosition.x) <= patrolRadius)
                {
                    moveInput = Mathf.Sign(xDiff);
                }
            }
        }
        else
        {
            float xDiffToStart = _startPosition.x - transform.position.x;
            if (Mathf.Abs(xDiffToStart) > 0.5f)
            {
                moveInput = Mathf.Sign(xDiffToStart);
                bool shouldFlip = moveInput < 0;
                _sr.flipX = shouldFlip;

                if (gunTransform != null)
                {
                    Vector3 gunPos = gunTransform.localPosition;
                    gunPos.x = Mathf.Abs(gunPos.x) * (shouldFlip ? -1 : 1);
                    gunTransform.localPosition = gunPos;
                    var gunSR = gunTransform.GetComponent<SpriteRenderer>();
                    if (gunSR != null) gunSR.flipX = shouldFlip;
                }
            }
        }

        // Apply movement
        _rb.linearVelocity = new Vector2(moveInput * moveSpeed, _rb.linearVelocity.y);

        // Animation
        if (_anim != null)
        {
            _anim.SetBool(IsRunningHash, Mathf.Abs(moveInput) > 0.1f);
        }
    }

    private void Shoot()
    {
        if (bulletPrefab == null || firePoint == null || _player == null) return;

        if (_anim != null) _anim.SetTrigger(IsShootingHash);

        GameObject bulletObj = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        Bullet bullet = bulletObj.GetComponent<Bullet>();
        if (bullet != null)
        {
            Vector2 direction = (_player.position - firePoint.position).normalized;
            bullet.InitializeAimed(direction * bulletSpeed, gameObject, bulletRange);
        }
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        if (_isDead || !col.gameObject.CompareTag("Player")) return;

        PlayerHealth ph = col.gameObject.GetComponent<PlayerHealth>();
        Rigidbody2D playerRb = col.gameObject.GetComponent<Rigidbody2D>();

        if (ph == null || playerRb == null) return;

        // Determine if player is stomping (normal points down)
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

    public void TakeDamage(int damage)
    {
        if (_isDead) return;

        health -= damage;
        _sr.color = hitColor;
        _flashTimer = flashDuration;

        if (health <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        _isDead = true;
        _rb.simulated = false; 
        
        if (_anim != null)
        {
            // Assuming there is a "Die" trigger or state
            _anim.SetTrigger("Die");
            Destroy(gameObject, 2f); // Give time for animation
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
