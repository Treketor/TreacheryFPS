using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    public bool isUnlocked = true;

    void OnDrawGizmosSelected()
    {
        Gizmos.color = isUnlocked ? Color.green : Color.red;
        Gizmos.DrawSphere(transform.position + Vector3.up * 0.1f, 0.2f);
    }
}