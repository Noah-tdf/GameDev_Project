using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class PlayerShooting : MonoBehaviour
{
    private const string EquippedPrimaryKey = "EquippedPrimaryWeapon";
    private const string EquippedSecondaryKey = "EquippedSecondaryWeapon";
    private const string EquippedWeaponSlotKey = "EquippedWeaponSlot";

    private enum WeaponSlot
    {
        Primary,
        Secondary
    }

    [Header("Primary Weapon")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float fireRate = 0.2f;
    [SerializeField] private float bulletSpeed = 14f;
    [SerializeField] private Vector2 primarySpawnOffset = new Vector2(0.9f, 0.15f);

    [Header("Secondary Weapon")]
    [SerializeField] private Transform secondaryFirePoint;
    [SerializeField] private GameObject secondaryBulletPrefab;
    [SerializeField] private float secondaryFireRate = 0.5f;
    [SerializeField] private float secondaryBulletSpeed = 20f;
    [SerializeField] private Vector2 secondarySpawnOffset = new Vector2(0.9f, 0.05f);

    [Header("Visuals")]
    [SerializeField] private WeaponSlot equippedWeapon;
    [SerializeField] private Transform primaryWeaponVisual;
    [SerializeField] private Transform secondaryWeaponVisual;

    [Header("Weapon HUD")]
    [SerializeField] private bool createWeaponHud = true;
    [SerializeField] private Sprite weaponHudFrameSprite;
    [SerializeField] private Sprite primaryWeaponIcon;
    [SerializeField] private Sprite secondaryWeaponIcon;
    [SerializeField] private Color activeColor = new Color(0.3f, 1f, 0.3f, 1f);
    [SerializeField] private Color inactiveColor = new Color(0.35f, 0.35f, 0.4f, 0.85f);

    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private AudioSource shootingSound;

    [SerializeField, HideInInspector] private Vector2 spawnOffset = new Vector2(0.9f, 0.15f);

    private Image primarySlotPanel;
    private Image secondarySlotPanel;
    private Image primaryIconImage;
    private Image secondaryIconImage;
    private TextMeshProUGUI primaryLabel;
    private TextMeshProUGUI secondaryLabel;
    private GameObject weaponSwitchPopUp;
    private float nextFireTime;

    private bool IsPrimaryEquipped => equippedWeapon == WeaponSlot.Primary;

    private void Awake()
    {
        ResolveReferences();
        CreateWeaponHud();
        EquipWeapon(equippedWeapon, true);
    }

    private void OnEnable()
    {
        ResolveReferences();
        CreateWeaponHud();
        EquipWeapon(equippedWeapon, true);
    }

    private void Update()
    {
        HandleWeaponSwitchInput();

        if (Keyboard.current == null && Mouse.current == null)
            return;

        bool shootHeld =
            (Keyboard.current != null && Keyboard.current.jKey.isPressed) ||
            (Mouse.current != null && Mouse.current.leftButton.isPressed);

        if (shootHeld)
            Shoot();
    }

    private void HandleWeaponSwitchInput()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;
        bool switched = false;

        if (keyboard != null)
        {
            if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame)
            {
                EquipWeapon(WeaponSlot.Primary);
                switched = true;
            }

            if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame)
            {
                EquipWeapon(WeaponSlot.Secondary);
                switched = true;
            }

            if (keyboard.qKey.wasPressedThisFrame || keyboard.tabKey.wasPressedThisFrame)
            {
                ToggleWeapon();
                switched = true;
            }
        }

        if (mouse != null && Mathf.Abs(mouse.scroll.ReadValue().y) > 0.01f)
        {
            ToggleWeapon();
            switched = true;
        }

        if (switched)
            DismissWeaponSwitchPopUp();
    }

    private void DismissWeaponSwitchPopUp()
    {
        if (weaponSwitchPopUp == null)
            weaponSwitchPopUp = GameObject.Find("WeaponSwitchPopUp");

        if (weaponSwitchPopUp != null && weaponSwitchPopUp.activeSelf)
            weaponSwitchPopUp.SetActive(false);
    }

    private void Shoot()
    {
        if (Time.time < nextFireTime)
            return;

        GameObject currentBulletPrefab = GetCurrentBulletPrefab();
        if (currentBulletPrefab == null)
        {
            Debug.LogWarning("PlayerShooting: bullet prefab missing.", this);
            return;
        }

        float direction = GetFacingDirection();
        Vector3 spawnPosition = GetCurrentSpawnPosition(direction);
        Quaternion rotation = direction > 0f ? Quaternion.identity : Quaternion.Euler(0f, 180f, 0f);
        GameObject bulletObj = Instantiate(currentBulletPrefab, spawnPosition, rotation);

        if (!bulletObj.activeSelf)
            bulletObj.SetActive(true);

        Bullet bullet = bulletObj.GetComponent<Bullet>();
        if (bullet != null)
            bullet.Initialize(direction, GetCurrentBulletSpeed(), gameObject);

        if (shootingSound != null)
            shootingSound.Play();

        nextFireTime = Time.time + Mathf.Max(0.01f, GetCurrentFireRate());
    }

    private void EquipWeapon(WeaponSlot weaponSlot, bool forceRefresh = false)
    {
        if (!forceRefresh && equippedWeapon == weaponSlot)
            return;

        equippedWeapon = weaponSlot;
        nextFireTime = 0f;
        RefreshWeaponVisuals();
        RefreshWeaponHud();
    }

    private void ToggleWeapon()
    {
        EquipWeapon(IsPrimaryEquipped ? WeaponSlot.Secondary : WeaponSlot.Primary);
    }

    private void ResolveReferences()
    {
        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();

        if (shootingSound == null)
            shootingSound = GetComponent<AudioSource>();

        string savedWeaponSlot = PlayerPrefs.GetString(EquippedWeaponSlotKey, string.Empty);
        if (savedWeaponSlot == "Secondary")
            equippedWeapon = WeaponSlot.Secondary;
        else if (savedWeaponSlot == "Primary")
            equippedWeapon = WeaponSlot.Primary;

        if (firePoint == null)
            firePoint = transform.Find("FirePoint");

        if (secondaryFirePoint == null)
            secondaryFirePoint = transform.Find("FirePointSecondary");

        if (primaryWeaponVisual == null)
            primaryWeaponVisual = transform.Find("Gun");

        if (secondaryWeaponVisual == null)
            secondaryWeaponVisual = transform.Find("GunSecondary");

        if (primaryWeaponIcon == null)
            primaryWeaponIcon = GetSpriteFromVisual(primaryWeaponVisual);

        if (secondaryWeaponIcon == null)
            secondaryWeaponIcon = GetSpriteFromVisual(secondaryWeaponVisual);

#if UNITY_EDITOR
        ApplySavedWeaponSpritesInEditor();

        if (weaponHudFrameSprite == null)
            weaponHudFrameSprite = LoadEditorSprite("Assets/Art/Noah's/Gemini_Generated_Image_jex9o0jex9o0jex9-removebg-preview.png");

        if (primaryWeaponIcon == null)
            primaryWeaponIcon = LoadEditorSprite("Assets/Art/Weapons/weapon-primary.png");

        if (secondaryWeaponIcon == null)
            secondaryWeaponIcon = LoadEditorSprite("Assets/Art/Weapons/weapon-secondary.png");
#endif
    }

