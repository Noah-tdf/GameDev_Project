using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class DroneEnemy : MonoBehaviour
{
    [Header("Stats")]
    public int health = 2;
    public float patrolRadius = 5f;
    public float moveSpeed = 2f;
    
    [Header("Combat")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float fireRate = 1.5f;
    public float attackRange = 8f;
    public float bulletSpeed = 10f;
    public float bulletRange = 5f;
    public int contactDamage = 1;
    public float stompBounceForce = 12f;

    [Header("Animation/Feedback")]
    public float hoverAmplitude = 0.5f;
    public float hoverFrequency = 2f;
    public Color hitColor = new Color(1f, 0.2f, 0.2f, 1f);
    public float flashDuration = 0.15f;

    [Header("Audio")]
    public AudioClip hitSound;
    public AudioClip shootSound;

    private Vector3 _startPosition;
    private Transform _player;
    private float _fireTimer;
    private Rigidbody2D _rb;
    private SpriteRenderer _sr;
    private float _flashTimer;
    private bool _isDead;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _sr = GetComponent<SpriteRenderer>();
        
        // Ensure Rigidbody settings
        _rb.bodyType = RigidbodyType2D.Kinematic;
        _rb.useFullKinematicContacts = true; // Important for OnCollisionEnter2D on Kinematic
        _rb.gravityScale = 0f;
        _rb.freezeRotation = true;
        
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

        // Tick flash timer
        if (_flashTimer > 0)
        {
            _flashTimer -= Time.deltaTime;
            if (_flashTimer <= 0) _sr.color = Color.white;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, _player.position);
        float hover = Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude;
        Vector3 targetPos = _startPosition + Vector3.up * hover;

        // Try to follow player if in range
        if (distanceToPlayer < attackRange)
        {
            Vector3 dirToPlayer = (_player.position - transform.position).normalized;
            Vector3 desiredPos = transform.position + dirToPlayer * moveSpeed * Time.deltaTime;
            
            // Only move towards player if within patrol radius
            if (Vector2.Distance(_startPosition, desiredPos) <= patrolRadius)
            {
                targetPos = desiredPos + Vector3.up * hover;
            }
            else
            {
                // If at edge, just stay at edge but include hover
                Vector3 edgePos = _startPosition + (desiredPos - _startPosition).normalized * patrolRadius;
                targetPos = edgePos + Vector3.up * hover;
            }
        }

        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * moveSpeed);

        // Turn towards player
        if (_player.position.x < transform.position.x)
            _sr.flipX = true;
        else
            _sr.flipX = false;

        // Combat
        _fireTimer += Time.deltaTime;
        if (distanceToPlayer <= attackRange && _fireTimer >= fireRate)
        {
            Shoot();
            _fireTimer = 0f;
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

    private void Shoot()
    {
        if (bulletPrefab == null || firePoint == null || _player == null) return;

        if (shootSound != null) AudioSource.PlayClipAtPoint(shootSound, transform.position);

        GameObject bulletObj = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        Bullet bullet = bulletObj.GetComponent<Bullet>();
        if (bullet != null)
        {
            Vector2 direction = (_player.position - firePoint.position).normalized;
            bullet.InitializeAimed(direction * bulletSpeed, gameObject, bulletRange);
        }
    }

    public void TakeDamage(int damage)
    {
        if (_isDead) return;

        health -= damage;
        
        if (hitSound != null) AudioSource.PlayClipAtPoint(hitSound, transform.position);

        // Flash feedback
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
        _rb.simulated = false; // Disable physics
        
        // Simple death "animation": spin and fall
        StartCoroutine(DeathAnimation());
    }

    private System.Collections.IEnumerator DeathAnimation()
    {
        float duration = 0.5f;
        float elapsed = 0f;
        Vector3 startScale = transform.localScale;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            transform.Rotate(0, 0, 720 * Time.deltaTime);
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            _sr.color = Color.Lerp(Color.white, new Color(1, 1, 1, 0), t);
            
            yield return null;
        }
        
        Destroy(gameObject);
    }
    }
