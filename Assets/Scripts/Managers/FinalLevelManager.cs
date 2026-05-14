using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class FinalLevelManager : MonoBehaviour
{
    [Header("Timer Settings")]
    [SerializeField] private float countdownDuration = 120f;
    [SerializeField] private TextMeshProUGUI timerText;

    [Header("Spawning Settings")]
    [SerializeField] private GameObject[] enemyPrefabs;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float spawnInterval = 5f;
    [SerializeField] private Transform enemiesContainer;

    [Header("Boss Settings")]
    [SerializeField] private GameObject bossPrefab;
    [SerializeField] private Transform bossSpawnPoint;

    [Header("Victory Sequence")]
    [SerializeField] private Sprite victoryTransitionImage;
    [SerializeField] private float victoryDelay = 3.0f;
    [SerializeField] private float zoomSize = 3.0f;

    private float _timer;
    private bool _isTimerRunning = true;
    private bool _bossSpawned = false;
    private GameObject _bossInstance;
    private bool _levelCompleted = false;
    private bool _isVictorySequencePlaying = false;

    private void Start()
    {
        _timer = countdownDuration;
        UpdateTimerUI();
        StartCoroutine(EnemySpawningRoutine());
    }

    private void Update()
    {
        if (_isVictorySequencePlaying) return;

        if (_isTimerRunning)
        {
            _timer -= Time.deltaTime;
            if (_timer <= 0)
            {
                _timer = 0;
                _isTimerRunning = false;
                
                // Trigger victory sequence when timer runs out
                StartCoroutine(VictorySequence());
            }
            UpdateTimerUI();
        }

        if (_bossSpawned && _bossInstance == null && !_levelCompleted)
        {
            _levelCompleted = true;
            OnBossDefeated();
        }
    }

    private void UpdateTimerUI()
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(_timer / 60);
            int seconds = Mathf.FloorToInt(_timer % 60);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }

    private IEnumerator EnemySpawningRoutine()
    {
        while (_isTimerRunning)
        {
            SpawnEnemy();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnEnemy()
    {
        if (!_isTimerRunning || enemyPrefabs.Length == 0 || spawnPoints.Length == 0) return;

        int prefabIndex = Random.Range(0, enemyPrefabs.Length);
        int spawnIndex = Random.Range(0, spawnPoints.Length);

        Vector3 spawnPos = spawnPoints[spawnIndex].position;
        spawnPos.z = -1.0f; // More aggressive offset to prevent background clipping
        GameObject enemy = Instantiate(enemyPrefabs[prefabIndex], spawnPos, Quaternion.identity, enemiesContainer);
        
        // Setup patrol points for the enemy
        Enemy enemyScript = enemy.GetComponent<Enemy>();
        if (enemyScript != null)
        {
            // Level bounds roughly -12 to 17
            float leftBound = Mathf.Max(-11.5f, spawnPos.x - 5f);
            float rightBound = Mathf.Min(16.5f, spawnPos.x + 5f);
            
            enemyScript.SetPatrolPoints(new Vector3(leftBound, 0, 0), new Vector3(rightBound, 0, 0));
        }
    }

    private void SpawnBoss()
    {
        if (bossPrefab != null && bossSpawnPoint != null)
        {
            Vector3 spawnPos = bossSpawnPoint.position;
            spawnPos.z = -1.0f;
            _bossInstance = Instantiate(bossPrefab, spawnPos, Quaternion.identity, enemiesContainer);
            _bossSpawned = true;
            if (timerText != null) timerText.text = "DEFEAT THE BOSS!";
        }
    }

    private void OnBossDefeated()
    {
        Debug.Log("Boss Defeated! Starting victory sequence...");
        if (!_isVictorySequencePlaying)
        {
            StartCoroutine(VictorySequence());
        }
    }

    private IEnumerator VictorySequence()
    {
        if (_isVictorySequencePlaying) yield break;
        _isVictorySequencePlaying = true;
        _isTimerRunning = false;

        if (timerText != null) timerText.text = "VICTORY!";

        // 1. Stop everyone
        StopAllMovement();

        // 2. Find the player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            // Trigger Victory Animation
            Animator anim = player.GetComponent<Animator>();
            if (anim != null)
            {
                anim.SetTrigger("Victory");
            }

            // Camera Focus
            CameraFollow camFollow = Object.FindFirstObjectByType<CameraFollow>();
            if (camFollow != null)
            {
                camFollow.SetTarget(player.transform);
                camFollow.SetZoom(zoomSize);
            }
        }

        // 3. Delay for victory pose
        yield return new WaitForSeconds(victoryDelay);

        // 4. Transition to Credits via image2
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.TransitionWithPortal("Credits", victoryTransitionImage);
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Credits");
        }
        }

    private void StopAllMovement()
    {
        // 1. Disable all MonoBehaviours that handle logic for enemies and player
        MonoBehaviour[] allScripts = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        
        string[] typesToDisable = new string[] 
        { 
            "Enemy", "DroneEnemy", "UFOEnemy", "GroundRobotEnemy", "CarMovement", 
            "PlayerMovement", "PlayerShooting", "PlayerHealth", "Bullet",
            "WeaponHandFollower", "WeaponHand"
        };

        foreach (var script in allScripts)
        {
            if (script == null) continue;
            
            string typeName = script.GetType().Name;
            bool shouldDisable = false;
            foreach (var t in typesToDisable)
            {
                if (typeName == t)
                {
                    shouldDisable = true;
                    break;
                }
            }

            if (shouldDisable)
            {
                script.enabled = false;
                
                // Stop Rigidbody velocity
                Rigidbody2D rb = script.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector2.zero;
                    rb.isKinematic = true; // Prevent further physics movement
                }
            }
        }

        // 2. Clear all bullets immediately
        foreach (var projectile in GameObject.FindGameObjectsWithTag("Bullet"))
        {
            Destroy(projectile);
        }
        
        // 3. Stop spawning flag
        _isTimerRunning = false;
    }
}
