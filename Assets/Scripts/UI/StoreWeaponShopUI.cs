using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class StoreWeaponShopUI : MonoBehaviour
{
    public const string CreditsKey = "PlayerCredits";
    public const string CreditsInitializedKey = "PlayerCreditsInitialized_v2";
    public const string EquippedPrimaryKey = "EquippedPrimaryWeapon";
    public const string EquippedSecondaryKey = "EquippedSecondaryWeapon";
    public const string EquippedWeaponSlotKey = "EquippedWeaponSlot";
    private const int StartingCredits = 0;
    private const int WeaponPrice = 1;

    // Reset credits when the game application is closed
    private void OnApplicationQuit()
    {
        PlayerPrefs.DeleteKey(CreditsKey);
        PlayerPrefs.DeleteKey(CreditsInitializedKey);
        PlayerPrefs.Save();
    }

    private static readonly HashSet<string> excludedPrimaryWeaponFiles = new HashSet<string>
    {
        "ERA Weapon Primary (1).png",
        "ERA Weapon Primary (37).png",
        "ERA Weapon Primary (38).png",
        "ERA Weapon Primary (57).png",
        "ERA Weapon Primary (58).png",
        "ERA Weapon Primary (62).png"
    };

    private readonly struct WeaponProfile
    {
        public WeaponProfile(string name, int damage, int fireRate, string description)
        {
            Name = name;
            Damage = damage;
            FireRate = fireRate;
            Description = description;
        }

        public string Name { get; }
        public int Damage { get; }
        public int FireRate { get; }
        public string Description { get; }
    }

    private static readonly Dictionary<string, WeaponProfile> weaponProfiles = new Dictionary<string, WeaponProfile>
    {
        { "weapon-primary",              new WeaponProfile("Starter Rifle",        35, 480, "Standard-issue kinetic carbine. Reliable but unremarkable.") },
        { "ERA Weapon Primary (36)",     new WeaponProfile("Pulse Carbine",        42, 620, "Burst-cooled pulse rifle. Balanced for close-quarters skirmishes.") },
        { "ERA Weapon Primary (55)",     new WeaponProfile("Hammerfall AR",        58, 540, "Heavy slug repeater. Hits like a freight train, kicks like one too.") },
        { "ERA Weapon Primary (56)",     new WeaponProfile("Voltspike DMR",        72, 360, "Long-range coil rifle. Precision over volume.") },
        { "ERA Weapon Primary (60)",     new WeaponProfile("Nova Lance",           65, 720, "Particle lance built for sustained suppression.") },
        { "ERA Weapon Primary (61)",     new WeaponProfile("Stormbreaker AR",      48, 850, "Auto-rifle tuned for storm trooper doctrine.") },
        { "ERA Weapon Primary (72)",     new WeaponProfile("Singularity",          95, 240, "Experimental gravity-well cannon. Use with caution.") },

        { "ERA Weapon Secondary (2)",    new WeaponProfile("Tracer Sidearm",       18, 480, "Reliable backup pistol. Standard officer loadout.") },
        { "ERA Weapon Secondary (3)",    new WeaponProfile("Spark Holdout",        22, 540, "Compact spark pistol. Pulls fast, pulls clean.") },
        { "ERA Weapon Secondary (4)",    new WeaponProfile("Riptide Coilgun",      34, 300, "Heavy coil sidearm. One shot, one statement.") },
        { "ERA Weapon Secondary (5)",    new WeaponProfile("Phantom Stinger",      26, 600, "Suppressed plasma pistol. Whisper-quiet, kicks hard.") },
        { "ERA Weapon Secondary (8)",    new WeaponProfile("Cinder Burner",        30, 420, "Short-range incendiary pistol. Closes the gap, then closes the gap.") },
        { "ERA Weapon Secondary (9)",    new WeaponProfile("Hex Revolver",         44, 220, "Six rounds of heavy hex slugs. Make every one count.") },
        { "ERA Weapon Secondary (13)",   new WeaponProfile("Vex-9 Burst SMG",      16, 900, "High-rate machine pistol. Spray now, regret later.") }
    };

    [Header("References")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private Sprite slotSprite;
    [SerializeField] private Sprite buyButtonCoverSprite;
    [SerializeField] private TMP_FontAsset storeFont;
    [SerializeField] private Font stencilSourceFont;
    [SerializeField] private List<Sprite> primaryWeaponSprites = new List<Sprite>();
    [SerializeField] private List<Sprite> secondaryWeaponSprites = new List<Sprite>();
    [SerializeField] private List<Sprite> goldCoinSprites = new List<Sprite>();
    [SerializeField] private CoinRainUI coinRain;

    [Header("Audio")]
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip selectSound;
    [SerializeField] private AudioClip coinRainSound;
    [SerializeField] private AudioClip bgmClip;
    private AudioSource audioSource;

    [Header("Numpad Digits")]
    private const string NumpadPath = "Assets/Art/Coins/NumpadFree/Numpad_Light.png";
    private Sprite[] numpadSprites;
    private RectTransform creditsDigitsContainer;
    private List<Image> creditDigitImages = new List<Image>();

    private static TMP_FontAsset cachedStencilFontAsset;

    [Header("Layout")]
    [SerializeField] private Vector2 scrollPosition = new Vector2(162f, -246.5f);
    [SerializeField] private Vector2 scrollSize = new Vector2(555f, 723f);
    [SerializeField] private Vector2 slotSize = new Vector2(260f, 320f);
    [SerializeField] private Vector2 slotSpacing = new Vector2(18f, 12f);
    [SerializeField] private float scrollSpeed = 55f;

    [Header("Details")]
    [SerializeField] private Vector2 previewPosition = new Vector2(830f, -405f);
    [SerializeField] private Vector2 previewSize = new Vector2(360f, 260f);
    [SerializeField] private Vector2 detailsPosition = new Vector2(1265f, -345f);
    [SerializeField] private Vector2 detailsSize = new Vector2(420f, 330f);
    [SerializeField] private Vector2 creditsPosition = new Vector2(1816f, -36f); 
    [SerializeField] private float digitSize = 32f; 
    [SerializeField] private Vector2 buyButtonPosition = new Vector2(895f, -805f);
    [SerializeField] private Vector2 buyButtonSize = new Vector2(340f, 125f);

    [Header("Colors")]
    [SerializeField] private Color normalSlotColor = Color.white;
    [SerializeField] private Color selectedSlotColor = new Color(0.45f, 1f, 0.45f, 1f);
    [SerializeField] private Color textColor = new Color(0.82f, 0.78f, 0.70f, 1f);
    [SerializeField, Range(-0.5f, 0.5f)] private float textFaceDilate = -0.1f;

    private readonly List<ShopSlot> slots = new List<ShopSlot>();
    private readonly List<ShopWeapon> weapons = new List<ShopWeapon>();
    private RectTransform viewport;
    private RectTransform content;
    private RectTransform previewArea;
    private Image previewImage;
    private TextMeshProUGUI detailsText;
    private TextMeshProUGUI buyButtonText;
    private Image buyButtonTextCover;
    private RectTransform buyButtonRect;
    private float maxScroll;
    private float scrollOffset;
    private int selectedIndex = -1;

    private void Awake()
    {
        ResolveReferences();

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

    #if UNITY_EDITOR
        if (hoverSound == null) hoverSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Button/MECHSwtch_BLEEOOP_Lazer_Click.ogg");
        if (selectSound == null) selectSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Button/UIClick_BLEEOOP_Digi_Select.ogg");
        if (coinRainSound == null) coinRainSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Coins sounds [OGG]/1_Coins.ogg");
        if (bgmClip == null) bgmClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Store/ikoliks_aj-jazz-lounge-elevator-music-332339.mp3");
    #endif

        if (bgmClip != null)
        {
            audioSource.clip = bgmClip;
            audioSource.loop = true;
            audioSource.volume = 0.5f;
            audioSource.Play();
        }

        BuildWeaponList();
        InitializeCredits();

        if (targetCanvas == null || slotSprite == null || weapons.Count == 0)
            return;

        BuildShop();
        SelectWeapon(0);
    }

    private void Update()
    {
        if (viewport == null)
            return;

        Vector2 mousePosition = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        bool mouseOverList = RectTransformUtility.RectangleContainsScreenPoint(viewport, mousePosition, null);

        if (mouseOverList)
            HandleScroll();

        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            return;

        if (mouseOverList)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(slots[i].RectTransform, mousePosition, null))
                {
                    SelectWeapon(i);
                    break;
                }
            }

            return;
        }

        if (buyButtonRect != null && RectTransformUtility.RectangleContainsScreenPoint(buyButtonRect, mousePosition, null))
            BuyOrEquipSelectedWeapon();
    }

    private static void InitializeCredits()
    {
        if (PlayerPrefs.GetInt(CreditsInitializedKey, 0) == 1)
            return;

        PlayerPrefs.SetInt(CreditsKey, StartingCredits);
        PlayerPrefs.SetInt(CreditsInitializedKey, 1);
        PlayerPrefs.Save();
    }

    private void ResolveReferences()
    {
        if (targetCanvas == null)
            targetCanvas = GetComponent<Canvas>();

#if UNITY_EDITOR
        if (slotSprite == null)
            slotSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Armory/slot.png");

        if (buyButtonCoverSprite == null)
            buyButtonCoverSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Armory/Screenshot 2026-05-13 122102.png");

        if (stencilSourceFont == null)
            stencilSourceFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/BlackOpsOne-Regular.ttf");

        if (storeFont == null)
            storeFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Dunkerque-Regular-FREE-FOR-PERSONAL-USE-ONLY SDF.asset");

        List<Sprite> editorPrimarySprites = LoadWeaponSpritesInEditor("ERA Weapon Primary", true);
        if (editorPrimarySprites.Count > primaryWeaponSprites.Count)
            primaryWeaponSprites = editorPrimarySprites;

        List<Sprite> editorSecondarySprites = LoadWeaponSpritesInEditor("ERA Weapon Secondary", false);
        if (editorSecondarySprites.Count > secondaryWeaponSprites.Count)
            secondaryWeaponSprites = editorSecondarySprites;

        if (goldCoinSprites.Count == 0)
            goldCoinSprites = LoadGoldCoinSprites();

        if (numpadSprites == null || numpadSprites.Length == 0)
            numpadSprites = LoadNumpadSprites();
        #endif

        TMP_FontAsset stencilFont = ResolveStencilFontAsset();
        if (stencilFont != null)
            storeFont = stencilFont;

        primaryWeaponSprites.RemoveAll(sprite => sprite == null || IsExcludedPrimaryWeapon(sprite));
        secondaryWeaponSprites.RemoveAll(sprite => sprite == null);
    }

    private TMP_FontAsset ResolveStencilFontAsset()
    {
        if (cachedStencilFontAsset != null)
            return cachedStencilFontAsset;

        if (stencilSourceFont == null)
            return null;

        cachedStencilFontAsset = TMP_FontAsset.CreateFontAsset(stencilSourceFont);
        if (cachedStencilFontAsset != null)
        {
            cachedStencilFontAsset.name = stencilSourceFont.name + " SDF (Runtime)";
            Material material = cachedStencilFontAsset.material;
            if (material != null && material.HasProperty(ShaderUtilities.ID_FaceDilate))
                material.SetFloat(ShaderUtilities.ID_FaceDilate, textFaceDilate);
        }
        return cachedStencilFontAsset;
    }

