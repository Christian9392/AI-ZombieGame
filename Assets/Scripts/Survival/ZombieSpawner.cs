using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class ZombieSpawner : MonoBehaviour
{
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

    void Start()
    {
        UpdateWaveUI();
        SpawnZombies(zombiesPerWave);
    }

    void Update()
    {
        CleanupDestroyedZombies();

        waveTimer += Time.deltaTime;

        UpdateCountdownUI(Mathf.Max(0, timeBetweenWaves - waveTimer));

        if (waveTimer >= timeBetweenWaves)
        {
            waveTimer = 0f;
            currentWave++;
            UpdateWaveUI();
            Debug.Log($"Wave {currentWave}! Spawning {zombiesPerWave} zombies.");
            SpawnZombies(zombiesPerWave);
        }
    }

    void UpdateWaveUI()
    {
        if (waveText != null)
        {
            waveText.text = $"Wave {currentWave}";
        }
    }

    void UpdateCountdownUI(float secondsLeft)
    {
        if (countdownText != null)
        {
            int rounded = Mathf.CeilToInt(secondsLeft);
            countdownText.text = $"Next wave in: {rounded}s";
        }
    }

    void CleanupDestroyedZombies()
    {
        spawnedZombies.RemoveAll(z => z == null || z.Equals(null));
    }

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

        // Try to find valid, well‐spaced positions
        while (spawnedCount < totalToSpawn && attempts < totalToSpawn * maxAttemptsPerZombie)
        {
            attempts++;

            // Pick a random point in the full spawn area
            float x = Random.Range(spawnAreaMin.x, spawnAreaMax.x);
            float y = Random.Range(spawnAreaMin.y, spawnAreaMax.y);
            Vector2 candidate = new Vector2(x, y);

            bool tooClose = false;

            // Check against same‐wave positions
            foreach (var pos in newPositions)
            {
                if (Vector2.Distance(pos, candidate) < minSpawnSeparation)
                {
                    tooClose = true;
                    break;
                }
            }
            if (tooClose) continue;

            // Optionally, check against previously spawned zombies still alive
            foreach (var go in spawnedZombies)
            {
                if (go != null && Vector2.Distance(go.transform.position, candidate) < minSpawnSeparation)
                {
                    tooClose = true;
                    break;
                }
            }
            if (tooClose) continue;

            // Accept this position
            newPositions.Add(candidate);
            spawnedCount++;
        }

        if (spawnedCount < totalToSpawn)
        {
            Debug.LogWarning($"ZombieSpawner: only found {spawnedCount}/{totalToSpawn} positions after {attempts} attempts.");
        }

        // Instantiate new zombies at the validated positions
        foreach (var pos in newPositions)
        {
            var go = Instantiate(zombiePrefab, pos, Quaternion.identity);
            spawnedZombies.Add(go);
        }
    }
}
