using UnityEngine;
using SSR.Logic;
using TMPro;
using UnityEngine.UI;

namespace SSR.Visual
{
    public class GameController : MonoBehaviour
    {
        // ── Systems ───────────────────────────────────────────────
        private GameState _state;
        private RoundStateMachine _rsm;
        private ResolutionStack _stack;
        private TriggerSystem _triggerSystem;

        // ── UI State ──────────────────────────────────────────────
        private UIState _uiState = UIState.Idle;
        private int _selectedHandCardID = -1;
        private int _pendingTargetCardID = -1;
        private EffectData _pendingTargetEffect;

        // ── Serialized UI References ──────────────────────────────
        [SerializeField] private Transform _p1HandContainer;
        [SerializeField] private Transform _p2HandContainer;
        [SerializeField] private Transform _p1FieldContainer;
        [SerializeField] private Transform _p2FieldContainer;
        [SerializeField] private Transform _spiritSelectionContainer;
        [SerializeField] private GameObject _spiritSelectionPanel;
        [SerializeField] private TextMeshProUGUI _p1SoulsText;
        [SerializeField] private TextMeshProUGUI _p2SoulsText;
        [SerializeField] private TextMeshProUGUI _p1SpiritText;
        [SerializeField] private TextMeshProUGUI _p2SpiritText;
        [SerializeField] private TextMeshProUGUI _pileCountText;
        [SerializeField] private TextMeshProUGUI _logText;
        [SerializeField] private TextMeshProUGUI _statusText;
        [SerializeField] private Button _passPriorityButton;
        [SerializeField] private Button _endTurnButton;
        [SerializeField] private Button _playCardButton;
        [SerializeField] private ScrollRect _logScrollRect;

        // ── Card Panel Prefab ─────────────────────────────────────
        [SerializeField] private GameObject _cardPanelPrefab;

        // ── Stored delegates for static event cleanup ─────────────
        private System.Action<int, int, int> _onSoulsChanged;
        private System.Action<int, CardLocation, CardLocation> _onCardMoved;
        private System.Action<int, int> _onSpiritSelected;

        // ── P2 Auto-turn ──────────────────────────────────────────
        private float _p2ActionDelay = 1f;
        private float _p2ActionTimer = 0f;
        private bool _p2WaitingToEndTurn = false;

        private const int P1 = 1;
        private const int P2 = 2;

        // ── Unity Lifecycle ───────────────────────────────────────

        private void Start()
        {
            _state = new GameState
            {
                Players =
                {
                    [0] = new PlayerState { PlayerID = P1, Souls = 30 },
                    [1] = new PlayerState { PlayerID = P2, Souls = 30 }
                }
            };

            LoadSpiritCards();
            LoadPlayerDecks();

            _rsm = new RoundStateMachine(_state);
            _stack = new ResolutionStack(_state);
            _triggerSystem = new TriggerSystem(_state, _rsm, _stack);

            SubscribeToEvents();

            _passPriorityButton.onClick.AddListener(OnPassPriorityClicked);
            _endTurnButton.onClick.AddListener(OnEndTurnClicked);
            _playCardButton.onClick.AddListener(OnPlayCardClicked);

            UpdateButtonStates();
            _spiritSelectionPanel.SetActive(false);

            _rsm.StartGame();
        }

        private void Update()
        {
            _rsm.Tick(Time.deltaTime);
            HandleP2AutoTurn();
            HandleEscapeDeselect();
        }

        private void OnDestroy()
        {
            _triggerSystem?.Dispose();
            _stack?.Dispose();
            _rsm?.Dispose();

            EffectResolver.OnSoulsChanged -= _onSoulsChanged;
            ZoneMover.OnCardMoved -= _onCardMoved;
            SpiritPoolSystem.OnSpiritSelected -= _onSpiritSelected;
        }

        // ── Data Loading ──────────────────────────────────────────

