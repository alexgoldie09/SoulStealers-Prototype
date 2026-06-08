namespace SSR.Logic
{
    /// <summary>
    /// Defines the various types of cards in the game, each with unique mechanics and interactions.
    /// </summary>
    public enum CardType
    {
        Spirit,
        Spell,
        Secret,
        Ritual,
        Curse,
        Prayer
    }

    /// <summary>
    /// Defines the super types of cards, which can be used to categorise cards with shared mechanics or themes.
    /// </summary>
    public enum CardSuperType
    {
        None,
        Sorcery,
        Incantation
    }

    /// <summary>
    /// Defines the various zones in the game where cards can be located, each with specific rules for how cards interact within them.
    /// For example, the Main Deck is where players draw cards from, while the Hand is where players hold their cards before playing them.
    /// The Discard Pile is where used or discarded cards go, and the Spirit Zone is where active spirits are placed.
    /// The Incantation Zone and Sorcery Zone are specific to those card types,
    /// while the Resolution Pile is where effects resolve.
    /// </summary>
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

    /// <summary>
    /// Defines the various types of effects that can be applied to cards or players, each with unique mechanics and interactions.
    /// </summary>
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

    /// <summary>
    /// Defines the different ways numeric values can be represented in card effects, allowing for more flexible and thematic effect descriptions.
    /// </summary>
    public enum NumericValueType
    {
        Symbolic,
        WordForm,
        X
    }

    /// <summary>
    /// Defines the status of an effect, which can affect how it interacts with other effects and game mechanics.
    /// </summary>
    public enum EffectStatus
    {
        Active,
        Standby,
        Silenced
    }

    /// <summary>
    /// Defines the dependency of an effect, which can determine how it interacts with other effects and game mechanics.
    /// </summary>
    public enum EffectDependency
    {
        Unlinked,
        Linked,
        CardIndependent,
        CardDependent
    }

    /// <summary>
    /// Defines the different phases of a game round, which can affect how players can take actions and how effects resolve.
    /// </summary>
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

    /// <summary>
    /// Defines the ownership types for cards and effects, which can affect how they interact with players and game mechanics.
    /// </summary>
    public enum OwnershipType
    {
        Owner,
        Controller
    }

    /// <summary>
    /// Defines the visibility of information in the game, which can affect how players make decisions and strategise.
    /// </summary>
    public enum InformationVisibility
    {
        Public,
        Hidden
    }
    
    /// <summary>
    /// Defines the state of a card's face, which can affect how it interacts with other cards and game mechanics.
    /// </summary>
    public enum CardFaceState
    {
        FaceUp,
        FaceDown
    }

    /// <summary>
    /// Defines the various locations where a card can be during the game, which can affect how it interacts with other cards and game mechanics.
    /// </summary>
    public enum CardLocation
    {
        MainDeck,
        Hand,
        DiscardPile,
        SpiritZone,
        IncantationZone,
        SorceryZone,
        ResolutionPile,
        SideGame,
        Attached
    }

    /// <summary>
    /// Defines the timing for how long an effect lasts, which can affect how it interacts with other effects and game mechanics.
    /// </summary>
    public enum EffectDurationTiming
    {
        Permanent,
        UntilEndOfTurn,
        UntilNextTurn,
        WhileOnField,
        WhileSourceCardOnField
    }

    /// <summary>
    /// Defines the different types of plays that can be made during the
    /// Action Phase. Reveal is turning a face-down Spell face-up as an
    /// action. Rule 602.3, 600.1.
    /// </summary>
    public enum PlayType
    {
        NormalPlay,
        SecretPlay,
        MergePlay,
        SpecialPlay,
        Put,
        Reveal
    }
}
