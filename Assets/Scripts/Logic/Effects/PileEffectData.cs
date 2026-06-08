using System;

namespace SSR.Logic
{
    /// <summary>
    /// Negate: remove a card or effect object from the Resolution Pile
    /// before it resolves. The negated card goes to its owner's Discard
    /// Pile. Negated effect objects cease to exist. Rule 806.
    ///
    /// A Negate targeting a specific type (e.g. "Negate a Sorcery")
    /// can only target cards of that type, not effect objects. Rule 806.3.
    /// Static effects can never be Negated. Rule 705.
    /// </summary>
    [Serializable]
    public class NegateEffectData : EffectData
    {
        public override EffectType EffectType => EffectType.Negate;

        // The card or effect object ID on the pile to Negate.
        // Declared at pile entry. Rule 806.
        public int TargetOnPileID => TargetIDs.Count > 0 ? TargetIDs[0] : -1;

        // If set, only cards of this type can be targeted.
        // Null means any card or effect object. Rule 806.3.
        public CardType? TypeRestriction;

        // True = targeting a card on the pile.
        // False = targeting an effect object on the pile.
        // A type-restricted Negate can only target cards. Rule 806.3.
        public bool TargetsCard = true;
    }

    /// <summary>
    /// CONSPIRACY: keyword on Secret cards.
    /// When the Secret is turned face-up, its controller may Put (not
    /// Play) a face-down Sorcery from their Hand onto the Field before
    /// the Secret resolves. The Put Sorcery may immediately be revealed
    /// if its condition is met. Rule 303.9.
    ///
    /// Put bypasses the Resolution Pile and cannot be Negated. Rule 604.4.
    /// </summary>
    [Serializable]
    public class ConspiracyEffectData : EffectData
    {
        public override EffectType EffectType => EffectType.Conspiracy;
        // No additional fields.
        // The choice of which Sorcery to Put is made by the controller
        // at resolution time. The resolver queries the controller's Hand.
    }
}