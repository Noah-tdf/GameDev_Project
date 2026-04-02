using UnityEngine;
using TMPro;

/// <summary>
/// Tracks Luca's hit points, invincibility frames, damage flashing, and death.
/// Attach to the Player GameObject alongside PlayerMovement.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    // ── Inspector-exposed settings ───────────────────────────────────────────
    [Header("Health")]
    [SerializeField] private int maxHP = 5;

    [Header("Invincibility")]
    [SerializeField] private float invincibilityDuration = 1.5f;  // Seconds of i-frames after hit
    [SerializeField] private float flashInterval = 0.1f;           // Seconds between each sprite blink

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI hpText;               // Drag the HP label in via Inspector

    // ── Internal state ───────────────────────────────────────────────────────
    private int _currentHP;
    private bool _isInvincible;
    private float _invincibilityTimer;
    private float _flashTimer;
    private bool _spriteVisible = true;

    private SpriteRenderer _sr;
    private PlayerMovement _movement;
    private Animator _animator;

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
        UpdateHPUI();
    }

    // ────────────────────────────────────────────────────────────────────────
    private void Update()
    {
        if (_isInvincible)
            TickInvincibility();
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
        Debug.Log("Luca died!");
        _movement?.DisableInput();
        _animator?.SetBool("IsDead", true);
        SetAlpha(0.3f);
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
            hpText.text = "HP: " + _currentHP;
    }
}
