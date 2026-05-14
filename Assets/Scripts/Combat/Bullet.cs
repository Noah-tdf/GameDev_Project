using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class Bullet : MonoBehaviour
{
    [SerializeField] private int damage = 1;
    [SerializeField] private float lifetime = 2f;
    [SerializeField] private float maxDistance = 12f;
    [SerializeField] private float environmentCollisionDelay = 0.05f;

    private static Sprite fallbackSprite;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private GameObject owner;
    private Vector2 startPosition;
    private float environmentCollisionEnabledTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        EnsureVisibleSprite();
    }

    private void Start()
    {
        startPosition = transform.position;
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        if (Vector2.Distance(startPosition, transform.position) >= maxDistance)
            Destroy(gameObject);
    }

    public void Initialize(float direction, float speed, GameObject bulletOwner)
    {
        owner = bulletOwner;
        startPosition = transform.position;
        environmentCollisionEnabledTime = Time.time + environmentCollisionDelay;
        rb.linearVelocity = new Vector2(direction * speed, 0f);
        
        // Face the move direction
        float angle = direction < 0 ? 180 : 0;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    public void InitializeAimed(Vector2 velocity, GameObject bulletOwner, float range)
    {
        owner = bulletOwner;
        startPosition = transform.position;
        maxDistance = range;
        environmentCollisionEnabledTime = Time.time + environmentCollisionDelay;
        rb.linearVelocity = velocity;

        // Rotate to match velocity
        float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private void EnsureVisibleSprite()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        if (spriteRenderer.sprite == null)
        {
            fallbackSprite ??= CreateFallbackSprite();
            spriteRenderer.sprite = fallbackSprite;
        }

        if (spriteRenderer.color.a <= 0f)
        {
            spriteRenderer.color = new Color(1f, 0.9f, 0.2f, 1f);
        }

        if (spriteRenderer.sortingOrder < 10)
        {
            spriteRenderer.sortingOrder = 10;
        }
    }

    private static Sprite CreateFallbackSprite()
    {
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject == owner)
        {
            return;
        }

        bool ownerIsPlayer = owner != null && owner.CompareTag("Player");
        bool targetIsEnemy = other.CompareTag("Enemy");
        bool targetIsPlayer = other.CompareTag("Player");

        // Player bullet hitting enemy
        if (ownerIsPlayer && targetIsEnemy)
        {
            // Damage Enemy component
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
                Destroy(gameObject);
                return;
            }

            // Damage Drone component
            DroneEnemy drone = other.GetComponent<DroneEnemy>();
            if (drone != null)
            {
                drone.TakeDamage(damage);
                Destroy(gameObject);
                return;
            }

            // Damage GroundRobot component
            GroundRobotEnemy groundRobot = other.GetComponent<GroundRobotEnemy>();
            if (groundRobot != null)
            {
                groundRobot.TakeDamage(damage);
                Destroy(gameObject);
                return;
            }
            
            // If it's tagged enemy but has no specific component, still destroy
            Destroy(gameObject);
            return;
        }

        // Enemy bullet hitting player
        if (!ownerIsPlayer && targetIsPlayer)
        {
            PlayerHealth ph = other.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                ph.TakeDamage(damage);
                Destroy(gameObject);
                return;
            }
        }

        // Environment collision
        if (other.CompareTag("Ground") || other.CompareTag("Platform"))
        {
            if (Time.time < environmentCollisionEnabledTime)
            {
                return;
            }

            Destroy(gameObject);
            return;
        }

        // If an enemy bullet hits another enemy, just pass through (don't even destroy the bullet?)
        // The user said "they shouldn't be able to damage", usually this implies ignoring them.
        // If we want bullets to be blocked by enemies but not damage them, we destroy but don't damage.
        // But "ignore" usually means pass through. I'll make them pass through for better "don't shoot each other" feel.
        if (!ownerIsPlayer && targetIsEnemy)
        {
            return;
        }
    }
}
