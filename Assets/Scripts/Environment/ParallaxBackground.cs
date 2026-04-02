using UnityEngine;

/// <summary>
/// Moves a background layer at a fraction of the camera's speed, creating parallax depth.
/// Attach to each background layer (SkyLayer, BuildingsLayer, PropsLayer).
/// Set parallaxFactor: 0 = stationary, 1 = moves with camera (no parallax).
/// </summary>
public class ParallaxBackground : MonoBehaviour
{
    // ── Inspector-exposed settings ───────────────────────────────────────────
    [Header("Parallax")]
    [SerializeField] private float parallaxFactor = 0.2f;  // 0.2 = moves at 20% of camera speed

    [Header("Infinite Scroll (optional)")]
    [SerializeField] private bool infiniteScroll = false;   // Tile horizontally for seamless loop
    [SerializeField] private float tileWidth = 20f;         // Width of one tile in world units

    // ── Internal state ───────────────────────────────────────────────────────
    private Transform _cam;
    private float _startX;
    private float _startCamX;

    // ────────────────────────────────────────────────────────────────────────
    private void Start()
    {
        _cam = Camera.main.transform;
        _startX    = transform.position.x;
        _startCamX = _cam.position.x;
    }

    private void LateUpdate()
    {
        // How far the camera has moved since start
        float cameraDelta = _cam.position.x - _startCamX;

        // Move this layer at the parallax fraction of the camera's travel
        float newX = _startX + cameraDelta * parallaxFactor;

        transform.position = new Vector3(newX, transform.position.y, transform.position.z);

        // Optional: wrap the layer when it drifts a full tile width
        if (infiniteScroll)
        {
            float distFromCam = _cam.position.x - transform.position.x;
            if (Mathf.Abs(distFromCam) >= tileWidth)
            {
                float shift = Mathf.Sign(distFromCam) * tileWidth;
                _startX += shift;
            }
        }
    }
}
