using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    [Header("Spawn Settings")]
    public Transform player;
    public float spawnRadius = 25f;
    [Tooltip("Min/Max enemies that can spawn at once (at different spawn points)")]
    public Vector2Int simultaneousSpawnsRange = new(1, 5);
    [Tooltip("Min/Max time delay between spawn groups")]
    public Vector2 timeBetweenSpawnsRange = new(0.5f, 2f);
    public float timeBetweenWaves = 5f;
    public List<Wave> waves = new();

    [Header("Endless Mode Scaling")]
    public bool endlessMode = true;
    public int enemiesPerWaveIncrease = 5;
    public float enemyScaleMultiplier = 1.1f;

    [Header("Enemy Speed Scaling")]
    [Tooltip("Base speed range for enemies at wave 1")]
    public Vector2 baseSpeedRange = new Vector2(2.0f, 2.8f);
    [Tooltip("Speed increase per wave")]
    public float speedIncreasePerWave = 0.15f;
    [Tooltip("Maximum speed multiplier cap")]
    public float maxSpeedMultiplier = 2.5f;
    [Tooltip("Wave at which speed reaches maximum")]
    public int waveToReachMaxSpeed = 20;

    int currentWaveIndex = 0;
    int enemiesRemainingInWave;
    List<SpawnPoint> spawnPoints;
    readonly List<GameObject> aliveEnemies = new();

    public System.Action<int> OnWaveChanged; // current wave index
    public int CurrentWaveIndex { get; private set; } = -1;

    void Start()
    {
        spawnPoints = new List<SpawnPoint>(FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None));
        CurrentWaveIndex = 0;
        OnWaveChanged?.Invoke(CurrentWaveIndex);
        StartCoroutine(RunWaves());
    }

    IEnumerator RunWaves()
    {
        while (true)
        {
            Wave currentWave;
            
            // Use configured wave or generate a scaled wave
            if (currentWaveIndex < waves.Count) currentWave = waves[currentWaveIndex];
            else if (endlessMode)  currentWave = GenerateScaledWave(currentWaveIndex);
            else
            {
                Debug.Log("All waves completed!");
                break;
            }

            enemiesRemainingInWave = currentWave.enemiesToSpawn;

            // spawn the wave
            int enemiesToSpawn = currentWave.enemiesToSpawn;
            while (enemiesToSpawn > 0)
            {
                // Random spawn count within the range
                int randomSpawnCount = Random.Range(simultaneousSpawnsRange.x, simultaneousSpawnsRange.y + 1);
                int spawnCount = Mathf.Min(randomSpawnCount, enemiesToSpawn);
                SpawnEnemies(currentWave, spawnCount);
                enemiesToSpawn -= spawnCount;
                enemiesRemainingInWave -= spawnCount;
                
                if (enemiesToSpawn > 0)
                {
                    // Random delay between spawn groups
                    float randomDelay = Random.Range(timeBetweenSpawnsRange.x, timeBetweenSpawnsRange.y);
                    yield return new WaitForSeconds(randomDelay);
                }
            }

            // wait until all spawned enemies die before starting next wave
            while (aliveEnemies.Count > 0)
            {
                aliveEnemies.RemoveAll(e => e == null); // clean up dead entries
                yield return null;
            }

            // Wave complete - register with score system
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.RegisterWaveComplete(currentWaveIndex);
            }

            currentWaveIndex++;
            CurrentWaveIndex = currentWaveIndex;
            OnWaveChanged?.Invoke(CurrentWaveIndex);
            
            yield return new WaitForSeconds(timeBetweenWaves);
        }
    }

    Wave GenerateScaledWave(int waveIndex)
    {
        if (waves.Count == 0)
        {
            Debug.LogWarning("No configured waves to base scaling on!");
            return new Wave { enemiesToSpawn = 10, enemyTypes = new List<EnemyTypeWeight>() };
        }

        // Use the last configured wave as a template
        Wave lastConfiguredWave = waves[waves.Count - 1];
        int wavesAfterConfigured = waveIndex - waves.Count + 1;

        // Calculate scaled enemy count
        int baseEnemies = lastConfiguredWave.enemiesToSpawn;
        int scaledEnemies = baseEnemies + (wavesAfterConfigured * enemiesPerWaveIncrease);
        
        // Apply multiplier scaling
        scaledEnemies = Mathf.RoundToInt(scaledEnemies * Mathf.Pow(enemyScaleMultiplier, wavesAfterConfigured));

        // Create a new wave with the same enemy types as the last configured wave
        Wave scaledWave = new()
        {
            enemiesToSpawn = scaledEnemies,
            enemyTypes = new List<EnemyTypeWeight>(lastConfiguredWave.enemyTypes)
        };

        return scaledWave;
    }

    void SpawnEnemies(Wave wave, int count)
    {
        // Collect unlocked spawn points within radius of the player
        List<SpawnPoint> candidates = new();
        foreach (var sp in spawnPoints)
        {
            if (!sp.isUnlocked) continue;

            float dist = Vector3.Distance(player.position, sp.transform.position);
            if (dist <= spawnRadius)
            {
                candidates.Add(sp);
            }
        }

        if (candidates.Count == 0)
        {
            candidates.AddRange(spawnPoints.FindAll(sp => sp.isUnlocked));
        }

        // Track used spawn points to avoid spawning multiple enemies at the same point
        HashSet<SpawnPoint> usedSpawnPoints = new();

        for (int i = 0; i < count; i++)
        {
            // Filter out already used spawn points
            List<SpawnPoint> availableCandidates = new();
            foreach (var candidate in candidates)
            {
                if (!usedSpawnPoints.Contains(candidate))
                {
                    availableCandidates.Add(candidate);
                }
            }

            // If all spawn points are used, reuse them
            if (availableCandidates.Count == 0)
            {
                availableCandidates.AddRange(candidates);
                usedSpawnPoints.Clear();
            }

            SpawnPoint chosen = availableCandidates[Random.Range(0, availableCandidates.Count)];
            usedSpawnPoints.Add(chosen);

            GameObject enemyPrefab = PickRandomEnemy(wave.enemyTypes);
            GameObject enemyInstance = Instantiate(enemyPrefab, chosen.transform.position, Quaternion.identity);
            aliveEnemies.Add(enemyInstance);

            // Apply scaled speed to the enemy
            ApplySpeedScaling(enemyInstance);
        }
    }

    void ApplySpeedScaling(GameObject enemy)
    {
        // Calculate speed multiplier based on current wave
        float waveProgress = Mathf.Clamp01((float)currentWaveIndex / waveToReachMaxSpeed);
        float speedMultiplier = Mathf.Lerp(1f, maxSpeedMultiplier, waveProgress);
        
        // Also apply per-wave linear increase (with cap)
        float linearIncrease = 1f + (currentWaveIndex * speedIncreasePerWave);
        speedMultiplier = Mathf.Min(speedMultiplier * linearIncrease, maxSpeedMultiplier);

        // Get random speed within base range for this specific enemy
        float randomBaseSpeed = Random.Range(baseSpeedRange.x, baseSpeedRange.y);
        
        // Apply the wave multiplier
        float finalSpeed = randomBaseSpeed * speedMultiplier;

        // Apply to HoardLocomotion component if it exists
        if (enemy.TryGetComponent<HoardLocomotion>(out var locomotion))
        {
            locomotion.walkSpeed = finalSpeed;
        }

        // Also update NavMeshAgent if it exists
        if (enemy.TryGetComponent<UnityEngine.AI.NavMeshAgent>(out var agent))
        {
            agent.speed = finalSpeed;
        }
    }

    GameObject PickRandomEnemy(List<EnemyTypeWeight> list)
    {
        float total = 0f;
        foreach (var e in list) total += e.weight;
        float r = Random.Range(0f, total);
        float c = 0f;
        foreach (var e in list)
        {
            c += e.weight;
            if (r <= c) return e.prefab;
        }
        return list[0].prefab; // fallback
    }
}