#if UNITY_EDITOR
    private static List<Sprite> LoadWeaponSpritesInEditor(string filePrefix, bool includeStarterPrimary)
    {
        List<Sprite> sprites = new List<Sprite>();
        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { "Assets/Art/Weapons" });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = System.IO.Path.GetFileName(path);

            if (excludedPrimaryWeaponFiles.Contains(fileName))
                continue;

            bool isStarterPrimary = includeStarterPrimary && fileName == "weapon-primary.png";
            if (!fileName.StartsWith(filePrefix) && !isStarterPrimary)
                continue;

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null && !sprites.Contains(sprite))
                sprites.Add(sprite);
        }

        sprites.Sort((left, right) => string.CompareOrdinal(left.name, right.name));
        return sprites;
    }
private static List<Sprite> LoadGoldCoinSprites()
{
    List<Sprite> sprites = new List<Sprite>();
    string path = "Assets/Art/Coins/Coin Flip (animation frames)";
    if (!System.IO.Directory.Exists(path)) return sprites;

    string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { path });
    foreach (string guid in guids)
    {
        string assetPath = AssetDatabase.GUIDToAssetPath(guid);
        string fileName = System.IO.Path.GetFileName(assetPath);
        if (fileName.StartsWith("goldcoin"))
        {
            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (s != null) sprites.Add(s);
        }
    }
    return sprites;
    }

    private static Sprite[] LoadNumpadSprites()
    {
    #if UNITY_EDITOR
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(NumpadPath);
        List<Sprite> sprites = new List<Sprite>();
        foreach (var asset in assets)
        {
            if (asset is Sprite s) sprites.Add(s);
        }
        sprites.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return sprites.ToArray();
    #else
        return new Sprite[0];
    #endif
    }
    #endif

    private static bool IsExcludedPrimaryWeapon(Sprite sprite)
{
        if (sprite == null)
            return true;

        return excludedPrimaryWeaponFiles.Contains(sprite.name + ".png");
    }

    private void BuildWeaponList()
    {
        weapons.Clear();

        foreach (Sprite sprite in primaryWeaponSprites)
        {
            if (sprite != null && !IsExcludedPrimaryWeapon(sprite))
                weapons.Add(new ShopWeapon(sprite, "Primary"));
        }

        foreach (Sprite sprite in secondaryWeaponSprites)
        {
            if (sprite != null)
                weapons.Add(new ShopWeapon(sprite, "Secondary"));
        }
    }

    private void BuildShop()
    {
        RectTransform root = CreateRect("StoreWeaponShopUI", targetCanvas.transform);
        Stretch(root);

        viewport = CreateRect("PrimaryWeaponViewport", root);
        SetTopLeft(viewport, scrollPosition, scrollSize);
        viewport.gameObject.AddComponent<RectMask2D>();

        content = CreateRect("PrimaryWeaponContent", viewport);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(0f, 1f);
        content.pivot = new Vector2(0f, 1f);
        content.anchoredPosition = Vector2.zero;

        int rowCount = Mathf.CeilToInt(weapons.Count / 2f);
        float contentHeight = Mathf.Max(scrollSize.y, (rowCount * slotSize.y) + ((rowCount - 1) * slotSpacing.y));
        content.sizeDelta = new Vector2(scrollSize.x, contentHeight);
        maxScroll = Mathf.Max(0f, contentHeight - scrollSize.y);

        for (int i = 0; i < weapons.Count; i++)
            CreateWeaponSlot(i);

        previewArea = CreateRect("SelectedWeaponPreviewArea", root);
        SetTopLeft(previewArea, previewPosition, previewSize);

        previewImage = CreateImage("SelectedWeaponPreview", previewArea, null, Color.white);
        Center(previewImage.rectTransform);
        previewImage.raycastTarget = false;

        detailsText = CreateText("SelectedWeaponDetails", root, string.Empty, 28f, TextAlignmentOptions.Top);
        SetTopLeft(detailsText.rectTransform, detailsPosition, detailsSize);

        // Digits Container for Credits (No Label)
        GameObject existing = GameObject.Find("CreditsDigitsContainer");
        if (existing != null)
        {
            creditsDigitsContainer = existing.GetComponent<RectTransform>();
            // Clear any editor-time sample digits
            foreach (Transform child in creditsDigitsContainer) {
                Destroy(child.gameObject);
            }
        }
        else
        {
            creditsDigitsContainer = CreateRect("CreditsDigitsContainer", root);
            creditsDigitsContainer.anchorMin = new Vector2(0f, 1f);
            creditsDigitsContainer.anchorMax = new Vector2(0f, 1f);
            creditsDigitsContainer.pivot = new Vector2(0f, 1f);
            creditsDigitsContainer.anchoredPosition = creditsPosition;
        }
creditsDigitsContainer.sizeDelta = new Vector2(300f, digitSize);

        HorizontalLayoutGroup layout = creditsDigitsContainer.gameObject.GetComponent<HorizontalLayoutGroup>();
if (layout == null) layout = creditsDigitsContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlHeight = false; // Manual height
        layout.childControlWidth = false; // Manual width
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = false;
        layout.spacing = -6f; // Tight pixel-art spacing

        buyButtonRect = CreateRect("BuyEquipClickArea", root);
SetTopLeft(buyButtonRect, buyButtonPosition, buyButtonSize);

        // Cover size and offset derived from the reference Square placed over the buy button inner area.
        // Square world scale (263.27, 71.76) / canvas scale (1.333) = ~(197.5, 53.8) canvas px.
        // Square center offset from button center = (-16, -24).
        Vector2 innerCoverSize = new Vector2(197.5f, 53.8f);
        Vector2 innerCoverOffset = new Vector2(-16f, -24f);

        buyButtonTextCover = CreateImage("BuyButtonTextCover", buyButtonRect, buyButtonCoverSprite, Color.white);
        Center(buyButtonTextCover.rectTransform);
        buyButtonTextCover.rectTransform.anchoredPosition = innerCoverOffset;
        buyButtonTextCover.rectTransform.sizeDelta = innerCoverSize;

        buyButtonText = CreateText("BuyEquipText", buyButtonRect, string.Empty, 40f, TextAlignmentOptions.Center);
        ApplyOverlayTextStyle(buyButtonText);
        buyButtonText.enableWordWrapping = false;
        Center(buyButtonText.rectTransform);
        buyButtonText.rectTransform.anchoredPosition = innerCoverOffset;
        buyButtonText.rectTransform.sizeDelta = innerCoverSize;

        GameObject rainObj = new GameObject("CoinRainEffect", typeof(RectTransform), typeof(CoinRainUI));
        rainObj.transform.SetParent(root, false);
        Stretch(rainObj.GetComponent<RectTransform>());
        coinRain = rainObj.GetComponent<CoinRainUI>();
        coinRain.SetSprites(goldCoinSprites);
        }

    private void CreateWeaponSlot(int index)
    {
        int row = index / 2;
        int column = index % 2;

        ShopWeapon weapon = weapons[index];
        Image frame = CreateImage(weapon.Type + "WeaponSlot_" + (index + 1), content, slotSprite, normalSlotColor);
        RectTransform slotRect = frame.rectTransform;
        slotRect.anchorMin = new Vector2(0f, 1f);
        slotRect.anchorMax = new Vector2(0f, 1f);
        slotRect.pivot = new Vector2(0f, 1f);
        slotRect.sizeDelta = slotSize;
        slotRect.anchoredPosition = new Vector2(column * (slotSize.x + slotSpacing.x), -row * (slotSize.y + slotSpacing.y));
        frame.raycastTarget = false;

        Image icon = CreateImage("Icon", slotRect, weapon.Sprite, Color.white);
        Center(icon.rectTransform);
        icon.rectTransform.anchoredPosition = new Vector2(0f, 42f);
        FitPixelSprite(icon, new Vector2(190f, 112f));
        icon.raycastTarget = false;

        TextMeshProUGUI label = CreateText("TypeLabel", slotRect, weapon.Type.ToUpperInvariant(), 24f, TextAlignmentOptions.Center);
        ApplyOverlayTextStyle(label);
        label.rectTransform.anchorMin = new Vector2(0.10f, 0.14f);
        label.rectTransform.anchorMax = new Vector2(0.90f, 0.24f);
        label.rectTransform.offsetMin = Vector2.zero;
        label.rectTransform.offsetMax = Vector2.zero;

        TextMeshProUGUI price = CreateText("PriceLabel", slotRect, WeaponPrice + " CREDIT", 21f, TextAlignmentOptions.Center);
        ApplyOverlayTextStyle(price);
        price.enableWordWrapping = false;
        price.rectTransform.anchorMin = new Vector2(0.10f, 0.27f);
        price.rectTransform.anchorMax = new Vector2(0.90f, 0.36f);
        price.rectTransform.offsetMin = Vector2.zero;
        price.rectTransform.offsetMax = Vector2.zero;

        slots.Add(new ShopSlot(slotRect, frame));
    }

    private void HandleScroll()
    {
        if (Mouse.current == null || maxScroll <= 0f)
            return;

        float wheel = Mouse.current.scroll.ReadValue().y;
        if (Mathf.Approximately(wheel, 0f))
            return;

        scrollOffset = Mathf.Clamp(scrollOffset - (wheel * scrollSpeed), 0f, maxScroll);
        content.anchoredPosition = new Vector2(0f, scrollOffset);
    }

    private void SelectWeapon(int index)
    {
        if (index < 0 || index >= weapons.Count)
            return;

        if (index != selectedIndex && hoverSound != null && audioSource != null)
            audioSource.PlayOneShot(hoverSound);

        selectedIndex = index;
        ShopWeapon selectedWeapon = weapons[index];
        Sprite selectedSprite = selectedWeapon.Sprite;

        if (previewImage != null)
        {
            previewImage.sprite = selectedSprite;
            FitPixelSprite(previewImage, previewSize);
        }

        if (detailsText != null)
            detailsText.text = BuildDetailsText(selectedWeapon, index);

        RefreshStoreState();

        for (int i = 0; i < slots.Count; i++)
            slots[i].Frame.color = i == selectedIndex ? selectedSlotColor : normalSlotColor;
    }

    private string BuildDetailsText(ShopWeapon weapon, int index)
    {
        WeaponProfile profile = GetWeaponProfile(weapon, index);

        return profile.Name + "\n"
            + "Type: " + weapon.Type + "\n"
            + "Price: " + WeaponPrice + " credit\n"
            + "Dmg: " + profile.Damage + "\n"
            + "Fire Rate: " + profile.FireRate + " RPM\n\n"
            + profile.Description;
    }

    private void BuyOrEquipSelectedWeapon()
    {
        if (selectedIndex < 0 || selectedIndex >= weapons.Count)
            return;

        ShopWeapon weapon = weapons[selectedIndex];
        string weaponId = GetWeaponId(weapon);
        bool owned = IsOwned(weapon);

        if (!owned)
        {
            int credits = PlayerPrefs.GetInt(CreditsKey, StartingCredits);
            if (credits < WeaponPrice)
                return;

            PlayerPrefs.SetInt(CreditsKey, credits - WeaponPrice);
            PlayerPrefs.SetInt(GetOwnedKey(weaponId), 1);

            if (selectSound != null && audioSource != null) audioSource.PlayOneShot(selectSound);
            if (coinRainSound != null && audioSource != null) audioSource.PlayOneShot(coinRainSound, 1.5f);

            if (coinRain != null)
                coinRain.StartRain();
        }
        else
        {
            if (selectSound != null && audioSource != null) audioSource.PlayOneShot(selectSound);
        }

        PlayerPrefs.SetString(GetEquippedKey(weapon.Type), weaponId);
        PlayerPrefs.SetString(EquippedWeaponSlotKey, weapon.Type);
        PlayerPrefs.Save();
        RefreshStoreState();
    }

    private void RefreshStoreState()
    {
        RefreshCreditDigits();

        if (buyButtonText == null || selectedIndex < 0 || selectedIndex >= weapons.Count)
            return;

        ShopWeapon weapon = weapons[selectedIndex];
        if (IsEquipped(weapon))
        {
            SetBuyButtonOverlayVisible(true);
            buyButtonText.text = "EQUIPPED";
        }
        else if (IsOwned(weapon))
        {
            SetBuyButtonOverlayVisible(true);
            buyButtonText.text = "EQUIP";
        }
        else
        {
            buyButtonText.text = string.Empty;
            SetBuyButtonOverlayVisible(false);
        }
    }

    private void RefreshCreditDigits()
    {
        if (creditsDigitsContainer == null || numpadSprites == null || numpadSprites.Length < 10)
            return;

        int credits = PlayerPrefs.GetInt(CreditsKey, StartingCredits);
        string coinStr = credits.ToString("D3"); // Force 3 digits (e.g. 012)

        // Ensure exactly 3 digit images exist
        while (creditDigitImages.Count < 3)
{
            GameObject digitObj = new GameObject("Digit", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            digitObj.transform.SetParent(creditsDigitsContainer, false);
            Image img = digitObj.GetComponent<Image>();
            img.raycastTarget = false;
            img.preserveAspect = true;
            RectTransform rt = digitObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(digitSize, digitSize);
            creditDigitImages.Add(img);
        }

        // Update sprites and visibility
        for (int i = 0; i < creditDigitImages.Count; i++)
        {
            if (i < coinStr.Length)
            {
                creditDigitImages[i].gameObject.SetActive(true);
                int digitChar = int.Parse(coinStr[i].ToString());
                creditDigitImages[i].sprite = numpadSprites[digitChar];
            }
            else
            {
                creditDigitImages[i].gameObject.SetActive(false);
            }
        }
    }

    private void SetBuyButtonOverlayVisible(bool visible)
    {
        if (buyButtonTextCover != null)
            buyButtonTextCover.gameObject.SetActive(visible);

        if (buyButtonText != null)
            buyButtonText.gameObject.SetActive(visible);
    }

    private static bool IsOwned(ShopWeapon weapon)
    {
        if (weapon.Type == "Primary" && GetWeaponId(weapon) == "weapon-primary")
            return true;

        return PlayerPrefs.GetInt(GetOwnedKey(GetWeaponId(weapon)), 0) == 1;
    }

    private static bool IsEquipped(ShopWeapon weapon)
    {
        return PlayerPrefs.GetString(GetEquippedKey(weapon.Type), string.Empty) == GetWeaponId(weapon);
    }

    private static string GetWeaponId(ShopWeapon weapon)
    {
        return weapon.Sprite != null ? weapon.Sprite.name.Replace("_0", string.Empty) : string.Empty;
    }

    private static string GetOwnedKey(string weaponId)
    {
        return "OwnedWeapon_" + weaponId;
    }

    private static string GetEquippedKey(string weaponType)
    {
        return weaponType == "Secondary" ? EquippedSecondaryKey : EquippedPrimaryKey;
    }

    private static WeaponProfile GetWeaponProfile(ShopWeapon weapon, int index)
    {
        Sprite weaponSprite = weapon.Sprite;
        string key = weaponSprite != null ? weaponSprite.name.Replace("_0", string.Empty) : null;

        if (key != null && weaponProfiles.TryGetValue(key, out WeaponProfile profile))
            return profile;

        string fallbackName = weapon.Type + " Weapon " + (index + 1);
        return new WeaponProfile(fallbackName, 30, 500, weapon.Type + " weapon.");
    }

    private static RectTransform CreateRect(string objectName, Transform parent)
    {
        GameObject rectObject = new GameObject(objectName, typeof(RectTransform));
        RectTransform rectTransform = rectObject.GetComponent<RectTransform>();
        rectTransform.SetParent(parent, false);
        return rectTransform;
    }

    private static Image CreateImage(string objectName, Transform parent, Sprite sprite, Color color)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rectTransform = imageObject.GetComponent<RectTransform>();
        rectTransform.SetParent(parent, false);

        Image image = imageObject.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private TextMeshProUGUI CreateText(string objectName, Transform parent, string text, float fontSize, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.SetParent(parent, false);

        TextMeshProUGUI textComponent = textObject.GetComponent<TextMeshProUGUI>();
        textComponent.text = text;
        if (storeFont != null)
            textComponent.font = storeFont;

        textComponent.fontSize = fontSize;
        textComponent.alignment = alignment;
        textComponent.color = textColor;
        textComponent.fontStyle = FontStyles.Normal;
        textComponent.raycastTarget = false;
        textComponent.enableWordWrapping = true;
        return textComponent;
    }

    private static void ApplyOverlayTextStyle(TextMeshProUGUI textComponent)
    {
        if (textComponent == null)
            return;

        textComponent.color = Color.white;
        textComponent.fontStyle = FontStyles.Normal;
        textComponent.outlineColor = Color.black;
        textComponent.outlineWidth = 0.18f;
    }

    private static void FitPixelSprite(Image image, Vector2 maxSize)
    {
        if (image == null || image.sprite == null)
            return;

        image.sprite.texture.filterMode = FilterMode.Point;
        image.preserveAspect = true;

        Vector2 spriteSize = image.sprite.rect.size;
        float rawScale = Mathf.Min(maxSize.x / spriteSize.x, maxSize.y / spriteSize.y);
        float pixelScale = Mathf.Max(1f, Mathf.Floor(rawScale));

        RectTransform rectTransform = image.rectTransform;
        rectTransform.sizeDelta = spriteSize;
        rectTransform.localScale = new Vector3(pixelScale, pixelScale, 1f);
    }

    private static void SetTopLeft(RectTransform rectTransform, Vector2 anchoredPosition, Vector2 size)
    {
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;
    }

    private static void Center(RectTransform rectTransform)
    {
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
    }

    private static void Stretch(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private readonly struct ShopSlot
    {
        public ShopSlot(RectTransform rectTransform, Image frame)
        {
            RectTransform = rectTransform;
            Frame = frame;
        }

        public RectTransform RectTransform { get; }
        public Image Frame { get; }
    }

    private readonly struct ShopWeapon
    {
        public ShopWeapon(Sprite sprite, string type)
        {
            Sprite = sprite;
            Type = type;
        }

        public Sprite Sprite { get; }
        public string Type { get; }
    }
}
