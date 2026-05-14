using UnityEngine;

/// <summary>
/// Smooth X-axis camera follow for Level 1.
/// Locks Y to a fixed value and clamps within level bounds.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private float smoothTime = 0.2f;

    [Header("Y Lock")]
    [SerializeField] private float lockedY = 0f;            // Resting Y when player is on the ground
    [SerializeField] private float zDepth = -10f;

    [Header("Y Follow")]
    [SerializeField] private bool  followY = true;          // Track player Y when they climb above lockedY
    [SerializeField] private float maxY = 8f;               // Highest Y the camera will rise to
    [SerializeField] private float yDeadzone = 1.5f;        // Vertical wiggle room before camera reacts (kills jump bob)

    [Header("Level Bounds")]
    [SerializeField] private float minX = -5f;              // Left edge — camera stops here
    [SerializeField] private float maxX = 55f;              // Right edge — camera stops here

    [Header("Lookahead (optional)")]
    [SerializeField] private float lookaheadDistance = 1.5f;// Units ahead of player to peek
    [SerializeField] private float lookaheadSpeed = 4f;     // How fast lookahead shifts

    private Vector3 _velocity = Vector3.zero;
    private float _currentLookahead;

    // ────────────────────────────────────────────────────────────────────────
    private void LateUpdate()
    {
        if (target == null) return;

        // Resolve lookahead direction based on player facing
        PlayerMovement pm = target.GetComponent<PlayerMovement>();
        float facing = pm != null ? pm.FacingDirection : 1f;
        _currentLookahead = Mathf.Lerp(_currentLookahead, facing * lookaheadDistance, lookaheadSpeed * Time.deltaTime);

        // Target X is the player X + lookahead, clamped within level bounds
        float halfWidth = GetComponent<Camera>().orthographicSize * GetComponent<Camera>().aspect;
        float clampedX = Mathf.Clamp(target.position.x + _currentLookahead, minX + halfWidth, maxX - halfWidth);

        float desiredY = lockedY;
        if (followY)
        {
            float playerY = target.position.y;
            float currentY = transform.position.y;
            if (playerY > currentY + yDeadzone)      desiredY = playerY - yDeadzone;
            else if (playerY < currentY - yDeadzone) desiredY = playerY + yDeadzone;
            else                                     desiredY = currentY;
            desiredY = Mathf.Clamp(desiredY, lockedY, maxY);
        }

        Vector3 desired = new Vector3(clampedX, desiredY, zDepth);
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref _velocity, smoothTime);
    }

    /// <summary>Assign the follow target at runtime (called by scene builders).</summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    /// <summary>Set level bounds from code (e.g., from a level manager).</summary>
    public void SetBounds(float newMinX, float newMaxX)
    {
        minX = newMinX;
        maxX = newMaxX;
    }
}
