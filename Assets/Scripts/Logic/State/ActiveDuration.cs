using System;

namespace SSR.Logic
{
    /// <summary>
    /// Tracks a temporary effect that needs to expire at a specific game moment.
    /// Used for "until end of turn" and "until your next turn" effects.
    /// </summary>
    [Serializable]
    public class ActiveDuration
    {
        public int EffectOwnerID;
        public int SourceCardID;
        public int AffectedCardID;
        public EffectType EffectType;
        public EffectDurationTiming Timing;
        public int AppliedOnRound;
        public int AppliedOnTurnOfPlayerID;

        public ActiveDuration(
            int effectOwnerID,
            int sourceCardID,
            int affectedCardID,
            EffectType effectType,
            EffectDurationTiming timing,
            int appliedOnRound,
            int appliedOnTurnOfPlayerID)
        {
            EffectOwnerID = effectOwnerID;
            SourceCardID = sourceCardID;
            AffectedCardID = affectedCardID;
            EffectType = effectType;
            Timing = timing;
            AppliedOnRound = appliedOnRound;
            AppliedOnTurnOfPlayerID = appliedOnTurnOfPlayerID;
        }
    }
}