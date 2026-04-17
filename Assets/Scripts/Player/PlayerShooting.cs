using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShooting : MonoBehaviour
{
    [Header("Primary Weapon")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float fireRate = 0.2f;
    [SerializeField] private float bulletSpeed = 14f;

    [Header("Secondary Weapon")]
    [SerializeField] private Transform secondaryFirePoint;
    [SerializeField] private GameObject secondaryBulletPrefab;
    [SerializeField] private float secondaryFireRate = 0.5f;
    [SerializeField] private float secondaryBulletSpeed = 20f;

    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement;

    private float nextPrimaryFireTime;
    private float nextSecondaryFireTime;

    private void Awake()
    {
        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();
    }

    private void Update()
    {
        if (Keyboard.current == null && Mouse.current == null) return;

        bool primaryPressed =
            (Keyboard.current != null && Keyboard.current.jKey.wasPressedThisFrame) ||
            (Mouse.current   != null && Mouse.current.leftButton.wasPressedThisFrame);

        bool secondaryPressed =
            (Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame) ||
            (Mouse.current   != null && Mouse.current.rightButton.wasPressedThisFrame);

        if (primaryPressed)   TryShoot(firePoint,          bulletPrefab,          bulletSpeed,          ref nextPrimaryFireTime,   fireRate);
        if (secondaryPressed) TryShoot(secondaryFirePoint, secondaryBulletPrefab, secondaryBulletSpeed, ref nextSecondaryFireTime, secondaryFireRate);
    }

    private void TryShoot(Transform point, GameObject prefab, float speed, ref float nextFireTime, float rate)
    {
        if (Time.time < nextFireTime) return;
        if (point == null || prefab == null)
        {
            Debug.LogWarning("PlayerShooting: fire point or bullet prefab missing.", this);
            return;
        }

        float direction = playerMovement != null ? playerMovement.FacingDirection : Mathf.Sign(transform.localScale.x);
        Quaternion rotation = direction > 0f ? Quaternion.identity : Quaternion.Euler(0f, 180f, 0f);

        GameObject bulletObj = Instantiate(prefab, point.position, rotation);
        Bullet bullet = bulletObj.GetComponent<Bullet>();
        if (bullet != null)
            bullet.Initialize(direction, speed, gameObject);

        nextFireTime = Time.time + rate;
    }
}
