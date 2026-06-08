using System.Collections.Generic;

namespace SSR.Logic
{
    /// <summary>
    /// Represents a zone in the game, such as a player's hand, deck, or discard pile.
    /// </summary>
    public interface IZone
    {
        /// <summary>
        /// Gets the type of this zone, which determines its behavior and how it interacts with game rules.
        /// </summary>
        ZoneType ZoneType { get; }
        /// <summary>
        /// Gets a read-only list of card IDs currently in this zone.
        /// The order of card IDs in the list represents the order of cards in the zone (e.g., top to bottom for a deck).
        /// </summary>
        IReadOnlyList<int> CardIDs { get; }
        /// <summary>
        /// Adds a card to this zone by its ID.
        /// The specific behavior (e.g., adding to the top or bottom) may depend on the zone type and game rules.
        /// </summary>
        /// <param name="cardID"></param>
        void Add(int cardID);
        /// <summary>
        /// Removes a card from this zone by its ID.
        /// The specific behavior (e.g., removing from the top or bottom) may depend on the zone type and game rules.
        /// </summary>
        /// <param name="cardID"></param>
        void Remove(int cardID);
        /// <summary>
        /// Checks if a card with the given ID is currently in this zone.
        /// </summary>
        /// <param name="cardID"></param>
        /// <returns></returns>
        bool Contains(int cardID);
    }
}