        private void LoadSpiritCards()
        {
            var spiritAssets = Resources.LoadAll<SSR.Data.CardData>("Spirits");
            foreach (var asset in spiritAssets)
            {
                var card = SSR.Data.CardFactory.CreateRuntimeCard(asset, ownerID: 0);
                _state.CardRegistry[card.ID] = card;
                _state.SpiritPool.AvailablePool.Add(card.ID);
            }
            AppendLog($"[Setup] Loaded {spiritAssets.Length} spirit cards.");
        }

        private void LoadPlayerDecks()
        {
            var p1Cards = Resources.LoadAll<SSR.Data.CardData>("Cards/P1");
            foreach (var asset in p1Cards)
            {
                var card = SSR.Data.CardFactory.CreateRuntimeCard(asset, ownerID: P1);
                card.Location = CardLocation.Hand;
                _state.CardRegistry[card.ID] = card;
                _state.GetPlayer(P1).Hand.Add(card.ID);
            }

            var p2Cards = Resources.LoadAll<SSR.Data.CardData>("Cards/P2");
            foreach (var asset in p2Cards)
            {
                var card = SSR.Data.CardFactory.CreateRuntimeCard(asset, ownerID: P2);
                card.Location = CardLocation.Hand;
                _state.CardRegistry[card.ID] = card;
                _state.GetPlayer(P2).Hand.Add(card.ID);
            }

            AppendLog($"[Setup] P1 hand: {_state.GetPlayer(P1).Hand.Count} cards, " +
                      $"P2 hand: {_state.GetPlayer(P2).Hand.Count} cards.");
        }

        // ── Update Helpers ────────────────────────────────────────

        private void HandleP2AutoTurn()
        {
            if (!_p2WaitingToEndTurn) return;
            _p2ActionTimer -= Time.deltaTime;
            if (_p2ActionTimer <= 0f)
            {
                _p2WaitingToEndTurn = false;
                if (_state.CurrentPhase == GamePhase.ActionPhase &&
                    _state.ActivePlayerID == P2)
                {
                    _rsm.EndTurn(P2);
                }
            }
        }

        private void HandleEscapeDeselect()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                DeselectCard();
        }

        // ── UI State Management ───────────────────────────────────

        private void SetUIState(UIState newState)
        {
            _uiState = newState;
            UpdateButtonStates();
            UpdateStatusText();
            RefreshHandUI();
        }

        private void UpdateButtonStates()
        {
            bool isP1Turn = _state.ActivePlayerID == P1;
            bool pileEmpty = _state.ResolutionPile.IsEmpty;
            bool inActionPhase = _state.CurrentPhase == GamePhase.ActionPhase;

            bool canPass = _stack.IsWaitingForPriority && _stack.PriorityPlayerID == P1;
            _passPriorityButton.interactable = canPass;

            bool canEndTurn = inActionPhase && isP1Turn && pileEmpty;
            _endTurnButton.interactable = canEndTurn;

            bool canPlay = _uiState == UIState.CardSelected
                           && inActionPhase
                           && isP1Turn
                           && pileEmpty
                           && _state.GetPlayer(P1)?.ActionsRemaining > 0;
            _playCardButton.interactable = canPlay;
        }

        private void UpdateStatusText()
        {
            if (_statusText == null) return;
            _statusText.text = _uiState switch
            {
                UIState.Idle => $"Phase: {_state.CurrentPhase} | Player {_state.ActivePlayerID}'s turn",
                UIState.CardSelected => "Card selected. Click Play or press Escape to cancel.",
                UIState.AwaitingTarget => "Click a card on the field to target it. Escape to cancel.",
                UIState.WaitingForPriority => "Waiting for priority to pass...",
                UIState.SpiritSelection => "Choose a spirit for this round.",
                UIState.P2Turn => "P2 is taking their turn...",
                _ => ""
            };
        }

        // ── Hand UI ───────────────────────────────────────────────

        private void RefreshHandUI()
        {
            RefreshZoneUI(_p1HandContainer, _state.GetPlayer(P1).Hand.CardIDs,
                isP1: true, faceDown: false);
            RefreshZoneUI(_p2HandContainer, _state.GetPlayer(P2).Hand.CardIDs,
                isP1: false, faceDown: true);
        }

