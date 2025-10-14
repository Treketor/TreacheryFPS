using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    public Transform player;
    public float spawnRadius = 25f;
    public float timeBetweenSpawns = 1f;
    public float timeBetweenWaves = 5f;
    public List<Wave> waves = new List<Wave>();

    int currentWaveIndex = 0;
    int enemiesRemainingInWave;
    List<SpawnPoint> spawnPoints;
    readonly List<GameObject> aliveEnemies = new();

    void Start()
    {
        spawnPoints = new List<SpawnPoint>(FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None));
        StartCoroutine(RunWaves());
    }

    IEnumerator RunWaves()
    {
        while (currentWaveIndex < waves.Count)
        {
            Wave currentWave = waves[currentWaveIndex];
            enemiesRemainingInWave = currentWave.enemiesToSpawn;

            // spawn the wave
            for (int i = 0; i < currentWave.enemiesToSpawn; i++)
            {
                SpawnEnemy(currentWave);
                enemiesRemainingInWave--;
                yield return new WaitForSeconds(timeBetweenSpawns);
            }

            // wait until all spawned enemies die before starting next wave
            while (aliveEnemies.Count > 0)
            {
                aliveEnemies.RemoveAll(e => e == null); // clean up dead entries
                yield return null;
            }

            currentWaveIndex++;
            yield return new WaitForSeconds(timeBetweenWaves);
        }

        Debug.Log("All waves completed!");
    }

    void SpawnEnemy(Wave wave)
    {
        // Collect unlocked spawn points within radius of the player
        List<SpawnPoint> candidates = new List<SpawnPoint>();
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

        SpawnPoint chosen = candidates[Random.Range(0, candidates.Count)];
        GameObject enemyPrefab = PickRandomEnemy(wave.enemyTypes);
        GameObject enemyInstance = Instantiate(enemyPrefab, chosen.transform.position, Quaternion.identity);
        aliveEnemies.Add(enemyInstance);
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