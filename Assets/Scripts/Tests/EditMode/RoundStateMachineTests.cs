using NUnit.Framework;
using SSR.Logic;
using System;
using UnityEngine;

namespace SSR.Tests
{
    /// <summary>
    /// Tests for the RoundStateMachine, which manages the flow of each round in the game.
    /// </summary>
    public class RoundStateMachineTests
    {
        private GameState _state;
        private RoundStateMachine _machine;

        [SetUp]
        public void SetUp()
        {
            IDFactory.ResetIDs();
            _state = new GameState();
            _state.Players[0] = new PlayerState { PlayerID = 1, Souls = 30 };
            _state.Players[1] = new PlayerState { PlayerID = 2, Souls = 30 };

            for (int i = 1; i <= 16; i++)
            {
                var spirit = new RuntimeCard(
                    $"Spirit_{i}", 1, CardType.Spirit,
                    CardSuperType.None, $"Spirit {i}",
                    spiritRank: i);
                _state.CardRegistry[spirit.ID] = spirit;
                _state.SpiritPool.AvailablePool.Add(spirit.ID);
            }

            _machine = new RoundStateMachine(_state, new System.Random(42));
            
            _machine.OnTurnStarted += _ => _machine.AdvanceToActionPhase();

            // Wire up simulation trace — fires automatically for every test.
            _machine.OnRoundStarted       += r   =>
                Debug.Log($"  [Round]  ── Round {r} started ── pool={_state.SpiritPool.PoolSize} available");
            _machine.OnPhaseEntered       += (phase, pid) =>
                Debug.Log($"  [Phase]  → {phase,-20} activePlayer={pid}");
            _machine.OnSpiritsRevealed    += s   =>
            {
                var order = _machine.TurnContext.TurnOrder;
                Debug.Log($"  [Reveal] Spirits revealed — turn order: [{string.Join(" → ", order)}]");
                foreach (var p in s.Players)
                {
                    if (p == null) continue;
                    var spirit = s.GetCard(p.SelectedSpiritID);
                    Debug.Log($"           Player {p.PlayerID} chose: {spirit?.CurrentName ?? "none"} (rank {spirit?.SpiritRank})");
                }
            };
            _machine.OnTurnStarted        += pid =>
                Debug.Log($"  [Turn]   Player {pid} turn START");
            _machine.OnActionPhaseStarted += pid =>
                Debug.Log($"  [Action] Player {pid} Action Phase  (actions={_state.GetPlayer(pid)?.ActionsRemaining})");
            _machine.OnTurnEnded          += pid =>
                Debug.Log($"  [Turn]   Player {pid} turn END");
            _machine.OnDrawPhaseStarted   += pid =>
                Debug.Log($"  [Draw]   Player {pid} Draw Phase");
            _machine.OnRoundEnded         += r   =>
                Debug.Log($"  [Round]  Round {r} ended  (pool available={_state.SpiritPool.PoolSize}, unavailable={_state.SpiritPool.UnavailablePool.Count})");
        }

        [Test]
        public void StartGame_SetsRoundNumberToOne()
        {
            Debug.Log("=== StartGame_SetsRoundNumberToOne ===");
            _machine.StartGame();
            Debug.Log($"  RoundNumber = {_state.RoundNumber}  |  Phase = {_state.CurrentPhase}");
            Assert.AreEqual(1, _state.RoundNumber);
        }

        [Test]
        public void StartGame_FiresOnRoundStarted()
        {
            Debug.Log("=== StartGame_FiresOnRoundStarted ===");
            int firedRound = -1;
            _machine.OnRoundStarted += r => firedRound = r;
            _machine.StartGame();
            Debug.Log($"  OnRoundStarted fired with round={firedRound}");
            Assert.AreEqual(1, firedRound);
        }

        [Test]
        public void StartGame_TransitionsToSpiritSelection()
        {
            Debug.Log("=== StartGame_TransitionsToSpiritSelection ===");
            _machine.StartGame();
            Debug.Log($"  Final phase = {_state.CurrentPhase}");
            Assert.AreEqual(GamePhase.SpiritSelection, _state.CurrentPhase);
        }

