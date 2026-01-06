using Treachery.Weapons.Interfaces;

namespace Treachery.Weapons.Runtime
{
    /// <summary>
    /// Marker receiver for detached ragdolls so impact effects treat them as "enemy hit".
    /// </summary>
    public class RagdollHitReceiver : UnityEngine.MonoBehaviour, IHitReceiver
    {
        public bool CountsAsEnemyHit => true;

        public void ReceiveHit(in HitPayload payload)
        {
            // Intentionally does nothing (ragdoll is already dead).
        }
    }
}
