using System;

namespace SSR.Logic
{
    /// <summary>
    /// A meta-effect that wraps a triggered payload. TriggerSystem scans
    /// field cards for these, creates a PileObject containing TriggeredEffect,
    /// and places it on the Resolution Stack when the timing condition is met.
    ///
    /// TriggerEffectData itself is never resolved by EffectResolver -
    /// only the TriggeredEffect inside it is placed on the pile. Rule 700.
    /// </summary>
    [Serializable]
    public class TriggerEffectData : EffectData
    {
        public override EffectType EffectType => EffectType.Trigger;

        // When this trigger fires.
        public TriggerTiming Timing;

        // The effect that gets placed on the pile when the trigger condition is met.
        public EffectData TriggeredEffect;

        // If true, this trigger only fires on the controller's own turn
        // ("Beginning of YOUR turn"). If false, fires on any player's turn.
        public bool OnlyOnOwnerTurn = true;
    }
}