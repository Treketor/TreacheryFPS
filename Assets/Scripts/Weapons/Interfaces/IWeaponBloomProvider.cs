namespace Treachery.Weapons.Interfaces
{
    /// <summary>
    /// Optional extension interface for weapons that expose bloom/spread UI info.
    /// </summary>
    public interface IWeaponBloomProvider
    {
        /// <summary>
        /// Bloom percentage in range [0..1].
        /// </summary>
        float GetBloomPercentage();
    }
}