        private void RefreshZoneUI(Transform container,
            System.Collections.Generic.IReadOnlyList<int> cardIDs,
            bool isP1, bool faceDown)
        {
            if (container == null) return;
            foreach (Transform child in container)
                Destroy(child.gameObject);

            foreach (int id in cardIDs)
            {
                var card = _state.GetCard(id);
                if (card == null) continue;

                var panel = Instantiate(_cardPanelPrefab, container);
                var display = panel.GetComponent<CardPanelDisplay>();
                display.Setup(card, faceDown);

                if (isP1 && !faceDown)
                {
                    bool isSelected = id == _selectedHandCardID;
                    display.SetHighlight(isSelected);
                    int capturedID = id;
                    display.SetClickAction(() => OnHandCardClicked(capturedID));
                }
            }
        }

        // ── Field UI ──────────────────────────────────────────────

        private void RefreshFieldUI()
        {
            RefreshFieldZone(_p1FieldContainer, P1);
            RefreshFieldZone(_p2FieldContainer, P2);
        }

        private void RefreshFieldZone(Transform container, int playerID)
        {
            if (container == null) return;
            foreach (Transform child in container)
                Destroy(child.gameObject);

            var player = _state.GetPlayer(playerID);
            if (player == null) return;

            foreach (int id in player.IncantationZone.CardIDs)
                AddFieldCardPanel(container, id, playerID);

            foreach (int id in player.SorceryZone.CardIDs)
            {
                var card = _state.GetCard(id);
                bool faceDown = card?.FaceState == CardFaceState.FaceDown;
                AddFieldCardPanel(container, id, playerID, faceDown);
            }
        }

        private void AddFieldCardPanel(Transform container, int cardID,
            int ownerPlayerID, bool faceDown = false)
        {
            var card = _state.GetCard(cardID);
            if (card == null) return;

            var panel = Instantiate(_cardPanelPrefab, container);
            var display = panel.GetComponent<CardPanelDisplay>();
            display.Setup(card, faceDown);
            int capturedID = cardID;
            display.SetClickAction(() => OnFieldCardClicked(capturedID));
        }

        // ── Click Handlers ────────────────────────────────────────

        private void OnHandCardClicked(int cardID)
        {
            if (_uiState == UIState.P2Turn) return;
            if (_state.CurrentPhase != GamePhase.ActionPhase) return;
            if (_state.ActivePlayerID != P1) return;

            if (_selectedHandCardID == cardID)
            {
                DeselectCard();
                return;
            }

            _selectedHandCardID = cardID;
            SetUIState(UIState.CardSelected);
            AppendLog($"[Input] Selected: {_state.GetCard(cardID)?.CurrentName}");
        }

        private void OnFieldCardClicked(int cardID)
        {
            if (_uiState == UIState.AwaitingTarget)
                TrySetTarget(cardID);
        }

        private void OnPlayCardClicked()
        {
            if (_uiState != UIState.CardSelected) return;
            if (_selectedHandCardID == -1) return;

            var card = _state.GetCard(_selectedHandCardID);
            if (card == null) return;

            if (NeedsFieldTarget(card, out var targetingEffect))
            {
                _pendingTargetCardID = _selectedHandCardID;
                _pendingTargetEffect = targetingEffect;
                SetUIState(UIState.AwaitingTarget);
                AppendLog("[Input] Choose a target on the field.");
                return;
            }

            CommitPlay(_selectedHandCardID, PlayType.NormalPlay);
        }

        private void TrySetTarget(int fieldCardID)
        {
            if (_pendingTargetEffect == null) return;

            var targetCard = _state.GetCard(fieldCardID);
            if (targetCard == null || !IsValidTarget(targetCard, _pendingTargetEffect))
            {
                AppendLog("[Input] Invalid target. Play cancelled.");
                CancelPlay();
                return;
            }

            _pendingTargetEffect.TargetIDs.Clear();
            _pendingTargetEffect.TargetIDs.Add(fieldCardID);
            AppendLog($"[Input] Target set: {targetCard.CurrentName}");
            CommitPlay(_pendingTargetCardID, PlayType.NormalPlay);
        }

