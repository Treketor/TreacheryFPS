using UnityEditor;
using UnityEngine;
using Treachery.Weapons.Data;

namespace Treachery.Weapons.Editor
{
    [CustomEditor(typeof(WeaponDefinition))]
    public class WeaponDefinitionEditor : UnityEditor.Editor
    {
        static bool _showPelletSettings;
        static bool _showFalloffSettings;
        static bool _showReloadSettings;

        SerializedProperty _viewPrefab;
        SerializedProperty _crosshairSprite;

        SerializedProperty _displayName;

        SerializedProperty _baseDamage;
        SerializedProperty _baseFireRate;
        SerializedProperty _baseMagSize;
        SerializedProperty _baseReloadTime;
        SerializedProperty _baseSpread;
        SerializedProperty _baseBulletForce;

        SerializedProperty _startingReserveAmmo;

        SerializedProperty _maxRange;
        SerializedProperty _hitMask;
        SerializedProperty _ignoreLayerMask;

        SerializedProperty _usePelletSystem;
        SerializedProperty _pelletsPerShot;
        SerializedProperty _bulletsPerShot;
        SerializedProperty _pelletSpreadMultiplier;
        SerializedProperty _pelletDamageMultiplier;

        SerializedProperty _enableDamageFalloff;
        SerializedProperty _damageFalloffCurve;
        SerializedProperty _maxDamageRange;

        SerializedProperty _supportsADS;
        SerializedProperty _adsFOV;
        SerializedProperty _adsTransitionSpeed;
        SerializedProperty _adsPosition;
        SerializedProperty _adsPositionSpeed;
        SerializedProperty _adsRecoilMultiplier;

        SerializedProperty _recoilMultiplier;

        SerializedProperty _reloadType;
        SerializedProperty _startReloadDuration;
        SerializedProperty _singleBulletReloadDuration;
        SerializedProperty _finishReloadDuration;

