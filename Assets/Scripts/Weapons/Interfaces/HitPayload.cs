using UnityEngine;

namespace Treachery.Weapons.Interfaces
{
    /// <summary>
    /// Data-only description of a hit.
    /// </summary>
    public readonly struct HitPayload
    {
        public readonly float Damage;
        public readonly float BulletForce;
        public readonly Vector3 Point;
        public readonly Vector3 Normal;
        public readonly Collider HitCollider;
        public readonly GameObject Source;

        public HitPayload(float damage, float bulletForce, Vector3 point, Vector3 normal, Collider hitCollider, GameObject source)
        {
            Damage = damage;
            BulletForce = bulletForce;
            Point = point;
            Normal = normal;
            HitCollider = hitCollider;
            Source = source;
        }
    }
}
