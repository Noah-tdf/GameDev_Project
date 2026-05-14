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

    private float _timer;
    private bool _isTimerRunning = true;
    private bool _bossSpawned = false;
    private GameObject _bossInstance;
    private bool _levelCompleted = false;

    private void Start()
    {
        _timer = countdownDuration;
        UpdateTimerUI();
        StartCoroutine(EnemySpawningRoutine());
    }

    private void Update()
    {
        if (_isTimerRunning)
        {
            _timer -= Time.deltaTime;
            if (_timer <= 0)
            {
                _timer = 0;
                _isTimerRunning = false;
                SpawnBoss();
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
        Debug.Log("Boss Defeated! Transitioning to cutscene...");
        // For now, just log. Cutscene transition will be added later.
        if (timerText != null) timerText.text = "VICTORY!";
    }
}
