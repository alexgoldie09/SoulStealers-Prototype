using System;

namespace SSR.Logic
{
    /// <summary>
    /// Represents an activated or triggered effect placed on the Resolution Pile
    /// as an independent object, separate from its source card. Rule 405.3.
    /// A PileObject ceases to exist if Negated; it has no destination zone.
    /// </summary>
    [Serializable]
    public class PileObject
    {
        public int ID; // IDFactory.GetUniqueID()
        public int SourceCardID; // -1 if none
        public int ControllerID;
        public EffectData Effect;
    }
}