        private void CommitPlay(int cardID, PlayType playType)
        {
            ActionSystem.SpendAction(_state, P1);
            _stack.PlaceCard(cardID, P1, playType);
            _selectedHandCardID = -1;
            _pendingTargetCardID = -1;
            _pendingTargetEffect = null;
            SetUIState(UIState.WaitingForPriority);
            AutoPassIfP2();
        }

        private void CancelPlay()
        {
            _pendingTargetCardID = -1;
            _pendingTargetEffect = null;
            _selectedHandCardID = -1;
            SetUIState(UIState.Idle);
        }

        private void DeselectCard()
        {
            _selectedHandCardID = -1;
            if (_uiState == UIState.CardSelected || _uiState == UIState.AwaitingTarget)
                SetUIState(UIState.Idle);
        }

        private void OnPassPriorityClicked()
        {
            if (!_stack.IsWaitingForPriority) return;
            if (_stack.PriorityPlayerID != P1) return;
            _stack.PassPriority(P1);
            UpdateButtonStates();
        }

        private void OnEndTurnClicked()
        {
            if (_state.ActivePlayerID != P1) return;
            _rsm.EndTurn(P1);
        }

        // ── Targeting Helpers ─────────────────────────────────────

        private bool NeedsFieldTarget(RuntimeCard card, out EffectData targetingEffect)
        {
            foreach (var effect in card.Effects)
            {
                if (effect is DestroyEffectData destroy && destroy.TargetIDs.Count == 0)
                {
                    targetingEffect = destroy;
                    return true;
                }
                if (effect is SilenceEffectData silence && silence.TargetIDs.Count == 0)
                {
                    targetingEffect = silence;
                    return true;
                }
                if (effect is CounterEffectData counter && counter.TargetIDs.Count == 0)
                {
                    targetingEffect = counter;
                    return true;
                }
            }
            targetingEffect = null;
            return false;
        }

        private bool IsValidTarget(RuntimeCard target, EffectData effect)
        {
            if (effect is DestroyEffectData)
                return target.Location == CardLocation.IncantationZone
                    || target.Location == CardLocation.SorceryZone
                    || target.Location == CardLocation.SpiritZone;

            if (effect is SilenceEffectData)
                return (target.Location == CardLocation.IncantationZone
                    || target.Location == CardLocation.SorceryZone)
                    && target.CurrentType != CardType.Spirit;

            if (effect is CounterEffectData)
                return target.Location == CardLocation.IncantationZone
                    || target.Location == CardLocation.SorceryZone;

            return false;
        }

        // ── Spirit Selection UI ───────────────────────────────────

        private void ShowSpiritSelectionPanel()
        {
            _spiritSelectionPanel.SetActive(true);
            SetUIState(UIState.SpiritSelection);

            foreach (Transform child in _spiritSelectionContainer)
                Destroy(child.gameObject);

            var p1 = _state.GetPlayer(P1);
            foreach (int spiritID in p1.DealtSpiritIDs)
            {
                var card = _state.GetCard(spiritID);
                if (card == null) continue;

                var panel = Instantiate(_cardPanelPrefab, _spiritSelectionContainer);
                var display = panel.GetComponent<CardPanelDisplay>();
                display.Setup(card, faceDown: false);
                int capturedID = spiritID;
                display.SetClickAction(() => OnSpiritSelected(capturedID));
            }
        }

        private void OnSpiritSelected(int spiritID)
        {
            if (_state.CurrentPhase != GamePhase.SpiritSelection) return;

            bool valid = _rsm.SelectSpirit(P1, spiritID);
            if (!valid) return;

            AppendLog($"[Input] P1 selected spirit: {_state.GetCard(spiritID)?.CurrentName}");

            var p2 = _state.GetPlayer(P2);
            if (p2.SelectedSpiritID == -1 && p2.DealtSpiritIDs.Count > 0)
            {
                _rsm.SelectSpirit(P2, p2.DealtSpiritIDs[0]);
                AppendLog("[Input] P2 auto-selected spirit.");
            }

            _spiritSelectionPanel.SetActive(false);
            SetUIState(UIState.Idle);
        }

