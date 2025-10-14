using UnityEngine;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    public float maxHealth = 50f;
    
    [Header("Soul Drop")]
    [Tooltip("Prefab to spawn when enemy dies")]
    public GameObject soulPickupPrefab;
    [Tooltip("Number of souls to drop")]
    public int soulsToDropMin = 1;
    public int soulsToDropMax = 3;

    public System.Action<EnemyHealth> OnDeath;
    float _hp;

    void Awake() { _hp = maxHealth; }

    public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (_hp <= 0f) return;
        _hp -= amount;
        // TODO: hit FX / stagger
        if (_hp <= 0f)
        {
            OnDeath?.Invoke(this);
            SpawnSouls();
            Destroy(gameObject);
        }
    }

    void SpawnSouls()
    {
        if (soulPickupPrefab == null) return;

        int soulCount = Random.Range(soulsToDropMin, soulsToDropMax + 1);
        Vector3 spawnPosition = transform.position + Vector3.up * 1f; // Spawn above enemy center

        for (int i = 0; i < soulCount; i++)
        {
            GameObject soulObj = Instantiate(soulPickupPrefab, spawnPosition, Quaternion.identity);
            
            // Tell the soul to eject
            if (soulObj.TryGetComponent<SoulPickup>(out var soulPickup))
            {
                soulPickup.Eject(spawnPosition);
            }
        }
    }
}