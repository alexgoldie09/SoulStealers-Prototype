namespace SSR.Logic
{
    /// <summary>
    /// Represents an effect that can be applied to the game state, such as damage, healing, or card draw.
    /// </summary>
    public interface ICardEffect
    {
        EffectType EffectType { get; }
        EffectDependency Dependency { get; }
        EffectStatus Status { get; set; }
        void Apply(GameState state, int controllerID);
    }
}
