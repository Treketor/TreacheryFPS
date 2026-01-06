using UnityEngine;

namespace Treachery.Weapons.Behaviors
{
    /// <summary>
    /// Hitscan fire behavior extracted from WeaponInstance_Hitscan.
    /// Currently uses a narrow "owner" contract to remain behavior-preserving.
    /// </summary>
    [System.Serializable]
    public class HitscanFireBehavior : IFireBehavior
    {
        public void Fire(object weaponOwner, float spread)
        {
            if (weaponOwner is not WeaponInstance_Hitscan weapon)
                return;

            if (weapon.raycaster == null)
                return;

            if (weapon.UsesPelletSystem)
            {
                FirePellets(weapon, spread);
            }
            else
            {
                FireSingleBullet(weapon, spread);
            }
        }

        private void FirePellets(WeaponInstance_Hitscan weapon, float baseSpread)
        {
            float pelletSpread = baseSpread * weapon.PelletSpreadMultiplier;
            float pelletDamage = weapon.PelletDamagePerPellet;

            for (int i = 0; i < weapon.PelletsPerShot; i++)
            {
                if (weapon.raycaster.TryShoot(out var hit, pelletSpread))
                {
                    weapon.ProcessPelletHit_Public(hit, pelletDamage);
                }
            }
        }

        private void FireSingleBullet(WeaponInstance_Hitscan weapon, float spread)
        {
            if (weapon.raycaster.TryShoot(out var hit, spread))
            {
                weapon.ProcessBulletHit_Public(hit, weapon.CurrentDamage);
            }
        }
    }
}
