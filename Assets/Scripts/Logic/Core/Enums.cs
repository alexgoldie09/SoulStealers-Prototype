namespace SSR.Logic
{
    public enum CardType
    {
        Spirit,
        Spell,
        Secret,
        Ritual,
        Curse,
        Prayer
    }

    public enum CardSuperType
    {
        None,
        Sorcery,
        Incantation
    }

    public enum ZoneType
    {
        MainDeck,
        Hand,
        DiscardPile,
        SpiritZone,
        IncantationZone,
        SorceryZone,
        ResolutionPile,
        SideGame
    }

    public enum EffectType
    {
        Steal,
        Banish,
        GiveSouls,
        Defense,
        Modifier,
        Merge,
        Negate,
        Ignore,
        Silence,
        Conspiracy,
        SpecialPlay,
        Pact,
        Recall,
        Destroy,
        Discard,
        Indestructible,
        Copy,
        Counter
    }

    public enum NumericValueType
    {
        Symbolic,
        WordForm,
        X
    }

    public enum EffectStatus
    {
        Active,
        Standby,
        Silenced
    }

    public enum EffectDependency
    {
        Unlinked,
        Linked,
        CardIndependent,
        CardDependent
    }

    public enum GamePhase
    {
        BeginRound,
        SpiritDeal,
        SpiritSelection,
        RevealSpirits,
        StartOfTurn,
        ActionPhase,
        EndOfTurn,
        DrawPhase,
        EndOfRound
    }

    public enum OwnershipType
    {
        Owner,
        Controller
    }

    public enum InformationVisibility
    {
        Public,
        Hidden
    }
}
