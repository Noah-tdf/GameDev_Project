using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float jumpForce = 12f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.15f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private float jumpBufferTime = 0.12f;

    [Header("Tutorial Prompt")]
    [SerializeField] private GameObject movementPopUp;
    [SerializeField] private GameObject combatPopUp;
    [SerializeField] private GameObject levelEndPopUp;
    [SerializeField] private GameObject weaponSwitchPopUp;
    [SerializeField] private GameObject healthBarPopUp;
    private bool combatTipDone;
    private bool weaponSwitchTipShown;
    private bool levelEndTipShown;
    private bool healthBarTipShown;

    private Rigidbody2D rb;
    private Collider2D bodyCollider;
    private readonly ContactPoint2D[] groundContacts = new ContactPoint2D[8];
    private readonly RaycastHit2D[] groundHits = new RaycastHit2D[8];
    private float moveInput;
    private bool isFacingRight = true;
    private float coyoteTimer;
    private float jumpBufferTimer;

    public bool IsGrounded { get; private set; }
    public float FacingDirection => isFacingRight ? 1f : -1f;
    public bool FacingRight => isFacingRight;

    private bool inputDisabled;
    private Animator _animator;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
        _animator = GetComponent<Animator>();

        if (groundCheck == null)
        {
            Transform existingGroundCheck = transform.Find("GroundCheck");
            if (existingGroundCheck != null)
            {
                groundCheck = existingGroundCheck;
            }
        }

        if (groundLayer.value == 0)
        {
            int namedGroundLayer = LayerMask.NameToLayer("Ground");
            if (namedGroundLayer >= 0)
            {
                groundLayer = 1 << namedGroundLayer;
            }
        }
    }

    private void Start()
    {
        if (movementPopUp == null)
            movementPopUp = GameObject.Find("MovementPopUp");

        if (movementPopUp != null)
            movementPopUp.SetActive(true);

        if (healthBarPopUp == null)
            healthBarPopUp = GameObject.Find("HealthBarPopUp");

        if (healthBarPopUp == null)
        {
            TextMeshProUGUI promptStyle = movementPopUp != null
                ? movementPopUp.GetComponentInChildren<TextMeshProUGUI>(true)
                : null;
            healthBarPopUp = BuildHealthBarPopUp(promptStyle);
        }

        if (healthBarPopUp != null)
            healthBarPopUp.SetActive(false);

        if (combatPopUp == null)
            combatPopUp = GameObject.Find("CombatPopUp");

        if (combatPopUp != null)
            combatPopUp.SetActive(false);

        if (levelEndPopUp == null)
            levelEndPopUp = GameObject.Find("LevelEndPopUp");

        if (levelEndPopUp != null)
            levelEndPopUp.SetActive(false);

        if (weaponSwitchPopUp == null)
            weaponSwitchPopUp = GameObject.Find("WeaponSwitchPopUp");

        if (weaponSwitchPopUp != null)
            weaponSwitchPopUp.SetActive(false);
    }

    /// <summary>Called by PlayerHealth on death — freezes all movement.</summary>
    public void DisableInput()
    {
        inputDisabled = true;
        moveInput = 0f;
        if (rb != null) rb.linearVelocity = Vector2.zero;
    }

    private void Update()
    {
        if (inputDisabled) return;
        ReadInput();
        CheckGrounded();
        UpdateTimers();
        FlipCharacter();
        UpdateAnimator();
    }

    private void UpdateAnimator()
    {
        if (_animator == null) return;
        _animator.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
        _animator.SetBool("IsGrounded", IsGrounded);
    }

    private void FixedUpdate()
    {
        if (inputDisabled) return;
        Move();
        Jump();
    }

    private void ReadInput()
    {
        moveInput = 0f;

        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
        {
            moveInput = -1f;
        }
        else if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
        {
            moveInput = 1f;
        }

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            jumpBufferTimer = jumpBufferTime;
        }
    }

    private void Move()
    {
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
    }

    private void Jump()
    {
        if (jumpBufferTimer <= 0f)
        {
            return;
        }

        if (coyoteTimer > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
        }
    }

    private void CheckGrounded()
    {
        bool groundedByCheck = false;
        if (groundCheck != null)
        {
            groundedByCheck = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        }

        if (bodyCollider == null)
        {
            if (groundCheck == null)
            {
                Debug.LogWarning("PlayerMovement: Ground Check is not assigned.", this);
            }

            IsGrounded = groundedByCheck;
            return;
        }

        IsGrounded = groundedByCheck
            || HasGroundContact(true)
            || CastForGroundBelow(true)
            || HasGroundContact(false)
            || CastForGroundBelow(false);
    }

    private bool HasGroundContact(bool useGroundLayer)
    {
        ContactFilter2D filter = new ContactFilter2D
        {
            useTriggers = false
        };

        if (useGroundLayer)
        {
            filter.useLayerMask = true;
            filter.layerMask = groundLayer;
        }

        int contactCount = bodyCollider.GetContacts(filter, groundContacts);
        for (int i = 0; i < contactCount; i++)
        {
            if (groundContacts[i].normal.y > 0.45f)
            {
                return true;
            }
        }

        return false;
    }

    private bool CastForGroundBelow(bool useGroundLayer)
    {
        ContactFilter2D filter = new ContactFilter2D
        {
            useTriggers = false
        };

        if (useGroundLayer)
        {
            filter.useLayerMask = true;
            filter.layerMask = groundLayer;
        }

        int hitCount = bodyCollider.Cast(Vector2.down, filter, groundHits, 0.18f);
        for (int i = 0; i < hitCount; i++)
        {
            if (groundHits[i].collider != null && groundHits[i].normal.y > 0.45f)
            {
                return true;
            }
        }

        return false;
    }

    private void UpdateTimers()
    {
        if (IsGrounded)
        {
            coyoteTimer = coyoteTime;
        }
        else
        {
            coyoteTimer -= Time.deltaTime;
        }

        if (jumpBufferTimer > 0f)
        {
            jumpBufferTimer -= Time.deltaTime;
        }
    }

    private void FlipCharacter()
    {
        if (moveInput > 0f && !isFacingRight)
        {
            Flip();
        }
        else if (moveInput < 0f && isFacingRight)
        {
            Flip();
        }
    }

    private void Flip()
    {
        isFacingRight = !isFacingRight;

        Vector3 localScale = transform.localScale;
        localScale.x *= -1f;
        transform.localScale = localScale;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HideTutorialPromptIfLandedOnTarget(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        HideTutorialPromptIfLandedOnTarget(collision);
    }

    private void HideTutorialPromptIfLandedOnTarget(Collision2D collision)
    {
        for (int i = 0; i < collision.contactCount; i++)
        {
            if (collision.GetContact(i).normal.y > 0.45f)
            {
                string platformName = collision.gameObject.name;

                if (movementPopUp != null && IsFirstAirPlatformSet(platformName))
                {
                    movementPopUp.SetActive(false);
                    ShowHealthBarTip();
                }

                if (!IsFirstAirPlatformSet(platformName))
                    HideHealthBarTip();

                if (!combatTipDone && combatPopUp != null && platformName.StartsWith("2ndPlatform"))
                {
                    HideHealthBarTip();
                    combatPopUp.SetActive(true);
                }

                if (!weaponSwitchTipShown && weaponSwitchPopUp != null && platformName.StartsWith("3rdPlatform"))
                {
                    weaponSwitchTipShown = true;
                    HideHealthBarTip();
                    if (combatPopUp != null)
                        combatPopUp.SetActive(false);
                    weaponSwitchPopUp.SetActive(true);
                }
                else if (weaponSwitchTipShown && !levelEndTipShown && IsLevelEndPlatform(platformName))
                {
                    levelEndTipShown = true;
                    if (weaponSwitchPopUp != null)
                        weaponSwitchPopUp.SetActive(false);
                    if (levelEndPopUp != null)
                        levelEndPopUp.SetActive(true);
                }

                return;
            }
        }
    }

    private static bool IsLevelEndPlatform(string platformName)
    {
        if (string.IsNullOrEmpty(platformName))
            return false;

        // Any platform reached AFTER 3rdPlatform — exclude only the platform we just left
        // and earlier ground platforms so backtracking doesn't trigger it. Air platforms count
        // because the next one comes right after 3rdPlatform.
        if (platformName.StartsWith("3rdPlatform")) return false;
        if (platformName.StartsWith("2ndPlatform")) return false;
        if (platformName.StartsWith("1stPlatform")) return false;

        return platformName.Contains("Platform") || platformName.Contains("platform");
    }

    private static bool IsFirstAirPlatformSet(string platformName)
    {
        return platformName == "AirPlatforms";
    }

    public void FinishCombatTip()
    {
        combatTipDone = true;
        HideHealthBarTip();
        if (combatPopUp != null)
            combatPopUp.SetActive(false);
    }

    private void ShowHealthBarTip()
    {
        if (healthBarTipShown)
            return;

        healthBarTipShown = true;
        if (healthBarPopUp != null)
            healthBarPopUp.SetActive(true);
    }

    private void HideHealthBarTip()
    {
        if (healthBarPopUp != null)
            healthBarPopUp.SetActive(false);
    }

    private static GameObject BuildHealthBarPopUp(TextMeshProUGUI styleSource)
    {
        GameObject canvasObject = new GameObject("HealthBarPopUp", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f;
        scaler.referencePixelsPerUnit = 100f;

        GameObject textObject = new GameObject("Text (TMP)", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(canvasObject.transform, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(-1f, 102f);
        rect.sizeDelta = new Vector2(760f, 90f);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = "The health bar in the top-left shows Luca's HP. If it reaches 0, it's game over.";
        if (styleSource != null)
        {
            text.font = styleSource.font;
            text.fontSharedMaterial = styleSource.fontSharedMaterial;
            text.color = styleSource.color;
        }
        text.fontSize = 20f;
        text.enableAutoSizing = true;
        text.fontSizeMin = 18f;
        text.fontSizeMax = 22f;
        text.alignment = TextAlignmentOptions.Center;
        if (styleSource == null)
            text.color = Color.white;
        text.raycastTarget = false;

        return canvasObject;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        if (groundCheck != null)
        {
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        Collider2D collider = bodyCollider != null ? bodyCollider : GetComponent<Collider2D>();
        if (collider == null)
        {
            return;
        }

        Bounds bounds = collider.bounds;
        Vector3 center = new Vector3(bounds.center.x, bounds.min.y - 0.06f, transform.position.z);
        Vector3 size = new Vector3(bounds.size.x * 0.9f, 0.12f, 0f);
        Gizmos.DrawWireCube(center, size);
    }
}
