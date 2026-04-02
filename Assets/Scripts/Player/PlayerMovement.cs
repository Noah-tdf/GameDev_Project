using UnityEngine;
using UnityEngine.InputSystem;

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

    private Rigidbody2D rb;
    private Collider2D bodyCollider;
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
        if (groundCheck != null)
        {
            IsGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
            return;
        }

        if (bodyCollider == null)
        {
            Debug.LogWarning("PlayerMovement: Ground Check is not assigned.", this);
            IsGrounded = false;
            return;
        }

        Bounds bounds = bodyCollider.bounds;
        Vector2 origin = new Vector2(bounds.center.x, bounds.min.y);
        float castDistance = 0.08f;
        RaycastHit2D hit = Physics2D.BoxCast(origin, new Vector2(bounds.size.x * 0.9f, 0.05f), 0f, Vector2.down, castDistance, groundLayer);
        IsGrounded = hit.collider != null;
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

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
