using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EnemyTypeWeight
{
    public GameObject prefab;
    [Range(0f, 1f)] public float weight = 1f;
}

[System.Serializable]
public class Wave
{
    public int enemiesToSpawn = 3;
    public List<EnemyTypeWeight> enemyTypes;
}