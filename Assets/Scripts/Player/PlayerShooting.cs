using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShooting : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject bulletPrefab;

    [Header("Shooting")]
    [SerializeField] private float fireRate = 0.25f;
    [SerializeField] private float bulletSpeed = 12f;

    private float nextFireTime;

    private void Awake()
    {
        if (playerMovement == null)
        {
            playerMovement = GetComponent<PlayerMovement>();
        }
    }

    private void Update()
    {
        bool shootPressed = false;

        if (Keyboard.current != null && Keyboard.current.jKey.wasPressedThisFrame)
        {
            shootPressed = true;
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            shootPressed = true;
        }

        if (shootPressed)
        {
            TryShoot();
        }
    }

    private void TryShoot()
    {
        if (Time.time < nextFireTime)
        {
            return;
        }

        if (firePoint == null || bulletPrefab == null)
        {
            Debug.LogWarning("PlayerShooting: Fire Point or Bullet Prefab is missing.", this);
            return;
        }

        float direction = playerMovement != null ? playerMovement.FacingDirection : Mathf.Sign(transform.localScale.x);
        Quaternion rotation = direction > 0f ? Quaternion.identity : Quaternion.Euler(0f, 180f, 0f);

        GameObject bulletObject = Instantiate(bulletPrefab, firePoint.position, rotation);
        bulletObject.SetActive(true);
        Bullet bullet = bulletObject.GetComponent<Bullet>();

        if (bullet != null)
        {
            bullet.Initialize(direction, bulletSpeed, gameObject);
        }

        nextFireTime = Time.time + fireRate;
    }
}
