using System;

namespace SSR.Logic
{
    /// <summary>
    /// Tracks the turn order and current position within a round.
    /// Turn order is determined by ascending Spirit Rank after
    /// spirits are revealed. Rule 201, 501.7.
    /// </summary>
    [Serializable]
    public class TurnContext
    {
        // Player IDs in ascending Spirit Rank order for this round.
        // Populated when spirits are revealed. Rule 501.7.
        public int[] TurnOrder = Array.Empty<int>();

        // Index into TurnOrder for the player currently taking their turn.
        public int CurrentTurnIndex;

        /// <summary>
        /// The player ID whose turn it currently is.
        /// Returns -1 if TurnOrder has not been set.
        /// </summary>
        public int ActivePlayerID =>
            TurnOrder.Length > 0 && CurrentTurnIndex < TurnOrder.Length
                ? TurnOrder[CurrentTurnIndex]
                : -1;

        /// <summary>
        /// Returns true if all players have completed their turn
        /// this round and the round should end.
        /// </summary>
        public bool AllTurnsComplete =>
            CurrentTurnIndex >= TurnOrder.Length;

        /// <summary>
        /// Advances to the next player's turn.
        /// </summary>
        public void AdvanceToNextTurn()
        {
            CurrentTurnIndex++;
        }

        /// <summary>
        /// Resets the turn index to the start of the round.
        /// Called at the beginning of each new round.
        /// </summary>
        public void ResetForNewRound()
        {
            CurrentTurnIndex = 0;
        }
    }
}