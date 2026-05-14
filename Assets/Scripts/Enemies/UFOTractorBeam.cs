using UnityEngine;

public class UFOTractorBeam : MonoBehaviour
{
    [SerializeField] private int damage = 1;
    [SerializeField] private float beamLength = 40f; 

    private SpriteRenderer sr;

    private void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
    }

    public void Activate(float duration)
    {
        // 199 pixels / 100 PPU = 1.99 units.
        float spriteHeight = 1.99f;
        float targetScaleY = beamLength / spriteHeight;
        
        if (sr != null)
        {
            // Position the sprite so its top is at the parent's center
            // Parent is at UFO center. UFO is high up.
            sr.transform.localPosition = new Vector3(0, -beamLength / 2f, 0);
            sr.transform.localScale = new Vector3(3f, targetScaleY, 1f); 
        }
        
        // Setup Collider
        BoxCollider2D bc = GetComponent<BoxCollider2D>();
        if (bc == null) bc = gameObject.AddComponent<BoxCollider2D>();
        bc.isTrigger = true;
        bc.size = new Vector2(1.5f, beamLength);
        bc.offset = new Vector2(0, -beamLength / 2f);

        Destroy(gameObject, duration);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerHealth ph = other.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                ph.TakeDamage(damage);
            }
        }
    }
}
