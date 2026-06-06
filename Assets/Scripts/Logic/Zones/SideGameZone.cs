using System;
using System.Collections.Generic;

namespace SSR.Logic
{
    /// <summary>
    /// Holds objects outside the main game flow:
    /// the Spirit Deck awaiting distribution, Spirit Discard Pile,
    /// Namara's Word and Namara's Scream copies, and the Side Deck.
    /// Accessible and examinable by any player at any time. Rule 406.
    /// </summary>
    [Serializable]
    public class SideGameZone
    {
        public MainDeckZone SpiritDeck = new MainDeckZone();
        public DiscardPileZone SpiritDiscardPile = new DiscardPileZone();
        public List<int> NamaraWordIDs = new List<int>();
        public List<int> NamaraScreamIDs = new List<int>();
    }
}