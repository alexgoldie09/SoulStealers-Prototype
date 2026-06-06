using System.Collections.Generic;

namespace SSR.Logic
{
    /// <summary>
    /// Represents a zone in the game, such as a player's hand, deck, or discard pile.
    /// </summary>
    public interface IZone
    {
        ZoneType ZoneType { get; }
        IReadOnlyList<int> CardIDs { get; }
        void Add(int cardID);
        void Remove(int cardID);
        bool Contains(int cardID);
    }
}
