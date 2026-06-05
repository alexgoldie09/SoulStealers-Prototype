namespace SSR.Logic
{
    public interface ICardEffect
    {
        EffectType EffectType { get; }
        EffectDependency Dependency { get; }
        EffectStatus Status { get; set; }
        void Apply(GameState state, int controllerID);
    }
}
