using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.IO;

/// <summary>
/// Tracks Luca's hit points, invincibility frames, damage flashing, and death.
/// Attach to the Player GameObject alongside PlayerMovement.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    // ── Inspector-exposed settings ───────────────────────────────────────────
    [Header("Health")]
    [SerializeField] private int maxHP = 5;
    [SerializeField] private bool isGodMode = false;

    [Header("Invincibility")]
    [SerializeField] private float invincibilityDuration = 1.5f;  // Seconds of i-frames after hit
    [SerializeField] private float flashInterval = 0.1f;           // Seconds between each sprite blink

    [Header("Fall Death")]
    [SerializeField] private bool killWhenBelowMap = true;
    [SerializeField] private float fallDeathY = -12f;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI hpText;               // Drag the HP label in via Inspector
    [SerializeField] private Image healthBarFill;

    private const string HealthBarAssetPath = "Art/Characters/Pixel Health Bar Asset Pack 1 by Adwit Rahman/Pixel Health Bar 1/RPG Style (1).png";

    // ── Internal state ───────────────────────────────────────────────────────
    private int _currentHP;
    private bool _isInvincible;
    private float _invincibilityTimer;
    private float _flashTimer;
    private bool _spriteVisible = true;
    private bool _isDead;

    private SpriteRenderer _sr;
    private PlayerMovement _movement;
    private Animator _animator;
    private Texture2D _healthBarTexture;

    // ── Public accessor ──────────────────────────────────────────────────────
    /// <summary>Current HP — read-only from outside.</summary>
    public int CurrentHP => _currentHP;

    // ────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        _sr       = GetComponent<SpriteRenderer>();
        _movement = GetComponent<PlayerMovement>();
        _animator = GetComponent<Animator>();
        _currentHP = maxHP;
    }

    private void Start()
    {
        BuildHealthBarUI();
        UpdateHPUI();
    }

    // ────────────────────────────────────────────────────────────────────────
    private void Update()
    {
        if (_isDead)
            return;

        // Toggle God Mode with 'G' key
        if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.gKey.wasPressedThisFrame)
        {
            isGodMode = !isGodMode;
            Debug.Log($"[PlayerHealth] God Mode toggled: {isGodMode}");
            
            // Visual feedback: if entering God Mode, flash once or set a tint (optional)
            if (isGodMode) HealFull();
        }

        if (killWhenBelowMap && transform.position.y <= fallDeathY)
{
            KillInstantly();
            return;
        }

        if (_isInvincible)
            TickInvincibility();
    }

    /// <summary>
    /// Restores player health to its maximum value.
    /// </summary>
    public void HealFull()
    {
        if (_isDead) return;
        _currentHP = maxHP;
        UpdateHPUI();
    }

    /// <summary>
    /// Counts down the invincibility window and toggles sprite alpha to create a
    /// flicker effect. Restores full opacity when the window expires.
    /// </summary>
    private void TickInvincibility()
    {
        _invincibilityTimer -= Time.deltaTime;
        _flashTimer         -= Time.deltaTime;

        if (_flashTimer <= 0f)
        {
            _spriteVisible = !_spriteVisible;
            SetAlpha(_spriteVisible ? 1f : 0.15f);
            _flashTimer = flashInterval;
        }

        if (_invincibilityTimer <= 0f)
        {
            _isInvincible = false;
            SetAlpha(1f);   // Guarantee fully visible when i-frames end
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Public entry point for dealing damage to the player.
    /// Ignored while invincibility frames are active.
    /// </summary>
    /// <param name="amount">How many HP to subtract (typically 1).</param>
    public void TakeDamage(int amount)
    {
        if (_isDead || isGodMode) return;
        if (_isInvincible) return;

        _currentHP = Mathf.Max(_currentHP - amount, 0);
        UpdateHPUI();

        if (_currentHP <= 0)
            Die();
        else
        {
            BeginInvincibility();
            _animator?.SetTrigger("IsHurt");
        }
    }

    public void KillInstantly()
    {
        if (_isDead || isGodMode)
            return;

        _currentHP = 0;
        UpdateHPUI();
        Die();
    }

    /// <summary>Kick off the post-hit invincibility window.</summary>
    private void BeginInvincibility()
    {
        _isInvincible       = true;
        _invincibilityTimer = invincibilityDuration;
        _flashTimer         = flashInterval;
    }

    /// <summary>
    /// Called when HP hits zero.
    /// Locks player movement. Replace with death animation / game-over screen later.
    /// </summary>
    private void Die()
    {
        if (_isDead)
            return;

        _isDead = true;
        Debug.Log("Luca died!");
        _movement?.DisableInput();
        _animator?.SetBool("IsDead", true);
        SetAlpha(0.3f);
        if (PauseMenuController.Instance != null)
            PauseMenuController.Instance.ShowGameOver();
        else
            ShowGameOverUI();
    }

    private void SetAlpha(float alpha)
    {
        if (_sr == null) return;
        Color c = _sr.color;
        c.a = alpha;
        _sr.color = c;
    }

    private void UpdateHPUI()
    {
        if (hpText != null)
            hpText.gameObject.SetActive(false);

        if (healthBarFill != null)
            healthBarFill.fillAmount = maxHP > 0 ? (float)_currentHP / maxHP : 0f;
    }

    private void BuildHealthBarUI()
    {
        if (healthBarFill != null)
            return;

        Sprite emptyBarSprite;
        Sprite fullBarSprite;
        if (!TryLoadHealthBarSprites(out emptyBarSprite, out fullBarSprite))
            return;

        GameObject canvasObject = new GameObject("HealthBarCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject rootObject = new GameObject("RPGStyleHealthBar", typeof(RectTransform));
        rootObject.transform.SetParent(canvasObject.transform, false);
        RectTransform root = rootObject.GetComponent<RectTransform>();
        root.anchorMin = new Vector2(0f, 1f);
        root.anchorMax = new Vector2(0f, 1f);
        root.pivot = new Vector2(0f, 1f);
        root.anchoredPosition = new Vector2(6f, -6f);
        root.sizeDelta = new Vector2(356f, 60f);

        Image emptyBar = CreateHealthBarImage("EmptyBar", root, emptyBarSprite);
        Stretch(emptyBar.rectTransform);

        healthBarFill = CreateHealthBarImage("FillBar", root, fullBarSprite);
        Stretch(healthBarFill.rectTransform);
        healthBarFill.type = Image.Type.Filled;
        healthBarFill.fillMethod = Image.FillMethod.Horizontal;
        healthBarFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        healthBarFill.fillAmount = 1f;
    }

    private bool TryLoadHealthBarSprites(out Sprite emptyBarSprite, out Sprite fullBarSprite)
    {
        emptyBarSprite = null;
        fullBarSprite = null;

        string path = Path.Combine(Application.dataPath, HealthBarAssetPath);
        if (!File.Exists(path))
        {
            Debug.LogWarning("Health bar asset not found: " + path);
            return false;
        }

        byte[] bytes = File.ReadAllBytes(path);
        _healthBarTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        _healthBarTexture.filterMode = FilterMode.Point;

        if (!_healthBarTexture.LoadImage(bytes))
            return false;

        emptyBarSprite = Sprite.Create(_healthBarTexture, new Rect(2f, 75f, 89f, 15f), new Vector2(0.5f, 0.5f), 1f);
        fullBarSprite = Sprite.Create(_healthBarTexture, new Rect(2f, 56f, 89f, 15f), new Vector2(0.5f, 0.5f), 1f);
        return emptyBarSprite != null && fullBarSprite != null;
    }

    private static Image CreateHealthBarImage(string objectName, Transform parent, Sprite sprite)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);

        Image image = imageObject.GetComponent<Image>();
        image.sprite = sprite;
        image.color = Color.white;
        image.preserveAspect = false;
        image.raycastTarget = false;
        return image;
    }

    private static void Stretch(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private void ShowGameOverUI()
    {
        if (GameObject.Find("GameOverCanvas") != null)
            return;

        GameObject canvasObject = new GameObject("GameOverCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject textObject = new GameObject("GameOverText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(canvasObject.transform, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(700f, 140f);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = "GAME OVER";
        text.fontSize = 92f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.red;
        text.raycastTarget = false;
    }
}
