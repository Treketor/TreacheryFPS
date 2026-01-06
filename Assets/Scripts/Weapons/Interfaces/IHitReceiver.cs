namespace Treachery.Weapons.Interfaces
{
    /// <summary>
    /// Target-driven damage entry point.
    /// Weapons should not special-case target types.
    /// </summary>
    public interface IHitReceiver
    {
        void ReceiveHit(in HitPayload payload);

        /// <summary>
        /// Used for presentation/impact effect decisions.
        /// </summary>
        bool CountsAsEnemyHit { get; }
    }
}
