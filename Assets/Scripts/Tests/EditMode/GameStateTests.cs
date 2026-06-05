using NUnit.Framework;
using SSR.Logic;
using System.Collections.Generic;

namespace SSR.Tests
{
    public class GameStateTests
    {
        [Test]
        public void GameState_Holds_Player_Values_Correctly()
        {
            var state = new GameState();
            state.RoundNumber = 1;
            state.CurrentPhase = GamePhase.ActionPhase;
            state.Players[0] = new PlayerState { PlayerID = 1, Souls = 30 };
            state.Players[1] = new PlayerState { PlayerID = 2, Souls = 30 };

            Assert.AreEqual(1, state.RoundNumber);
            Assert.AreEqual(GamePhase.ActionPhase, state.CurrentPhase);
            Assert.AreEqual(30, state.Players[0].Souls);
            Assert.AreEqual(30, state.Players[1].Souls);
        }

        [Test]
        public void SpiritPool_DealCount_Returns_Correct_Values()
        {
            var pool = new SpiritPoolState();

            for (int i = 0; i < 8; i++) pool.AvailablePool.Add(i);
            Assert.AreEqual(4, pool.GetDealCount());

            pool.AvailablePool.Clear();
            for (int i = 0; i < 6; i++) pool.AvailablePool.Add(i);
            Assert.AreEqual(3, pool.GetDealCount());

            pool.AvailablePool.Clear();
            for (int i = 0; i < 4; i++) pool.AvailablePool.Add(i);
            Assert.AreEqual(2, pool.GetDealCount());

            pool.AvailablePool.Clear();
            pool.AvailablePool.Add(0);
            pool.AvailablePool.Add(1);
            Assert.AreEqual(1, pool.GetDealCount());
        }

        [Test]
        public void IDFactory_Returns_Unique_Sequential_IDs()
        {
            IDFactory.ResetIDs();
            Assert.AreEqual(1, IDFactory.GetUniqueID());
            Assert.AreEqual(2, IDFactory.GetUniqueID());
            Assert.AreEqual(3, IDFactory.GetUniqueID());
        }

        [Test]
        public void PlayerState_Zone_Slots_Initialise_Empty()
        {
            var player = new PlayerState();

            Assert.AreEqual(3, player.IncantationZone.Length);
            Assert.AreEqual(3, player.SorceryZone.Length);
            Assert.AreEqual(-1, player.SpiritZoneCardID);

            foreach (var slot in player.IncantationZone)
                Assert.AreEqual(-1, slot);

            foreach (var slot in player.SorceryZone)
                Assert.AreEqual(-1, slot);
        }
    }
}