        void OnEnable()
        {
            _viewPrefab = serializedObject.FindProperty("viewPrefab");
            _crosshairSprite = serializedObject.FindProperty("crosshairSprite");

            _displayName = serializedObject.FindProperty("displayName");

            _baseDamage = serializedObject.FindProperty("baseDamage");
            _baseFireRate = serializedObject.FindProperty("baseFireRate");
            _baseMagSize = serializedObject.FindProperty("baseMagSize");
            _baseReloadTime = serializedObject.FindProperty("baseReloadTime");
            _baseSpread = serializedObject.FindProperty("baseSpread");
            _baseBulletForce = serializedObject.FindProperty("baseBulletForce");

            _startingReserveAmmo = serializedObject.FindProperty("startingReserveAmmo");

            _maxRange = serializedObject.FindProperty("maxRange");
            _hitMask = serializedObject.FindProperty("hitMask");
            _ignoreLayerMask = serializedObject.FindProperty("ignoreLayerMask");

            _usePelletSystem = serializedObject.FindProperty("usePelletSystem");
            _pelletsPerShot = serializedObject.FindProperty("pelletsPerShot");
            _bulletsPerShot = serializedObject.FindProperty("bulletsPerShot");
            _pelletSpreadMultiplier = serializedObject.FindProperty("pelletSpreadMultiplier");
            _pelletDamageMultiplier = serializedObject.FindProperty("pelletDamageMultiplier");

            _enableDamageFalloff = serializedObject.FindProperty("enableDamageFalloff");
            _damageFalloffCurve = serializedObject.FindProperty("damageFalloffCurve");
            _maxDamageRange = serializedObject.FindProperty("maxDamageRange");

            _supportsADS = serializedObject.FindProperty("supportsADS");
            _adsFOV = serializedObject.FindProperty("adsFOV");
            _adsTransitionSpeed = serializedObject.FindProperty("adsTransitionSpeed");
            _adsPosition = serializedObject.FindProperty("adsPosition");
            _adsPositionSpeed = serializedObject.FindProperty("adsPositionSpeed");
            _adsRecoilMultiplier = serializedObject.FindProperty("adsRecoilMultiplier");

            _recoilMultiplier = serializedObject.FindProperty("recoilMultiplier");

            _reloadType = serializedObject.FindProperty("reloadType");
            _startReloadDuration = serializedObject.FindProperty("startReloadDuration");
            _singleBulletReloadDuration = serializedObject.FindProperty("singleBulletReloadDuration");
            _finishReloadDuration = serializedObject.FindProperty("finishReloadDuration");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawScriptField();
            EditorGUILayout.PropertyField(_viewPrefab);
            EditorGUILayout.PropertyField(_crosshairSprite);
            EditorGUILayout.PropertyField(_displayName);
            EditorGUILayout.PropertyField(_baseDamage);
            EditorGUILayout.PropertyField(_baseFireRate);
            EditorGUILayout.PropertyField(_baseMagSize);
            EditorGUILayout.PropertyField(_baseReloadTime);
            EditorGUILayout.PropertyField(_baseSpread);
            EditorGUILayout.PropertyField(_baseBulletForce);
            EditorGUILayout.PropertyField(_startingReserveAmmo);
            EditorGUILayout.PropertyField(_maxRange);
            EditorGUILayout.PropertyField(_hitMask);
            EditorGUILayout.PropertyField(_ignoreLayerMask);

            EditorGUILayout.PropertyField(_usePelletSystem);
            if (_usePelletSystem.boolValue)
            {
                EditorGUI.indentLevel++;
                _showPelletSettings = EditorGUILayout.Foldout(_showPelletSettings, "Pellet Settings", true);
                if (_showPelletSettings)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(_pelletsPerShot);
                    EditorGUILayout.PropertyField(_bulletsPerShot);
                    EditorGUILayout.PropertyField(_pelletSpreadMultiplier);
                    EditorGUILayout.PropertyField(_pelletDamageMultiplier);
                    EditorGUI.indentLevel--;
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.PropertyField(_enableDamageFalloff);
            EditorGUI.indentLevel++;
            using (new EditorGUI.DisabledScope(!_enableDamageFalloff.boolValue))
            {
                _showFalloffSettings = EditorGUILayout.Foldout(_showFalloffSettings, "Falloff Settings", true);
                if (_showFalloffSettings)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(_damageFalloffCurve);
                    EditorGUILayout.PropertyField(_maxDamageRange);
                    EditorGUI.indentLevel--;
                }
            }
            EditorGUI.indentLevel--;

            EditorGUILayout.PropertyField(_supportsADS);
            using (new EditorGUI.DisabledScope(!_supportsADS.boolValue))
            {
                EditorGUILayout.PropertyField(_adsFOV);
                EditorGUILayout.PropertyField(_adsTransitionSpeed);
                EditorGUILayout.PropertyField(_adsPosition);
                EditorGUILayout.PropertyField(_adsPositionSpeed);
                EditorGUILayout.PropertyField(_adsRecoilMultiplier);
            }

            EditorGUILayout.PropertyField(_recoilMultiplier);

            EditorGUILayout.PropertyField(_reloadType);
            EditorGUI.indentLevel++;
            _showReloadSettings = EditorGUILayout.Foldout(_showReloadSettings, "Reload Settings", true);
            if (_showReloadSettings)
            {
                EditorGUI.indentLevel++;
                // Only show single-bullet timings when that mode is selected.
                if (_reloadType.enumValueIndex == (int)ReloadType.SingleBullet)
                {
                    EditorGUILayout.PropertyField(_startReloadDuration);
                    EditorGUILayout.PropertyField(_singleBulletReloadDuration);
                    EditorGUILayout.PropertyField(_finishReloadDuration);
                }
                else
                {
                    EditorGUILayout.HelpBox("No extra settings for Magazine reload.", MessageType.Info);
                }
                EditorGUI.indentLevel--;
            }
            EditorGUI.indentLevel--;

            serializedObject.ApplyModifiedProperties();
        }

        void DrawScriptField()
        {
            using (new EditorGUI.DisabledScope(true))
            {
                var script = MonoScript.FromScriptableObject((WeaponDefinition)target);
                if (script != null)
                    EditorGUILayout.ObjectField("Script", script, typeof(MonoScript), false);
            }
        }
    }
}
