using System;

namespace SSR.Logic
{
    /// <summary>
    /// Drives the round and turn sequence for Soulstealers Rising.
    /// Manages GamePhase transitions, fires events at key moments,
    /// handles the spirit selection timer, and delegates to
    /// SpiritPoolSystem and ActionSystem for their respective logic.
    ///
    /// The state machine is driven externally:
    /// — Auto-advancing phases transition immediately after entry logic.
    /// — Waiting phases (SpiritSelection, ActionPhase) wait for
    ///   external calls (SelectSpirit / EndTurn) or timer expiry.
    ///
    /// Does NOT execute effects, process the resolution pile,
    /// draw cards, or make AI decisions. These are handled by
    /// other systems that subscribe to the events fired here.
    ///
    /// Rule 500–506.
    /// </summary>
    public class RoundStateMachine
    {
        // ── Dependencies ──────────────────────────────────────────
        private readonly GameState _state;
        private readonly TurnContext _turnContext;
        private readonly Random _rng;

        // ── Spirit Selection Timer ────────────────────────────────
        // Driven externally via Tick(float deltaTime).
        // Set per-selection phase. Client rule: timer-based selection.
        private float _selectionTimer;
        public float SelectionTimeLimit = 30f;

        // ── Events ────────────────────────────────────────────────
        // Auto-advancing phases fire these on entry. Subscribers
        // (trigger system, visual layer, AI) react to them.

        // Fired when any phase is entered. Args: phase, activePlayerID
        // (-1 for round-level phases that aren't tied to one player).
        public event Action<GamePhase, int> OnPhaseEntered;

        // Fired when a round begins. Arg: round number.
        public event Action<int> OnRoundStarted;

        // Fired when a player's turn begins. Arg: playerID.
        // Subscribe here for "Beginning of Turn" triggered effects. Rule 502.
        public event Action<int> OnTurnStarted;

        // Fired when a player's turn ends. Arg: playerID.
        // Subscribe here for "End of Turn" triggered effects. Rule 504.
        public event Action<int> OnTurnEnded;

        // Fired after all spirits are revealed simultaneously. Rule 501.6.
        // Subscribe here for "when spirits are revealed" effects.
        public event Action<GameState> OnSpiritsRevealed;

        // Fired when the Action Phase begins for a player.
        // Subscribe here to enable player input or start AI decision loop.
        public event Action<int> OnActionPhaseStarted;

        // Fired when the Draw Phase begins. Arg: playerID.
        // Subscribe here to perform the actual card draw. Rule 505.
        public event Action<int> OnDrawPhaseStarted;

        // Fired when all rounds in the pool cycle have completed.
        public event Action OnPoolCycleReset;

        // Fired when a round ends.
        public event Action<int> OnRoundEnded; // arg: round number

        // ── Public State ──────────────────────────────────────────
        public GamePhase CurrentPhase => _state.CurrentPhase;
        public TurnContext TurnContext => _turnContext;
        public bool IsRunning { get; private set; }

        // ── Constructor ───────────────────────────────────────────
        public RoundStateMachine(GameState state, Random rng = null)
        {
            _state = state;
            _turnContext = new TurnContext();
            _rng = rng ?? new Random();
            SpiritPoolSystem.OnPoolCycleReset += () => OnPoolCycleReset?.Invoke();
        }

        // ── External Drive ────────────────────────────────────────

        /// <summary>
        /// Begins the game from the first round. Call once after
        /// game setup is complete.
        /// </summary>
        public void StartGame()
        {
            IsRunning = true;
            TransitionTo(GamePhase.BeginRound);
        }

        /// <summary>
        /// Called every frame by the Unity MonoBehaviour driver.
        /// Only active during timed phases (SpiritSelection).
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (!IsRunning) return;
            if (_state.CurrentPhase != GamePhase.SpiritSelection) return;

            _selectionTimer -= deltaTime;
            if (_selectionTimer <= 0f)
                HandleSelectionTimeout();
        }

