using System;
using System.Collections.Generic;

namespace SSR.Logic
{
    /// <summary>
    /// DEFENSE X: static keyword on Rituals and Prayers.
    /// Reduces each Banish or Steal attempt against its controller by X.
    /// Multiple DEFENSE effects are cumulative. Rule 804.
    /// Never enters the Resolution Pile. Cannot be Negated.
    /// </summary>
    [Serializable]
    public class DefenseEffectData : NumericEffectData
    {
        public override EffectType EffectType => EffectType.Defense;
        // Value is in BaseValue (inherited from NumericEffectData).
        // No additional fields — Defense has no targets.
    }

    /// <summary>
    /// INDESTRUCTIBLE: static effect preventing Destruction.
    /// The card can still be Sacrificed. Rule 810.
    /// Lost if the card is Silenced (rule 810).
    /// Can be a keyword on the card itself, or granted temporarily
    /// by another effect with a specified duration.
    /// </summary>
    [Serializable]
    public class IndestructibleEffectData : EffectData
    {
        public override EffectType EffectType => EffectType.Indestructible;

        // Duration if granted by another effect.
        // Permanent if this is the card's own keyword. Rule 810.
        public EffectDurationTiming Duration = EffectDurationTiming.Permanent;

        // The card that becomes Indestructible.
        // -1 means the source card itself (keyword on the card). Rule 810.
        public int TargetCardID => TargetIDs.Count > 0 ? TargetIDs[0] : -1;
    }

    /// <summary>
    /// IGNORE: static effect. The card and all its effects are not
    /// affected by the specified types or effects. Rule 807.
    ///
    /// Note: A card with Ignore CAN still be targeted by effects it
    /// Ignores — it just isn't affected by them. Rule 807 example.
    /// </summary>
    [Serializable]
    public class IgnoreEffectData : EffectData
    {
        public override EffectType EffectType => EffectType.Ignore;

        // Card types that this card ignores (e.g. Secrets, Curses).
        public List<CardType> IgnoredCardTypes = new List<CardType>();

        // True if Banish and Steal effects on this card bypass DEFENSE.
        // Rule 807 example: "Spell that Ignores Secrets and DEFENSE."
        public bool IgnoresDefense;

        // True if Secrets cannot respond to this card's effects.
        // Secrets can still target the card — they just don't affect it.
        // Rule 807.
        public bool IgnoresSecretResponses;
    }
}
