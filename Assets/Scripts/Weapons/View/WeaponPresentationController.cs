using UnityEngine;
using Treachery.Weapons.Interfaces;

namespace Treachery.Weapons.View
{
    /// <summary>
    /// Owns camera FOV, weapon offsets, recoil and sway integration.
    /// Weapons request changes through IWeaponPresentation.
    /// </summary>
    public class WeaponPresentationController : MonoBehaviour, IWeaponPresentation
    {
        [Header("References")]
        [SerializeField] Camera playerCamera;
        [SerializeField] WeaponRecoil recoil;

        [Header("Optional")]
        [SerializeField] WeaponSway sway;

        WeaponPresentationProfile _profile;
        bool _hasProfile;

        bool _isAiming;
        float _originalFov;
        Vector3 _originalWeaponGfxLocalPos;

        public bool IsAiming => _isAiming;

        void Awake()
        {
            if (playerCamera != null)
                _originalFov = playerCamera.fieldOfView;
        }

        public void Bind(in WeaponPresentationProfile profile)
        {
            _profile = profile;
            _hasProfile = true;

            if (_profile.WeaponGfx != null)
                _originalWeaponGfxLocalPos = _profile.WeaponGfx.localPosition;

            // Reset to hip-fire on bind
            SetAiming(false);
        }

        public void Unbind()
        {
            // Return to original state
            SetAiming(false);
            _hasProfile = false;
        }

        public void SetAiming(bool aiming)
        {
            if (!_hasProfile || !_profile.SupportsADS)
            {
                _isAiming = false;
                return;
            }

            _isAiming = aiming;

            if (sway != null)
            {
                sway.SendMessage("SetExternalIsAiming", _isAiming, SendMessageOptions.DontRequireReceiver);
            }
        }

        public void ApplyRecoil(float recoilMultiplier)
        {
            if (recoil != null)
                recoil.ApplyRecoil(recoilMultiplier);
        }

        void LateUpdate()
        {
            if (!_hasProfile)
                return;

            // Camera FOV
            if (playerCamera != null)
            {
                float targetFov = (_profile.SupportsADS && _isAiming) ? _profile.AdsFov : _originalFov;
                playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFov, _profile.AdsFovLerpSpeed * Time.deltaTime);
            }

            // Weapon GFX position
            if (_profile.WeaponGfx != null)
            {
                Vector3 target = _isAiming ? (_originalWeaponGfxLocalPos + _profile.AdsLocalOffset) : _originalWeaponGfxLocalPos;
                _profile.WeaponGfx.localPosition = Vector3.Lerp(_profile.WeaponGfx.localPosition, target, _profile.AdsPositionLerpSpeed * Time.deltaTime);
            }
        }
    }
}
