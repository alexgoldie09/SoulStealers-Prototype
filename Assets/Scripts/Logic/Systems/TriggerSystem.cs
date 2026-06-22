using System;

namespace SSR.Logic
{
    /// <summary>
    /// Evaluates triggered card effects and places them on the Resolution
    /// Stack as PileObjects when their timing condition is met.
    ///
    /// Subscribes to RoundStateMachine and ResolutionStack events in the
    /// constructor and unsubscribes in Dispose(). Rule 700–703.
    /// </summary>
    public class TriggerSystem : IDisposable
    {
        private readonly GameState _state;
        private readonly RoundStateMachine _rsm;
        private readonly ResolutionStack _stack;

        // Stored delegates for clean unsubscription.
        private readonly Action<int> _onTurnStarted;
        private readonly Action<int> _onTurnEnded;
        private readonly Action<int> _onCardEnteredField;
        private readonly Action<int> _onCardPlaced;
        private readonly Action _onPileEmpty;

        public TriggerSystem(GameState state, RoundStateMachine rsm, ResolutionStack stack)
        {
            _state = state;
            _rsm = rsm;
            _stack = stack;

            _onTurnStarted = EvaluateBeginningOfTurnTriggers;
            _onTurnEnded = EvaluateEndOfTurnTriggers;
            _onCardEnteredField = EvaluateEnterFieldTriggers;
            _onCardPlaced = EvaluateCardPlayedTriggers;
            _onPileEmpty = OnPileEmpty;

            _rsm.OnTurnStarted += _onTurnStarted;
            _rsm.OnTurnEnded += _onTurnEnded;
            _stack.OnCardEnteredField += _onCardEnteredField;
            _stack.OnCardPlaced += _onCardPlaced;
            _stack.OnPileEmpty += _onPileEmpty;
        }

        #region Public Methods
        /// <summary>
        /// Scans all field cards in priority order for Beginning of Turn
        /// triggers and places matching PileObjects on the stack.
        /// If no triggers fire, immediately advances to ActionPhase.
        /// Rule 502, 704.
        /// </summary>
        public void EvaluateBeginningOfTurnTriggers(int activePlayerID)
        {
            bool anyPlaced = false;

            var activePlayer = _state.GetPlayer(activePlayerID);
            var opponent = _state.GetOpponent(activePlayerID);

            if (activePlayer != null)
                anyPlaced |= ScanPlayerTriggers(activePlayer, activePlayerID);

            if (opponent != null)
                anyPlaced |= ScanPlayerTriggers(opponent, activePlayerID);

            if (!anyPlaced)
                _rsm.AdvanceToActionPhase();
        }

        /// <summary>
        /// Stub — no cards use End of Turn triggers in the current set.
        /// </summary>
        public void EvaluateEndOfTurnTriggers(int playerID)
        {
            // Stub — not yet implemented.
        }

        /// <summary>
        /// Stub — Enter Field triggers not yet implemented.
        /// </summary>
        public void EvaluateEnterFieldTriggers(int cardID)
        {
            // Stub — not yet implemented.
        }

        /// <summary>
        /// Stub — Card Played triggers not yet implemented.
        /// </summary>
        public void EvaluateCardPlayedTriggers(int cardID)
        {
            // Stub — not yet implemented.
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Scans one player's field cards in trigger priority order:
        /// Spirit → Curses → Rituals → Prayers. Rule 704.
        /// </summary>
        private bool ScanPlayerTriggers(PlayerState player, int activePlayerID)
        {
            bool anyPlaced = false;

            // 1. Spirit
            anyPlaced |= ScanCardForBoTTrigger(player.SpiritZone.SpiritID, activePlayerID);

            // 2. Curses (ascending field position)
            foreach (int id in player.IncantationZone.CardIDs)
            {
                var card = _state.GetCard(id);
                if (card != null && card.CurrentType == CardType.Curse)
                    anyPlaced |= ScanCardForBoTTrigger(id, activePlayerID);
            }

            // 3. Rituals (ascending field position)
            foreach (int id in player.IncantationZone.CardIDs)
            {
                var card = _state.GetCard(id);
                if (card != null && card.CurrentType == CardType.Ritual)
                    anyPlaced |= ScanCardForBoTTrigger(id, activePlayerID);
            }

            // 4. Prayers (ascending field position)
            foreach (int id in player.IncantationZone.CardIDs)
            {
                var card = _state.GetCard(id);
                if (card != null && card.CurrentType == CardType.Prayer)
                    anyPlaced |= ScanCardForBoTTrigger(id, activePlayerID);
            }

            return anyPlaced;
        }

        /// <summary>
        /// Checks a single card for active Beginning of Turn triggers and
        /// places them on the pile. Returns true if any were placed.
        /// </summary>
        private bool ScanCardForBoTTrigger(int cardID, int activePlayerID)
        {
            if (cardID == -1) return false;

            var card = _state.GetCard(cardID);
            if (card == null || card.IsSilenced) return false;

            bool anyPlaced = false;
            foreach (var effect in card.Effects)
            {
                if (!(effect is TriggerEffectData trigger)) continue;
                if (trigger.Timing != TriggerTiming.BeginningOfTurn) continue;
                if (trigger.Status == EffectStatus.Silenced) continue;
                if (trigger.TriggeredEffect == null) continue;

                // "Only on owner's turn" gate.
                if (trigger.OnlyOnOwnerTurn && trigger.ControllerID != activePlayerID)
                    continue;

                var pileObj = new PileObject
                {
                    ID = IDFactory.GetUniqueID(),
                    SourceCardID = trigger.SourceCardID,
                    ControllerID = trigger.ControllerID,
                    Effect = trigger.TriggeredEffect
                };
                _stack.PlaceEffectObject(pileObj);
                anyPlaced = true;
            }
            return anyPlaced;
        }

        /// <summary>
        /// Called when the pile empties. Advances to ActionPhase if we
        /// are still in StartOfTurn (BoT triggers have all resolved).
        /// </summary>
        private void OnPileEmpty()
        {
            if (_state.CurrentPhase == GamePhase.StartOfTurn)
                _rsm.AdvanceToActionPhase();
        }
        #endregion

        // ── IDisposable ───────────────────────────────────────────

        public void Dispose()
        {
            _rsm.OnTurnStarted -= _onTurnStarted;
            _rsm.OnTurnEnded -= _onTurnEnded;
            _stack.OnCardEnteredField -= _onCardEnteredField;
            _stack.OnCardPlaced -= _onCardPlaced;
            _stack.OnPileEmpty -= _onPileEmpty;
        }
    }
}
