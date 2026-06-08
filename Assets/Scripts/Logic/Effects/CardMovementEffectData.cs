using System;

namespace SSR.Logic
{
    /// <summary>
    /// Destroy: Put a card from the Field into its owner's Discard Pile.
    /// Distinct from Sacrifice, Discard, and Negate. Rule 813.
    /// Target must be on the Field when this effect resolves; otherwise
    /// the effect Fizzles. INDESTRUCTIBLE prevents Destruction. Rule 810.
    /// If the Destroyed card has attached cards, they go to their
    /// owners' Discard Piles. Rule 805.6.
    /// </summary>
    [Serializable]
    public class DestroyEffectData : EffectData
    {
        public override EffectType EffectType => EffectType.Destroy;

        // Target card on the Field. Declared at pile entry. Rule 813.
        public int TargetCardID => TargetIDs.Count > 0 ? TargetIDs[0] : -1;

        // Optional card type restriction ("Destroy an Incantation").
        // Null means any card type. Rule 813.
        public CardType? TypeRestriction;
    }

    /// <summary>
    /// Discard: Put a card from its current zone into its owner's Discard
    /// Pile. Unless specified, Discard means from Hand. Rule 814.
    /// Distinct from Destroy and Sacrifice. Rule 814.
    /// </summary>
    [Serializable]
    public class DiscardEffectData : EffectData
    {
        public override EffectType EffectType => EffectType.Discard;

        // Target card to Discard. -1 means controller chooses. Rule 814.
        public int TargetCardID => TargetIDs.Count > 0 ? TargetIDs[0] : -1;

        // Zone to Discard from. Default is Hand per the rules. Rule 814.
        public ZoneType SourceZone = ZoneType.Hand;

        // Player who must Discard. -1 means the controller.
        public int TargetPlayerID = -1;
    }

    /// <summary>
    /// Recall: move cards from a specified zone to the specified
    /// player's Hand. Rule 812.
    /// </summary>
    [Serializable]
    public class RecallEffectData : EffectData
    {
        public override EffectType EffectType => EffectType.Recall;

        // Zone to recall cards from.
        public ZoneType SourceZone = ZoneType.DiscardPile;

        // The player whose zone is recalled from. Declared at pile entry.
        public int TargetPlayerID => TargetIDs.Count > 0 ? TargetIDs[0] : -1;

        // How many cards to recall.
        public int Count = 1;

        // True = takes from top of the zone (e.g. "top two cards").
        // False = controller or owner chooses which cards. Rule 812.
        public bool TakesFromTop;

        // Specific card IDs to recall if the effect names them.
        // Empty means the controller or rules determine which cards.
        public System.Collections.Generic.List<int> SpecificCardIDs
            = new System.Collections.Generic.List<int>();
    }

    /// <summary>
    /// Copy: acquire the name, supertype, type, and effects of the
    /// copied card. Rule 809.
    ///
    /// Sorcery copies: remain copies on the pile, return to original
    /// form in the Discard Pile when they leave. Rule 809.2.
    /// Incantation copies: remain copies until duration ends or they
    /// leave the Field. Rule 809.3.
    /// </summary>
    [Serializable]
    public class CopyEffectData : EffectData
    {
        public override EffectType EffectType => EffectType.Copy;

        // The card to copy. Declared at pile entry. Rule 809.
        public int TargetCardID => TargetIDs.Count > 0 ? TargetIDs[0] : -1;

        // How long the copy lasts.
        public EffectDurationTiming Duration = EffectDurationTiming.Permanent;
    }
}
