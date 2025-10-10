using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponController : MonoBehaviour
{
    public WeaponInstance_Hitscan currentWeapon;

    [SerializeField] InputActionAsset playerInput;
    InputAction _attackAction;
    InputAction _reloadAction;

    void Awake()
    {
        if (playerInput != null)
        {
            _attackAction = playerInput.FindAction("Attack");
            _reloadAction = playerInput.FindAction("Reload");
        }
    }

    void Update()
    {
        if (_attackAction != null && _attackAction.IsPressed())
            currentWeapon.TryFire();
        
        if (_reloadAction != null && _reloadAction.WasPressedThisFrame())
            currentWeapon.TryReload();
    }
}