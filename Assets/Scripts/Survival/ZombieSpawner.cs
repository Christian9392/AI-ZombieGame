using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class ZombieSpawner : MonoBehaviour
{
    // Controls the zombie spawning behavior
    public GameObject zombiePrefab;
    public int zombiesPerWave = 4;
    public float timeBetweenWaves = 10f;
    public int maxZombies = 50;
    public float minSpawnSeparation = 3f;
    public int maxAttemptsPerZombie = 20;
    public Vector2 spawnAreaMin = new Vector2(-10f, -10f);
    public Vector2 spawnAreaMax = new Vector2(10f, 10f);
    private List<GameObject> spawnedZombies = new List<GameObject>();
    private float waveTimer = 0f;
    private int currentWave = 1;
    public TMP_Text waveText;
    public TMP_Text countdownText;

    // Initialize the spawner
    void Start()
    {
        UpdateWaveUI();
        SpawnZombies(zombiesPerWave);
    }

    // Update is called once per frame
    void Update()
    {
        CleanupDestroyedZombies();

        waveTimer += Time.deltaTime;

        UpdateCountdownUI(Mathf.Max(0, timeBetweenWaves - waveTimer));

        // Spawn a new wave if the timer has reached the interval
        if (waveTimer >= timeBetweenWaves)
        {
            waveTimer = 0f;
            currentWave++;
            UpdateWaveUI();
            Debug.Log($"Wave {currentWave}! Spawning {zombiesPerWave} zombies.");
            SpawnZombies(zombiesPerWave);
        }
    }

    // Update the UI elements for the current wave
    void UpdateWaveUI()
    {
        if (waveText != null)
        {
            waveText.text = $"Wave {currentWave}";
        }
    }

    // Update the countdown timer UI
    void UpdateCountdownUI(float secondsLeft)
    {
        if (countdownText != null)
        {
            int rounded = Mathf.CeilToInt(secondsLeft);
            countdownText.text = $"Next wave in: {rounded}s";
        }
    }

    // Clean up any zombies that have been destroyed
    void CleanupDestroyedZombies()
    {
        spawnedZombies.RemoveAll(z => z == null || z.Equals(null));
    }

    // Spawn a specified number of zombies, ensuring they are well‐spaced
    void SpawnZombies(int totalToSpawn)
    {
        if (zombiePrefab == null)
        {
            Debug.LogError("zombiePrefab is NULL! Assign it in the Inspector.");
            return;
        }

        var newPositions = new List<Vector2>();
        int spawnedCount = 0;
        int attempts = 0;

        // Validate spawn area
        while (spawnedCount < totalToSpawn && attempts < totalToSpawn * maxAttemptsPerZombie)
        {
            attempts++;

            // Generate a random position within the spawn area
            float x = Random.Range(spawnAreaMin.x, spawnAreaMax.x);
            float y = Random.Range(spawnAreaMin.y, spawnAreaMax.y);
            Vector2 candidate = new Vector2(x, y);

            bool tooClose = false;

            // Check against already spawned positions
            foreach (var pos in newPositions)
            {
                if (Vector2.Distance(pos, candidate) < minSpawnSeparation)
                {
                    tooClose = true;
                    break;
                }
            }
            if (tooClose) continue;

            // Check against already spawned zombies
            foreach (var go in spawnedZombies)
            {
                if (go != null && Vector2.Distance(go.transform.position, candidate) < minSpawnSeparation)
                {
                    tooClose = true;
                    break;
                }
            }
            if (tooClose) continue;

            // Add the candidate position if it passes all checks
            newPositions.Add(candidate);
            spawnedCount++;
        }

        if (spawnedCount < totalToSpawn)
        {
            Debug.LogWarning($"ZombieSpawner: only found {spawnedCount}/{totalToSpawn} positions after {attempts} attempts.");
        }

        // Instantiate zombies at the valid positions
        foreach (var pos in newPositions)
        {
            var go = Instantiate(zombiePrefab, pos, Quaternion.identity);
            spawnedZombies.Add(go);
        }
    }
}
