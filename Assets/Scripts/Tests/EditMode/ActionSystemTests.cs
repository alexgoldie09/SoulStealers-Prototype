using NUnit.Framework;
using SSR.Logic;
using UnityEngine;

namespace SSR.Tests
{
    /// <summary>
    /// Unit tests for ActionSystem. Covers:
    /// - ResetActions sets the player's actions to 2 at the start of their Action Phase
    /// - SpendAction reduces the player's remaining actions by 1
    /// - ConsumesAction returns true for action-consuming play types and false for free play types
    /// - CanPerformPlay returns false if it's the wrong phase, wrong player, no actions remaining,
    /// or the resolution pile is not empty, and true if all conditions are met.
    /// </summary>
    public class ActionSystemTests
    {
        private GameState _state;

        [SetUp]
        public void SetUp()
        {
            IDFactory.ResetIDs();
            _state = new GameState();
            _state.CurrentPhase = GamePhase.ActionPhase;
            _state.Players[0] = new PlayerState { PlayerID = 1, Souls = 30 };
            _state.Players[1] = new PlayerState { PlayerID = 2, Souls = 30 };
            _state.ActivePlayerID = 1;
            ActionSystem.ResetActions(_state, 1);
        }

        private void LogActionState(string label = "")
        {
            var p = _state.GetPlayer(1);
            Debug.Log($"  [Actions{(label.Length > 0 ? $" {label}" : "")}] " +
                      $"Player 1 — remaining={p?.ActionsRemaining}  " +
                      $"phase={_state.CurrentPhase}  " +
                      $"activePlayer={_state.ActivePlayerID}  " +
                      $"pileEmpty={_state.ResolutionPile.IsEmpty}");
        }

        [Test]
        public void ResetActions_SetsBudgetToTwo()
        {
            Debug.Log("=== ResetActions_SetsBudgetToTwo ===");
            _state.Players[0].ActionsRemaining = 0;
            Debug.Log($"  Manually zeroed actions. Resetting...");
            ActionSystem.ResetActions(_state, 1);
            LogActionState("after reset");
            Assert.AreEqual(2, _state.Players[0].ActionsRemaining);
        }

        [Test]
        public void SpendAction_ReducesBudgetByOne()
        {
            Debug.Log("=== SpendAction_ReducesBudgetByOne ===");
            LogActionState("before spend");
            ActionSystem.SpendAction(_state, 1);
            LogActionState("after spend");
            Assert.AreEqual(1, _state.Players[0].ActionsRemaining);
        }

        [Test]
        public void ConsumesAction_TrueForNormalPlay()
        {
            bool result = ActionSystem.ConsumesAction(PlayType.NormalPlay);
            Debug.Log($"  ConsumesAction(NormalPlay) = {result}  (expected: true)");
            Assert.IsTrue(result);
        }

        [Test]
        public void ConsumesAction_TrueForSecretPlay()
        {
            bool result = ActionSystem.ConsumesAction(PlayType.SecretPlay);
            Debug.Log($"  ConsumesAction(SecretPlay) = {result}  (expected: true)");
            Assert.IsTrue(result);
        }

        [Test]
        public void ConsumesAction_TrueForMergePlay()
        {
            bool result = ActionSystem.ConsumesAction(PlayType.MergePlay);
            Debug.Log($"  ConsumesAction(MergePlay) = {result}  (expected: true)");
            Assert.IsTrue(result);
        }

        [Test]
        public void ConsumesAction_TrueForReveal()
        {
            bool result = ActionSystem.ConsumesAction(PlayType.Reveal);
            Debug.Log($"  ConsumesAction(Reveal) = {result}  (expected: true)");
            Assert.IsTrue(result);
        }

        [Test]
        public void ConsumesAction_FalseForSpecialPlay()
        {
            bool result = ActionSystem.ConsumesAction(PlayType.SpecialPlay);
            Debug.Log($"  ConsumesAction(SpecialPlay) = {result}  (expected: false — free play)");
            Assert.IsFalse(result);
        }

