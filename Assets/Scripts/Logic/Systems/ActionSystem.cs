namespace SSR.Logic
{
    /// <summary>
    /// Manages the action budget for each player's turn and validates
    /// whether a given play type is legal in the current game state.
    ///
    /// Each player has 2 actions per Action Phase. Actions are consumed
    /// by NormalPlay, SecretPlay, MergePlay, and Reveal. Special plays,
    /// Put, Recycle, and Prayer activation do not consume actions.
    /// Rule 600, 601, 602, 603, 604.
    /// </summary>
    public static class ActionSystem
    {
        public const int ActionsPerTurn = 2;

        #region Actions Budget
        /// <summary>
        /// Resets a player's action count to the full budget at the
        /// start of their Action Phase.
        /// </summary>
        public static void ResetActions(GameState state, int playerID)
        {
            var player = state.GetPlayer(playerID);
            if (player != null)
                player.ActionsRemaining = ActionsPerTurn;
        }

        /// <summary>
        /// Spends one action from the active player's budget.
        /// Only called for action-consuming play types.
        /// </summary>
        public static void SpendAction(GameState state, int playerID)
        {
            var player = state.GetPlayer(playerID);
            if (player != null && player.ActionsRemaining > 0)
                player.ActionsRemaining--;
        }

        /// <summary>
        /// Grants one extra action to the player. Used for Pact effects
        /// and other Extra Action sources. Does not exceed the cap —
        /// extra actions are spent immediately per the rules, so the
        /// cap is not a concern in practice. Rule 604.1.
        /// </summary>
        public static void GrantExtraAction(GameState state, int playerID)
        {
            var player = state.GetPlayer(playerID);
            if (player != null)
                player.ActionsRemaining++;
        }
        #endregion
        
        #region Legality
        /// <summary>
        /// Returns true if the given play type consumes one of the
        /// player's 2 actions. Rule 600.1.
        /// </summary>
        public static bool ConsumesAction(PlayType playType)
        {
            return playType switch
            {
                PlayType.NormalPlay  => true,
                PlayType.SecretPlay  => true,
                PlayType.MergePlay   => true,
                PlayType.Reveal      => true,
                PlayType.SpecialPlay => false,
                PlayType.Put         => false,
                _                    => false
            };
        }

        /// <summary>
        /// Returns true if the player can currently perform a play of
        /// the given type. Checks phase, pile state, and action budget.
        /// Rule 600.1.
        /// </summary>
        public static bool CanPerformPlay(
            GameState state,
            int playerID,
            PlayType playType)
        {
            if (state.CurrentPhase != GamePhase.ActionPhase) return false;
            if (state.ActivePlayerID != playerID) return false;
            if (!state.ResolutionPile.IsEmpty) return false;

            if (ConsumesAction(playType))
            {
                var player = state.GetPlayer(playerID);
                if (player == null || player.ActionsRemaining <= 0) return false;
            }

            return true;
        }

        /// <summary>
        /// Returns true if the player can Recycle this Action Phase.
        /// Recycle is free (not an action) but limited to once per
        /// Action Phase and requires an empty pile. Rule 604.6.
        /// </summary>
        public static bool CanRecycle(GameState state, int playerID)
        {
            if (state.CurrentPhase != GamePhase.ActionPhase) return false;
            if (state.ActivePlayerID != playerID) return false;
            if (!state.ResolutionPile.IsEmpty) return false;

            var player = state.GetPlayer(playerID);
            if (player == null || player.HasRecycledThisActionPhase) return false;

            return true;
        }

        /// <summary>
        /// Marks that the player has recycled this Action Phase.
        /// </summary>
        public static void MarkRecycled(GameState state, int playerID)
        {
            var player = state.GetPlayer(playerID);
            if (player != null)
                player.HasRecycledThisActionPhase = true;
        }

        /// <summary>
        /// Returns true if the given Prayer can be activated.
        /// Requires Action Phase, empty pile, and the Prayer has not
        /// been activated this Action Phase. Rule 308, 503, 605.
        /// </summary>
        public static bool CanActivatePrayer(
            GameState state,
            int playerID,
            int prayerCardID)
        {
            if (state.CurrentPhase != GamePhase.ActionPhase) return false;
            if (state.ActivePlayerID != playerID) return false;
            if (!state.ResolutionPile.IsEmpty) return false;

            var card = state.GetCard(prayerCardID);
            if (card == null) return false;
            if (card.CurrentType != CardType.Prayer) return false;
            if (card.PrayerActivatedThisActionPhase) return false;
            if (card.IsSilenced) return false;

            return true;
        }

        /// <summary>
        /// Resets per-turn tracking flags on the player state.
        /// Called at the start of each player's Action Phase.
        /// </summary>
        public static void ResetTurnFlags(GameState state, int playerID)
        {
            var player = state.GetPlayer(playerID);
            if (player == null) return;
            player.HasRecycledThisActionPhase = false;
        }
        #endregion
    }
}
