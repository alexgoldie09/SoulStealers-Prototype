using UnityEngine;
using SSR.Logic;
using TMPro;

namespace SSR.Visual
{
    /// <summary>
    /// White-box test scene driver. Wires all Phase 1 systems together
    /// and drives a live game loop via keyboard input.
    ///
    /// Keys:
    ///   P — Pass priority
    ///   E — End turn
    ///   S — Auto-select first available spirit for all players
    ///   Space — Play first card in active player's hand
    /// </summary>
    public class GameController : MonoBehaviour
    {
        // ── On-screen log ─────────────────────────────────────────
        [SerializeField] private TextMeshProUGUI _logText;

        // ── Systems ───────────────────────────────────────────────
        private GameState _state;
        private RoundStateMachine _rsm;
        private ResolutionStack _stack;
        private TriggerSystem _triggerSystem;

        // Player IDs
        private const int P1 = 1;
        private const int P2 = 2;

        // Stored static delegates for safe unsubscription.
        private System.Action<EffectData, EffectResolutionResult> _onEffectProcessed;
        private System.Action<int, int, int> _onSoulsChanged;
        private System.Action<int, CardLocation, CardLocation> _onCardMoved;
        private System.Action<int, int> _onSpiritSelected;
        private System.Action<GameState> _onSpiritsRevealed;

        // ── Unity Lifecycle ───────────────────────────────────────

        private void Start()
        {
            // 1. Create GameState
            _state = new GameState();

            // 2. Populate players
            _state.Players[0] = new PlayerState { PlayerID = P1, Souls = 30 };
            _state.Players[1] = new PlayerState { PlayerID = P2, Souls = 30 };
            _state.ActivePlayerID = P1;

            // 3. Create 16 spirit RuntimeCards, add to pool.
            CreateSpirits();

            // 4. Create test hands for both players.
            CreateTestHands();

            // 5-7. Create systems.
            _rsm = new RoundStateMachine(_state);
            _stack = new ResolutionStack(_state);
            _triggerSystem = new TriggerSystem(_state, _rsm, _stack);

            // 8. Subscribe to all events for logging.
            SubscribeToEvents();

            // 9. Start the game.
            _rsm.StartGame();
        }

        private void Update()
        {
            _rsm.Tick(Time.deltaTime);
            HandleKeyboardInput();
        }

        private void OnDestroy()
        {
            _triggerSystem?.Dispose();
            _stack?.Dispose();
            _rsm?.Dispose();

            EffectResolver.OnEffectProcessed -= _onEffectProcessed;
            EffectResolver.OnSoulsChanged -= _onSoulsChanged;
            ZoneMover.OnCardMoved -= _onCardMoved;
            SpiritPoolSystem.OnSpiritSelected -= _onSpiritSelected;
            if (_rsm != null) _rsm.OnSpiritsRevealed -= _onSpiritsRevealed;
        }

        // ── Input ─────────────────────────────────────────────────

        private void HandleKeyboardInput()
        {
            if (Input.GetKeyDown(KeyCode.P))
            {
                if (_stack.IsWaitingForPriority)
                {
                    var pid = _stack.PriorityPlayerID;
                    AppendLog($"[Input] Player {pid} passes priority.");
                    _stack.PassPriority(pid);
                }
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                AppendLog($"[Input] Player {_state.ActivePlayerID} ends turn.");
                _rsm.EndTurn(_state.ActivePlayerID);
            }

            if (Input.GetKeyDown(KeyCode.S))
            {
                AutoSelectSpirits();
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                AutoPlayFirstCard();
            }
        }

        private void AutoSelectSpirits()
        {
            foreach (var player in _state.Players)
            {
                if (player == null || player.SelectedSpiritID != -1) continue;
                if (player.DealtSpiritIDs.Count == 0) continue;

                int spiritID = player.DealtSpiritIDs[0];
                AppendLog($"[Input] Player {player.PlayerID} selects spirit {spiritID}.");
                _rsm.SelectSpirit(player.PlayerID, spiritID);
            }
        }

