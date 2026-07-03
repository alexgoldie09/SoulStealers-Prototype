using System;

namespace SSR.Logic
{
    /// <summary>
    /// A meta-effect that signals a triggered ability. TriggerSystem scans
    /// field cards for these and places the payload (looked up by index in
    /// the card's Effects list) on the Resolution Stack when the timing
    /// condition is met.
    ///
    /// TriggerEffectData itself is never resolved by EffectResolver -
    /// only the payload at PayloadEffectIndex is placed on the pile. Rule 700.
    /// </summary>
    [Serializable]
    public class TriggerEffectData : EffectData
    {
        public override EffectType EffectType => EffectType.Trigger;

        // When this trigger fires.
        public TriggerTiming Timing;

        // Index into RuntimeCard.Effects that holds the payload effect.
        // e.g. if this trigger is at Effects[1], set PayloadEffectIndex = 2
        // to use Effects[2] as the thing placed on the pile.
        // -1 = stub trigger - fires but does nothing.
        public int PayloadEffectIndex = -1;

        // If true, this trigger only fires on the controller's own turn
        // ("Beginning of YOUR turn"). If false, fires on any player's turn.
        public bool OnlyOnOwnerTurn = true;
    }
}