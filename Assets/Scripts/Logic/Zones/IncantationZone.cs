using System;

namespace SSR.Logic
{
    /// <summary>
    /// Holds up to three Incantations (Rituals, Curses, Prayers).
    /// Always face-up. An Incantation cannot be Played if the zone is
    /// already at capacity — checked BEFORE play (rule 601.3) AND at
    /// resolution time (rule 404).
    /// Merged cards do not occupy an additional slot (rule 805).
    /// </summary>
    [Serializable]
    public class IncantationZone : ZoneBase
    {
        public const int Capacity = 3;

        public IncantationZone()
        {
            ZoneType = ZoneType.IncantationZone;
            IsPublic = true;
        }

        /// <summary>
        /// Returns true if the card can be added to the zone.
        /// For IncantationZone, this is true if it currently holds fewer than 3 Incantations.
        /// </summary>
        /// <param name="cardID"></param>
        /// <returns></returns>
        public override bool CanAdd(int cardID) => _cardIDs.Count < Capacity;
        protected override string GetAddFailReason() =>
            $"Incantation Zone is full ({Capacity} slots)";

        /// <summary>
        /// Returns true if the zone currently holds 3 Incantations.
        /// </summary>
        public bool IsFull => _cardIDs.Count >= Capacity;
        
        /// <summary>
        /// Returns the number of available slots in the Incantation Zone (0-3).
        /// </summary>
        public int Available => Capacity - _cardIDs.Count;
    }
}