        [Test]
        public void StartGame_DealsSpiritsToPlayers()
        {
            Debug.Log("=== StartGame_DealsSpiritsToPlayers ===");
            _machine.StartGame();

            foreach (var player in _state.Players)
            {
                if (player == null) continue;
                var names = new System.Text.StringBuilder();
                foreach (int id in player.DealtSpiritIDs)
                    names.Append($"{_state.GetCard(id)?.CurrentName} ");
                Debug.Log($"  Player {player.PlayerID} dealt ({player.DealtSpiritIDs.Count}): {names}");
            }

            Assert.AreEqual(4, _state.Players[0].DealtSpiritIDs.Count);
            Assert.AreEqual(4, _state.Players[1].DealtSpiritIDs.Count);
        }

        [Test]
        public void SelectSpirit_BothSelected_AdvancesToActionPhase()
        {
            Debug.Log("=== SelectSpirit_BothSelected_AdvancesToActionPhase ===");
            _machine.StartGame();

            int p1Spirit = _state.Players[0].DealtSpiritIDs[0];
            int p2Spirit = _state.Players[1].DealtSpiritIDs[0];

            Debug.Log($"  Player 1 selecting: {_state.GetCard(p1Spirit)?.CurrentName}");
            bool r1 = _machine.SelectSpirit(1, p1Spirit);
            Debug.Log($"  → result={r1}  |  allSelected={SpiritPoolSystem.AllPlayersHaveSelected(_state)}");

            Debug.Log($"  Player 2 selecting: {_state.GetCard(p2Spirit)?.CurrentName}");
            bool r2 = _machine.SelectSpirit(2, p2Spirit);
            Debug.Log($"  → result={r2}  |  allSelected={SpiritPoolSystem.AllPlayersHaveSelected(_state)}");

            Debug.Log($"  Final phase = {_state.CurrentPhase}  |  activePlayer={_state.ActivePlayerID}");
            Assert.AreEqual(GamePhase.ActionPhase, _state.CurrentPhase);
        }

        [Test]
        public void SelectSpirit_InvalidID_ReturnsFalse()
        {
            Debug.Log("=== SelectSpirit_InvalidID_ReturnsFalse ===");
            _machine.StartGame();
            bool result = _machine.SelectSpirit(1, 99999);
            Debug.Log($"  SelectSpirit(1, 99999) = {result}  (expected: false — ID not in dealt cards)");
            Assert.IsFalse(result);
        }

        [Test]
        public void SelectSpirit_WrongPhase_ReturnsFalse()
        {
            Debug.Log("=== SelectSpirit_WrongPhase_ReturnsFalse ===");
            Debug.Log($"  Current phase before StartGame = {_state.CurrentPhase}");
            bool result = _machine.SelectSpirit(1, 1);
            Debug.Log($"  SelectSpirit before StartGame = {result}  (expected: false — not in SpiritSelection)");
            Assert.IsFalse(result);
        }

        [Test]
        public void Tick_TimerExpires_AutoAssignsAndAdvances()
        {
            Debug.Log("=== Tick_TimerExpires_AutoAssignsAndAdvances ===");
            _machine.SelectionTimeLimit = 1f;
            _machine.StartGame();
            Debug.Log($"  Timer limit set to 1s — ticking 2s...");
            _machine.Tick(2f);

            foreach (var player in _state.Players)
            {
                if (player == null) continue;
                var spirit = _state.GetCard(player.SelectedSpiritID);
                Debug.Log($"  Player {player.PlayerID} auto-assigned: {spirit?.CurrentName ?? "none"} (rank {spirit?.SpiritRank})");
            }
            Debug.Log($"  Final phase = {_state.CurrentPhase}");

            Assert.AreNotEqual(-1, _state.Players[0].SelectedSpiritID);
            Assert.AreNotEqual(-1, _state.Players[1].SelectedSpiritID);
            Assert.AreEqual(GamePhase.ActionPhase, _state.CurrentPhase);
        }

