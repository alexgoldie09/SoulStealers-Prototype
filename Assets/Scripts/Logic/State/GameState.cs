using System;
using System.Collections.Generic;

namespace SSR.Logic
{
    [Serializable]
    public class GameState
    {
        public int RoundNumber;
        public GamePhase CurrentPhase;
        public int ActivePlayerID;
        public int PriorityPlayerID;

        public PlayerState[] Players = new PlayerState[2];
        public List<int> ResolutionPile = new List<int>();
        public SpiritPoolState SpiritPool = new SpiritPoolState();
    }

    [Serializable]
    public class PlayerState
    {
        public int PlayerID;
        public int OwnerID;
        public int ControllerID;
        public int Souls;
        public int ActionsRemaining;
        public int Defense;
        public int BanishModifier;
        public int StealModifier;
        public bool IsEasyMode;

        public List<int> Deck = new List<int>();
        public List<int> Hand = new List<int>();
        public List<int> DiscardPile = new List<int>();
        public int SpiritZoneCardID = -1;
        public int[] IncantationZone = new int[3] { -1, -1, -1 };
        public int[] SorceryZone = new int[3] { -1, -1, -1 };
        public List<int> SideGameZone = new List<int>();
    }

    [Serializable]
    public class SpiritPoolState
    {
        public List<int> AvailablePool = new List<int>();
        public List<int> UnavailablePool = new List<int>();
        public int CycleCount;

        public int PoolSize => AvailablePool.Count;

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
    }
}