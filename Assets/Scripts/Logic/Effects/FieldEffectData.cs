using System;

namespace SSR.Logic
{
    /// <summary>
    /// Silence: removes all text from indicated cards on the Field.
    /// Silenced cards lose all current effects including attached cards.
    /// Can be temporary or permanent depending on source. Rule 808.
    /// </summary>
    [Serializable]
    public class SilenceEffectData : EffectData
    {
        public override EffectType EffectType => EffectType.Silence;

        // Target card(s) to Silence. Declared at pile entry. Rule 808.
        public int TargetCardID => TargetIDs.Count > 0 ? TargetIDs[0] : -1;

        // Duration of the Silence.
        // Temporary: "until your next turn" (most common). Rule 808.
        // Permanent: lasts as long as the Incantation applying it is on field.
        public EffectDurationTiming Duration = EffectDurationTiming.UntilNextTurn;

        // For "Silence up to X cards" effects.
        public int MaxTargets = 1;
    }

    /// <summary>
    /// Counter: add or remove counters from a card on the Field.
    /// Counters persist while the card is on the Field.
    /// Cease to exist if the card leaves the Field. Rule 107.
    /// </summary>
    [Serializable]
    public class CounterEffectData : NumericEffectData
    {
        public override EffectType EffectType => EffectType.Counter;

        // Target card on the Field. Declared at pile entry. Rule 107.
        public int TargetCardID => TargetIDs.Count > 0 ? TargetIDs[0] : -1;

        // True = add counters; False = remove counters.
        // BaseValue holds the count.
        public bool IsAddition = true;
    }

    /// <summary>
    /// MERGE: attach this Incantation to a valid host Incantation on the
    /// Field. The merged card loses its type and name; its effects are
    /// added to the host. Maximum two attached cards per host. Rule 805.
    ///
    /// Ritual MERGE: target must be under the controller's control.
    /// Curse MERGE: target can be any Incantation on the Field. Rule 805.
    ///
    /// If the MERGE effect is Negated, the Incantation goes to the
    /// owner's Discard Pile (not the Field). Rule 805.6.
    /// </summary>
    [Serializable]
    public class MergeEffectData : EffectData
    {
        public override EffectType EffectType => EffectType.Merge;

        // The host Incantation to attach to. Declared at pile entry.
        public int TargetIncantationID => TargetIDs.Count > 0 ? TargetIDs[0] : -1;

        // The type of the card doing the merging.
        // Determines valid targets: Ritual = own field only; Curse = any. Rule 805.
        public CardType SourceCardType;
    }
}
