using UnityEngine;

/// <summary>
/// Spawns cartoon cars at random intervals in two lanes.
/// Cars drive left-to-right or right-to-left, alternating each wave.
/// Place this GameObject off-screen to the right (or left) of the level.
/// </summary>
public class CarSpawner : MonoBehaviour
{
    // ── Inspector-exposed settings ───────────────────────────────────────────
    [Header("Prefab")]
    [SerializeField] private GameObject carPrefab;          // The Car prefab to spawn

    [Header("Timing")]
    [SerializeField] private float minInterval = 2f;        // Min seconds between spawns
    [SerializeField] private float maxInterval = 5f;        // Max seconds between spawns

    [Header("Speed")]
    [SerializeField] private float minSpeed = 6f;           // Min car speed (units/sec)
    [SerializeField] private float maxSpeed = 10f;          // Max car speed (units/sec)

    [Header("Lanes")]
    [SerializeField] private float lane1Y = -3f;            // Ground-level lane Y
    [SerializeField] private float lane2Y = -2f;            // Slightly elevated lane Y

    [Header("Spawn X positions")]
    [SerializeField] private float spawnXRight =  35f;      // Spawn point for right-to-left cars
    [SerializeField] private float spawnXLeft  = -15f;      // Spawn point for left-to-right cars

    // ── Car colour palette ────────────────────────────────────────────────────
    private readonly Color[] _carColors = new Color[]
    {
        new Color(0.20f, 0.44f, 0.85f),    // Cartoon blue
        new Color(0.85f, 0.20f, 0.20f),    // Cartoon red
        new Color(0.95f, 0.80f, 0.15f),    // Cartoon yellow
        new Color(0.20f, 0.72f, 0.30f),    // Cartoon green
    };

    // ── Internal state ───────────────────────────────────────────────────────
    private float _spawnTimer;
    private float _nextSpawnTime;
    private int   _laneIndex;               // Alternates between lane 0 and 1
    private int   _directionSign = -1;      // -1 = right-to-left, flips each spawn

    // ────────────────────────────────────────────────────────────────────────
    private void Start()
    {
        ScheduleNextSpawn();
    }

    private void Update()
    {
        _spawnTimer += Time.deltaTime;

        if (_spawnTimer >= _nextSpawnTime)
        {
            SpawnCar();
            ScheduleNextSpawn();
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    /// <summary>Roll the next spawn delay.</summary>
    private void ScheduleNextSpawn()
    {
        _spawnTimer    = 0f;
        _nextSpawnTime = Random.Range(minInterval, maxInterval);
    }

    /// <summary>Instantiate and configure a car for the current lane/direction.</summary>
    private void SpawnCar()
    {
        if (carPrefab == null)
        {
            Debug.LogWarning("CarSpawner: carPrefab is not assigned.");
            return;
        }

        // Pick lane height
        float laneY = (_laneIndex == 0) ? lane1Y : lane2Y;
        _laneIndex = 1 - _laneIndex;  // Alternate next time

        // Pick spawn X based on direction
        float spawnX = (_directionSign < 0) ? spawnXRight : spawnXLeft;
        Vector3 spawnPos = new Vector3(spawnX, laneY, 0f);

        GameObject car = Instantiate(carPrefab, spawnPos, Quaternion.identity);

        // Set speed and direction
        float carSpeed = Random.Range(minSpeed, maxSpeed);
        CarMovement cm = car.GetComponent<CarMovement>();
        if (cm != null)
            cm.Initialize(carSpeed, _directionSign);

        // Randomise colour
        SpriteRenderer sr = car.GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
            sr.color = _carColors[Random.Range(0, _carColors.Length)];

        // Flip direction for next car
        _directionSign = -_directionSign;
    }
}
