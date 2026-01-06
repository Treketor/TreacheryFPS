using UnityEngine;
using Treachery.Weapons.Interfaces;

namespace Treachery.Weapons.Runtime
{
    /// <summary>
    /// References injected into a weapon at equip time.
    /// Avoids scene searches inside weapon logic.
    /// </summary>
    public readonly struct WeaponContext
    {
        public readonly IWeaponPresentation Presentation;
        public readonly PlayerMovement PlayerMovement;
        public readonly Transform ShootOrigin;

        public WeaponContext(IWeaponPresentation presentation, PlayerMovement playerMovement, Transform shootOrigin = null)
        {
            Presentation = presentation;
            PlayerMovement = playerMovement;
            ShootOrigin = shootOrigin;
        }
    }
}