#if UNITY_EDITOR
    private void ApplySavedWeaponSpritesInEditor()
    {
        Sprite savedPrimary = LoadSavedWeaponSprite(PlayerPrefs.GetString(EquippedPrimaryKey, string.Empty));
        if (savedPrimary != null)
        {
            primaryWeaponIcon = savedPrimary;
            SetVisualSprite(primaryWeaponVisual, savedPrimary);
        }

        Sprite savedSecondary = LoadSavedWeaponSprite(PlayerPrefs.GetString(EquippedSecondaryKey, string.Empty));
        if (savedSecondary != null)
        {
            secondaryWeaponIcon = savedSecondary;
            SetVisualSprite(secondaryWeaponVisual, savedSecondary);
        }
    }

    private Sprite LoadSavedWeaponSprite(string weaponId)
    {
        if (string.IsNullOrEmpty(weaponId))
            return null;

        string path = weaponId == "weapon-primary"
            ? "Assets/Art/Weapons/weapon-primary.png"
            : "Assets/Art/Weapons/" + weaponId + ".png";

        return LoadEditorSprite(path);
    }

    private Sprite LoadEditorSprite(string path)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite != null)
            return sprite;

        Object[] sprites = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);
        foreach (Object asset in sprites)
        {
            if (asset is Sprite spriteAsset)
                return spriteAsset;
        }

        return null;
    }
