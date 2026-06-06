namespace SSR.Logic
{
    /// <summary>
    /// Holds objects outside the main game flow:
    /// the Spirit Deck awaiting distribution, Spirit Discard Pile,
    /// Namara's Word and Namara's Scream copies, and the Side Deck.
    /// Accessible and examinable by any player at any time. Rule 406.
    /// </summary>
    public class SideGameZone : ZoneBase
    {
        // Spirit Deck IDs — shuffled each round. Rule 501.
        public MainDeckZone SpiritDeck = new MainDeckZone();

        // Spirit Discard Pile — selected spirits go here at end of round.
        // When the pool cannot distribute at least one per player,
        // this is shuffled back into SpiritDeck. Rule 506.
        public DiscardPileZone SpiritDiscardPile = new DiscardPileZone();

        // Namara's Word and Namara's Scream card IDs. Rule 101.4.
        // Between 3–6 copies of each placed here at game setup.
        public System.Collections.Generic.List<int> NamaraWordIDs
            = new System.Collections.Generic.List<int>();
        public System.Collections.Generic.List<int> NamaraScreamIDs
            = new System.Collections.Generic.List<int>();

        public SideGameZone()
        {
            ZoneType = ZoneType.SideGame;
            IsPublic = true;
        }
    }
}