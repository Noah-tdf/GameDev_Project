using UnityEngine;

public class EnemyAimedShooting : MonoBehaviour
{
    [Header("Combat")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireRate = 2f;
    [SerializeField] private float attackRange = 15f;
    [SerializeField] private float bulletSpeed = 8f;
    [SerializeField] private float bulletRange = 10f;

    [Header("Audio")]
    [SerializeField] private AudioClip shootSound;

    private Transform _player;
    private float _fireTimer;
    private AudioSource _audioSource;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
        }
    }

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) _player = playerObj.transform;
        
        if (firePoint == null) firePoint = transform;
    }

    private void Update()
    {
        if (_player == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, _player.position);
        
        _fireTimer += Time.deltaTime;
        if (distanceToPlayer <= attackRange && _fireTimer >= fireRate)
        {
            Shoot();
            _fireTimer = 0f;
        }
    }

    private void Shoot()
    {
        if (bulletPrefab == null || _player == null) return;

        if (shootSound != null) _audioSource.PlayOneShot(shootSound);

        GameObject bulletObj = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        Bullet bullet = bulletObj.GetComponent<Bullet>();
        if (bullet != null)
        {
            Vector2 direction = (_player.position - firePoint.position).normalized;
            bullet.InitializeAimed(direction * bulletSpeed, gameObject, bulletRange);
        }
    }
}