        // ── P2 Auto-pass & Auto-turn ──────────────────────────────

        private void AutoPassIfP2()
        {
            StartCoroutine(AutoPassCoroutine());
        }

        private System.Collections.IEnumerator AutoPassCoroutine()
        {
            yield return null;
            while (_stack.IsWaitingForPriority && _stack.PriorityPlayerID == P2)
            {
                _stack.PassPriority(P2);
                yield return null;
            }
            UpdateButtonStates();
            RefreshAllUI();
        }

        private void StartP2Turn()
        {
            SetUIState(UIState.P2Turn);
            _p2WaitingToEndTurn = true;
            _p2ActionTimer = _p2ActionDelay;
            AppendLog($"[P2] Taking turn... (auto-ending in {_p2ActionDelay}s)");
        }

        // ── Event Subscriptions ───────────────────────────────────

        private void SubscribeToEvents()
        {
            _rsm.OnRoundStarted += n => AppendLog($"=== ROUND {n} ===");

            _rsm.OnPhaseEntered += (phase, pid) =>
            {
                AppendLog($"Phase: {phase}");
                if (phase == GamePhase.SpiritSelection)
                    ShowSpiritSelectionPanel();
                UpdateButtonStates();
                UpdateStatusText();
            };

            _rsm.OnTurnStarted += pid =>
            {
                AppendLog($"--- Player {pid} Turn Start ---");
                if (pid == P2)
                    StartP2Turn();
                else
                    SetUIState(UIState.Idle);
            };

            _rsm.OnActionPhaseStarted += pid =>
            {
                AppendLog($"Player {pid} Action Phase (actions={_state.GetPlayer(pid)?.ActionsRemaining})");
                RefreshAllUI();
                UpdateButtonStates();
            };

            _rsm.OnTurnEnded += pid =>
            {
                AppendLog($"Player {pid} Turn End");
                _p2WaitingToEndTurn = false;
            };

            _rsm.OnDrawPhaseStarted += pid =>
                AppendLog($"Player {pid} Draw Phase (stub — draw system not yet implemented)");

            _rsm.OnRoundEnded += n =>
            {
                AppendLog($"Round {n} ended");
                RefreshSpiritDisplay();
            };

            _rsm.OnSpiritsRevealed += gs =>
            {
                AppendLog("Spirits revealed!");
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
                    AppendLog($"  P{player.PlayerID} spirit: {spirit.CurrentName} (rank {spirit.SpiritRank})");
                }
                RefreshSpiritDisplay();
            };

            _stack.OnCardPlaced += id =>
            {
                AppendLog($"Card placed on pile: {_state.GetCard(id)?.CurrentName ?? id.ToString()}");
                RefreshAllUI();
                UpdateButtonStates();
                AutoPassIfP2();
            };

            _stack.OnPriorityOpened += pid =>
            {
                AppendLog($"Priority: Player {pid}");
                UpdateButtonStates();
            };

            _stack.OnItemResolved += id =>
            {
                AppendLog($"Resolved: {id}");
                RefreshAllUI();
                UpdateButtonStates();
            };

            _stack.OnPileEmpty += () =>
            {
                AppendLog("Pile empty.");
                UpdatePileCountUI();
                if (_uiState == UIState.WaitingForPriority)
                    SetUIState(UIState.Idle);
                UpdateButtonStates();
            };

            _stack.OnCardEnteredField += id =>
            {
                AppendLog($"Entered field: {_state.GetCard(id)?.CurrentName}");
                RefreshFieldUI();
            };

            _stack.OnCardNegated += id =>
                AppendLog($"Negated: {id}");

            _onSoulsChanged = (pid, oldV, newV) =>
            {
                AppendLog($"Player {pid} souls: {oldV} → {newV}");
                RefreshSoulUI();
            };
            EffectResolver.OnSoulsChanged += _onSoulsChanged;

