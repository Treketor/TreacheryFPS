using System;
using System.Collections;
using UnityEngine;

namespace Treachery.Weapons.Behaviors.Reload
{
    [Serializable]
    public class MagazineReloadBehavior : IWeaponReloadBehavior
    {
        [SerializeField] float reloadDuration = 1.2f;
        [SerializeField] string reloadTriggerName = "Reload";

        bool _isReloading;
        Coroutine _routine;
        MonoBehaviour _runner;
        Animator _animator;

        Action<int> _onAmmoAdded;
        Action _onReloadComplete;
        Action _onReloadCancelled;

        public bool IsReloading => _isReloading;

        public void Initialize(MonoBehaviour runner, Animator animator)
        {
            _runner = runner;
            _animator = animator;
        }

        public void SetReloadDuration(float seconds) => reloadDuration = seconds;

        public void SetReloadTrigger(string triggerName) => reloadTriggerName = triggerName;

        public bool CanReload(int currentAmmo, int maxAmmo, int reserveAmmo)
            => !_isReloading && currentAmmo < maxAmmo && reserveAmmo > 0;

        public void StartReload(int currentAmmo, int maxAmmo, int reserveAmmo, Action<int> onAmmoAdded, Action onReloadComplete, Action onReloadCancelled)
        {
            if (_isReloading || _runner == null)
                return;

            _onAmmoAdded = onAmmoAdded;
            _onReloadComplete = onReloadComplete;
            _onReloadCancelled = onReloadCancelled;

            _isReloading = true;
            _routine = _runner.StartCoroutine(ReloadRoutine(currentAmmo, maxAmmo, reserveAmmo));
        }

        public void CancelReload()
        {
            if (!_isReloading)
                return;

            Stop(true);
        }

        public void Tick(float deltaTime)
        {
            // coroutine-driven
        }

        IEnumerator ReloadRoutine(int currentAmmo, int maxAmmo, int reserveAmmo)
        {
            if (_animator != null && !string.IsNullOrEmpty(reloadTriggerName))
                _animator.SetTrigger(reloadTriggerName);

            yield return new WaitForSeconds(reloadDuration);

            int needed = maxAmmo - currentAmmo;
            int toAdd = Mathf.Min(needed, reserveAmmo);
            if (toAdd > 0)
                _onAmmoAdded?.Invoke(toAdd);

            Stop(false);
        }

        void Stop(bool cancelled)
        {
            if (_routine != null && _runner != null)
                _runner.StopCoroutine(_routine);

            _routine = null;
            _isReloading = false;

            if (cancelled) _onReloadCancelled?.Invoke();
            else _onReloadComplete?.Invoke();
        }
    }
}
