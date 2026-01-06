using System.Collections;
using UnityEngine;

namespace Treachery.Weapons.View
{
    /// <summary>
    /// View-only component spawned from a WeaponDefinition prefab.
    /// Owns animator params and muzzle flash objects.
    /// </summary>
    public class WeaponView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] Transform weaponGfx;
        [SerializeField] Animator animator;

        [Header("Animation Params")]
        [SerializeField] string shootTriggerName = "Shoot";
        [SerializeField] string switchOutTriggerName = "Switch Out";
        [SerializeField] string currentAmmoParameterName = "Current Ammo";
        [SerializeField] string adsAnimationBool = "IsAiming";

        [Header("Timings")]
        [SerializeField] float switchOutAnimationDelay = 0.5f;

        [Header("Muzzle Flash")]
        [SerializeField] GameObject[] muzzleFlashObjects;
        [SerializeField] float muzzleFlashDuration = 0.05f;

        Coroutine _muzzleRoutine;

        public Transform WeaponGfx => weaponGfx != null ? weaponGfx : transform;

        public void SetAiming(bool aiming)
        {
            if (animator != null && !string.IsNullOrEmpty(adsAnimationBool))
                animator.SetBool(adsAnimationBool, aiming);
        }

        public void SetAmmoInMag(int inMag)
        {
            if (animator != null && !string.IsNullOrEmpty(currentAmmoParameterName))
                animator.SetInteger(currentAmmoParameterName, inMag);
        }

        public void PlayShoot()
        {
            if (animator != null && !string.IsNullOrEmpty(shootTriggerName))
                animator.SetTrigger(shootTriggerName);

            TriggerMuzzleFlash();
        }

        public float PlaySwitchOut()
        {
            if (animator != null && !string.IsNullOrEmpty(switchOutTriggerName))
                animator.SetTrigger(switchOutTriggerName);
            return switchOutAnimationDelay;
        }

        void TriggerMuzzleFlash()
        {
            if (muzzleFlashObjects == null || muzzleFlashObjects.Length == 0)
                return;

            if (_muzzleRoutine != null)
                StopCoroutine(_muzzleRoutine);

            _muzzleRoutine = StartCoroutine(MuzzleFlashRoutine());
        }

        IEnumerator MuzzleFlashRoutine()
        {
            for (int i = 0; i < muzzleFlashObjects.Length; i++)
            {
                var obj = muzzleFlashObjects[i];
                if (obj != null) obj.SetActive(true);
            }

            yield return new WaitForSeconds(muzzleFlashDuration);

            for (int i = 0; i < muzzleFlashObjects.Length; i++)
            {
                var obj = muzzleFlashObjects[i];
                if (obj != null) obj.SetActive(false);
            }

            _muzzleRoutine = null;
        }

        void Awake()
        {
            if (muzzleFlashObjects != null)
            {
                for (int i = 0; i < muzzleFlashObjects.Length; i++)
                {
                    var obj = muzzleFlashObjects[i];
                    if (obj != null) obj.SetActive(false);
                }
            }
        }
    }
}
