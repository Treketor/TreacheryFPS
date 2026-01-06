using UnityEngine;

namespace Treachery.Weapons.Interfaces
{
    public readonly struct WeaponPresentationProfile
    {
        public readonly bool SupportsADS;
        public readonly Transform WeaponGfx;
        public readonly float AdsFov;
        public readonly float AdsFovLerpSpeed;
        public readonly Vector3 AdsLocalOffset;
        public readonly float AdsPositionLerpSpeed;

        public WeaponPresentationProfile(
            bool supportsADS,
            Transform weaponGfx,
            float adsFov,
            float adsFovLerpSpeed,
            Vector3 adsLocalOffset,
            float adsPositionLerpSpeed)
        {
            SupportsADS = supportsADS;
            WeaponGfx = weaponGfx;
            AdsFov = adsFov;
            AdsFovLerpSpeed = adsFovLerpSpeed;
            AdsLocalOffset = adsLocalOffset;
            AdsPositionLerpSpeed = adsPositionLerpSpeed;
        }
    }

    /// <summary>
    /// Presentation layer (camera FOV, sway, recoil, weapon offsets).
    /// Weapon logic should request changes through this interface.
    /// </summary>
    public interface IWeaponPresentation
    {
        bool IsAiming { get; }

        void Bind(in WeaponPresentationProfile profile);
        void Unbind();

        void SetAiming(bool aiming);

        void ApplyRecoil(float recoilMultiplier);
    }
}
