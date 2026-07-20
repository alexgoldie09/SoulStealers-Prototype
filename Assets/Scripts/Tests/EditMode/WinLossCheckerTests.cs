using NUnit.Framework;
using SSR.Logic;
using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace SSR.Tests
{
    /// <summary>
    /// EditMode tests for WinLossChecker — soul-loss detection, simultaneous
    /// draw, tiebreakers, and all forced-outcome paths.
    /// Output: Assets/Debug/WinLossCheckerTests.txt
    /// </summary>
    public class WinLossCheckerTests
    {
        private static readonly string LogPath = Path.Combine(
            Application.dataPath, "Debug", "WinLossCheckerTests.txt");

        private GameState _state;
        private WinLossChecker _checker;
        private StringBuilder _log;

        private const int P1 = 1;
        private const int P2 = 2;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogPath));
            File.WriteAllText(LogPath,
                $"=== WinLossCheckerTests Run: {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===" +
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
            _state.CurrentPhase = GamePhase.ActionPhase;

            _checker = new WinLossChecker(_state);

            _log = new StringBuilder();
            _log.AppendLine($"--- {TestContext.CurrentContext.Test.Name} ---");
        }

        [TearDown]
        public void TearDown()
        {
            _checker.Dispose();

            _log.AppendLine();
            File.AppendAllText(LogPath, _log.ToString());
        }

        private void Log(string msg) { Debug.Log(msg); _log.AppendLine(msg); }

        // ── Helpers ───────────────────────────────────────────────

        /// <summary>
        /// Resolves a banish against a player to reduce their souls.
        /// Fires EffectResolver.OnSoulsChanged which the checker listens to.
        /// </summary>
        private void BanishSouls(int fromPlayerID, int targetPlayerID, int amount)
        {
            _state.GetPlayer(targetPlayerID).Souls = Math.Max(0,
                _state.GetPlayer(targetPlayerID).Souls - amount);
            var banish = new BanishEffectData
            {
                BaseValue = amount,
                ValueType = NumericValueType.Symbolic,
                ControllerID = fromPlayerID,
                SourceCardID = -1
            };
            banish.TargetIDs.Add(targetPlayerID);
            EffectResolver.Resolve(banish, _state);
        }

        /// <summary>
        /// Places a spirit card in the given player's SpiritZone with the given rank.
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

        // ── Tests ─────────────────────────────────────────────────

        [Test]
        public void SoulsReachZero_P1AtZero_P2Wins()
        {
            GameResult result = null;
            _checker.OnGameOver += r => result = r;

            _state.GetPlayer(P1).Souls = 1;
            BanishSouls(P2, P1, 1);

            Log($"ResultType={result?.ResultType} Reason={result?.Reason} WinnerID={result?.WinnerID}");
            Assert.IsNotNull(result);
            Assert.AreEqual(GameResultType.P2Wins, result.ResultType);
            Assert.AreEqual(GameOverReason.SoulsReachedZero, result.Reason);
            Assert.AreEqual(P2, result.WinnerID);
        }

        [Test]
        public void SoulsReachZero_P2AtZero_P1Wins()
        {
            GameResult result = null;
            _checker.OnGameOver += r => result = r;

            _state.GetPlayer(P2).Souls = 1;
            BanishSouls(P1, P2, 1);

            Log($"ResultType={result?.ResultType} Reason={result?.Reason} WinnerID={result?.WinnerID}");
            Assert.IsNotNull(result);
            Assert.AreEqual(GameResultType.P1Wins, result.ResultType);
            Assert.AreEqual(GameOverReason.SoulsReachedZero, result.Reason);
            Assert.AreEqual(P1, result.WinnerID);
        }

        [Test]
        public void SimultaneousZero_BothAtZero_Draw()
        {
            GameResult result = null;
            _checker.OnGameOver += r => result = r;

            // Set both to 1 soul, then resolve a steal from P1 that zeroes P2
            // and simultaneously zeroes P1 (by manually setting P1 souls to 0 first).
            _state.GetPlayer(P1).Souls = 0;
            _state.GetPlayer(P2).Souls = 1;

            // Banish P2's last soul — at evaluation time P1 is already at 0.
            BanishSouls(P1, P2, 1);

            Log($"ResultType={result?.ResultType} Reason={result?.Reason}");
            Assert.IsNotNull(result);
            Assert.AreEqual(GameResultType.Draw, result.ResultType);
            Assert.AreEqual(GameOverReason.SimultaneousLoss, result.Reason);
            Assert.AreEqual(-1, result.WinnerID);
        }

        [Test]
        public void SoulsAboveZero_NoGameOver()
        {
            GameResult result = null;
            _checker.OnGameOver += r => result = r;

            BanishSouls(P2, P1, 5); // P1 goes 30 → 25

            Log($"result={result?.ResultType.ToString() ?? "null"} P1Souls={_state.GetPlayer(P1).Souls}");
            Assert.IsNull(result);
        }

        [Test]
        public void Concede_P1Concedes_P2Wins()
        {
            GameResult result = null;
            _checker.OnGameOver += r => result = r;

            _checker.Concede(P1);

            Log($"ResultType={result?.ResultType} Reason={result?.Reason} WinnerID={result?.WinnerID}");
            Assert.IsNotNull(result);
            Assert.AreEqual(GameResultType.P2Wins, result.ResultType);
            Assert.AreEqual(GameOverReason.Concede, result.Reason);
            Assert.AreEqual(P2, result.WinnerID);
        }

        [Test]
        public void ForceWin_P1_P1Wins()
        {
            GameResult result = null;
            _checker.OnGameOver += r => result = r;

            _checker.ForceWin(P1);

            Log($"ResultType={result?.ResultType} Reason={result?.Reason} WinnerID={result?.WinnerID}");
            Assert.IsNotNull(result);
            Assert.AreEqual(GameResultType.P1Wins, result.ResultType);
            Assert.AreEqual(GameOverReason.ForceWin, result.Reason);
            Assert.AreEqual(P1, result.WinnerID);
        }

        [Test]
        public void ForceLoss_P1_P2Wins()
        {
            GameResult result = null;
            _checker.OnGameOver += r => result = r;

            _checker.ForceLoss(P1);

            Log($"ResultType={result?.ResultType} Reason={result?.Reason} WinnerID={result?.WinnerID}");
            Assert.IsNotNull(result);
            Assert.AreEqual(GameResultType.P2Wins, result.ResultType);
            Assert.AreEqual(GameOverReason.ForceLoss, result.Reason);
            Assert.AreEqual(P2, result.WinnerID);
        }

        [Test]
        public void TimeExpired_EqualSoulsAndRanks_Draw()
        {
            // Equal souls, no spirits → tiebreak falls through to draw.
            GameResult result = null;
            _checker.OnGameOver += r => result = r;

            _checker.TimeExpired();

            Log($"ResultType={result?.ResultType} Reason={result?.Reason}");
            Assert.IsNotNull(result);
            Assert.AreEqual(GameResultType.Draw, result.ResultType);
            Assert.AreEqual(GameOverReason.TimeExpired, result.Reason);
        }

        [Test]
        public void Idempotent_FiresOnlyOnce()
        {
            int fireCount = 0;
            _checker.OnGameOver += _ => fireCount++;

            _checker.Concede(P1);
            _checker.Concede(P2);   // second call after game over — must be ignored
            _checker.ForceWin(P1);  // third call — must be ignored

            Log($"fireCount={fireCount}");
            Assert.AreEqual(1, fireCount);
        }

        [Test]
        public void AfterDispose_SoulsChange_NoGameOver()
        {
            GameResult result = null;
            _checker.OnGameOver += r => result = r;

            _checker.Dispose();

            // After disposing, OnSoulsChanged is unsubscribed — no game over fires.
            _state.GetPlayer(P1).Souls = 0;
            BanishSouls(P2, P1, 1); // would trigger if still subscribed

            Log($"result={result?.ResultType.ToString() ?? "null"}");
            Assert.IsNull(result);
        }

        [Test]
        public void TiebreakBySouls_P1MoreSouls_P1Wins()
        {
            GameResult result = null;
            _checker.OnGameOver += r => result = r;

            _state.GetPlayer(P1).Souls = 25;
            _state.GetPlayer(P2).Souls = 20;

            _checker.TimeExpired();

            Log($"ResultType={result?.ResultType} WinnerID={result?.WinnerID}");
            Assert.IsNotNull(result);
            Assert.AreEqual(GameResultType.P1Wins, result.ResultType);
            Assert.AreEqual(P1, result.WinnerID);
        }

        [Test]
        public void TiebreakBySpiritRank_P2HigherRank_P2Wins()
        {
            GameResult result = null;
            _checker.OnGameOver += r => result = r;

            // Equal souls — tiebreak falls to current spirit rank.
            MakeSpiritInZone("p1-spirit", P1, rank: 5);
            MakeSpiritInZone("p2-spirit", P2, rank: 11);

            _checker.TimeExpired();

            Log($"ResultType={result?.ResultType} WinnerID={result?.WinnerID}");
            Assert.IsNotNull(result);
            Assert.AreEqual(GameResultType.P2Wins, result.ResultType);
            Assert.AreEqual(P2, result.WinnerID);
        }

        [Test]
        public void TiebreakByLastRoundSpiritRank_P1HigherLastRank_P1Wins()
        {
            GameResult result = null;
            _checker.OnGameOver += r => result = r;

            // Give both players equal souls and equal current spirit ranks.
            MakeSpiritInZone("p1-spirit", P1, rank: 7);
            MakeSpiritInZone("p2-spirit", P2, rank: 7);

            // Record last-round ranks: P1 had rank 11, P2 had rank 3.
            _checker.RecordRoundSpiritRanks(); // stores current (7/7)

            // Swap P1's spirit for a different rank to simulate a new round,
            // but keep souls and current ranks equal so tiebreak falls through.
            _state.GetPlayer(P1).SpiritZone.Clear();
            _state.GetPlayer(P2).SpiritZone.Clear();

            // No current spirits → current rank 0 / 0 (equal) → falls to last-round rank.
            // Last-round was 7 / 7 → still equal, but we can manipulate _lastRoundSpiritRanks
            // by calling RecordRoundSpiritRanks on a state where ranks differ.

            // Re-setup: put different spirits, record, then remove them.
            var p1Spirit = MakeSpiritInZone("p1-hi", P1, rank: 11);
            var p2Spirit = MakeSpiritInZone("p2-lo", P2, rank: 3);
            _checker.RecordRoundSpiritRanks(); // stores 11 for P1, 3 for P2

            // Remove current spirits so current-rank tiebreak is 0/0 (equal).
            _state.GetPlayer(P1).SpiritZone.Clear();
            _state.GetPlayer(P2).SpiritZone.Clear();

            _checker.TimeExpired();

            Log($"ResultType={result?.ResultType} WinnerID={result?.WinnerID}");
            Assert.IsNotNull(result);
            Assert.AreEqual(GameResultType.P1Wins, result.ResultType);
            Assert.AreEqual(P1, result.WinnerID);
        }
    }
}
