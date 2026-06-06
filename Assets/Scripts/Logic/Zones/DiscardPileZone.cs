using System.Collections.Generic;

namespace SSR.Logic
{
    /// <summary>
    /// A player's Discard Pile. Face-up, insertion-order preserved.
    /// Index 0 = top of pile. Multiple simultaneous insertions require
    /// the owner to choose order — handled externally via AddMultiple.
    /// Rule 404 (Discard Pile section).
    /// </summary>
    public class DiscardPileZone : ZoneBase
    {
        public DiscardPileZone()
        {
            ZoneType = ZoneType.DiscardPile;
            IsPublic = true;
        }

        /// <summary>
        /// Puts a card on TOP of the Discard Pile. Rule 302.2, 404.
        /// Sorceries go to the top after resolving.
        /// </summary>
        public void AddToTop(int cardID)
        {
            _cardIDs.Insert(0, cardID);
        }

        /// <summary>
        /// Default Add puts the card on top of the pile.
        /// </summary>
        public override void Add(int cardID) => AddToTop(cardID);

        /// <summary>
        /// Adds multiple cards simultaneously in the owner's chosen order.
        /// The first element in the list will be placed on top. Rule 404.
        /// </summary>
        public void AddMultiple(IEnumerable<int> cardIDsInChosenOrder)
        {
            foreach (int id in cardIDsInChosenOrder)
                _cardIDs.Insert(0, id);
        }

        /// <summary>Returns the top card ID without removing it. -1 if empty.</summary>
        public int PeekTop() => _cardIDs.Count > 0 ? _cardIDs[0] : -1;

        /// <summary>Returns the card at a specific depth from the top (0 = top).</summary>
        public int PeekAt(int depth) =>
            depth < _cardIDs.Count ? _cardIDs[depth] : -1;

        /// <summary>
        /// Removes and returns the top card. Used for the empty deck
        /// shuffle procedure. Rule 604.7.
        /// </summary>
        public int TakeFromTop()
        {
            if (_cardIDs.Count == 0) return -1;
            int top = _cardIDs[0];
            _cardIDs.RemoveAt(0);
            return top;
        }

        public bool IsEmpty => _cardIDs.Count == 0;
    }
}
