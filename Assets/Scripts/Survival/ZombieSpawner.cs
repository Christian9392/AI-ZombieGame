using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class ZombieSpawner : MonoBehaviour
{
    public GameObject zombiePrefab;
    public int zombiesPerWave = 4;
    public float timeBetweenWaves = 10f;
    public int maxZombies = 50;

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

        // Define spawn areas for each corner
        Vector2 midPoint = new Vector2(
            (spawnAreaMin.x + spawnAreaMax.x) / 2f,
            (spawnAreaMin.y + spawnAreaMax.y) / 2f
        );

        Vector2[,] corners = new Vector2[4, 2]
        {
            { new Vector2(spawnAreaMin.x, spawnAreaMin.y), new Vector2(midPoint.x, midPoint.y) }, // 0 - Bottom-left
            { new Vector2(spawnAreaMin.x, midPoint.y), new Vector2(midPoint.x, spawnAreaMax.y) }, // 1 - Top-left
            { new Vector2(midPoint.x, midPoint.y), new Vector2(spawnAreaMax.x, spawnAreaMax.y) }, // 2 - Top-right
            { new Vector2(midPoint.x, spawnAreaMin.y), new Vector2(spawnAreaMax.x, midPoint.y) }  // 3 - Bottom-right
        };

        int[] distribution = { 3, 3, 3, 3 };
        int sum = 0;
        foreach (int val in distribution) sum += val;
        if (sum != totalToSpawn)
        {
            Debug.LogWarning("Spawn distribution doesn't match total zombies. Adjusting...");
            distribution = new int[4];
            for (int i = 0; i < totalToSpawn; i++)
            {
                distribution[i % 4]++;
            }
        }

        int zombiesAllowed = Mathf.Min(totalToSpawn, maxZombies - spawnedZombies.Count);
        if (zombiesAllowed <= 0) return;

        // Spawn per zone
        for (int zone = 0; zone < 4 && zombiesAllowed > 0; zone++)
        {
            for (int i = 0; i < distribution[zone] && zombiesAllowed > 0; i++, zombiesAllowed--)
            {
                Vector2 min = corners[zone, 0];
                Vector2 max = corners[zone, 1];

                Vector2 spawnPos = new Vector2(
                    Random.Range(min.x, max.x),
                    Random.Range(min.y, max.y)
                );

                GameObject newZombie = Instantiate(zombiePrefab, spawnPos, Quaternion.identity);
                spawnedZombies.Add(newZombie);
            }
        }
    }
}
