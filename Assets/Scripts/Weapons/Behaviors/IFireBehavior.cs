namespace Treachery.Weapons.Behaviors
{
    /// <summary>
    /// Strategy for weapon firing.
    /// Hitscan is the first implementation.
    /// </summary>
    public interface IFireBehavior
    {
        void Fire(object weaponOwner, float spread);
    }
}