        /// <summary>
        /// Called by the player input or AI system to select a spirit.
        /// If both players have selected, immediately advances to
        /// RevealSpirits. Returns false if the selection is invalid.
        /// </summary>
        public bool SelectSpirit(int playerID, int spiritID)
        {
            if (_state.CurrentPhase != GamePhase.SpiritSelection) return false;

            var valid = SpiritPoolSystem.TrySelectSpirit(_state, playerID, spiritID);
            if (!valid) return false;

            if (SpiritPoolSystem.AllPlayersHaveSelected(_state))
                TransitionTo(GamePhase.RevealSpirits);

            return true;
        }

        /// <summary>
        /// Called by the active player (or AI) to end their Action Phase
        /// and move to End of Turn. The pile must be empty. Rule 503.
        /// </summary>
        public bool EndTurn(int playerID)
        {
            if (_state.CurrentPhase != GamePhase.ActionPhase) return false;
            if (_state.ActivePlayerID != playerID) return false;
            if (!_state.ResolutionPile.IsEmpty) return false;

            TransitionTo(GamePhase.EndOfTurn);
            return true;
        }

        // ── Phase Transitions ─────────────────────────────────────
        /// <summary>
        /// Transitions to the given phase, updates GameState, and fires
        /// the appropriate events.
        /// </summary>
        /// <param name="phase"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private void TransitionTo(GamePhase phase)
        {
            _state.CurrentPhase = phase;
            OnPhaseEntered?.Invoke(phase, _state.ActivePlayerID);

            switch (phase)
            {
                case GamePhase.BeginRound:      EnterBeginRound();      break;
                case GamePhase.SpiritDeal:      EnterSpiritDeal();      break;
                case GamePhase.SpiritSelection: EnterSpiritSelection(); break;
                case GamePhase.RevealSpirits:   EnterRevealSpirits();   break;
                case GamePhase.StartOfTurn:     EnterStartOfTurn();     break;
                case GamePhase.ActionPhase:     EnterActionPhase();     break;
                case GamePhase.EndOfTurn:       EnterEndOfTurn();       break;
                case GamePhase.DrawPhase:       EnterDrawPhase();       break;
                case GamePhase.EndOfRound:      EnterEndOfRound();      break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(phase), phase, null);
            }
        }

        #region Phase Entry
        /// <summary>
        /// Increments round number, resets turn context, fires OnRoundStarted,
        /// and advances to SpiritDeal. Rule 500.
        /// </summary>
        private void EnterBeginRound()
        {
            _state.RoundNumber++;
            _turnContext.ResetForNewRound();
            OnRoundStarted?.Invoke(_state.RoundNumber);
            TransitionTo(GamePhase.SpiritDeal);
        }

        /// <summary>
        /// Deals spirits to each player from the AvailablePool. Rule 501.2.
        /// </summary>
        private void EnterSpiritDeal()
        {
            SpiritPoolSystem.DealSpirits(_state, _rng);
            TransitionTo(GamePhase.SpiritSelection);
        }

        /// <summary>
        /// Starts the spirit selection timer and waits for both players to select
        /// a spirit. If the timer expires, auto-assigns spirits for any players
        /// who haven't selected and advances to RevealSpirits. Client rule:
        /// timer-based selection. Rule 501.3.
        /// </summary>
        private void EnterSpiritSelection()
        {
            _selectionTimer = SelectionTimeLimit;
            // Waiting phase — no auto-advance.
            // Advances via SelectSpirit() or HandleSelectionTimeout().
        }

        /// <summary>
        /// Returns unselected spirits to the AvailablePool, determines turn order from
        /// revealed spirit ranks, fires OnSpiritsRevealed, and advances to StartOfTurn. Rule 501.4–6.
        /// </summary>
        private void EnterRevealSpirits()
        {
            SpiritPoolSystem.ReturnUnselectedSpirits(_state);

            // Determine turn order from revealed spirit ranks. Rule 501.6.
            _turnContext.TurnOrder = SpiritPoolSystem.GetTurnOrder(_state);
            _turnContext.ResetForNewRound();

            _state.ActivePlayerID = _turnContext.ActivePlayerID;

            OnSpiritsRevealed?.Invoke(_state);
            TransitionTo(GamePhase.StartOfTurn);
        }

        /// <summary>
        /// Fires OnTurnStarted for the active player. Waiting for "Beginning of Turn"
        /// trigger effects to resolve before advancing to Action Phase. Rule 502.
        /// </summary>
        private void EnterStartOfTurn()
        {
            _state.ActivePlayerID = _turnContext.ActivePlayerID;
            _state.PriorityPlayerID = _state.ActivePlayerID;
            OnTurnStarted?.Invoke(_state.ActivePlayerID);
            // Waiting for "Beginning of Turn" trigger effects to resolve
            // before advancing. In this chunk, auto-advance immediately.
            // The trigger system (later chunk) will intercept here.
            TransitionTo(GamePhase.ActionPhase);
        }

        /// <summary>
        /// Resets the active player's actions and turn flags, fires OnActionPhaseStarted,
        /// and waits for the player to perform actions until they call EndTurn(). Rule 503.
        /// </summary>
        private void EnterActionPhase()
        {
            ActionSystem.ResetActions(_state, _state.ActivePlayerID);
            ActionSystem.ResetTurnFlags(_state, _state.ActivePlayerID);
            OnActionPhaseStarted?.Invoke(_state.ActivePlayerID);
            // Waiting phase — no auto-advance.
            // Advances via EndTurn().
        }

        /// <summary>
        /// Fires OnTurnEnded for the active player and advances to DrawPhase. Waiting for
        /// "End of Turn" trigger effects to resolve before advancing. Rule 504.
        /// </summary>
        private void EnterEndOfTurn()
        {
            OnTurnEnded?.Invoke(_state.ActivePlayerID);
            // Waiting for "End of Turn" trigger effects to resolve.
            // In this chunk, auto-advance immediately.
            TransitionTo(GamePhase.DrawPhase);
        }

        /// <summary>
        /// Fires OnDrawPhaseStarted for the active player and advances to the next player's turn or
        /// EndOfRound if all turns are complete. Rule 505.
        /// </summary>
        private void EnterDrawPhase()
        {
            // Fire event so the draw system (later chunk) can perform
            // the actual draw. The draw procedure is non-trivial
            // (empty deck reshuffle, Namara card handling). Rule 505, 604.7.
            OnDrawPhaseStarted?.Invoke(_state.ActivePlayerID);

            _turnContext.AdvanceToNextTurn();

            if (_turnContext.AllTurnsComplete)
                TransitionTo(GamePhase.EndOfRound);
            else
            {
                _state.ActivePlayerID = _turnContext.ActivePlayerID;
                TransitionTo(GamePhase.StartOfTurn);
            }
        }

        /// <summary>
        /// Performs end-of-round cleanup in the SpiritPoolSystem, fires OnRoundEnded,
        /// and advances to BeginRound for the next round. Rule 506.
        /// </summary>
        private void EnterEndOfRound()
        {
            SpiritPoolSystem.EndRoundCleanup(_state);
            OnRoundEnded?.Invoke(_state.RoundNumber);
            TransitionTo(GamePhase.BeginRound);
        }
        #endregion

        // ── Timer ─────────────────────────────────────────────────

        /// <summary>
        /// Handles the spirit selection timer expiring. Auto-assigns spirits for any players
        /// who haven't selected and advances to RevealSpirits. Client rule: timer-based selection.
        /// </summary>
        private void HandleSelectionTimeout()
        {
            foreach (var player in _state.Players)
            {
                if (player == null) continue;
                if (player.SelectedSpiritID == -1)
                    SpiritPoolSystem.AutoAssignSpirit(_state, player.PlayerID, _rng);
            }
            TransitionTo(GamePhase.RevealSpirits);
        }
    }
}