        private void AutoPlayFirstCard()
        {
            if (_state.CurrentPhase != GamePhase.ActionPhase) return;
            var player = _state.GetPlayer(_state.ActivePlayerID);
            if (player == null || player.ActionsRemaining <= 0) return;
            if (!_state.ResolutionPile.IsEmpty) return;
            if (player.Hand.Count == 0) return;

            int cardID = player.Hand.CardIDs[0];
            var card = _state.GetCard(cardID);
            if (card == null) return;

            AppendLog($"[Input] Player {_state.ActivePlayerID} plays card {cardID} ({card.CurrentName}).");
            ActionSystem.SpendAction(_state, _state.ActivePlayerID);
            _stack.PlaceCard(cardID, _state.ActivePlayerID, PlayType.NormalPlay);
        }

        // ── Setup Helpers ─────────────────────────────────────────

        private void CreateSpirits()
        {
            // Name, rank pairs for all 16 spirits.
            var spirits = new (string name, int rank)[]
            {
                ("Ulrich",   1),
                ("Freus",    2),
                ("Goros",    3),
                ("Dovos",    4),
                ("Uzilda",   5),
                ("Valaria",  6),
                ("Liria",    7),
                ("Torouk",   8),
                ("Trish",    9),
                ("Heinrich", 10),
                ("Molok",    11),
                ("Nilite",   12),
                ("Valek",    13),
                ("Zull",     14),
                ("Namara",   15),
                ("Tez'Nura", 16)
            };

            foreach (var (name, rank) in spirits)
            {
                var card = new RuntimeCard(
                    cardDataID: name.ToLower(),
                    ownerID: 0,
                    type: CardType.Spirit,
                    superType: CardSuperType.None,
                    cardName: name,
                    spiritRank: rank);

                _state.CardRegistry[card.ID] = card;
                _state.SpiritPool.AvailablePool.Add(card.ID);
            }
        }

        private void CreateTestHands()
        {
            // P1: Steal 3 Spell, Defense 2 Ritual, Negate Secret.
            CreateAndAddToHand(P1, "steal-spell", CardType.Spell, CardSuperType.Sorcery, "Steal 3", MakeSteal3);
            CreateAndAddToHand(P1, "defense-ritual", CardType.Ritual, CardSuperType.Incantation, "Defense 2", MakeDefense2);
            CreateAndAddToHand(P1, "negate-secret", CardType.Secret, CardSuperType.Sorcery, "Negate", MakeNegate);

            // P2: Banish 4 Spell, Steal 2 Spell.
            CreateAndAddToHand(P2, "banish-spell", CardType.Spell, CardSuperType.Sorcery, "Banish 4", MakeBanish4);
            CreateAndAddToHand(P2, "steal2-spell", CardType.Spell, CardSuperType.Sorcery, "Steal 2", MakeSteal2);
        }

        private void CreateAndAddToHand(int playerID, string dataID,
            CardType type, CardSuperType superType, string name,
            System.Action<RuntimeCard, int> effectBuilder)
        {
            var card = new RuntimeCard(dataID, playerID, type, superType, name);
            card.Location = CardLocation.Hand;
            _state.CardRegistry[card.ID] = card;
            _state.GetPlayer(playerID)?.Hand.Add(card.ID);
            effectBuilder?.Invoke(card, playerID);
        }

        private void MakeSteal3(RuntimeCard card, int ownerID)
        {
            var e = new StealEffectData
            {
                BaseValue = 3,
                ValueType = NumericValueType.Symbolic,
                ControllerID = ownerID,
                SourceCardID = card.ID
            };
            e.TargetIDs.Add(ownerID == P1 ? P2 : P1);
            card.Effects.Add(e);
        }

        private void MakeDefense2(RuntimeCard card, int ownerID)
        {
            var e = new DefenseEffectData
            {
                BaseValue = 2,
                ValueType = NumericValueType.Symbolic,
                ControllerID = ownerID,
                SourceCardID = card.ID
            };
            card.Effects.Add(e);
        }

        private void MakeNegate(RuntimeCard card, int ownerID)
        {
            var e = new NegateEffectData
            {
                ControllerID = ownerID,
                SourceCardID = card.ID,
                TargetsCard = true
            };
            card.Effects.Add(e);
        }

