using System;
using System.Collections.Generic;

namespace SSR.Logic
{
    /// <summary>
    /// Represents the entire state of the game at a given moment, including player states, the resolution pile, and the spirit pool.
    /// </summary>
    [Serializable]
    public class GameState
    {
        public int RoundNumber;
        public GamePhase CurrentPhase;
        public int ActivePlayerID;
        public int PriorityPlayerID;

        public PlayerState[] Players = new PlayerState[2];
        // The shared Resolution Pile. Rule 405.
        public ResolutionPileZone ResolutionPile = new ResolutionPileZone();
        // The shared Side Game zone. Rule 406.
        public SideGameZone SideGame = new SideGameZone();
        // Spirit pool depletion state. Client-defined system.
        public SpiritPoolState SpiritPool = new SpiritPoolState();
        
        // All active duration-tracked effects across the game.
        // Expire at Beginning of Turn (UntilNextTurn) or End of Turn
        // (UntilEndOfTurn). Rule 502, 504.
        public List<ActiveDuration> ActiveDurations = new List<ActiveDuration>();

        // Registry of all RuntimeCards created this game, keyed by ID.
        // This is the single source of truth for card state during a game.
        public Dictionary<int, RuntimeCard> CardRegistry
            = new Dictionary<int, RuntimeCard>();
        
        // Registry of all PileObjects currently on the Resolution Pile, keyed by ID.
        // Effect objects are distinct from cards on the pile. Rule 405.3.
        public Dictionary<int, PileObject> PileObjectRegistry
            = new Dictionary<int, PileObject>();
    }

    /// <summary>
    /// Represents the state of an individual player, including their resources, zones, and any modifiers or status effects.
    /// </summary>
    [Serializable]
    public class PlayerState
    {
        public int PlayerID;
        public int Souls;
        public int ActionsRemaining;
        public bool IsEasyMode;

        // Player zones
        public MainDeckZone MainDeck = new MainDeckZone();
        public HandZone Hand = new HandZone();
        public DiscardPileZone DiscardPile = new DiscardPileZone();

        // Field subzones
        public SpiritZone SpiritZone = new SpiritZone();
        public IncantationZone IncantationZone = new IncantationZone();
        public SorceryZone SorceryZone = new SorceryZone();

        // Spirit cards dealt this round, awaiting selection
        public List<int> DealtSpiritIDs = new List<int>();
        public int SelectedSpiritID = -1;

        // Tracking flags
        public bool HasRecycledThisActionPhase;
        public int MaxHandSize = 5;
    }

    /// <summary>
    /// Represents the state of the spirit pool, including which spirits are available for dealing and
    /// which are currently unavailable, as well as the number of cycles that have occurred.
    /// </summary>
    [Serializable]
    public class SpiritPoolState
    {
        // 16 total spirits in a standard game. Rule 301.1.
        public List<int> AvailablePool = new List<int>();
        public List<int> UnavailablePool = new List<int>();
        public int CycleCount;

        public int PoolSize => AvailablePool.Count;

        /// <summary>
        /// Returns the number of spirits to deal to each player at the start of the round,
        /// based on the current pool size.
        /// </summary>
        /// <returns></returns>
        public int GetDealCount()
        {
            return PoolSize switch
            {
                >= 8 => 4,
                6 => 3,
                4 => 2,
                2 => 1,
                _ => 0
            };
        }
        
        /// <summary>
        /// Resets the pool when all 16 spirits have been used.
        /// All unavailable spirits become available again. Client rule.
        /// </summary>
        public void ResetCycle()
        {
            AvailablePool.AddRange(UnavailablePool);
            UnavailablePool.Clear();
            CycleCount++;
        }

    }
}