using UnityEngine;

namespace Treachery.Weapons.Data
{
    /// <summary>
    /// Data-only definition for a weapon. Not wired yet (behavior-preserving refactor step).
    /// </summary>
    [CreateAssetMenu(menuName = "Treachery/Weapons/Weapon Definition", fileName = "WeaponDefinition")]
    public class WeaponDefinition : ScriptableObject
    {
        [Header("View")]
        public GameObject viewPrefab;
        public Sprite crosshairSprite;

        [Header("Identity")]
        public string displayName = "Weapon";

        [Header("Base Stats")]
        public float baseDamage = 20f;
        public float baseFireRate = 5f;
        public int baseMagSize = 12;
        public float baseReloadTime = 1.2f;
        public float baseSpread = 1.5f;
        public float baseBulletForce = 400f;

        [Header("Ammo")]
        public int startingReserveAmmo = 60;

        [Header("Hitscan")]
        public float maxRange = 100f;
        public LayerMask hitMask = ~0;
        public LayerMask ignoreLayerMask = 0;

        [Header("Pellet System")]
        public bool usePelletSystem = false;
        public int pelletsPerShot = 8;
        public int bulletsPerShot = 1;
        public float pelletSpreadMultiplier = 3f;
        public float pelletDamageMultiplier = 1f;

        [Header("Damage Falloff")]
        public bool enableDamageFalloff = false;
        public AnimationCurve damageFalloffCurve = AnimationCurve.Linear(0f, 1f, 1f, 0.5f);
        public float maxDamageRange = 50f;

        [Header("ADS")]
        public bool supportsADS = true;
        public float adsFOV = 60f;
        public float adsTransitionSpeed = 16f;
        public Vector3 adsPosition = new Vector3(0f, 0f, 0.2f);
        public float adsPositionSpeed = 12f;
        public float adsRecoilMultiplier = 0.5f;

        [Header("Recoil")]
        [Tooltip("Scales the player's WeaponRecoil kick for this weapon. Higher = more kick.")]
        public float recoilMultiplier = 1f;

        [Header("Reload")]
        public ReloadType reloadType = ReloadType.Magazine;

        [Tooltip("Single bullet reload: time for 'StartReload'")]
        public float startReloadDuration = 0.8f;

        [Tooltip("Single bullet reload: time per bullet")]
        public float singleBulletReloadDuration = 0.6f;

        [Tooltip("Single bullet reload: time for 'FinishReload'")]
        public float finishReloadDuration = 0.5f;
    }
}
