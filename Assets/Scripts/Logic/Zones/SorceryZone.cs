using System;

namespace SSR.Logic
{
    /// <summary>
    /// Holds up to three face-down Sorceries (Secrets or Spells played face-down).
    /// A player cannot Secret Play if this zone is already at capacity. Rule 601.4.
    /// The identity of face-down cards is hidden; their count is public. Rule 400.2.
    /// </summary>
    [Serializable]
    public class SorceryZone : ZoneBase
    {
        public const int Capacity = 3;

        public SorceryZone()
        {
            ZoneType = ZoneType.SorceryZone;
            IsPublic = true;
        }

        /// <summary>
        /// Returns true if the card can be added to the zone.
        /// For SorceryZone, this is true if it currently holds fewer than 3 Sorceries.
        /// </summary>
        /// <param name="cardID"></param>
        /// <returns></returns>
        public override bool CanAdd(int cardID) => _cardIDs.Count < Capacity;
        protected override string GetAddFailReason() =>
            $"Sorcery Zone is full ({Capacity} slots)";

        /// <summary>
        /// Returns true if the zone currently holds 3 Sorceries.
        /// </summary>
        public bool IsFull => _cardIDs.Count >= Capacity;
        
        /// <summary>
        /// Returns the number of available slots in the Sorcery Zone (0-3).
        /// </summary>
        public int Available => Capacity - _cardIDs.Count;
    }
}