namespace SSR.Logic
{
    /// <summary>
    /// Represents an effect that can be applied to the game state, such as damage, healing, or card draw.
    /// </summary>
    public interface ICardEffect
    {
        /// <summary>
        /// The type of effect, which determines how the effect is applied and interacts with other effects.
        /// </summary>
        EffectType EffectType { get; }
        /// <summary>
        /// The dependency of the effect, which determines when the effect can be applied and how it interacts with other effects.
        /// </summary>
        EffectDependency Dependency { get; }
        /// <summary>
        /// The status of the effect, which can affect how it interacts with other effects and game mechanics.
        /// For example, a silenced effect may not trigger or may be ignored by certain mechanics.
        /// </summary>
        EffectStatus Status { get; set; }
        /// <summary>
        /// Applies the effect to the game state, modifying it according to the effect's type and parameters.
        /// </summary>
        /// <param name="state"></param>
        /// <param name="controllerID"></param>
        void Apply(GameState state, int controllerID);
    }
}
