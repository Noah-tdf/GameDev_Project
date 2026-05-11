using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShooting : MonoBehaviour
{
    [Header("Shooting")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float fireRate = 0.2f;
    [SerializeField] private float bulletSpeed = 14f;
    [SerializeField] private Vector2 spawnOffset = new Vector2(0.9f, 0.15f);

    [Header("Visuals")]
    [SerializeField] private Transform primaryWeaponVisual;
    [SerializeField] private Transform secondaryWeaponVisual;

    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private AudioSource shootingSound;

    private float nextFireTime;

    private void Awake()
    {
        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();

        if (shootingSound == null)
            shootingSound = GetComponent<AudioSource>();

        if (primaryWeaponVisual == null)
            primaryWeaponVisual = transform.Find("Gun");

        if (secondaryWeaponVisual == null)
            secondaryWeaponVisual = transform.Find("GunSecondary");

        ShowPrimaryWeaponOnly();
    }

    private void OnEnable()
    {
        ShowPrimaryWeaponOnly();
    }

    private void Update()
    {
        if (Keyboard.current == null && Mouse.current == null)
            return;

        bool shootHeld =
            (Keyboard.current != null && Keyboard.current.jKey.isPressed) ||
            (Mouse.current != null && Mouse.current.leftButton.isPressed);

        if (shootHeld)
            Shoot();
    }

    private void Shoot()
    {
        if (Time.time < nextFireTime)
            return;

        if (bulletPrefab == null)
        {
            Debug.LogWarning("PlayerShooting: bullet prefab missing.", this);
            return;
        }

        float direction = GetFacingDirection();
        Vector3 spawnPosition = transform.position;
        spawnPosition.x += direction * Mathf.Abs(spawnOffset.x);
        spawnPosition.y += spawnOffset.y;

        Quaternion rotation = direction > 0f ? Quaternion.identity : Quaternion.Euler(0f, 180f, 0f);
        GameObject bulletObj = Instantiate(bulletPrefab, spawnPosition, rotation);

        if (!bulletObj.activeSelf)
            bulletObj.SetActive(true);

        Bullet bullet = bulletObj.GetComponent<Bullet>();
        if (bullet != null)
            bullet.Initialize(direction, bulletSpeed, gameObject);

        if (shootingSound != null)
            shootingSound.Play();

        nextFireTime = Time.time + Mathf.Max(0.01f, fireRate);
    }

    private float GetFacingDirection()
    {
        if (playerMovement != null)
            return playerMovement.FacingDirection;

        return transform.localScale.x >= 0f ? 1f : -1f;
    }

    private void ShowPrimaryWeaponOnly()
    {
        if (primaryWeaponVisual != null)
            primaryWeaponVisual.gameObject.SetActive(true);

        if (secondaryWeaponVisual != null)
            secondaryWeaponVisual.gameObject.SetActive(false);
    }
}
