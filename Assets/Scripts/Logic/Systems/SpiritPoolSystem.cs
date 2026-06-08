using System;
using System.Collections.Generic;

namespace SSR.Logic
{
    /// <summary>
    /// Handles all Spirit Pool operations: dealing spirits to players,
    /// validating and processing spirit selection, auto-assigning on
    /// timer expiry, returning unchosen spirits, determining turn order
    /// from revealed spirits, and managing the depletion cycle.
    ///
    /// The spirit pool uses a client-defined depletion system:
    /// 16 spirits total, pool decrements by 2 each round, deal count
    /// scales with pool size (4 / 3 / 2 / 1), resets when exhausted.
    ///
    /// Rule 301, 501. Client rules for pool depletion.
    /// </summary>
    public static class SpiritPoolSystem
    {
        // ── Events ────────────────────────────────────────────────
        public static event Action<int, int> OnSpiritSelected;     // playerID, spiritID
        public static event Action<int, int> OnSpiritAutoAssigned; // playerID, spiritID
        public static event Action OnPoolCycleReset;

        // ── Dealing ───────────────────────────────────────────────

        /// <summary>
        /// Shuffles the available pool and deals the correct number of
        /// spirit cards to each player's DealtSpiritIDs list.
        /// Undealt cards remain in AvailablePool. Rule 501.1–501.2.
        /// </summary>
        public static void DealSpirits(GameState state, Random rng)
        {
            var pool = state.SpiritPool;

            // Shuffle the available pool
            pool.AvailablePool.Shuffle();

            int dealCount = pool.GetDealCount();
            if (dealCount == 0) return;

            int poolIndex = 0;
            foreach (var player in state.Players)
            {
                if (player == null) continue;
                player.DealtSpiritIDs.Clear();
                player.SelectedSpiritID = -1;

                for (int i = 0; i < dealCount; i++)
                {
                    if (poolIndex >= pool.AvailablePool.Count) break;
                    player.DealtSpiritIDs.Add(pool.AvailablePool[poolIndex]);
                    poolIndex++;
                }
            }

            // Remove dealt cards from the available pool
            pool.AvailablePool.RemoveRange(0, poolIndex);
        }

        // ── Selection ─────────────────────────────────────────────

        /// <summary>
        /// Attempts to select a spirit for a player. Returns true if
        /// the selection is valid (spiritID is in the player's dealt
        /// cards and no spirit has been selected yet).
        /// </summary>
        public static bool TrySelectSpirit(GameState state, int playerID, int spiritID)
        {
            var player = state.GetPlayer(playerID);
            if (player == null) return false;
            if (player.SelectedSpiritID != -1) return false;
            if (!player.DealtSpiritIDs.Contains(spiritID)) return false;

            player.SelectedSpiritID = spiritID;
            OnSpiritSelected?.Invoke(playerID, spiritID);
            return true;
        }

        /// <summary>
        /// Randomly assigns a spirit from the player's dealt cards.
        /// Called when the selection timer expires and the player
        /// has not made a selection. Client rule (timer-based selection).
        /// </summary>
        public static void AutoAssignSpirit(GameState state, int playerID, Random rng)
        {
            var player = state.GetPlayer(playerID);
            if (player == null) return;
            if (player.SelectedSpiritID != -1) return;
            if (player.DealtSpiritIDs.Count == 0) return;

            int index = rng.Next(player.DealtSpiritIDs.Count);
            int assigned = player.DealtSpiritIDs[index];
            player.SelectedSpiritID = assigned;
            OnSpiritAutoAssigned?.Invoke(playerID, assigned);
        }

        /// <summary>
        /// Returns true if all players have selected a spirit.
        /// Used to determine when to advance from SpiritSelection
        /// to RevealSpirits.
        /// </summary>
        public static bool AllPlayersHaveSelected(GameState state)
        {
            foreach (var player in state.Players)
            {
                if (player == null) continue;
                if (player.SelectedSpiritID == -1) return false;
            }
            return true;
        }

        // ── Reveal ────────────────────────────────────────────────

        /// <summary>
        /// Returns unchosen spirits to the AvailablePool and clears
        /// each player's DealtSpiritIDs. Called immediately after
        /// spirits are revealed. Rule 501.4.
        /// </summary>
        public static void ReturnUnselectedSpirits(GameState state)
        {
            foreach (var player in state.Players)
            {
                if (player == null) continue;
                foreach (int id in player.DealtSpiritIDs)
                {
                    if (id != player.SelectedSpiritID)
                        state.SpiritPool.AvailablePool.Add(id);
                }
                player.DealtSpiritIDs.Clear();
            }
        }

        /// <summary>
        /// Returns player IDs sorted by their selected spirit's rank
        /// in ascending order. Lowest rank goes first. Rule 201, 501.7.
        /// The spirit rank is read from the RuntimeCard in CardRegistry.
        /// </summary>
        public static int[] GetTurnOrder(GameState state)
        {
            var players = new List<(int playerID, int rank)>();

            foreach (var player in state.Players)
            {
                if (player == null) continue;
                var rank = int.MaxValue;
                var spirit = state.GetCard(player.SelectedSpiritID);
                if (spirit != null) rank = spirit.SpiritRank;
                players.Add((player.PlayerID, rank));
            }

            players.Sort((a, b) => a.rank.CompareTo(b.rank));

            var order = new int[players.Count];
            for (var i = 0; i < players.Count; i++)
                order[i] = players[i].playerID;
            return order;
        }

        // ── End of Round ──────────────────────────────────────────

        /// <summary>
        /// Moves each player's selected spirit to the UnavailablePool
        /// and clears SelectedSpiritID. If the pool is now empty,
        /// triggers a cycle reset. Rule 506. Client depletion rule.
        /// </summary>
        public static void EndRoundCleanup(GameState state)
        {
            foreach (var player in state.Players)
            {
                if (player == null) continue;
                if (player.SelectedSpiritID != -1)
                {
                    state.SpiritPool.UnavailablePool.Add(player.SelectedSpiritID);
                    player.SelectedSpiritID = -1;
                }
            }

            if (state.SpiritPool.PoolSize == 0)
            {
                state.SpiritPool.ResetCycle();
                OnPoolCycleReset?.Invoke();
            }
        }
    }
}