        private void MakeBanish4(RuntimeCard card, int ownerID)
        {
            var e = new BanishEffectData
            {
                BaseValue = 4,
                ValueType = NumericValueType.Symbolic,
                ControllerID = ownerID,
                SourceCardID = card.ID
            };
            e.TargetIDs.Add(ownerID == P1 ? P2 : P1);
            card.Effects.Add(e);
        }

        private void MakeSteal2(RuntimeCard card, int ownerID)
        {
            var e = new StealEffectData
            {
                BaseValue = 2,
                ValueType = NumericValueType.Symbolic,
                ControllerID = ownerID,
                SourceCardID = card.ID
            };
            e.TargetIDs.Add(ownerID == P1 ? P2 : P1);
            card.Effects.Add(e);
        }

        /// <summary>
        /// Builds Beginning of Turn TriggerEffectData for a spirit once its
        /// owner is known (called from OnSpiritsRevealed).
        /// </summary>
        private static void BuildSpiritEffects(RuntimeCard spirit, int ownerID, int opponentID)
        {
            spirit.Effects.Clear();
            switch (spirit.SpiritRank)
            {
                case 3: // Goros — May destroy an incantation; if you do, banish 2 souls.
                {
                    var destroy = new DestroyEffectData
                    {
                        IsOptional = true,
                        TypeRestriction = null, // any Incantation
                        ControllerID = ownerID,
                        SourceCardID = spirit.ID,
                        PrintedEffectIndex = 0
                    };

                    var banish = new BanishEffectData
                    {
                        BaseValue = 2,
                        ValueType = NumericValueType.Symbolic,
                        ControllerID = ownerID,
                        SourceCardID = spirit.ID,
                        PrintedEffectIndex = 1,
                        Dependency = EffectDependency.Linked,
                        LinkedToPrecedingEffectIndex = 0
                    };
                    banish.TargetIDs.Add(opponentID);

                    // Auto-accept optional — trigger packages both effects.
                    var trigger = new TriggerEffectData
                    {
                        Timing = TriggerTiming.BeginningOfTurn,
                        TriggeredEffect = destroy,
                        OnlyOnOwnerTurn = true,
                        ControllerID = ownerID,
                        SourceCardID = spirit.ID
                    };
                    spirit.Effects.Add(trigger);
                    break;
                }

                case 8: // Torouk — Recall all cards from field to controllers' hands.
                {
                    var recall = new RecallEffectData
                    {
                        SourceZone = ZoneType.IncantationZone,
                        Count = 99,
                        ControllerID = ownerID,
                        SourceCardID = spirit.ID
                    };
                    recall.TargetIDs.Add(ownerID);
                    var trigger = new TriggerEffectData
                    {
                        Timing = TriggerTiming.BeginningOfTurn,
                        TriggeredEffect = recall,
                        OnlyOnOwnerTurn = true,
                        ControllerID = ownerID,
                        SourceCardID = spirit.ID
                    };
                    spirit.Effects.Add(trigger);
                    break;
                }

                case 9: // Trish — May silence a card until next turn.
                {
                    var silence = new SilenceEffectData
                    {
                        Duration = EffectDurationTiming.UntilNextTurn,
                        IsOptional = true,
                        ControllerID = ownerID,
                        SourceCardID = spirit.ID
                    };
                    var trigger = new TriggerEffectData
                    {
                        Timing = TriggerTiming.BeginningOfTurn,
                        TriggeredEffect = silence,
                        OnlyOnOwnerTurn = true,
                        ControllerID = ownerID,
                        SourceCardID = spirit.ID
                    };
                    spirit.Effects.Add(trigger);
                    break;
                }

                case 11: // Molok — An opponent of your choice gives you 2 souls.
                {
                    var giveSouls = new GiveSoulsEffectData
                    {
                        BaseValue = 2,
                        ValueType = NumericValueType.Symbolic,
                        IsImposed = false,
                        ControllerID = ownerID,
                        SourceCardID = spirit.ID
                    };
                    giveSouls.TargetIDs.Add(opponentID);
                    var trigger = new TriggerEffectData
                    {
                        Timing = TriggerTiming.BeginningOfTurn,
                        TriggeredEffect = giveSouls,
                        OnlyOnOwnerTurn = true,
                        ControllerID = ownerID,
                        SourceCardID = spirit.ID
                    };
                    spirit.Effects.Add(trigger);
                    break;
                }

                default:
                    // Stub for spirits not yet fully implemented.
                    Debug.Log($"[TriggerSetup] {spirit.CurrentName} (rank {spirit.SpiritRank}): BoT trigger is a stub.");
                    break;
            }
        }

