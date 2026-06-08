using NUnit.Framework;
using SSR.Logic;
using System;
using UnityEngine;
using Random = System.Random;

namespace SSR.Tests
{
    public class SpiritPoolSystemTests
    {
        private GameState _state;
        private Random _rng;
        private Action<int, int> _onSpiritSelected;
        private Action<int, int> _onSpiritAutoAssigned;
        private Action _onPoolCycleReset;

        [SetUp]
        public void SetUp()
        {
            IDFactory.ResetIDs();
            _state = new GameState();
            _state.Players[0] = new PlayerState { PlayerID = 1 };
            _state.Players[1] = new PlayerState { PlayerID = 2 };

            for (int i = 1; i <= 16; i++)
            {
                var spirit = new RuntimeCard(
                    $"Spirit_{i}", 1, CardType.Spirit,
                    CardSuperType.None, $"Spirit {i}",
                    spiritRank: i);
                _state.CardRegistry[spirit.ID] = spirit;
                _state.SpiritPool.AvailablePool.Add(spirit.ID);
            }

            _rng = new Random(42);
            
            _onSpiritSelected = (pid, sid) =>
                Debug.Log($"  [Select]  Player {pid} selected: {_state.GetCard(sid)?.CurrentName} (rank {_state.GetCard(sid)?.SpiritRank})");
            _onSpiritAutoAssigned = (pid, sid) =>
                Debug.Log($"  [Auto]    Player {pid} auto-assigned: {_state.GetCard(sid)?.CurrentName} (rank {_state.GetCard(sid)?.SpiritRank})");
            _onPoolCycleReset = () =>
                Debug.Log($"  [Cycle]   Pool reset! CycleCount={_state.SpiritPool.CycleCount}  available={_state.SpiritPool.PoolSize}");

            SpiritPoolSystem.OnSpiritSelected += _onSpiritSelected;
            SpiritPoolSystem.OnSpiritAutoAssigned += _onSpiritAutoAssigned;
            SpiritPoolSystem.OnPoolCycleReset += _onPoolCycleReset;
        }

        [TearDown]
        public void TearDown()
        {
            // Clear static event subscriptions added in SetUp to prevent accumulation.
            // in TearDown(), replace = null lines
            SpiritPoolSystem.OnSpiritSelected -= _onSpiritSelected;
            SpiritPoolSystem.OnSpiritAutoAssigned -= _onSpiritAutoAssigned;
            SpiritPoolSystem.OnPoolCycleReset -= _onPoolCycleReset;
        }

        private void LogPoolState(string label = "")
        {
            Debug.Log($"  [Pool{(label.Length > 0 ? $" {label}" : "")}] " +
                      $"available={_state.SpiritPool.AvailablePool.Count}  " +
                      $"unavailable={_state.SpiritPool.UnavailablePool.Count}  " +
                      $"dealCount={_state.SpiritPool.GetDealCount()}");
        }

        private void LogDealtSpirits()
        {
            foreach (var player in _state.Players)
            {
                if (player == null) continue;
                var names = new System.Text.StringBuilder();
                foreach (int id in player.DealtSpiritIDs)
                    names.Append($"{_state.GetCard(id)?.CurrentName}(r{_state.GetCard(id)?.SpiritRank}) ");
                Debug.Log($"  [Dealt]   Player {player.PlayerID}: {names}");
            }
        }

        [Test]
        public void DealSpirits_PopulatesDealtSpiritIDs()
        {
            Debug.Log("=== DealSpirits_PopulatesDealtSpiritIDs ===");
            LogPoolState("before deal");
            SpiritPoolSystem.DealSpirits(_state, _rng);
            LogDealtSpirits();
            LogPoolState("after deal");
            Assert.AreEqual(4, _state.Players[0].DealtSpiritIDs.Count);
            Assert.AreEqual(4, _state.Players[1].DealtSpiritIDs.Count);
        }

        [Test]
        public void DealSpirits_ReducesAvailablePool()
        {
            Debug.Log("=== DealSpirits_ReducesAvailablePool ===");
            LogPoolState("before deal");
            SpiritPoolSystem.DealSpirits(_state, _rng);
            LogPoolState("after deal (8 dealt, 8 remain)");
            Assert.AreEqual(8, _state.SpiritPool.AvailablePool.Count);
        }

        [Test]
        public void DealSpirits_NoOverlap_BetweenPlayers()
        {
            Debug.Log("=== DealSpirits_NoOverlap_BetweenPlayers ===");
            SpiritPoolSystem.DealSpirits(_state, _rng);
            LogDealtSpirits();
            Debug.Log("  Checking no ID appears in both players' dealt lists...");
            foreach (int id in _state.Players[0].DealtSpiritIDs)
            {
                bool overlap = _state.Players[1].DealtSpiritIDs.Contains(id);
                Debug.Log($"  ID {id} ({_state.GetCard(id)?.CurrentName}) in P2 dealt? {overlap}");
                Assert.IsFalse(overlap);
            }
        }

