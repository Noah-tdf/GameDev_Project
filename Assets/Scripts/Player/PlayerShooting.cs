using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShooting : MonoBehaviour
{
    private enum EquippedWeapon
    {
        Primary,
        Secondary
    }

    [Header("Primary Weapon")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float fireRate = 0.2f;
    [SerializeField] private float bulletSpeed = 14f;
    [SerializeField] private Vector2 primarySpawnOffset = new Vector2(1.2f, 0.35f);
    [SerializeField] private float primaryInputBufferTime = 0.12f;

    [Header("Secondary Weapon")]
    [SerializeField] private Transform secondaryFirePoint;
    [SerializeField] private GameObject secondaryBulletPrefab;
    [SerializeField] private float secondaryFireRate = 0.5f;
    [SerializeField] private float secondaryBulletSpeed = 20f;
    [SerializeField] private Vector2 secondarySpawnOffset = new Vector2(1.2f, 0.2f);
    [SerializeField] private float secondaryInputBufferTime = 0.12f;

    [Header("Weapon Switching")]
    [SerializeField] private EquippedWeapon equippedWeapon = EquippedWeapon.Primary;
    [SerializeField] private Transform primaryWeaponVisual;
    [SerializeField] private Transform secondaryWeaponVisual;

    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement;

    private float nextPrimaryFireTime;
    private float nextSecondaryFireTime;
    private float primaryBufferTimer;
    private float secondaryBufferTimer;

    private void Awake()
    {
        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();

        if (primaryWeaponVisual == null)
            primaryWeaponVisual = transform.Find("Gun");

        if (secondaryWeaponVisual == null)
            secondaryWeaponVisual = transform.Find("GunSecondary");

        UpdateWeaponVisuals();
    }

    private void Update()
    {
        if (Keyboard.current == null && Mouse.current == null) return;

        HandleWeaponSwitchInput();

        bool fireHeld =
            (Keyboard.current != null && Keyboard.current.jKey.isPressed) ||
            (Mouse.current   != null && Mouse.current.leftButton.isPressed);

        bool firePressedThisFrame =
            (Keyboard.current != null && Keyboard.current.jKey.wasPressedThisFrame) ||
            (Mouse.current   != null && Mouse.current.leftButton.wasPressedThisFrame);

        if (equippedWeapon == EquippedWeapon.Primary)
        {
            if (firePressedThisFrame)
            {
                primaryBufferTimer = primaryInputBufferTime;
            }

            if (fireHeld || primaryBufferTimer > 0f)
            {
                bool shotFired = TryShoot(firePoint, bulletPrefab, bulletSpeed, ref nextPrimaryFireTime, fireRate, primarySpawnOffset);
                if (shotFired)
                {
                    primaryBufferTimer = 0f;
                }
            }
        }
        else
        {
            if (firePressedThisFrame)
            {
                secondaryBufferTimer = secondaryInputBufferTime;
            }

            if (fireHeld || secondaryBufferTimer > 0f)
            {
                bool shotFired = TryShoot(secondaryFirePoint, secondaryBulletPrefab, secondaryBulletSpeed, ref nextSecondaryFireTime, secondaryFireRate, secondarySpawnOffset);
                if (shotFired)
                {
                    secondaryBufferTimer = 0f;
                }
            }
        }

        primaryBufferTimer = Mathf.Max(0f, primaryBufferTimer - Time.deltaTime);
        secondaryBufferTimer = Mathf.Max(0f, secondaryBufferTimer - Time.deltaTime);
    }

    private void HandleWeaponSwitchInput()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            EquipWeapon(EquippedWeapon.Primary);
            return;
        }

        if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            EquipWeapon(EquippedWeapon.Secondary);
            return;
        }

        if (Keyboard.current.qKey.wasPressedThisFrame || Keyboard.current.tabKey.wasPressedThisFrame)
        {
            EquippedWeapon nextWeapon = equippedWeapon == EquippedWeapon.Primary ? EquippedWeapon.Secondary : EquippedWeapon.Primary;
            EquipWeapon(nextWeapon);
        }
    }

    private void EquipWeapon(EquippedWeapon weapon)
    {
        if (equippedWeapon == weapon)
        {
            return;
        }

        equippedWeapon = weapon;
        primaryBufferTimer = 0f;
        secondaryBufferTimer = 0f;
        UpdateWeaponVisuals();
    }

    private void UpdateWeaponVisuals()
    {
        if (primaryWeaponVisual != null)
        {
            primaryWeaponVisual.gameObject.SetActive(equippedWeapon == EquippedWeapon.Primary);
        }

        if (secondaryWeaponVisual != null)
        {
            secondaryWeaponVisual.gameObject.SetActive(equippedWeapon == EquippedWeapon.Secondary);
        }
    }

    private bool TryShoot(Transform point, GameObject prefab, float speed, ref float nextFireTime, float rate, Vector2 spawnOffset)
    {
        if (Time.time < nextFireTime) return false;
        if (prefab == null)
        {
            Debug.LogWarning("PlayerShooting: bullet prefab missing.", this);
            return false;
        }

        float direction = playerMovement != null ? playerMovement.FacingDirection : Mathf.Sign(transform.localScale.x);
        if (Mathf.Approximately(direction, 0f))
        {
            direction = 1f;
        }

        Quaternion rotation = direction > 0f ? Quaternion.identity : Quaternion.Euler(0f, 180f, 0f);
        Vector3 spawnPosition = GetSpawnPosition(point, direction, spawnOffset);

        GameObject bulletObj = Instantiate(prefab, spawnPosition, rotation);
        if (!bulletObj.activeSelf)
        {
            bulletObj.SetActive(true);
        }

        Bullet bullet = bulletObj.GetComponent<Bullet>();
        if (bullet != null)
            bullet.Initialize(direction, speed, gameObject);

        nextFireTime = Time.time + Mathf.Max(0.01f, rate);
        return true;
    }

    private Vector3 GetSpawnPosition(Transform point, float direction, Vector2 spawnOffset)
    {
        if (point != null)
        {
            return point.position;
        }

        Vector3 position = transform.position;
        position.x += direction * Mathf.Abs(spawnOffset.x);
        position.y += spawnOffset.y;

        return position;
    }
}
