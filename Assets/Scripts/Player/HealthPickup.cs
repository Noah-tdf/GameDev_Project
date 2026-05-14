using UnityEngine;

public class HealthPickup : MonoBehaviour
{
    [SerializeField] private float bobAmplitude = 0.25f;
    [SerializeField] private float bobFrequency = 2f;

    private Vector3 _startPosition;

    private void Start()
    {
        _startPosition = transform.position;
    }

    private void Update()
    {
        // Visual bobbing
        float newY = _startPosition.y + Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
        transform.position = new Vector3(_startPosition.x, newY, _startPosition.z);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerHealth ph = other.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                ph.HealFull();
                Destroy(gameObject);
            }
        }
    }
}
