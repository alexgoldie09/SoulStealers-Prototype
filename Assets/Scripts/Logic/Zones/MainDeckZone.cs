using System;

namespace SSR.Logic
{
    /// <summary>
    /// The Main Deck. Face-down, insertion-order preserved.
    /// Any player may count it but not examine it. Rule 402.
    /// </summary>
    [Serializable]
    public class MainDeckZone : ZoneBase
    {
        public MainDeckZone()
        {
            ZoneType = ZoneType.MainDeck;
            IsPublic = false;
        }

        /// <summary>
        /// Draws the top card (index 0). Returns -1 if the deck is empty.
        /// </summary>
        public int DrawTop()
        {
            if (_cardIDs.Count == 0) return -1;
            int top = _cardIDs[0];
            _cardIDs.RemoveAt(0);
            return top;
        }

        /// <summary>
        /// Returns true if the deck is empty.
        /// </summary>
        public bool IsEmpty => _cardIDs.Count == 0;

        /// <summary>
        /// Shuffles the deck.
        /// This should be called after adding cards to the deck, or when a card is returned to the deck.
        /// </summary>
        public void Shuffle()
        {
            _cardIDs.Shuffle();
        }
    }
}