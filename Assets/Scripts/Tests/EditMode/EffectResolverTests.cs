using NUnit.Framework;
using SSR.Logic;
using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace SSR.Tests
{
    /// <summary>
    /// Tests for EffectResolver.Resolve and related effect resolution logic.
    /// </summary>
    public class EffectResolverTests
    {
        private static readonly string LogPath = Path.Combine(
            Application.dataPath, "Debug", "EffectResolverTests.txt");

        private GameState _state;
        private StringBuilder _log;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogPath));
            File.WriteAllText(LogPath,
                $"=== EffectResolverTests Run: {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===" +
                Environment.NewLine + Environment.NewLine);
        }

        [SetUp]
        public void SetUp()
        {
            IDFactory.ResetIDs();
            _state = new GameState();
            _state.Players[0] = new PlayerState { PlayerID = 1, Souls = 30 };
            _state.Players[1] = new PlayerState { PlayerID = 2, Souls = 30 };
            _state.ActivePlayerID = 1;
            _state.CurrentPhase = GamePhase.ActionPhase;
            _log = new StringBuilder();
            _log.AppendLine($"--- {TestContext.CurrentContext.Test.Name} ---");
        }

        [TearDown]
        public void TearDown()
        {
            _log.AppendLine();
            File.AppendAllText(LogPath, _log.ToString());
        }

        private void Log(string msg) { Debug.Log(msg); _log.AppendLine(msg); }

        // ── Helpers ───────────────────────────────────────────────

        private StealEffectData MakeSteal(int value, int controllerID,
            int targetPlayerID, int sourceCardID = -1)
        {
            var e = new StealEffectData
            {
                BaseValue = value,
                ValueType = NumericValueType.Symbolic,
                ControllerID = controllerID,
                SourceCardID = sourceCardID
            };
            e.TargetIDs.Add(targetPlayerID);
            return e;
        }

        private BanishEffectData MakeBanish(int value, int controllerID,
            int targetPlayerID, int sourceCardID = -1)
        {
            var e = new BanishEffectData
            {
                BaseValue = value,
                ValueType = NumericValueType.Symbolic,
                ControllerID = controllerID,
                SourceCardID = sourceCardID
            };
            e.TargetIDs.Add(targetPlayerID);
            return e;
        }

        private RuntimeCard AddCardToIncantationZone(
            int ownerID, string name, CardType type = CardType.Ritual)
        {
            var card = new RuntimeCard(name, ownerID, type,
                CardSuperType.Incantation, name);
            card.Location = CardLocation.IncantationZone;
            card.HasEnteredField = true;
            _state.CardRegistry[card.ID] = card;
            _state.GetPlayer(ownerID)?.IncantationZone.Add(card.ID);
            return card;
        }

        private RuntimeCard AddCardToHand(int ownerID, string name,
            CardType type = CardType.Spell)
        {
            var card = new RuntimeCard(name, ownerID, type,
                CardSuperType.Sorcery, name);
            card.Location = CardLocation.Hand;
            _state.CardRegistry[card.ID] = card;
            _state.GetPlayer(ownerID)?.Hand.Add(card.ID);
            return card;
        }

        private RuntimeCard AddCardToDiscardPile(int ownerID, string name)
        {
            var card = new RuntimeCard(name, ownerID, CardType.Spell,
                CardSuperType.Sorcery, name);
            card.Location = CardLocation.DiscardPile;
            _state.CardRegistry[card.ID] = card;
            _state.GetPlayer(ownerID)?.DiscardPile.AddToTop(card.ID);
            return card;
        }

        // ── Steal ─────────────────────────────────────────────────

        [Test]
        public void Steal_TransfersSouls_CorrectlyBothPlayers()
        {
            //Log("=== Steal_TransfersSouls_CorrectlyBothPlayers ===");
            Log($"  Before: P1={_state.Players[0].Souls} P2={_state.Players[1].Souls}");

            var effect = MakeSteal(3, controllerID: 1, targetPlayerID: 2);
            var result = EffectResolver.Resolve(effect, _state);

            Log($"  After:  P1={_state.Players[0].Souls} P2={_state.Players[1].Souls}  result={result}");

            Assert.AreEqual(EffectResolutionResult.Resolved, result);
            Assert.AreEqual(27, _state.Players[1].Souls);
            Assert.AreEqual(33, _state.Players[0].Souls);
        }

        [Test]
        public void Steal_ReducedByDefense()
        {
            //Log("=== Steal_ReducedByDefense ===");
            var defender = AddCardToIncantationZone(2, "Defense Ritual");
            defender.Effects.Add(new DefenseEffectData { BaseValue = 2 });
            Log($"  Defense card added to P2 IncantationZone  defense={EffectResolver.CalculateTotalDefense(_state, 2)}");
            Log($"  Before: P1={_state.Players[0].Souls} P2={_state.Players[1].Souls}");

            var effect = MakeSteal(3, controllerID: 1, targetPlayerID: 2);
            var result = EffectResolver.Resolve(effect, _state);

            Log($"  After:  P1={_state.Players[0].Souls} P2={_state.Players[1].Souls}  net=1  result={result}");

            Assert.AreEqual(EffectResolutionResult.Resolved, result);
            Assert.AreEqual(29, _state.Players[1].Souls); // 30 - (3-2) = 29
            Assert.AreEqual(31, _state.Players[0].Souls); // 30 + 1
        }

        [Test]
        public void Steal_ClampedToZeroWhenDefenseExceedsValue()
        {
            //Log("=== Steal_ClampedToZeroWhenDefenseExceedsValue ===");
            var defender = AddCardToIncantationZone(2, "High Defense Ritual");
            defender.Effects.Add(new DefenseEffectData { BaseValue = 5 });
            Log($"  Defense=5 vs Steal=3 → net should be 0");

            var effect = MakeSteal(3, controllerID: 1, targetPlayerID: 2);
            var result = EffectResolver.Resolve(effect, _state);

            Log($"  After: P1={_state.Players[0].Souls} P2={_state.Players[1].Souls}  result={result}");

            Assert.AreEqual(EffectResolutionResult.Resolved, result);
            Assert.AreEqual(30, _state.Players[1].Souls); // no change
            Assert.AreEqual(30, _state.Players[0].Souls); // no change
        }

        [Test]
        public void Steal_ModifierIncreasesValue()
        {
            //Log("=== Steal_ModifierIncreasesValue ===");
            var modCard = AddCardToIncantationZone(1, "Steal Modifier Ritual");
            var mod = new ModifierEffectData
            {
                BaseValue = 2,
                IsPositive = true,
                ControllerOnly = true,
                ControllerID = 1
            };
            mod.ModifiedEffectTypes.Add(EffectType.Steal);
            modCard.Effects.Add(mod);
            Log($"  Modifier +2 on Steal  base=2 → expected net=4 (no defense)");

            var effect = MakeSteal(2, controllerID: 1, targetPlayerID: 2);
            var result = EffectResolver.Resolve(effect, _state);

            Log($"  After: P1={_state.Players[0].Souls} P2={_state.Players[1].Souls}  result={result}");

            Assert.AreEqual(EffectResolutionResult.Resolved, result);
            Assert.AreEqual(26, _state.Players[1].Souls); // 30 - 4
            Assert.AreEqual(34, _state.Players[0].Souls); // 30 + 4
        }

        [Test]
        public void Steal_IgnoreDefense_BypassesAllDefense()
        {
            //Log("=== Steal_IgnoreDefense_BypassesAllDefense ===");
            var defender = AddCardToIncantationZone(2, "Defense Ritual");
            defender.Effects.Add(new DefenseEffectData { BaseValue = 3 });

            // Source card has Ignore Defense.
            var sourceCard = new RuntimeCard("Ignore Spell", 1,
                CardType.Spell, CardSuperType.Sorcery, "Ignore Spell");
            sourceCard.Effects.Add(new IgnoreEffectData { IgnoresDefense = true });
            _state.CardRegistry[sourceCard.ID] = sourceCard;

            Log($"  Defense=3 but source Ignores Defense  Steal=3 → net=3");
            var effect = MakeSteal(3, controllerID: 1,
                targetPlayerID: 2, sourceCardID: sourceCard.ID);
            var result = EffectResolver.Resolve(effect, _state);

            Log($"  After: P1={_state.Players[0].Souls} P2={_state.Players[1].Souls}  result={result}");

            Assert.AreEqual(EffectResolutionResult.Resolved, result);
            Assert.AreEqual(27, _state.Players[1].Souls);
            Assert.AreEqual(33, _state.Players[0].Souls);
        }

        [Test]
        public void Steal_MultipleDefenseEffectsCumulative()
        {
            //Log("=== Steal_MultipleDefenseEffectsCumulative ===");
            var ritual1 = AddCardToIncantationZone(2, "Defense Ritual 1");
            ritual1.Effects.Add(new DefenseEffectData { BaseValue = 1 });
            var ritual2 = AddCardToIncantationZone(2, "Defense Ritual 2");
            ritual2.Effects.Add(new DefenseEffectData { BaseValue = 1 });
            Log($"  Two Defense 1 cards = total Defense 2  Steal=3 → net=1");

            var effect = MakeSteal(3, controllerID: 1, targetPlayerID: 2);
            EffectResolver.Resolve(effect, _state);

            Log($"  After: P1={_state.Players[0].Souls} P2={_state.Players[1].Souls}");

            Assert.AreEqual(29, _state.Players[1].Souls);
            Assert.AreEqual(31, _state.Players[0].Souls);
        }

        // ── Banish ────────────────────────────────────────────────

        [Test]
        public void Banish_TargetLosesSouls_ControllerGainsNothing()
        {
            //Log("=== Banish_TargetLosesSouls_ControllerGainsNothing ===");
            Log($"  Before: P1={_state.Players[0].Souls} P2={_state.Players[1].Souls}");

            var effect = MakeBanish(4, controllerID: 1, targetPlayerID: 2);
            var result = EffectResolver.Resolve(effect, _state);

            Log($"  After:  P1={_state.Players[0].Souls} P2={_state.Players[1].Souls}  result={result}");

            Assert.AreEqual(EffectResolutionResult.Resolved, result);
            Assert.AreEqual(26, _state.Players[1].Souls);
            Assert.AreEqual(30, _state.Players[0].Souls); // no gain
        }

        [Test]
        public void Banish_ReducedByDefense()
        {
            //Log("=== Banish_ReducedByDefense ===");
            var defender = AddCardToIncantationZone(2, "Defense Ritual");
            defender.Effects.Add(new DefenseEffectData { BaseValue = 2 });

            var effect = MakeBanish(4, controllerID: 1, targetPlayerID: 2);
            EffectResolver.Resolve(effect, _state);

            Log($"  After: P2={_state.Players[1].Souls}  (expected 28 = 30 - (4-2))");
            Assert.AreEqual(28, _state.Players[1].Souls);
        }

        // ── GiveSouls ─────────────────────────────────────────────

        [Test]
        public void GiveSouls_Imposed_ControllerLosesTargetGains()
        {
            //Log("=== GiveSouls_Imposed_ControllerLosesTargetGains ===");
            var effect = new GiveSoulsEffectData
            {
                BaseValue = 2,
                IsImposed = true,
                ControllerID = 1
            };
            effect.TargetIDs.Add(2);

            EffectResolver.Resolve(effect, _state);

            Log($"  After: P1={_state.Players[0].Souls} P2={_state.Players[1].Souls}");
            Assert.AreEqual(28, _state.Players[0].Souls); // controller loses
            Assert.AreEqual(32, _state.Players[1].Souls); // target gains
        }

        [Test]
        public void GiveSouls_Inflicted_TargetLosesControllerGains()
        {
            //Log("=== GiveSouls_Inflicted_TargetLosesControllerGains ===");
            var effect = new GiveSoulsEffectData
            {
                BaseValue = 2,
                IsImposed = false,
                ControllerID = 1
            };
            effect.TargetIDs.Add(2);

            EffectResolver.Resolve(effect, _state);

            Log($"  After: P1={_state.Players[0].Souls} P2={_state.Players[1].Souls}");
            Assert.AreEqual(32, _state.Players[0].Souls); // controller gains
            Assert.AreEqual(28, _state.Players[1].Souls); // target loses
        }

        [Test]
        public void GiveSouls_IgnoresDefense()
        {
            //Log("=== GiveSouls_IgnoresDefense ===");
            var defender = AddCardToIncantationZone(2, "Defense Ritual");
            defender.Effects.Add(new DefenseEffectData { BaseValue = 5 });

            var effect = new GiveSoulsEffectData
            {
                BaseValue = 3,
                IsImposed = true,
                ControllerID = 1
            };
            effect.TargetIDs.Add(2);

            EffectResolver.Resolve(effect, _state);

            Log($"  After: P1={_state.Players[0].Souls} P2={_state.Players[1].Souls}");
            // Defense 5 irrelevant — full 3 transferred
            Assert.AreEqual(27, _state.Players[0].Souls);
            Assert.AreEqual(33, _state.Players[1].Souls);
        }

        // ── Destroy ───────────────────────────────────────────────

        [Test]
        public void Destroy_MovesCardToOwnerDiscard()
        {
            //Log("=== Destroy_MovesCardToOwnerDiscard ===");
            var ritual = AddCardToIncantationZone(2, "Target Ritual");
            Log($"  Ritual ID={ritual.ID}  location={ritual.Location}");

            var effect = new DestroyEffectData { ControllerID = 1 };
            effect.TargetIDs.Add(ritual.ID);

            var result = EffectResolver.Resolve(effect, _state);

            Log($"  After: location={ritual.Location}  " +
                $"inDiscard={_state.Players[1].DiscardPile.Contains(ritual.ID)}  " +
                $"result={result}");

            Assert.AreEqual(EffectResolutionResult.Resolved, result);
            Assert.AreEqual(CardLocation.DiscardPile, ritual.Location);
            Assert.IsTrue(_state.Players[1].DiscardPile.Contains(ritual.ID));
            Assert.IsFalse(_state.Players[1].IncantationZone.Contains(ritual.ID));
        }

        [Test]
        public void Destroy_Fizzles_WhenTargetLeavesField()
        {
            //Log("=== Destroy_Fizzles_WhenTargetLeavesField ===");
            var card = AddCardToHand(2, "Card In Hand");
            Log($"  Target card is in Hand, not on Field");

            var effect = new DestroyEffectData { ControllerID = 1 };
            effect.TargetIDs.Add(card.ID);

            var result = EffectResolver.Resolve(effect, _state);

            Log($"  result={result}  (expected: Fizzled)");
            Assert.AreEqual(EffectResolutionResult.Fizzled, result);
        }

        [Test]
        public void Destroy_Blocked_WhenTargetIsIndestructible()
        {
            //Log("=== Destroy_Blocked_WhenTargetIsIndestructible ===");
            var ritual = AddCardToIncantationZone(2, "Indestructible Ritual");
            ritual.IsIndestructible = true;
            ritual.Effects.Add(new IndestructibleEffectData());
            Log($"  Target is Indestructible");

            var effect = new DestroyEffectData { ControllerID = 1 };
            effect.TargetIDs.Add(ritual.ID);

            var result = EffectResolver.Resolve(effect, _state);

            Log($"  result={result}  (expected: Blocked)  " +
                $"stillOnField={_state.Players[1].IncantationZone.Contains(ritual.ID)}");

            Assert.AreEqual(EffectResolutionResult.Blocked, result);
            Assert.IsTrue(_state.Players[1].IncantationZone.Contains(ritual.ID));
        }

        [Test]
        public void Destroy_Blocked_WhenTargetIsSpirit()
        {
            //Log("=== Destroy_Blocked_WhenTargetIsSpirit ===");
            var spirit = new RuntimeCard("Spirit", 2, CardType.Spirit,
                CardSuperType.None, "Test Spirit", spiritRank: 5);
            spirit.Location = CardLocation.SpiritZone;
            _state.CardRegistry[spirit.ID] = spirit;
            _state.Players[1].SpiritZone.Add(spirit.ID);

            var effect = new DestroyEffectData { ControllerID = 1 };
            effect.TargetIDs.Add(spirit.ID);

            var result = EffectResolver.Resolve(effect, _state);

            Log($"  result={result}  (expected: Blocked — Spirits cannot be Destroyed)");
            Assert.AreEqual(EffectResolutionResult.Blocked, result);
        }

        [Test]
        public void Destroy_IndestructibleSilenced_CanBeDestroyed()
        {
            //Log("=== Destroy_IndestructibleSilenced_CanBeDestroyed ===");
            var ritual = AddCardToIncantationZone(2, "Silenced Indestructible");
            ritual.IsIndestructible = true;
            ritual.IsSilenced = true;
            Log($"  Target is Indestructible but also Silenced → Indestructible lost");

            var effect = new DestroyEffectData { ControllerID = 1 };
            effect.TargetIDs.Add(ritual.ID);

            var result = EffectResolver.Resolve(effect, _state);

            Log($"  result={result}  (expected: Resolved)");
            Assert.AreEqual(EffectResolutionResult.Resolved, result);
            Assert.AreEqual(CardLocation.DiscardPile, ritual.Location);
        }

        // ── Discard ───────────────────────────────────────────────

        [Test]
        public void Discard_MovesCardFromHandToDiscard()
        {
            //Log("=== Discard_MovesCardFromHandToDiscard ===");
            var card = AddCardToHand(2, "Hand Card");
            Log($"  Card in Hand: {card.CurrentName} ID={card.ID}");

            var effect = new DiscardEffectData
            {
                SourceZone = ZoneType.Hand,
                ControllerID = 1,
                TargetPlayerID = 2
            };
            effect.TargetIDs.Add(card.ID);

            var result = EffectResolver.Resolve(effect, _state);

            Log($"  After: location={card.Location}  " +
                $"inDiscard={_state.Players[1].DiscardPile.Contains(card.ID)}  " +
                $"result={result}");

            Assert.AreEqual(EffectResolutionResult.Resolved, result);
            Assert.AreEqual(CardLocation.DiscardPile, card.Location);
        }

        // ── Recall ────────────────────────────────────────────────

        [Test]
        public void Recall_MovesCardFromDiscardToHand()
        {
            //Log("=== Recall_MovesCardFromDiscardToHand ===");
            var card1 = AddCardToDiscardPile(2, "Discard Card 1");
            var card2 = AddCardToDiscardPile(2, "Discard Card 2");
            Log($"  P2 discard has 2 cards  recalling top 1");

            var effect = new RecallEffectData
            {
                Count = 1,
                TakesFromTop = true,
                SourceZone = ZoneType.DiscardPile,
                ControllerID = 2
            };
            effect.TargetIDs.Add(2);

            var result = EffectResolver.Resolve(effect, _state);
            Log($"  P2 Hand count={_state.Players[1].Hand.Count}  " +
                $"P2 Discard count={_state.Players[1].DiscardPile.Count}  " +
                $"result={result}");

            Assert.AreEqual(EffectResolutionResult.Resolved, result);
            Assert.AreEqual(1, _state.Players[1].Hand.Count);
            Assert.AreEqual(1, _state.Players[1].DiscardPile.Count);
        }

        [Test]
        public void Recall_Fizzles_WhenSourceZoneEmpty()
        {
            //Log("=== Recall_Fizzles_WhenSourceZoneEmpty ===");
            Log($"  P2 discard is empty");

            var effect = new RecallEffectData
            {
                Count = 2,
                TakesFromTop = true,
                SourceZone = ZoneType.DiscardPile,
                ControllerID = 2
            };
            effect.TargetIDs.Add(2);

            var result = EffectResolver.Resolve(effect, _state);
            Log($"  result={result}  (expected: Fizzled)");

            Assert.AreEqual(EffectResolutionResult.Fizzled, result);
        }

        // ── Silence ───────────────────────────────────────────────

        [Test]
        public void Silence_AppliesIsSilencedToTarget()
        {
            //Log("=== Silence_AppliesIsSilencedToTarget ===");
            var ritual = AddCardToIncantationZone(2, "Target Ritual");
            Log($"  Target IsSilenced before: {ritual.IsSilenced}");

            var effect = new SilenceEffectData
            {
                Duration = EffectDurationTiming.UntilNextTurn,
                ControllerID = 1
            };
            effect.TargetIDs.Add(ritual.ID);

            var result = EffectResolver.Resolve(effect, _state);
            Log($"  Target IsSilenced after: {ritual.IsSilenced}  result={result}");

            Assert.AreEqual(EffectResolutionResult.Resolved, result);
            Assert.IsTrue(ritual.IsSilenced);
        }

        [Test]
        public void Silence_RemovesIndestructible()
        {
            //Log("=== Silence_RemovesIndestructible ===");
            var ritual = AddCardToIncantationZone(2, "Indestructible Ritual");
            ritual.IsIndestructible = true;
            Log($"  IsIndestructible before: {ritual.IsIndestructible}");

            var effect = new SilenceEffectData
            {
                Duration = EffectDurationTiming.UntilNextTurn,
                ControllerID = 1
            };
            effect.TargetIDs.Add(ritual.ID);

            EffectResolver.Resolve(effect, _state);
            Log($"  IsIndestructible after: {ritual.IsIndestructible}  " +
                $"IsSilenced: {ritual.IsSilenced}");

            Assert.IsFalse(ritual.IsIndestructible);
            Assert.IsTrue(ritual.IsSilenced);
        }

        [Test]
        public void Silence_RegistersActiveDuration()
        {
            //Log("=== Silence_RegistersActiveDuration ===");
            var ritual = AddCardToIncantationZone(2, "Target Ritual");

            var effect = new SilenceEffectData
            {
                Duration = EffectDurationTiming.UntilNextTurn,
                ControllerID = 1
            };
            effect.TargetIDs.Add(ritual.ID);

            EffectResolver.Resolve(effect, _state);

            Log($"  ActiveDurations count: {_state.ActiveDurations.Count}  " +
                $"timing: {_state.ActiveDurations[0]?.Timing}");

            Assert.AreEqual(1, _state.ActiveDurations.Count);
            Assert.AreEqual(EffectDurationTiming.UntilNextTurn,
                _state.ActiveDurations[0].Timing);
        }

        [Test]
        public void Silence_Blocked_OnSpirit()
        {
            //Log("=== Silence_Blocked_OnSpirit ===");
            var spirit = new RuntimeCard("Spirit", 2, CardType.Spirit,
                CardSuperType.None, "Test Spirit", spiritRank: 3);
            spirit.Location = CardLocation.SpiritZone;
            _state.CardRegistry[spirit.ID] = spirit;
            _state.Players[1].SpiritZone.Add(spirit.ID);

            var effect = new SilenceEffectData
            {
                Duration = EffectDurationTiming.UntilNextTurn,
                ControllerID = 1
            };
            effect.TargetIDs.Add(spirit.ID);

            var result = EffectResolver.Resolve(effect, _state);
            Log($"  result={result}  spirit IsSilenced={spirit.IsSilenced}");

            Assert.AreEqual(EffectResolutionResult.Blocked, result);
            Assert.IsFalse(spirit.IsSilenced);
        }

        // ── Counter ───────────────────────────────────────────────

        [Test]
        public void Counter_AddsCorrectly()
        {
            //Log("=== Counter_AddsCorrectly ===");
            var ritual = AddCardToIncantationZone(2, "Counter Target");
            Log($"  CounterCount before: {ritual.CounterCount}");

            var effect = new CounterEffectData
            {
                BaseValue = 2,
                IsAddition = true,
                ControllerID = 1
            };
            effect.TargetIDs.Add(ritual.ID);

            var result = EffectResolver.Resolve(effect, _state);
            Log($"  CounterCount after: {ritual.CounterCount}  result={result}");

            Assert.AreEqual(EffectResolutionResult.Resolved, result);
            Assert.AreEqual(2, ritual.CounterCount);
        }

        [Test]
        public void Counter_RemovesCorrectly()
        {
            //Log("=== Counter_RemovesCorrectly ===");
            var ritual = AddCardToIncantationZone(2, "Counter Target");
            ritual.CounterCount = 3;

            var effect = new CounterEffectData
            {
                BaseValue = 2,
                IsAddition = false,
                ControllerID = 1
            };
            effect.TargetIDs.Add(ritual.ID);

            EffectResolver.Resolve(effect, _state);
            Log($"  CounterCount after: {ritual.CounterCount}  (expected 1)");

            Assert.AreEqual(1, ritual.CounterCount);
        }

        [Test]
        public void Counter_Fizzles_WhenTargetNotOnField()
        {
            //Log("=== Counter_Fizzles_WhenTargetNotOnField ===");
            var card = AddCardToHand(2, "Hand Card");

            var effect = new CounterEffectData
            {
                BaseValue = 1,
                IsAddition = true,
                ControllerID = 1
            };
            effect.TargetIDs.Add(card.ID);

            var result = EffectResolver.Resolve(effect, _state);
            Log($"  result={result}  (expected: Fizzled)");

            Assert.AreEqual(EffectResolutionResult.Fizzled, result);
        }

        // ── Defense Calculator ────────────────────────────────────

        [Test]
        public void CalculateTotalDefense_SumsAcrossMultipleCards()
        {
            //Log("=== CalculateTotalDefense_SumsAcrossMultipleCards ===");
            var r1 = AddCardToIncantationZone(2, "Defense Ritual 1");
            r1.Effects.Add(new DefenseEffectData { BaseValue = 2 });
            var r2 = AddCardToIncantationZone(2, "Defense Prayer 1",
                CardType.Prayer);
            r2.Effects.Add(new DefenseEffectData { BaseValue = 1 });

            int total = EffectResolver.CalculateTotalDefense(_state, 2);
            Log($"  Total Defense for P2: {total}  (expected 3)");

            Assert.AreEqual(3, total);
        }

        [Test]
        public void CalculateTotalDefense_SilencedCardNotCounted()
        {
            //Log("=== CalculateTotalDefense_SilencedCardNotCounted ===");
            var r1 = AddCardToIncantationZone(2, "Defense Ritual");
            r1.Effects.Add(new DefenseEffectData { BaseValue = 2 });
            r1.IsSilenced = true;

            int total = EffectResolver.CalculateTotalDefense(_state, 2);
            Log($"  Total Defense for P2: {total}  (silenced card not counted → 0)");

            Assert.AreEqual(0, total);
        }

        // ── Modifier Calculator ───────────────────────────────────

        [Test]
        public void ApplyModifiers_GlobalModifier_AppliesToAllPlayers()
        {
            //Log("=== ApplyModifiers_GlobalModifier_AppliesToAllPlayers ===");
            // Spirit Uzilda-style: global -2 to all banish and steal.
            var spiritCard = new RuntimeCard("Uzilda", 2, CardType.Spirit,
                CardSuperType.None, "Uzilda, Sword of Order", spiritRank: 5);
            spiritCard.Location = CardLocation.SpiritZone;
            var globalMod = new ModifierEffectData
            {
                BaseValue = 2,
                IsPositive = false,
                ControllerOnly = false,
                ControllerID = 2
            };
            globalMod.ModifiedEffectTypes.Add(EffectType.Steal);
            globalMod.ModifiedEffectTypes.Add(EffectType.Banish);
            spiritCard.Effects.Add(globalMod);
            _state.CardRegistry[spiritCard.ID] = spiritCard;
            _state.Players[1].SpiritZone.Add(spiritCard.ID);

            int result = EffectResolver.ApplyModifiers(
                _state, 4, EffectType.Steal, controllerID: 1,
                sourceCardID: -1, valueType: NumericValueType.Symbolic);

            Log($"  Base=4 global -2 → result={result}  (expected 2)");
            Assert.AreEqual(2, result);
        }

        [Test]
        public void ApplyModifiers_WordFormNotModified()
        {
            //Log("=== ApplyModifiers_WordFormNotModified ===");
            var modCard = AddCardToIncantationZone(1, "Modifier Ritual");
            var mod = new ModifierEffectData
            {
                BaseValue = 5,
                IsPositive = true,
                ControllerOnly = false,
                ControllerID = 1
            };
            mod.ModifiedEffectTypes.Add(EffectType.Steal);
            modCard.Effects.Add(mod);

            int result = EffectResolver.ApplyModifiers(
                _state, 2, EffectType.Steal, controllerID: 1,
                sourceCardID: -1, valueType: NumericValueType.WordForm);

            Log($"  WordForm value=2 with +5 modifier → result={result}  (unchanged)");
            Assert.AreEqual(2, result);
        }

        // ── Stubs ─────────────────────────────────────────────────

        [Test]
        public void Negate_ReturnsNotImplemented_UntilStackExists()
        {
            //Log("=== Negate_ReturnsNotImplemented_UntilStackExists ===");
            var effect = new NegateEffectData { ControllerID = 1 };
            effect.TargetIDs.Add(99);

            var result = EffectResolver.Resolve(effect, _state);
            Log($"  result={result}  (expected: NotImplemented)");

            Assert.AreEqual(EffectResolutionResult.NotImplemented, result);
        }
    }
}
