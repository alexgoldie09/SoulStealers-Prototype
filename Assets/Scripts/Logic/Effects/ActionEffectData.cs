using System;

namespace SSR.Logic
{
    /// <summary>
    /// Special Play: play a card as an Extra Action. The play is not
    /// an Action and does not consume the action budget. Rule 604.1.
    /// Cards played from a zone other than the Hand via Special Play
    /// are always played face-up. Rule 601.5.
    /// </summary>
    [Serializable]
    public class SpecialPlayEffectData : EffectData
    {
        public override EffectType EffectType => EffectType.SpecialPlay;

        // The card type that may be played as Extra Action.
        // Null means any card type. E.g. "Play a Spell as an Extra Action."
        public CardType? AllowedCardType;

        // True if the Extra Action can happen outside the Action Phase.
        // Some Spirit effects grant Extra Actions during Beginning of Turn.
        // Rule 604.1 example.
        public bool CanPlayOutsideActionPhase;

        // The specific card to play, if named by the effect.
        // -1 means the controller chooses from their Hand.
        public int CardToPlayID = -1;
    }

    /// <summary>
    /// PACT WITH [SPIRIT NAME]: the card may be Played as an Extra Action
    /// as long as its controller controls the named Spirit. Rule 811.
    /// Pact is a conditional Extra Action — it does not consume an action.
    /// </summary>
    [Serializable]
    public class PactEffectData : EffectData
    {
        public override EffectType EffectType => EffectType.Pact;

        // The first name of the Spirit required for this Pact.
        // e.g. "Liria" for "PACT WITH LIRIA". Rule 200.4, 811.
        public string RequiredSpiritName;
    }
}