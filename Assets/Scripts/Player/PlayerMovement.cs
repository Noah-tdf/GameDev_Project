using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private float ladderClimbSpeed = 5f;
    [SerializeField] private float ladderDetectionPadding = 0.45f;
    [SerializeField] private int ladderMaxVerticalGapCells = 1;
    [SerializeField] private string ladderNameContains = "ladder";
    [SerializeField] private string upperPlatformNameContains = "upperplatform";

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
    [SerializeField] private float tutorialPopUpAutoHideSeconds = 5f;
    private bool combatTipDone;
    private bool weaponSwitchTipShown;
    private bool levelEndTipShown;
    private bool healthBarTipShown;
    private readonly Dictionary<GameObject, Coroutine> popUpAutoHideRoutines = new Dictionary<GameObject, Coroutine>();

    private Rigidbody2D rb;
    private Collider2D bodyCollider;
    private readonly ContactPoint2D[] groundContacts = new ContactPoint2D[8];
    private readonly RaycastHit2D[] groundHits = new RaycastHit2D[8];
    private readonly List<Tilemap> ladderTilemaps = new List<Tilemap>();
    private readonly Collider2D[] ladderOverlapResults = new Collider2D[16];
    private float moveInput;
    private float verticalInput;
    private bool isFacingRight = true;
    private float coyoteTimer;
    private float jumpBufferTimer;
    private float defaultGravityScale;
    private bool isOnLadder;
    private bool isInLadderTrigger;
    private bool isClimbing;
    private bool hideWeaponUntilGrounded;

    public bool IsGrounded { get; private set; }
    public float FacingDirection => isFacingRight ? 1f : -1f;
    public bool FacingRight => isFacingRight;
    public bool IsClimbing => isClimbing;
    public bool ShouldHideWeapon => hideWeaponUntilGrounded;

    private bool inputDisabled;
    private bool movementLocked;
    private Animator _animator;

    public void SetMovementLocked(bool locked)
    {
        if (movementLocked == locked) return;
        movementLocked = locked;
        Debug.Log($"[PlayerMovement] Movement Locked: {locked}");
        if (locked)
        {
            moveInput = 0f;
            verticalInput = 0f;
            if (rb != null) rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
    }

    private void Awake()
    {
        PauseMenuController.SpawnIfMissing();

        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
        _animator = GetComponent<Animator>();
        defaultGravityScale = rb != null ? rb.gravityScale : 1f;

        if (string.IsNullOrWhiteSpace(ladderNameContains))
            ladderNameContains = "ladder";

        if (string.IsNullOrWhiteSpace(upperPlatformNameContains))
            upperPlatformNameContains = "upperplatform";

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
        ConfigureUpperPlatformTilemaps();
        RefreshLadderTilemaps();

        if (movementPopUp == null)
            movementPopUp = GameObject.Find("MovementPopUp");

        if (movementPopUp != null)
            ShowPopUp(movementPopUp);

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

    private void RefreshLadderTilemaps()
    {
        ladderTilemaps.Clear();

        Tilemap[] tilemaps = FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
        foreach (Tilemap tilemap in tilemaps)
        {
            if (tilemap == null || !TransformNameChainContains(tilemap.transform, ladderNameContains))
                continue;

            ladderTilemaps.Add(tilemap);
        }
    }

    private void ConfigureUpperPlatformTilemaps()
    {
        Tilemap[] tilemaps = FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
        foreach (Tilemap tilemap in tilemaps)
        {
            if (tilemap == null || !TransformNameChainContains(tilemap.transform, upperPlatformNameContains))
                continue;

            TilemapCollider2D tilemapCollider = tilemap.GetComponent<TilemapCollider2D>();
            if (tilemapCollider == null)
                tilemapCollider = tilemap.gameObject.AddComponent<TilemapCollider2D>();

            tilemapCollider.isTrigger = false;
            tilemapCollider.usedByEffector = true;

            PlatformEffector2D platformEffector = tilemap.GetComponent<PlatformEffector2D>();
            if (platformEffector == null)
                platformEffector = tilemap.gameObject.AddComponent<PlatformEffector2D>();

            platformEffector.useOneWay = true;
            platformEffector.useOneWayGrouping = true;
            platformEffector.surfaceArc = 170f;
            platformEffector.sideArc = 0f;
        }
    }

    /// <summary>Called by PlayerHealth on death — freezes all movement.</summary>
    public void DisableInput()
    {
        inputDisabled = true;
        moveInput = 0f;
        verticalInput = 0f;
        isClimbing = false;
        hideWeaponUntilGrounded = false;
        if (rb != null) rb.gravityScale = defaultGravityScale;
        if (rb != null) rb.linearVelocity = Vector2.zero;
    }

    private void Update()
    {
        if (inputDisabled || movementLocked)
        {
            UpdateAnimator();
            return;
        }
        ReadInput();
        UpdateLadderContact();
        CheckGrounded();
        UpdateWeaponReturnState();
        UpdateTimers();
        FlipCharacter();
        UpdateAnimator();
    }

    private void UpdateAnimator()
    {
        if (_animator == null) return;
        _animator.SetFloat("Speed", rb != null ? Mathf.Abs(rb.linearVelocity.x) : 0f);
        _animator.SetBool("IsGrounded", IsGrounded);
        SetAnimatorBoolIfExists("IsClimbing", isClimbing);
        SetAnimatorFloatIfExists("ClimbSpeed", Mathf.Abs(verticalInput));
    }

    private void FixedUpdate()
    {
        if (inputDisabled || movementLocked) return;
        if (isOnLadder)
        {
            MoveOnLadder();
            if (isClimbing)
                return;
        }

        Move();
        Jump();
    }

    private void ReadInput()
    {
        moveInput = 0f;
        verticalInput = 0f;

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

        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
        {
            verticalInput = 1f;
        }
        else if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
        {
            verticalInput = -1f;
        }
    }

    private void Move()
    {
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
    }

    private void MoveOnLadder()
    {
        if (Mathf.Abs(verticalInput) > 0.01f)
        {
            isClimbing = true;
            hideWeaponUntilGrounded = true;
        }

        if (!isClimbing)
            return;

        rb.gravityScale = 0f;
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, verticalInput * ladderClimbSpeed);

        if (jumpBufferTimer > 0f)
        {
            LeaveLadder();
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
        }
    }

    private void Jump()
    {
        if (isClimbing)
            return;

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

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsLadderTrigger(other))
        {
            isInLadderTrigger = true;
            isOnLadder = true;
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (IsLadderTrigger(other))
        {
            isInLadderTrigger = true;
            isOnLadder = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (IsLadderTrigger(other))
            isInLadderTrigger = false;
    }

    private void UpdateLadderContact()
    {
        bool touchingLadder = isInLadderTrigger || IsTouchingLadderCollider() || IsTouchingLadderTile();
        if (touchingLadder)
        {
            isOnLadder = true;
            return;
        }

        if (isOnLadder || isInLadderTrigger)
            LeaveLadder();
    }

    private bool IsTouchingLadderTile()
    {
        if (bodyCollider == null)
            return false;

        if (ladderTilemaps.Count == 0)
            RefreshLadderTilemaps();

        Bounds playerBounds = bodyCollider.bounds;
        for (int i = ladderTilemaps.Count - 1; i >= 0; i--)
        {
            Tilemap ladderTilemap = ladderTilemaps[i];
            if (ladderTilemap == null)
            {
                ladderTilemaps.RemoveAt(i);
                continue;
            }

            if (IsTouchingPaintedLadderCells(ladderTilemap, playerBounds))
                return true;
        }

        return false;
    }

    private bool IsTouchingLadderCollider()
    {
        if (bodyCollider == null)
            return false;

        Bounds playerBounds = bodyCollider.bounds;
        Vector2 center = playerBounds.center;
        Vector2 size = new Vector2(
            playerBounds.size.x + ladderDetectionPadding,
            playerBounds.size.y + ladderDetectionPadding);

        ContactFilter2D filter = new ContactFilter2D
        {
            useTriggers = true,
            useLayerMask = false
        };

        int hitCount = Physics2D.OverlapBox(center, size, 0f, filter, ladderOverlapResults);
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = ladderOverlapResults[i];
            if (hit == null || hit == bodyCollider)
                continue;

            if (IsLadderTrigger(hit))
                return true;
        }

        return false;
    }

    private bool IsTouchingPaintedLadderCells(Tilemap ladderTilemap, Bounds playerBounds)
    {
        Vector3Int minCell = ladderTilemap.WorldToCell(playerBounds.min - new Vector3(ladderDetectionPadding, ladderDetectionPadding, 0f));
        Vector3Int maxCell = ladderTilemap.WorldToCell(playerBounds.max + new Vector3(ladderDetectionPadding, ladderDetectionPadding, 0f));
        int verticalGapCells = Mathf.Max(0, ladderMaxVerticalGapCells);

        for (int x = minCell.x; x <= maxCell.x; x++)
        {
            for (int y = minCell.y - verticalGapCells; y <= maxCell.y + verticalGapCells; y++)
            {
                Vector3Int cell = new Vector3Int(x, y, 0);
                if (!ladderTilemap.HasTile(cell))
                    continue;

                Bounds cellBounds = GetCellWorldBounds(ladderTilemap, cell);
                cellBounds.Expand(new Vector3(ladderDetectionPadding, ladderDetectionPadding + verticalGapCells, 0f));
                if (cellBounds.Intersects(playerBounds))
                    return true;
            }
        }

        return false;
    }

    private static Bounds GetCellWorldBounds(Tilemap tilemap, Vector3Int cell)
    {
        Vector3 cellCenter = tilemap.GetCellCenterWorld(cell);
        Vector3 cellSize = Vector3.Scale(
            tilemap.layoutGrid != null ? tilemap.layoutGrid.cellSize : Vector3.one,
            tilemap.transform.lossyScale);

        cellSize.x = Mathf.Abs(cellSize.x);
        cellSize.y = Mathf.Abs(cellSize.y);
        return new Bounds(cellCenter, cellSize);
    }

    private void LeaveLadder()
    {
        isOnLadder = false;
        isInLadderTrigger = false;
        isClimbing = false;
        if (rb != null)
            rb.gravityScale = defaultGravityScale;
    }

    private void UpdateWeaponReturnState()
    {
        if (isClimbing && IsGrounded && Mathf.Abs(verticalInput) < 0.01f)
        {
            isClimbing = false;
            if (rb != null)
                rb.gravityScale = defaultGravityScale;
        }

        if (hideWeaponUntilGrounded && IsGrounded && !isClimbing)
            hideWeaponUntilGrounded = false;
    }

    private bool IsLadderTrigger(Collider2D other)
    {
        if (other == null || !other.isTrigger)
            return false;

        return TransformNameChainContains(other.transform, ladderNameContains);
    }

    private static bool TransformNameChainContains(Transform transformToCheck, string expectedNamePart)
    {
        if (transformToCheck == null || string.IsNullOrWhiteSpace(expectedNamePart))
            return false;

        string normalizedExpected = NormalizeObjectName(expectedNamePart);
        Transform current = transformToCheck;
        while (current != null)
        {
            if (NormalizeObjectName(current.name).Contains(normalizedExpected))
                return true;

            current = current.parent;
        }

        return false;
    }

    private void SetAnimatorBoolIfExists(string parameterName, bool value)
    {
        if (HasAnimatorParameter(parameterName))
            _animator.SetBool(parameterName, value);
    }

    private void SetAnimatorFloatIfExists(string parameterName, float value)
    {
        if (HasAnimatorParameter(parameterName))
            _animator.SetFloat(parameterName, value);
    }

    private bool HasAnimatorParameter(string parameterName)
    {
        foreach (AnimatorControllerParameter parameter in _animator.parameters)
        {
            if (parameter.name == parameterName)
                return true;
        }

        return false;
    }

    private static string NormalizeObjectName(string objectName)
    {
        return objectName
            .ToLowerInvariant()
            .Replace(" ", string.Empty)
            .Replace("_", string.Empty)
            .Replace("-", string.Empty)
            .Replace("=", string.Empty);
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
                    HideAllPopUps();
                    ShowHealthBarTip();
                }

                if (!IsFirstAirPlatformSet(platformName))
                    HideHealthBarTip();

                if (!combatTipDone && combatPopUp != null && platformName.StartsWith("2ndPlatform"))
                {
                    HideAllPopUps();
                    ShowPopUp(combatPopUp);
                }

                if (!weaponSwitchTipShown && weaponSwitchPopUp != null && platformName.StartsWith("3rdPlatform"))
                {
                    weaponSwitchTipShown = true;
                    HideAllPopUps();
                    ShowPopUp(weaponSwitchPopUp);
                }
                else if (weaponSwitchTipShown && !levelEndTipShown && IsLevelEndPlatform(platformName))
                {
                    levelEndTipShown = true;
                    HideAllPopUps();
                    if (levelEndPopUp != null)
                        ShowPopUp(levelEndPopUp);
                }

                return;
            }
        }
    }

    private void HideAllPopUps()
    {
        if (movementPopUp != null) movementPopUp.SetActive(false);
        if (combatPopUp != null) combatPopUp.SetActive(false);
        if (weaponSwitchPopUp != null) weaponSwitchPopUp.SetActive(false);
        if (levelEndPopUp != null) levelEndPopUp.SetActive(false);
        HideHealthBarTip();
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        // Optional: Hide dialogue when leaving a platform
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
            ShowPopUp(healthBarPopUp);
    }

    private void HideHealthBarTip()
    {
        if (healthBarPopUp != null)
            healthBarPopUp.SetActive(false);
    }

    private void ShowPopUp(GameObject popUp)
    {
        if (popUp == null) return;
        popUp.SetActive(true);

        if (popUpAutoHideRoutines.TryGetValue(popUp, out Coroutine running) && running != null)
            StopCoroutine(running);

        float duration = (popUp == levelEndPopUp) ? 10f : tutorialPopUpAutoHideSeconds;

        if (duration > 0f)
            popUpAutoHideRoutines[popUp] = StartCoroutine(AutoHidePopUp(popUp, duration));
    }

    private IEnumerator AutoHidePopUp(GameObject popUp, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (popUp != null && popUp.activeSelf)
            popUp.SetActive(false);
        popUpAutoHideRoutines[popUp] = null;
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