        [Test]
        public void ConsumesAction_FalseForPut()
        {
            bool result = ActionSystem.ConsumesAction(PlayType.Put);
            Debug.Log($"  ConsumesAction(Put) = {result}  (expected: false — free play)");
            Assert.IsFalse(result);
        }

        [Test]
        public void CanPerformPlay_ReturnsFalse_WrongPhase()
        {
            Debug.Log("=== CanPerformPlay_ReturnsFalse_WrongPhase ===");
            _state.CurrentPhase = GamePhase.StartOfTurn;
            LogActionState();
            bool result = ActionSystem.CanPerformPlay(_state, 1, PlayType.NormalPlay);
            Debug.Log($"  CanPerformPlay(NormalPlay) = {result}  (expected: false — phase is StartOfTurn)");
            Assert.IsFalse(result);
        }

        [Test]
        public void CanPerformPlay_ReturnsFalse_WrongPlayer()
        {
            Debug.Log("=== CanPerformPlay_ReturnsFalse_WrongPlayer ===");
            LogActionState();
            bool result = ActionSystem.CanPerformPlay(_state, 2, PlayType.NormalPlay);
            Debug.Log($"  CanPerformPlay(player=2) = {result}  (expected: false — activePlayer=1)");
            Assert.IsFalse(result);
        }

        [Test]
        public void CanPerformPlay_ReturnsFalse_NoActionsRemaining()
        {
            Debug.Log("=== CanPerformPlay_ReturnsFalse_NoActionsRemaining ===");
            _state.Players[0].ActionsRemaining = 0;
            LogActionState();
            bool result = ActionSystem.CanPerformPlay(_state, 1, PlayType.NormalPlay);
            Debug.Log($"  CanPerformPlay(NormalPlay) = {result}  (expected: false — 0 actions left)");
            Assert.IsFalse(result);
        }

        [Test]
        public void CanPerformPlay_ReturnsFalse_PileNotEmpty()
        {
            Debug.Log("=== CanPerformPlay_ReturnsFalse_PileNotEmpty ===");
            _state.ResolutionPile.Push(999);
            Debug.Log($"  Pushed card 999 to resolution pile (peek={_state.ResolutionPile.Peek()})");
            LogActionState();
            bool result = ActionSystem.CanPerformPlay(_state, 1, PlayType.NormalPlay);
            Debug.Log($"  CanPerformPlay(NormalPlay) = {result}  (expected: false — pile not empty)");
            Assert.IsFalse(result);
        }

        [Test]
        public void CanPerformPlay_ReturnsTrue_ValidConditions()
        {
            Debug.Log("=== CanPerformPlay_ReturnsTrue_ValidConditions ===");
            LogActionState();
            bool result = ActionSystem.CanPerformPlay(_state, 1, PlayType.NormalPlay);
            Debug.Log($"  CanPerformPlay(NormalPlay) = {result}  (expected: true — all conditions met)");
            Assert.IsTrue(result);
        }

        [Test]
        public void CanRecycle_ReturnsFalse_AlreadyRecycled()
        {
            Debug.Log("=== CanRecycle_ReturnsFalse_AlreadyRecycled ===");
            ActionSystem.MarkRecycled(_state, 1);
            Debug.Log($"  Marked Player 1 as recycled. HasRecycledThisActionPhase={_state.Players[0].HasRecycledThisActionPhase}");
            bool result = ActionSystem.CanRecycle(_state, 1);
            Debug.Log($"  CanRecycle = {result}  (expected: false)");
            Assert.IsFalse(result);
        }

        [Test]
        public void CanRecycle_ReturnsTrue_NotYetRecycled()
        {
            Debug.Log("=== CanRecycle_ReturnsTrue_NotYetRecycled ===");
            Debug.Log($"  HasRecycledThisActionPhase={_state.Players[0].HasRecycledThisActionPhase}");
            bool result = ActionSystem.CanRecycle(_state, 1);
            Debug.Log($"  CanRecycle = {result}  (expected: true)");
            Assert.IsTrue(result);
        }

