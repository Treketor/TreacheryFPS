using System;
using System.Collections;
using UnityEngine;

namespace Treachery.Weapons.Behaviors.Reload
{
    [Serializable]
    public class SingleBulletReloadBehavior : IWeaponReloadBehavior
    {
        [SerializeField] float startReloadDuration = 0.8f;
        [SerializeField] float singleBulletReloadDuration = 0.6f;
        [SerializeField] float finishReloadDuration = 0.5f;

        [Header("Animation Triggers")]
        [SerializeField] string startReloadTrigger = "StartReload";
        [SerializeField] string reloadSingleBulletTrigger = "ReloadSingleBullet";
        [SerializeField] string finishReloadTrigger = "FinishReload";

        [SerializeField] bool autoFinishWhenFull = true;

        enum ReloadPhase { None, Starting, LoadingSingleBullet, Finishing }

        bool _isReloading;
        ReloadPhase _phase;
        bool _finishRequested;

        Coroutine _routine;
        MonoBehaviour _runner;
        Animator _animator;

        int _currentAmmo;
        int _maxAmmo;
        int _reserveAmmo;

        Action<int> _onAmmoAdded;
        Action _onReloadComplete;
        Action _onReloadCancelled;

        public bool IsReloading => _isReloading;

        public void Initialize(MonoBehaviour runner, Animator animator)
        {
            _runner = runner;
            _animator = animator;
        }

        public void ConfigureTimings(float startSeconds, float perBulletSeconds, float finishSeconds)
        {
            startReloadDuration = startSeconds;
            singleBulletReloadDuration = perBulletSeconds;
            finishReloadDuration = finishSeconds;
        }

        public bool CanReload(int currentAmmo, int maxAmmo, int reserveAmmo)
            => !_isReloading && currentAmmo < maxAmmo && reserveAmmo > 0;

        public void StartReload(int currentAmmo, int maxAmmo, int reserveAmmo, Action<int> onAmmoAdded, Action onReloadComplete, Action onReloadCancelled)
        {
            if (_isReloading || _runner == null)
                return;

            _currentAmmo = currentAmmo;
            _maxAmmo = maxAmmo;
            _reserveAmmo = reserveAmmo;
            _onAmmoAdded = onAmmoAdded;
            _onReloadComplete = onReloadComplete;
            _onReloadCancelled = onReloadCancelled;

            _isReloading = true;
            _finishRequested = false;
            _routine = _runner.StartCoroutine(ReloadRoutine());
        }

        public void CancelReload()
        {
            if (!_isReloading)
                return;

            if (_phase == ReloadPhase.LoadingSingleBullet)
                Stop(true);
        }

        public void RequestFinishReload()
        {
            if (_isReloading && _phase == ReloadPhase.LoadingSingleBullet)
                _finishRequested = true;
        }

        public bool CanBeInterrupted() => _phase == ReloadPhase.LoadingSingleBullet;

        public void Tick(float deltaTime)
        {
            // coroutine-driven
        }

        IEnumerator ReloadRoutine()
        {
            _phase = ReloadPhase.Starting;
            if (_animator != null && !string.IsNullOrEmpty(startReloadTrigger))
                _animator.SetTrigger(startReloadTrigger);

            yield return new WaitForSeconds(startReloadDuration);

            _phase = ReloadPhase.LoadingSingleBullet;
            while (_currentAmmo < _maxAmmo && _reserveAmmo > 0 && _isReloading)
            {
                if (_animator != null && !string.IsNullOrEmpty(reloadSingleBulletTrigger))
                    _animator.SetTrigger(reloadSingleBulletTrigger);

                yield return new WaitForSeconds(singleBulletReloadDuration);

                if (_reserveAmmo > 0)
                {
                    _currentAmmo++;
                    _reserveAmmo--;
                    _onAmmoAdded?.Invoke(1);
                }

                if ((_currentAmmo >= _maxAmmo && autoFinishWhenFull) || _finishRequested)
                    break;
            }

            if (!_isReloading)
                yield break;

            _phase = ReloadPhase.Finishing;
            if (_animator != null && !string.IsNullOrEmpty(finishReloadTrigger))
                _animator.SetTrigger(finishReloadTrigger);

            yield return new WaitForSeconds(finishReloadDuration);

            Stop(false);
        }

        void Stop(bool cancelled)
        {
            if (_routine != null && _runner != null)
                _runner.StopCoroutine(_routine);

            _routine = null;
            _isReloading = false;
            _phase = ReloadPhase.None;
            _finishRequested = false;

            if (cancelled) _onReloadCancelled?.Invoke();
            else _onReloadComplete?.Invoke();
        }
    }
}
