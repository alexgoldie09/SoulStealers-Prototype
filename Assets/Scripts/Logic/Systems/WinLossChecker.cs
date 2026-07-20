using System;
using System.Collections.Generic;

namespace SSR.Logic
{
    /// <summary>
    /// Monitors soul totals and game-ending conditions.
    /// Fires OnGameOver exactly once then becomes inert.
    ///
    /// Subscribes to EffectResolver.OnSoulsChanged for live soul tracking.
    /// Concede/ForceWin/ForceLoss/TimeExpired are driven externally.
    /// RecordRoundSpiritRanks must be called at the end of each round
    /// (subscribe to RoundStateMachine.OnBeforeEndOfRound) to maintain
    /// the spirit-rank tiebreaker state. Rule 102.
    /// </summary>
    public class WinLossChecker : IDisposable
    {
        private readonly GameState _state;
        private bool _gameOver;

        // Spirit rank recorded at end of each round, keyed by playerID.
        // Used as the third tiebreaker (soul → current rank → last-round rank). Rule 102.3.
        private readonly Dictionary<int, int> _lastRoundSpiritRanks = new Dictionary<int, int>();

        // Stored delegate for clean unsubscription from static event.
        private readonly Action<int, int, int> _onSoulsChanged;

        public event Action<GameResult> OnGameOver;

        public WinLossChecker(GameState state)
        {
            _state = state;
            _onSoulsChanged = OnSoulsChangedHandler;
            EffectResolver.OnSoulsChanged += _onSoulsChanged;
        }

        #region Public API
        /// <summary>
        /// Call at the end of each round (OnBeforeEndOfRound) to snapshot
        /// the current spirit ranks for the third tiebreaker. Rule 102.3.
        /// </summary>
        public void RecordRoundSpiritRanks()
        {
            foreach (var player in _state.Players)
            {
                if (player == null) continue;
                var spirit = _state.GetCard(player.SpiritZone.SpiritID);
                _lastRoundSpiritRanks[player.PlayerID] = spirit?.SpiritRank ?? 0;
            }
        }

        /// <summary>
        /// The conceding player loses immediately. Rule 102.2.a.
        /// </summary>
        public void Concede(int playerID)
        {
            if (_gameOver) return;
            int opponentID = playerID == 1 ? 2 : 1;
            FireGameOver(new GameResult(
                opponentID == 1 ? GameResultType.P1Wins : GameResultType.P2Wins,
                GameOverReason.Concede,
                opponentID));
        }

        /// <summary>
        /// Forces a win for the specified player (e.g. rule interaction). Rule 102.
        /// </summary>
        public void ForceWin(int playerID)
        {
            if (_gameOver) return;
            FireGameOver(new GameResult(
                playerID == 1 ? GameResultType.P1Wins : GameResultType.P2Wins,
                GameOverReason.ForceWin,
                playerID));
        }

        /// <summary>
        /// Forces a loss for the specified player (e.g. rule interaction).
        /// The opponent wins. Rule 102.
        /// </summary>
        public void ForceLoss(int playerID)
        {
            if (_gameOver) return;
            int opponentID = playerID == 1 ? 2 : 1;
            FireGameOver(new GameResult(
                opponentID == 1 ? GameResultType.P1Wins : GameResultType.P2Wins,
                GameOverReason.ForceLoss,
                opponentID));
        }

        /// <summary>
        /// Game timer expired. Winner is determined by tiebreak:
        /// most souls → current spirit rank → last-round spirit rank → draw.
        /// Rule 102.3.
        /// </summary>
        public void TimeExpired()
        {
            if (_gameOver) return;
            FireGameOver(DetermineWinnerByTiebreak(GameOverReason.TimeExpired));
        }
        #endregion