        // ── Event Subscriptions ───────────────────────────────────

        private void SubscribeToEvents()
        {
            _rsm.OnRoundStarted += n => AppendLog($"=== ROUND {n} STARTED ===");
            _rsm.OnPhaseEntered += (phase, pid) => AppendLog($"Phase: {phase} (Player {pid})");
            _rsm.OnTurnStarted += pid => AppendLog($"--- Player {pid} Turn Start ---");
            _rsm.OnActionPhaseStarted += pid =>
            {
                var p = _state.GetPlayer(pid);
                AppendLog($"Player {pid} Action Phase (actions={p?.ActionsRemaining ?? 0})");
            };
            _rsm.OnTurnEnded += pid => AppendLog($"Player {pid} Turn End");
            _rsm.OnDrawPhaseStarted += pid => AppendLog($"Player {pid} Draw Phase (stub)");
            _rsm.OnRoundEnded += n => AppendLog($"Round {n} ended");

            _onSpiritsRevealed = gs =>
            {
                foreach (var player in gs.Players)
                {
                    if (player == null || player.SelectedSpiritID == -1) continue;
                    var spirit = gs.GetCard(player.SelectedSpiritID);
                    if (spirit == null) continue;

                    spirit.OwnerID = player.PlayerID;
                    spirit.ControllerID = player.PlayerID;
                    spirit.Location = CardLocation.SpiritZone;
                    player.SpiritZone.Add(player.SelectedSpiritID);

                    int opponentID = player.PlayerID == P1 ? P2 : P1;
                    BuildSpiritEffects(spirit, player.PlayerID, opponentID);
                    Debug.Log($"[Setup] Spirit {spirit.CurrentName} (rank {spirit.SpiritRank}) assigned to P{player.PlayerID}.");
                }

                var order = gs.SpiritPool.AvailablePool; // turn order logged via TurnContext
                AppendLog($"Spirits revealed! (see Debug.Log for spirit details)");
            };
            _rsm.OnSpiritsRevealed += _onSpiritsRevealed;

            _stack.OnCardPlaced += id => AppendLog($"Card placed on pile: {id}");
            _stack.OnPriorityOpened += pid => AppendLog($"Priority: Player {pid}");
            _stack.OnItemResolved += id => AppendLog($"Resolved: {id}");
            _stack.OnPileEmpty += () => AppendLog("Pile empty");
            _stack.OnCardEnteredField += id => AppendLog($"Card entered field: {id}");
            _stack.OnCardNegated += id => AppendLog($"Card negated: {id}");

            _onEffectProcessed = (e, r) =>
                Debug.Log($"[Effect] {e.EffectType} → {r}");
            EffectResolver.OnEffectProcessed += _onEffectProcessed;

            _onSoulsChanged = (pid, oldV, newV) =>
                AppendLog($"Player {pid} souls: {oldV} → {newV}");
            EffectResolver.OnSoulsChanged += _onSoulsChanged;

            _onCardMoved = (id, from, to) =>
                AppendLog($"Card {id}: {from} → {to}");
            ZoneMover.OnCardMoved += _onCardMoved;

            _onSpiritSelected = (pid, sid) =>
                AppendLog($"Player {pid} selected spirit {sid}");
            SpiritPoolSystem.OnSpiritSelected += _onSpiritSelected;
        }

        // ── Utility ───────────────────────────────────────────────

        private void AppendLog(string msg)
        {
            Debug.Log(msg);
            if (_logText != null)
                _logText.text += msg + "\n";
        }
    }
}