        [Test]
        public void CanActivatePrayer_ReturnsFalse_AlreadyActivated()
        {
            Debug.Log("=== CanActivatePrayer_ReturnsFalse_AlreadyActivated ===");
            var prayer = new RuntimeCard("Prayer", 1, CardType.Prayer,
                CardSuperType.Incantation, "Test Prayer");
            prayer.PrayerActivatedThisActionPhase = true;
            _state.CardRegistry[prayer.ID] = prayer;
            Debug.Log($"  Prayer '{prayer.CurrentName}' (ID={prayer.ID})  activated={prayer.PrayerActivatedThisActionPhase}  silenced={prayer.IsSilenced}");
            bool result = ActionSystem.CanActivatePrayer(_state, 1, prayer.ID);
            Debug.Log($"  CanActivatePrayer = {result}  (expected: false — already activated this phase)");
            Assert.IsFalse(result);
        }

        [Test]
        public void CanActivatePrayer_ReturnsFalse_CardIsSilenced()
        {
            Debug.Log("=== CanActivatePrayer_ReturnsFalse_CardIsSilenced ===");
            var prayer = new RuntimeCard("Prayer", 1, CardType.Prayer,
                CardSuperType.Incantation, "Test Prayer");
            prayer.IsSilenced = true;
            _state.CardRegistry[prayer.ID] = prayer;
            Debug.Log($"  Prayer '{prayer.CurrentName}' (ID={prayer.ID})  silenced={prayer.IsSilenced}");
            bool result = ActionSystem.CanActivatePrayer(_state, 1, prayer.ID);
            Debug.Log($"  CanActivatePrayer = {result}  (expected: false — card is silenced)");
            Assert.IsFalse(result);
        }

        [Test]
        public void CanActivatePrayer_ReturnsFalse_NotAPrayer()
        {
            Debug.Log("=== CanActivatePrayer_ReturnsFalse_NotAPrayer ===");
            var ritual = new RuntimeCard("Ritual", 1, CardType.Ritual,
                CardSuperType.Incantation, "Test Ritual");
            _state.CardRegistry[ritual.ID] = ritual;
            Debug.Log($"  Card '{ritual.CurrentName}' (ID={ritual.ID})  type={ritual.CurrentType}");
            bool result = ActionSystem.CanActivatePrayer(_state, 1, ritual.ID);
            Debug.Log($"  CanActivatePrayer = {result}  (expected: false — type is Ritual, not Prayer)");
            Assert.IsFalse(result);
        }

        [Test]
        public void GrantExtraAction_IncreasesActionsRemaining()
        {
            Debug.Log("=== GrantExtraAction_IncreasesActionsRemaining ===");
            _state.Players[0].ActionsRemaining = 0;
            LogActionState("before grant");
            ActionSystem.GrantExtraAction(_state, 1);
            LogActionState("after grant");
            Assert.AreEqual(1, _state.Players[0].ActionsRemaining);
        }

        [Test]
        public void ResetTurnFlags_ClearsRecycleFlag()
        {
            Debug.Log("=== ResetTurnFlags_ClearsRecycleFlag ===");
            ActionSystem.MarkRecycled(_state, 1);
            Debug.Log($"  After MarkRecycled: HasRecycled={_state.Players[0].HasRecycledThisActionPhase}  CanRecycle={ActionSystem.CanRecycle(_state, 1)}");
            ActionSystem.ResetTurnFlags(_state, 1);
            Debug.Log($"  After ResetTurnFlags: HasRecycled={_state.Players[0].HasRecycledThisActionPhase}  CanRecycle={ActionSystem.CanRecycle(_state, 1)}");
            Assert.IsTrue(ActionSystem.CanRecycle(_state, 1));
        }
    }
}