            _onCardMoved = (id, from, to) =>
            {
                AppendLog($"Card {id} ({_state.GetCard(id)?.CurrentName}): {from} → {to}");
                RefreshAllUI();
            };
            ZoneMover.OnCardMoved += _onCardMoved;

            _onSpiritSelected = (pid, sid) =>
                AppendLog($"Player {pid} selected spirit {_state.GetCard(sid)?.CurrentName}");
            SpiritPoolSystem.OnSpiritSelected += _onSpiritSelected;
        }

        // ── UI Refresh Helpers ────────────────────────────────────

        private void RefreshAllUI()
        {
            RefreshHandUI();
            RefreshFieldUI();
            RefreshSoulUI();
            RefreshSpiritDisplay();
            UpdatePileCountUI();
        }

        private void RefreshSoulUI()
        {
            if (_p1SoulsText != null)
                _p1SoulsText.text = $"P1 Souls: {_state.GetPlayer(P1)?.Souls ?? 0}";
            if (_p2SoulsText != null)
                _p2SoulsText.text = $"P2 Souls: {_state.GetPlayer(P2)?.Souls ?? 0}";
        }

        private void RefreshSpiritDisplay()
        {
            var p1Spirit = _state.GetCard(_state.GetPlayer(P1)?.SpiritZone.SpiritID ?? -1);
            var p2Spirit = _state.GetCard(_state.GetPlayer(P2)?.SpiritZone.SpiritID ?? -1);

            if (_p1SpiritText != null)
                _p1SpiritText.text = p1Spirit != null
                    ? $"Spirit: {p1Spirit.CurrentName} ({p1Spirit.SpiritRank})"
                    : "Spirit: None";

            if (_p2SpiritText != null)
                _p2SpiritText.text = p2Spirit != null
                    ? $"Spirit: {p2Spirit.CurrentName} ({p2Spirit.SpiritRank})"
                    : "Spirit: None";
        }

        private void UpdatePileCountUI()
        {
            if (_pileCountText != null)
                _pileCountText.text = $"Pile: {_state.ResolutionPile.CardIDs.Count}";
        }

        private void AppendLog(string msg)
        {
            Debug.Log(msg);
            if (_logText != null)
            {
                _logText.text += msg + "\n";
                Canvas.ForceUpdateCanvases();
                if (_logScrollRect != null)
                    _logScrollRect.verticalNormalizedPosition = 0f;
            }
        }

        // ── Spirit Effects Builder ────────────────────────────────

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
                        TypeRestriction = null,
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

                    var triggerGoros = new TriggerEffectData
                    {
                        Timing = TriggerTiming.BeginningOfTurn,
                        TriggeredEffect = destroy,
                        OnlyOnOwnerTurn = true,
                        ControllerID = ownerID,
                        SourceCardID = spirit.ID
                    };
                    spirit.Effects.Add(triggerGoros);
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
                    var triggerTorouk = new TriggerEffectData
                    {
                        Timing = TriggerTiming.BeginningOfTurn,
                        TriggeredEffect = recall,
                        OnlyOnOwnerTurn = true,
                        ControllerID = ownerID,
                        SourceCardID = spirit.ID
                    };
                    spirit.Effects.Add(triggerTorouk);
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
                    var triggerTrish = new TriggerEffectData
                    {
                        Timing = TriggerTiming.BeginningOfTurn,
                        TriggeredEffect = silence,
                        OnlyOnOwnerTurn = true,
                        ControllerID = ownerID,
                        SourceCardID = spirit.ID
                    };
                    spirit.Effects.Add(triggerTrish);
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
                    var triggerMolok = new TriggerEffectData
                    {
                        Timing = TriggerTiming.BeginningOfTurn,
                        TriggeredEffect = giveSouls,
                        OnlyOnOwnerTurn = true,
                        ControllerID = ownerID,
                        SourceCardID = spirit.ID
                    };
                    spirit.Effects.Add(triggerMolok);
                    break;
                }

                default:
                    Debug.Log($"[TriggerSetup] {spirit.CurrentName} (rank {spirit.SpiritRank}): BoT trigger is a stub.");
                    break;
            }
        }
    }
}
