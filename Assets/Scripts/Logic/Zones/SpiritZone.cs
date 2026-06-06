using System;

namespace SSR.Logic
{
    /// <summary>
    /// Holds the chosen Spirit card for the current round.
    /// Exactly one Spirit at a time. Rule 404 Field Layout.
    /// Spirit cards cannot be moved to non-Spirit zones by effects. Rule 301.3.
    /// </summary>
    [Serializable]
    public class SpiritZone : ZoneBase
    {
        private const int Capacity = 1;

        public SpiritZone()
        {
            ZoneType = ZoneType.SpiritZone;
            IsPublic = true;
        }

        /// <summary>
        /// Returns true if the card can be added to the zone.
        /// For SpiritZone, this is true if it currently holds no Spirit.
        /// </summary>
        /// <param name="cardID"></param>
        /// <returns></returns>
        public override bool CanAdd(int cardID) => _cardIDs.Count < Capacity;
        protected override string GetAddFailReason() => "Spirit Zone already holds a Spirit";

        /// <summary>
        /// Returns true if the zone currently holds a Spirit card.
        /// </summary>
        public bool HasSpirit => _cardIDs.Count > 0;
        
        /// <summary>
        /// Returns the ID of the Spirit card currently in the zone, or -1 if empty.
        /// </summary>
        public int SpiritID => _cardIDs.Count > 0 ? _cardIDs[0] : -1;
    }
}