        [Test]
        public void TrySelectSpirit_ValidSelection_ReturnsTrue()
        {
            Debug.Log("=== TrySelectSpirit_ValidSelection_ReturnsTrue ===");
            SpiritPoolSystem.DealSpirits(_state, _rng);
            LogDealtSpirits();
            int spiritID = _state.Players[0].DealtSpiritIDs[0];
            Debug.Log($"  Player 1 attempting to select: {_state.GetCard(spiritID)?.CurrentName}");
            bool result = SpiritPoolSystem.TrySelectSpirit(_state, 1, spiritID);
            Debug.Log($"  TrySelectSpirit result={result}  |  SelectedSpiritID={_state.Players[0].SelectedSpiritID}");
            Assert.IsTrue(result);
            Assert.AreEqual(spiritID, _state.Players[0].SelectedSpiritID);
        }

        [Test]
        public void TrySelectSpirit_SpiritNotDealt_ReturnsFalse()
        {
            Debug.Log("=== TrySelectSpirit_SpiritNotDealt_ReturnsFalse ===");
            SpiritPoolSystem.DealSpirits(_state, _rng);
            Debug.Log("  Player 1 attempting to select ID 99999 (not dealt)...");
            bool result = SpiritPoolSystem.TrySelectSpirit(_state, 1, 99999);
            Debug.Log($"  TrySelectSpirit(99999) = {result}  (expected: false)");
            Assert.IsFalse(result);
        }

        [Test]
        public void TrySelectSpirit_AlreadySelected_ReturnsFalse()
        {
            Debug.Log("=== TrySelectSpirit_AlreadySelected_ReturnsFalse ===");
            SpiritPoolSystem.DealSpirits(_state, _rng);
            int spiritID = _state.Players[0].DealtSpiritIDs[0];
            Debug.Log($"  First selection: {_state.GetCard(spiritID)?.CurrentName}");
            SpiritPoolSystem.TrySelectSpirit(_state, 1, spiritID);
            Debug.Log("  Attempting to select same spirit again...");
            bool result = SpiritPoolSystem.TrySelectSpirit(_state, 1, spiritID);
            Debug.Log($"  Second TrySelectSpirit = {result}  (expected: false — already selected)");
            Assert.IsFalse(result);
        }

        [Test]
        public void AutoAssignSpirit_AssignsFromDealtCards()
        {
            Debug.Log("=== AutoAssignSpirit_AssignsFromDealtCards ===");
            SpiritPoolSystem.DealSpirits(_state, _rng);
            LogDealtSpirits();
            Debug.Log("  Auto-assigning spirit for Player 1...");
            SpiritPoolSystem.AutoAssignSpirit(_state, 1, _rng);
            int assigned = _state.Players[0].SelectedSpiritID;
            bool wasInDealtList = _state.Players[0].DealtSpiritIDs.Contains(assigned);
            Debug.Log($"  Assigned: {_state.GetCard(assigned)?.CurrentName} (rank {_state.GetCard(assigned)?.SpiritRank})  |  wasInDealtList={wasInDealtList}");
            Assert.AreNotEqual(-1, assigned);
            Assert.IsTrue(wasInDealtList);
        }

        [Test]
        public void AllPlayersHaveSelected_ReturnsFalse_WhenOneHasnt()
        {
            Debug.Log("=== AllPlayersHaveSelected_ReturnsFalse_WhenOneHasnt ===");
            SpiritPoolSystem.DealSpirits(_state, _rng);
            SpiritPoolSystem.TrySelectSpirit(_state, 1, _state.Players[0].DealtSpiritIDs[0]);
            bool result = SpiritPoolSystem.AllPlayersHaveSelected(_state);
            Debug.Log($"  P1 selected={_state.Players[0].SelectedSpiritID != -1}  P2 selected={_state.Players[1].SelectedSpiritID != -1}");
            Debug.Log($"  AllPlayersHaveSelected = {result}  (expected: false)");
            Assert.IsFalse(result);
        }

        [Test]
        public void AllPlayersHaveSelected_ReturnsTrue_WhenBothHave()
        {
            Debug.Log("=== AllPlayersHaveSelected_ReturnsTrue_WhenBothHave ===");
            SpiritPoolSystem.DealSpirits(_state, _rng);
            SpiritPoolSystem.TrySelectSpirit(_state, 1, _state.Players[0].DealtSpiritIDs[0]);
            SpiritPoolSystem.TrySelectSpirit(_state, 2, _state.Players[1].DealtSpiritIDs[0]);
            bool result = SpiritPoolSystem.AllPlayersHaveSelected(_state);
            Debug.Log($"  P1 selected={_state.Players[0].SelectedSpiritID != -1}  P2 selected={_state.Players[1].SelectedSpiritID != -1}");
            Debug.Log($"  AllPlayersHaveSelected = {result}  (expected: true)");
            Assert.IsTrue(result);
        }

