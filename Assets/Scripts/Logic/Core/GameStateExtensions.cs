namespace SSR.Logic
{
    /// <summary>
    /// Extension methods on GameState for common lookups used
    /// across multiple systems. Avoids repeating foreach loops
    /// throughout the codebase.
    /// </summary>
    public static class GameStateExtensions
    {
        /// <summary>
        /// Returns the PlayerState with the given playerID, or null
        /// if not found.
        /// </summary>
        public static PlayerState GetPlayer(this GameState state, int playerID)
        {
            foreach (var player in state.Players)
                if (player != null && player.PlayerID == playerID)
                    return player;
            return null;
        }

        /// <summary>
        /// Returns the PlayerState that is NOT the given playerID.
        /// In a 2-player game this is always the opponent.
        /// </summary>
        public static PlayerState GetOpponent(this GameState state, int playerID)
        {
            foreach (var player in state.Players)
                if (player != null && player.PlayerID != playerID)
                    return player;
            return null;
        }

        /// <summary>
        /// Returns the RuntimeCard with the given cardID from the
        /// CardRegistry, or null if not found.
        /// </summary>
        public static RuntimeCard GetCard(this GameState state, int cardID)
        {
            state.CardRegistry.TryGetValue(cardID, out var card);
            return card;
        }

        /// <summary>
        /// Returns true if the given playerID is the active player
        /// for the current turn.
        /// </summary>
        public static bool IsActivePlayer(this GameState state, int playerID)
            => state.ActivePlayerID == playerID;

        /// <summary>
        /// Returns true if it is currently the given player's Action Phase.
        /// </summary>
        public static bool IsPlayerActionPhase(this GameState state, int playerID)
            => state.CurrentPhase == GamePhase.ActionPhase
            && state.ActivePlayerID == playerID;
    }
}
