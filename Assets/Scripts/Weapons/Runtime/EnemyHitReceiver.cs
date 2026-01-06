using UnityEngine;
using Treachery.Weapons.Interfaces;

namespace Treachery.Weapons.Runtime
{
    /// <summary>
    /// Adapter that turns legacy EnemyHealth/HeadshotZone into the new IHitReceiver entry point.
    /// Attach to the enemy root (same object as EnemyHealth / HeadshotZone).
    /// </summary>
    public class EnemyHitReceiver : MonoBehaviour, IHitReceiver
    {
        [SerializeField] EnemyHealth enemyHealth;
        [SerializeField] HeadshotZone headshotZone;
        [Tooltip("Register headshots into ScoreManager (once per frame).")]
        [SerializeField] bool registerHeadshotsInScore = true;

        static int _lastHeadshotFrame = -1;
        static int _lastHeadshotSourceId = 0;

        public bool CountsAsEnemyHit => true;

        void Awake()
        {
            if (enemyHealth == null)
                enemyHealth = GetComponent<EnemyHealth>();
            if (headshotZone == null)
                headshotZone = GetComponent<HeadshotZone>();
        }

        void OnValidate()
        {
            if (enemyHealth == null)
                enemyHealth = GetComponent<EnemyHealth>();
            if (headshotZone == null)
                headshotZone = GetComponent<HeadshotZone>();
        }

        public void ReceiveHit(in HitPayload payload)
        {
            if (enemyHealth == null)
                return;

            // Headshot path: preserve legacy multiplier + headshot marking behavior.
            if (headshotZone != null && payload.HitCollider != null && headshotZone.IsHeadCollider(payload.HitCollider))
            {
                bool wasHeadshot = headshotZone.ProcessHeadshot(payload.Damage, payload.Point, payload.Normal, payload.BulletForce, out _);

                if (wasHeadshot && registerHeadshotsInScore && ScoreManager.Instance != null && payload.Source != null)
                {
                    int sourceId = payload.Source.GetInstanceID();
                    if (_lastHeadshotFrame != Time.frameCount || _lastHeadshotSourceId != sourceId)
                    {
                        _lastHeadshotFrame = Time.frameCount;
                        _lastHeadshotSourceId = sourceId;
                        ScoreManager.Instance.RegisterHeadshot();
                    }
                }

                return;
            }

            // Body hit path.
            enemyHealth.TakeDamage(payload.Damage, payload.Point, payload.Normal, payload.BulletForce);
        }
    }
}