        #region Private
        /// <summary>
        /// Called by EffectResolver.OnSoulsChanged whenever a player's soul total changes.
        /// </summary>
        /// <param name="playerID">The ID of the player whose soul total changed.</param>
        /// <param name="oldValue">The previous soul total of the player.</param>
        /// <param name="newValue">The new soul total of the player.</param>
        private void OnSoulsChangedHandler(int playerID, int oldValue, int newValue)
        {
            if (_gameOver) return;

            var p1 = _state.GetPlayer(1);
            var p2 = _state.GetPlayer(2);
            if (p1 == null || p2 == null) return;

            bool p1Zero = p1.Souls <= 0;
            bool p2Zero = p2.Souls <= 0;

            // Rule 102.1.e — both reach zero simultaneously → draw.
            if (p1Zero && p2Zero)
            {
                FireGameOver(new GameResult(GameResultType.Draw,
                    GameOverReason.SimultaneousLoss, -1));
                return;
            }

            if (p1Zero)
            {
                FireGameOver(new GameResult(GameResultType.P2Wins,
                    GameOverReason.SoulsReachedZero, 2));
                return;
            }

            if (p2Zero)
            {
                FireGameOver(new GameResult(GameResultType.P1Wins,
                    GameOverReason.SoulsReachedZero, 1));
            }
        }

        /// <summary>
        /// Determines the winner based on tiebreaker rules when the game ends due to time expiration or other conditions that require a tiebreak.
        /// </summary>
        /// <param name="reason">The reason for the game over that triggered the tiebreak.</param>
        /// <returns>The result of the game after applying tiebreaker rules.</returns>
        private GameResult DetermineWinnerByTiebreak(GameOverReason reason)
        {
            var p1 = _state.GetPlayer(1);
            var p2 = _state.GetPlayer(2);

            // Tiebreak 1: most souls wins.
            if (p1 != null && p2 != null && p1.Souls != p2.Souls)
            {
                int winnerID = p1.Souls > p2.Souls ? 1 : 2;
                return new GameResult(
                    winnerID == 1 ? GameResultType.P1Wins : GameResultType.P2Wins,
                    reason, winnerID);
            }

            // Tiebreak 2: highest current spirit rank.
            int p1Rank = GetCurrentSpiritRank(1);
            int p2Rank = GetCurrentSpiritRank(2);
            if (p1Rank != p2Rank)
            {
                int winnerID = p1Rank > p2Rank ? 1 : 2;
                return new GameResult(
                    winnerID == 1 ? GameResultType.P1Wins : GameResultType.P2Wins,
                    reason, winnerID);
            }

            // Tiebreak 3: highest last-round spirit rank.
            _lastRoundSpiritRanks.TryGetValue(1, out int p1LastRank);
            _lastRoundSpiritRanks.TryGetValue(2, out int p2LastRank);
            if (p1LastRank != p2LastRank)
            {
                int winnerID = p1LastRank > p2LastRank ? 1 : 2;
                return new GameResult(
                    winnerID == 1 ? GameResultType.P1Wins : GameResultType.P2Wins,
                    reason, winnerID);
            }

            return new GameResult(GameResultType.Draw, reason, -1);
        }

        /// <summary>
        /// Returns the current spirit rank of the specified player by looking up their spirit card in the game state.
        /// </summary>
        /// <param name="playerID">The ID of the player whose spirit rank is being queried.</param>
        /// <returns>The current spirit rank of the specified player.</returns>
        private int GetCurrentSpiritRank(int playerID)
        {
            var player = _state.GetPlayer(playerID);
            if (player == null) return 0;
            var spirit = _state.GetCard(player.SpiritZone.SpiritID);
            return spirit?.SpiritRank ?? 0;
        }

        /// <summary>
        /// Fires the OnGameOver event with the specified GameResult and marks the game as over to prevent further processing.
        /// </summary>
        /// <param name="result">The result of the game to be passed to the OnGameOver event.</param>
        private void FireGameOver(GameResult result)
        {
            _gameOver = true;
            OnGameOver?.Invoke(result);
        }
        #endregion

        /// <summary>
        /// Disposes of the WinLossChecker by unsubscribing from events and performing any necessary cleanup.
        /// </summary>
        public void Dispose() => EffectResolver.OnSoulsChanged -= _onSoulsChanged;
    }
}