        [Test]
        public void ReturnUnselectedSpirits_ReturnsThreePerPlayer()
        {
            Debug.Log("=== ReturnUnselectedSpirits_ReturnsThreePerPlayer ===");
            SpiritPoolSystem.DealSpirits(_state, _rng);
            LogDealtSpirits();
            SpiritPoolSystem.TrySelectSpirit(_state, 1, _state.Players[0].DealtSpiritIDs[0]);
            SpiritPoolSystem.TrySelectSpirit(_state, 2, _state.Players[1].DealtSpiritIDs[0]);

            int poolBefore = _state.SpiritPool.AvailablePool.Count;
            Debug.Log($"  Pool before return = {poolBefore}  (3 per player will be returned → +6)");
            SpiritPoolSystem.ReturnUnselectedSpirits(_state);
            LogPoolState("after return");
            Debug.Log($"  P1 DealtSpiritIDs.Count = {_state.Players[0].DealtSpiritIDs.Count}  (expected: 0 — cleared)");
            Debug.Log($"  P2 DealtSpiritIDs.Count = {_state.Players[1].DealtSpiritIDs.Count}  (expected: 0 — cleared)");

            Assert.AreEqual(poolBefore + 6, _state.SpiritPool.AvailablePool.Count);
            Assert.AreEqual(0, _state.Players[0].DealtSpiritIDs.Count);
            Assert.AreEqual(0, _state.Players[1].DealtSpiritIDs.Count);
        }

        [Test]
        public void EndRoundCleanup_MovesSelectedToUnavailable()
        {
            Debug.Log("=== EndRoundCleanup_MovesSelectedToUnavailable ===");
            SpiritPoolSystem.DealSpirits(_state, _rng);
            SpiritPoolSystem.TrySelectSpirit(_state, 1, _state.Players[0].DealtSpiritIDs[0]);
            SpiritPoolSystem.TrySelectSpirit(_state, 2, _state.Players[1].DealtSpiritIDs[0]);
            SpiritPoolSystem.ReturnUnselectedSpirits(_state);

            Debug.Log($"  Running EndRoundCleanup...");
            SpiritPoolSystem.EndRoundCleanup(_state);
            LogPoolState("after cleanup");
            Debug.Log($"  P1.SelectedSpiritID = {_state.Players[0].SelectedSpiritID}  (expected: -1)");
            Debug.Log($"  P2.SelectedSpiritID = {_state.Players[1].SelectedSpiritID}  (expected: -1)");

            Assert.AreEqual(2, _state.SpiritPool.UnavailablePool.Count);
            Assert.AreEqual(-1, _state.Players[0].SelectedSpiritID);
            Assert.AreEqual(-1, _state.Players[1].SelectedSpiritID);
        }

        [Test]
        public void EndRoundCleanup_TriggersResetWhenPoolExhausted()
        {
            Debug.Log("=== EndRoundCleanup_TriggersResetWhenPoolExhausted ===");
            _state.SpiritPool.UnavailablePool.AddRange(
                _state.SpiritPool.AvailablePool.GetRange(0, 14));
            _state.SpiritPool.AvailablePool.RemoveRange(0, 14);
            LogPoolState("after draining to 2");

            SpiritPoolSystem.DealSpirits(_state, _rng);
            LogDealtSpirits();
            SpiritPoolSystem.TrySelectSpirit(_state, 1, _state.Players[0].DealtSpiritIDs[0]);
            SpiritPoolSystem.TrySelectSpirit(_state, 2, _state.Players[1].DealtSpiritIDs[0]);
            SpiritPoolSystem.ReturnUnselectedSpirits(_state);

            Debug.Log("  Running EndRoundCleanup — pool will reach 0 → cycle reset...");
            SpiritPoolSystem.EndRoundCleanup(_state);
            LogPoolState("after reset");
            Debug.Log($"  CycleCount = {_state.SpiritPool.CycleCount}  (expected: 1)");

            Assert.AreEqual(1, _state.SpiritPool.CycleCount);
            Assert.AreEqual(16, _state.SpiritPool.AvailablePool.Count);
        }

        [Test]
        public void GetTurnOrder_SortsByAscendingSpiritRank()
        {
            Debug.Log("=== GetTurnOrder_SortsByAscendingSpiritRank ===");
            SpiritPoolSystem.DealSpirits(_state, _rng);
            LogDealtSpirits();
            SpiritPoolSystem.TrySelectSpirit(_state, 1, _state.Players[0].DealtSpiritIDs[0]);
            SpiritPoolSystem.TrySelectSpirit(_state, 2, _state.Players[1].DealtSpiritIDs[0]);

            int[] order = SpiritPoolSystem.GetTurnOrder(_state);
            Debug.Log($"  Turn order: [{string.Join(" → ", order)}]");
            for (int i = 0; i < order.Length; i++)
            {
                var s = _state.GetCard(_state.GetPlayer(order[i]).SelectedSpiritID);
                Debug.Log($"    [{i}] Player {order[i]} — {s?.CurrentName} (rank {s?.SpiritRank})");
            }

            Assert.AreEqual(2, order.Length);
            var first  = _state.GetCard(_state.GetPlayer(order[0]).SelectedSpiritID);
            var second = _state.GetCard(_state.GetPlayer(order[1]).SelectedSpiritID);
            Assert.LessOrEqual(first.SpiritRank, second.SpiritRank);
        }
    }
}
