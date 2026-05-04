using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class Bullet : MonoBehaviour
{
    [SerializeField] private int damage = 1;
    [SerializeField] private float lifetime = 2f;
    [SerializeField] private float environmentCollisionDelay = 0.05f;

    private static Sprite fallbackSprite;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private GameObject owner;
    private float environmentCollisionEnabledTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        EnsureVisibleSprite();
    }

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    public void Initialize(float direction, float speed, GameObject bulletOwner)
    {
        owner = bulletOwner;
        environmentCollisionEnabledTime = Time.time + environmentCollisionDelay;
        rb.linearVelocity = new Vector2(direction * speed, 0f);
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

        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        if (other.CompareTag("Ground") || other.CompareTag("Platform"))
        {
            if (Time.time < environmentCollisionEnabledTime)
            {
                return;
            }

            Destroy(gameObject);
            return;
        }

        if (other.CompareTag("Enemy"))
        {
            Destroy(gameObject);
        }
    }
}