#endif

    private void SetVisualSprite(Transform weaponVisual, Sprite sprite)
    {
        if (weaponVisual == null || sprite == null)
            return;

        SpriteRenderer spriteRenderer = weaponVisual.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            spriteRenderer.sprite = sprite;
    }

    private Sprite GetSpriteFromVisual(Transform weaponVisual)
    {
        if (weaponVisual == null)
            return null;

        SpriteRenderer spriteRenderer = weaponVisual.GetComponent<SpriteRenderer>();
        return spriteRenderer != null ? spriteRenderer.sprite : null;
    }

    private GameObject GetCurrentBulletPrefab()
    {
        if (!IsPrimaryEquipped && secondaryBulletPrefab != null)
            return secondaryBulletPrefab;

        return bulletPrefab;
    }

    private float GetCurrentFireRate()
    {
        return IsPrimaryEquipped ? fireRate : secondaryFireRate;
    }

    private float GetCurrentBulletSpeed()
    {
        return IsPrimaryEquipped ? bulletSpeed : secondaryBulletSpeed;
    }

    private Vector2 GetCurrentSpawnOffset()
    {
        if (!IsPrimaryEquipped)
            return secondarySpawnOffset;

        return primarySpawnOffset == Vector2.zero ? spawnOffset : primarySpawnOffset;
    }

    private Vector3 GetCurrentSpawnPosition(float direction)
    {
        Transform currentFirePoint = IsPrimaryEquipped ? firePoint : secondaryFirePoint;
        if (currentFirePoint != null)
            return currentFirePoint.position;

        Vector2 offset = GetCurrentSpawnOffset();
        Vector3 spawnPosition = transform.position;
        spawnPosition.x += direction * Mathf.Abs(offset.x);
        spawnPosition.y += offset.y;
        return spawnPosition;
    }

    private float GetFacingDirection()
    {
        if (playerMovement != null)
            return playerMovement.FacingDirection;

        return transform.localScale.x >= 0f ? 1f : -1f;
    }

    private void RefreshWeaponVisuals()
    {
        if (primaryWeaponVisual != null)
            primaryWeaponVisual.gameObject.SetActive(IsPrimaryEquipped);

        if (secondaryWeaponVisual != null)
            secondaryWeaponVisual.gameObject.SetActive(!IsPrimaryEquipped);
    }

    private void CreateWeaponHud()
    {
        if (!createWeaponHud || primarySlotPanel != null)
            return;

        Canvas canvas = FindOrCreateWeaponCanvas();
        RectTransform root = CreateRect("WeaponHUDRoot", canvas.transform);
        root.anchorMin = new Vector2(1f, 0f);
        root.anchorMax = new Vector2(1f, 0f);
        root.pivot = new Vector2(1f, 0f);
        root.anchoredPosition = new Vector2(-24f, 24f);
        root.sizeDelta = new Vector2(390f, 137f);

        Image frame = CreateImage("Frame", root, weaponHudFrameSprite, Color.white);
        RectTransform frameRect = frame.rectTransform;
        frameRect.anchorMin = Vector2.zero;
        frameRect.anchorMax = Vector2.one;
        frameRect.offsetMin = Vector2.zero;
        frameRect.offsetMax = Vector2.zero;
        frame.preserveAspect = true;

        primarySlotPanel = CreateSlotPanel(root, "PrimarySlot", new Vector2(-96f, 8f));
        secondarySlotPanel = CreateSlotPanel(root, "SecondarySlot", new Vector2(96f, 8f));

        primaryIconImage = CreateWeaponIcon(primarySlotPanel.rectTransform, "PrimaryIcon", primaryWeaponIcon);
        secondaryIconImage = CreateWeaponIcon(secondarySlotPanel.rectTransform, "SecondaryIcon", secondaryWeaponIcon);

        primaryLabel = CreateLabel(root, "PrimaryLabel", "PRIMARY", new Vector2(-96f, 34f), new Vector2(150f, 26f), 21f);
        secondaryLabel = CreateLabel(root, "SecondaryLabel", "SECONDARY", new Vector2(96f, 34f), new Vector2(170f, 26f), 21f);
    }

    private Canvas FindOrCreateWeaponCanvas()
    {
        GameObject existingCanvas = GameObject.Find("WeaponHUDCanvas");
        if (existingCanvas != null && existingCanvas.TryGetComponent(out Canvas foundCanvas))
            return foundCanvas;

        GameObject canvasObject = new GameObject("WeaponHUDCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GraphicRaycaster raycaster = canvasObject.GetComponent<GraphicRaycaster>();
        raycaster.enabled = false;

        return canvas;
    }

    private Image CreateSlotPanel(RectTransform parent, string name, Vector2 anchoredPosition)
    {
        // Invisible container — active state is shown via a bottom accent bar only.
        Image panel = CreateImage(name, parent, null, Color.clear);
        RectTransform rectTransform = panel.rectTransform;
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = new Vector2(150f, 78f);

        // Thin accent bar at the bottom of the slot.
        Image accent = CreateImage(name + "Accent", rectTransform, null, Color.clear);
        RectTransform accentRect = accent.rectTransform;
        accentRect.anchorMin = new Vector2(0.1f, 0f);
        accentRect.anchorMax = new Vector2(0.9f, 0f);
        accentRect.pivot = new Vector2(0.5f, 0f);
        accentRect.anchoredPosition = new Vector2(0f, 4f);
        accentRect.sizeDelta = new Vector2(0f, 3f);

        return panel;
    }

    private Image CreateWeaponIcon(RectTransform parent, string name, Sprite sprite)
    {
        Image icon = CreateImage(name, parent, sprite, Color.white);
        RectTransform rectTransform = icon.rectTransform;
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = new Vector2(0f, -12f);
        rectTransform.sizeDelta = new Vector2(112f, 48f);
        icon.preserveAspect = true;
        return icon;
    }

    private TextMeshProUGUI CreateLabel(RectTransform parent, string name, string text, Vector2 anchoredPosition, Vector2 size, float fontSize)
    {
        GameObject labelObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(parent, false);

        RectTransform rectTransform = labelObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = fontSize;
        label.fontStyle = FontStyles.Bold;
        label.enableAutoSizing = true;
        label.fontSizeMin = 9f;
        label.fontSizeMax = fontSize;
        label.raycastTarget = false;
        return label;
    }

    private Image CreateImage(string name, Transform parent, Sprite sprite, Color color)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);

        Image image = imageObject.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private RectTransform CreateRect(string name, Transform parent)
    {
        GameObject rectObject = new GameObject(name, typeof(RectTransform));
        rectObject.transform.SetParent(parent, false);
        return rectObject.GetComponent<RectTransform>();
    }

    private void RefreshWeaponHud()
    {
        if (primarySlotPanel == null || secondarySlotPanel == null)
            return;

        // Slot panels stay transparent — only the accent bar, icon and label carry the active color.
        RefreshSlotAccent(primarySlotPanel, IsPrimaryEquipped);
        RefreshSlotAccent(secondarySlotPanel, !IsPrimaryEquipped);

        SetGraphicColor(primaryIconImage, IsPrimaryEquipped ? activeColor : inactiveColor);
        SetGraphicColor(secondaryIconImage, IsPrimaryEquipped ? inactiveColor : activeColor);

        SetTextColor(primaryLabel, IsPrimaryEquipped ? activeColor : inactiveColor);
        SetTextColor(secondaryLabel, IsPrimaryEquipped ? inactiveColor : activeColor);
    }

    private void RefreshSlotAccent(Image slotPanel, bool isActive)
    {
        // The accent bar is the first child of the slot panel.
        if (slotPanel.rectTransform.childCount == 0)
            return;

        Transform accentTransform = slotPanel.rectTransform.GetChild(0);
        if (accentTransform.TryGetComponent(out Image accentImage))
            accentImage.color = isActive ? activeColor : Color.clear;
    }

    private void SetGraphicColor(Graphic graphic, Color color)
    {
        if (graphic != null)
            graphic.color = color;
    }

    private void SetTextColor(TMP_Text text, Color color)
    {
        if (text != null)
            text.color = color;
    }
}
