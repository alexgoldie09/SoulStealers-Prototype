using NUnit.Framework;
using SSR.Logic;
using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace SSR.Tests
{
    /// <summary>
    /// EditMode tests for TriggerSystem — Beginning of Turn trigger evaluation,
    /// priority gating, phase advancement, and Dispose behaviour.
    /// Output: Assets/Debug/TriggerSystemTests.txt
    /// </summary>
    public class TriggerSystemTests
    {
        private static readonly string LogPath = Path.Combine(
            Application.dataPath, "Debug", "TriggerSystemTests.txt");

        private GameState _state;
        private RoundStateMachine _rsm;
        private ResolutionStack _stack;
        private TriggerSystem _triggerSystem;
        private StringBuilder _log;

        // Stored static delegates for clean unsubscription.
        private Action<int, int, int> _onSoulsChanged;

        private const int P1 = 1;
        private const int P2 = 2;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogPath));
            File.WriteAllText(LogPath,
                $"=== TriggerSystemTests Run: {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===" +
                Environment.NewLine + Environment.NewLine);
        }

        [SetUp]
        public void SetUp()
        {
            IDFactory.ResetIDs();
            _state = new GameState();
            _state.Players[0] = new PlayerState { PlayerID = P1, Souls = 30 };
            _state.Players[1] = new PlayerState { PlayerID = P2, Souls = 30 };
            _state.ActivePlayerID = P1;
            _state.CurrentPhase = GamePhase.StartOfTurn;

            _rsm = new RoundStateMachine(_state);
            _stack = new ResolutionStack(_state);
            _triggerSystem = new TriggerSystem(_state, _rsm, _stack);

            _log = new StringBuilder();
            _log.AppendLine($"--- {TestContext.CurrentContext.Test.Name} ---");
        }

        [TearDown]
        public void TearDown()
        {
            _triggerSystem.Dispose();
            _stack.Dispose();
            _rsm.Dispose();

            EffectResolver.OnSoulsChanged -= _onSoulsChanged;
            _onSoulsChanged = null;

            _log.AppendLine();
            File.AppendAllText(LogPath, _log.ToString());
        }

        private void Log(string msg) { Debug.Log(msg); _log.AppendLine(msg); }

        // ── Helpers ───────────────────────────────────────────────

        /// <summary>
        /// Creates a spirit card and places it in the given player's SpiritZone.
        /// </summary>
        private RuntimeCard MakeSpiritInZone(string name, int ownerID, int rank)
        {
            var card = new RuntimeCard(name, ownerID, CardType.Spirit,
                CardSuperType.None, name, spiritRank: rank);
            _state.CardRegistry[card.ID] = card;
            card.Location = CardLocation.SpiritZone;
            _state.GetPlayer(ownerID).SpiritZone.Add(card.ID);
            return card;
        }

        /// <summary>
        /// Builds a Molok-style GiveSouls trigger (rank 11) on the given spirit card.
        /// "Beginning of your turn: an opponent gives you 2 souls."
        /// </summary>
        private void AddMolokTrigger(RuntimeCard spirit, int ownerID, int targetID)
        {
            var payload = new GiveSoulsEffectData
            {
                BaseValue = 2,
                ValueType = NumericValueType.Symbolic,
                IsImposed = false,
                ControllerID = ownerID,
                SourceCardID = spirit.ID
            };
            payload.TargetIDs.Add(targetID);

            var trigger = new TriggerEffectData
            {
                Timing = TriggerTiming.BeginningOfTurn,
                TriggeredEffect = payload,
                OnlyOnOwnerTurn = true,
                ControllerID = ownerID,
                SourceCardID = spirit.ID
            };
            spirit.Effects.Add(trigger);
        }

        /// <summary>
        /// Passes priority for both players so the top pile item resolves.
        /// </summary>
        private void BothPass()
        {
            if (_stack.IsWaitingForPriority)
                _stack.PassPriority(_stack.PriorityPlayerID);
            if (_stack.IsWaitingForPriority)
                _stack.PassPriority(_stack.PriorityPlayerID);
        }

        // ── Tests ─────────────────────────────────────────────────

        [Test]
        public void BeginningOfTurn_NoTriggers_AdvancesToActionPhase()
        {
            // Spirit with no effects.
            MakeSpiritInZone("spirit-blank", P1, rank: 3);

            _triggerSystem.EvaluateBeginningOfTurnTriggers(P1);

            Log($"Phase after no triggers={_state.CurrentPhase}");
            Assert.AreEqual(GamePhase.ActionPhase, _state.CurrentPhase);
        }

        [Test]
        public void BeginningOfTurn_WithTrigger_PileNotEmptyBeforeResolution()
        {
            var spirit = MakeSpiritInZone("molok", P1, rank: 11);
            AddMolokTrigger(spirit, P1, P2);

            _triggerSystem.EvaluateBeginningOfTurnTriggers(P1);

            Log($"PileEmpty={_state.ResolutionPile.IsEmpty}");
            Assert.IsFalse(_state.ResolutionPile.IsEmpty);
        }

        [Test]
        public void BeginningOfTurn_TriggerResolves_SoulsTransferred()
        {
            var spirit = MakeSpiritInZone("molok", P1, rank: 11);
            AddMolokTrigger(spirit, P1, P2);

            _onSoulsChanged = (pid, oldV, newV) =>
                Log($"Souls changed: Player {pid}: {oldV} → {newV}");
            EffectResolver.OnSoulsChanged += _onSoulsChanged;

            _triggerSystem.EvaluateBeginningOfTurnTriggers(P1);
            BothPass();

            int p1Souls = _state.GetPlayer(P1).Souls;
            int p2Souls = _state.GetPlayer(P2).Souls;
            Log($"P1 souls={p1Souls}, P2 souls={p2Souls}");

            // Molok: P2 gives 2 souls to P1. P2 loses 2, P1 gains 2.
            Assert.AreEqual(32, p1Souls);
            Assert.AreEqual(28, p2Souls);
        }

        [Test]
        public void BeginningOfTurn_TriggerResolves_AdvancesToActionPhase()
        {
            var spirit = MakeSpiritInZone("molok", P1, rank: 11);
            AddMolokTrigger(spirit, P1, P2);

            _triggerSystem.EvaluateBeginningOfTurnTriggers(P1);
            BothPass();

            Log($"Phase after trigger drained={_state.CurrentPhase}");
            Assert.AreEqual(GamePhase.ActionPhase, _state.CurrentPhase);
        }

        [Test]
        public void BeginningOfTurn_OpponentTrigger_DoesNotFireOnActivePlayerTurn()
        {
            // P2's spirit has a BoT trigger that only fires on P2's own turn.
            var spirit = MakeSpiritInZone("p2-spirit", P2, rank: 5);
            AddMolokTrigger(spirit, P2, P1); // ControllerID = P2

            // P1's turn starts — P2's trigger should NOT fire.
            _triggerSystem.EvaluateBeginningOfTurnTriggers(P1);

            Log($"Pile empty (P2 trigger should not fire on P1's turn)={_state.ResolutionPile.IsEmpty}");
            Log($"Phase={_state.CurrentPhase}");

            // No triggers placed → pile stays empty → advances to ActionPhase directly.
            Assert.IsTrue(_state.ResolutionPile.IsEmpty);
            Assert.AreEqual(GamePhase.ActionPhase, _state.CurrentPhase);
        }

        [Test]
        public void BeginningOfTurn_MultipleSpirits_NotPossible()
        {
            // SpiritZone capacity is 1 — adding a second spirit should throw.
            var s1 = MakeSpiritInZone("spirit-one", P1, rank: 3);

            var s2 = new RuntimeCard("spirit-two", P1, CardType.Spirit,
                CardSuperType.None, "spirit-two", spiritRank: 5);
            _state.CardRegistry[s2.ID] = s2;

            bool threw = false;
            try
            {
                _state.GetPlayer(P1).SpiritZone.Add(s2.ID);
            }
            catch (System.InvalidOperationException)
            {
                threw = true;
            }

            Log($"Second spirit add threw InvalidOperationException={threw}");
            Assert.IsTrue(threw);
        }

        [Test]
        public void EvaluateEndOfTurnTriggers_IsStub_DoesNotThrow()
        {
            bool threw = false;
            try
            {
                _triggerSystem.EvaluateEndOfTurnTriggers(P1);
            }
            catch
            {
                threw = true;
            }

            Log($"EvaluateEndOfTurnTriggers threw={threw}");
            Assert.IsFalse(threw);
        }

        [Test]
        public void EvaluateEnterFieldTriggers_IsStub_DoesNotThrow()
        {
            var card = new RuntimeCard("card-test", P1, CardType.Ritual,
                CardSuperType.Incantation, "test");
            _state.CardRegistry[card.ID] = card;

            bool threw = false;
            try
            {
                _triggerSystem.EvaluateEnterFieldTriggers(card.ID);
            }
            catch
            {
                threw = true;
            }

            Log($"EvaluateEnterFieldTriggers threw={threw}");
            Assert.IsFalse(threw);
        }

        [Test]
        public void Dispose_UnsubscribesAllEvents_NoEventsFireAfterDispose()
        {
            var spirit = MakeSpiritInZone("molok", P1, rank: 11);
            AddMolokTrigger(spirit, P1, P2);

            // Dispose before any triggers fire.
            _triggerSystem.Dispose();

            bool disposeAgainThrew = false;
            try { _triggerSystem.Dispose(); }
            catch { disposeAgainThrew = true; }

            Log($"Phase unchanged after dispose={_state.CurrentPhase == GamePhase.StartOfTurn}");
            Log($"Second dispose threw={disposeAgainThrew}");
            Assert.AreEqual(GamePhase.StartOfTurn, _state.CurrentPhase);
            Assert.IsFalse(disposeAgainThrew);
        }
    }
}