        [Test]
        public void EndTurn_AdvancesToNextPlayerTurn()
        {
            Debug.Log("=== EndTurn_AdvancesToNextPlayerTurn ===");
            _machine.StartGame();

            int p1Spirit = _state.Players[0].DealtSpiritIDs[0];
            int p2Spirit = _state.Players[1].DealtSpiritIDs[0];
            _machine.SelectSpirit(1, p1Spirit);
            _machine.SelectSpirit(2, p2Spirit);

            int firstPlayer = _state.ActivePlayerID;
            Debug.Log($"  First active player = {firstPlayer}  |  ending their turn...");
            _machine.EndTurn(firstPlayer);

            Debug.Log($"  New active player = {_state.ActivePlayerID}  |  Phase = {_state.CurrentPhase}");
            Assert.AreNotEqual(firstPlayer, _state.ActivePlayerID);
            Assert.AreEqual(GamePhase.ActionPhase, _state.CurrentPhase);
        }

        [Test]
        public void EndTurn_WrongPlayer_ReturnsFalse()
        {
            Debug.Log("=== EndTurn_WrongPlayer_ReturnsFalse ===");
            _machine.StartGame();

            int p1Spirit = _state.Players[0].DealtSpiritIDs[0];
            int p2Spirit = _state.Players[1].DealtSpiritIDs[0];
            _machine.SelectSpirit(1, p1Spirit);
            _machine.SelectSpirit(2, p2Spirit);

            int wrongPlayer = _state.ActivePlayerID == 1 ? 2 : 1;
            Debug.Log($"  Active player = {_state.ActivePlayerID}  |  calling EndTurn for wrong player {wrongPlayer}");
            bool result = _machine.EndTurn(wrongPlayer);
            Debug.Log($"  EndTurn(wrongPlayer) = {result}  (expected: false)");
            Assert.IsFalse(result);
        }

        [Test]
        public void TurnOrder_SortedByAscendingSpiritRank()
        {
            Debug.Log("=== TurnOrder_SortedByAscendingSpiritRank ===");
            _machine.StartGame();

            int p1Spirit = _state.Players[0].DealtSpiritIDs[3];
            int p2Spirit = _state.Players[1].DealtSpiritIDs[0];

            var p1Card = _state.GetCard(p1Spirit);
            var p2Card = _state.GetCard(p2Spirit);
            Debug.Log($"  Player 1 selects: {p1Card?.CurrentName} (rank {p1Card?.SpiritRank})");
            Debug.Log($"  Player 2 selects: {p2Card?.CurrentName} (rank {p2Card?.SpiritRank})");

            _machine.SelectSpirit(1, p1Spirit);
            _machine.SelectSpirit(2, p2Spirit);

            var order = _machine.TurnContext.TurnOrder;
            Debug.Log($"  Turn order: [{string.Join(" → ", order)}]");
            for (int i = 0; i < order.Length; i++)
            {
                var s = _state.GetCard(_state.GetPlayer(order[i]).SelectedSpiritID);
                Debug.Log($"    [{i}] Player {order[i]} — {s?.CurrentName} (rank {s?.SpiritRank})");
            }

            int firstPlayerID = order[0];
            var firstSpirit  = _state.GetCard(_state.GetPlayer(firstPlayerID).SelectedSpiritID);
            var secondSpirit = _state.GetCard(_state.GetPlayer(order[1]).SelectedSpiritID);
            Assert.LessOrEqual(firstSpirit.SpiritRank, secondSpirit.SpiritRank);
        }

        [Test]
        public void RoundNumber_IncrementsEachRound()
        {
            Debug.Log("=== RoundNumber_IncrementsEachRound ===");
            _machine.StartGame();
            Debug.Log($"  After StartGame: round={_state.RoundNumber}");
            Assert.AreEqual(1, _state.RoundNumber);

            Debug.Log("  Completing round 1 fast...");
            CompleteFastRound();
            Debug.Log($"  After completing round 1: round={_state.RoundNumber}");
            Assert.AreEqual(2, _state.RoundNumber);
        }

        private void CompleteFastRound()
        {
            if (_state.CurrentPhase == GamePhase.SpiritSelection)
                _machine.Tick(_machine.SelectionTimeLimit + 1f);

            while (_state.CurrentPhase == GamePhase.ActionPhase)
            {
                Debug.Log($"    EndTurn: Player {_state.ActivePlayerID}");
                _machine.EndTurn(_state.ActivePlayerID);
            }
        }
    }
}
