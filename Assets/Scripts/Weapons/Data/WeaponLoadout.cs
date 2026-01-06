using UnityEngine;

namespace Treachery.Weapons.Data
{
    [CreateAssetMenu(menuName = "Treachery/Weapons/Weapon Loadout", fileName = "WeaponLoadout")]
    public class WeaponLoadout : ScriptableObject
    {
        [Tooltip("Weapon definitions in slot order.")]
        public WeaponDefinition[] slots = new WeaponDefinition[2];

        public int SlotCount => slots != null ? slots.Length : 0;

        public WeaponDefinition Get(int index)
        {
            if (slots == null || index < 0 || index >= slots.Length)
                return null;
            return slots[index];
        }
    }
}
