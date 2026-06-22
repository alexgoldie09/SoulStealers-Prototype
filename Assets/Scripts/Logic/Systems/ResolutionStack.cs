using System;
using System.Collections.Generic;

namespace SSR.Logic
{
    /// <summary>
    /// Manages the Resolution Pile priority loop and LIFO resolution order.
    /// Owns the priority window (who may respond and when), Conspiracy
    /// put windows, and dispatches each item to EffectResolver.
    ///
    /// Call PlaceCard or PlaceEffectObject to push an item, then drive
    /// the priority loop with PassPriority until the pile is empty.
    /// Rule 405, 708, 303.9.
    /// </summary>
    public class ResolutionStack : IDisposable
    {
        // ── Private State ─────────────────────────────────────────
        private readonly GameState _state;
        private int _lastActingPlayerID;
        private int[] _priorityOrder;
        private int _priorityIndex;
        private int _consecutivePasses;
        private int _conspiracySecretID;
        private int _conspiracyController;

        // ── Public Properties ─────────────────────────────────────
        public bool IsWaitingForPriority { get; private set; }
        public bool IsWaitingForConspiracyPut { get; private set; }

        /// <summary>
        /// The player who currently holds priority, or -1 if none.
        /// </summary>
        public int PriorityPlayerID =>
            IsWaitingForPriority
            && _priorityOrder != null
            && _priorityIndex < _priorityOrder.Length
                ? _priorityOrder[_priorityIndex]
                : -1;

        // ── Events ────────────────────────────────────────────────
        public event Action<int> OnCardPlaced;              // cardID
        public event Action<int> OnEffectObjectPlaced;      // pileObjectID
        public event Action<int> OnPriorityOpened;          // playerID
        public event Action<int> OnPriorityPassed;          // playerID
        public event Action<int> OnItemResolved;            // cardID or pileObjectID
        public event Action OnPileEmpty;
        public event Action<int> OnCardEnteredField;        // cardID
        public event Action<int> OnCardNegated;             // negated card or object ID
        public event Action<int> OnConspiracyWindowOpened;  // controllerID

        // ── Constructor ───────────────────────────────────────────
        public ResolutionStack(GameState state)
        {
            _state = state;
            _conspiracySecretID = -1;
            _conspiracyController = -1;
        }

        #region Public API
        /// <summary>
        /// Places a card on the Resolution Pile and opens the priority window.
        /// Sets IsPlayed and LastPlayType on the card.
        /// </summary>
        public bool PlaceCard(int cardID, int actingPlayerID, PlayType playType)
        {
            var card = _state.GetCard(cardID);
            if (card == null) return false;

            card.IsPlayed = true;
            card.LastPlayType = playType;
            ZoneMover.MoveCard(_state, cardID, CardLocation.ResolutionPile);

            _lastActingPlayerID = actingPlayerID;
            _consecutivePasses = 0;

            OnCardPlaced?.Invoke(cardID);
            OpenPriorityWindow();
            return true;
        }

        /// <summary>
        /// Places an effect object on the pile and opens the priority window.
        /// The object is registered in PileObjectRegistry.
        /// </summary>
        public bool PlaceEffectObject(PileObject pileObject)
        {
            if (pileObject == null) return false;

            _state.PileObjectRegistry[pileObject.ID] = pileObject;
            _state.ResolutionPile.Push(pileObject.ID);

            _lastActingPlayerID = pileObject.ControllerID;
            _consecutivePasses = 0;

            OnEffectObjectPlaced?.Invoke(pileObject.ID);
            OpenPriorityWindow();
            return true;
        }

        /// <summary>
        /// The current priority holder passes. When all players have consecutively
        /// passed, the top item resolves.
        /// </summary>
        public bool PassPriority(int playerID)
        {
            if (!IsWaitingForPriority) return false;
            if (_priorityOrder == null || _priorityIndex >= _priorityOrder.Length) return false;
            if (_priorityOrder[_priorityIndex] != playerID) return false;

            OnPriorityPassed?.Invoke(playerID);
            _consecutivePasses++;

            if (_consecutivePasses >= _priorityOrder.Length)
            {
                ResolveTop();
            }
            else
            {
                _priorityIndex = (_priorityIndex + 1) % _priorityOrder.Length;
                OnPriorityOpened?.Invoke(_priorityOrder[_priorityIndex]);
            }

            return true;
        }

        /// <summary>
        /// Reveals a face-down Sorcery in the SorceryZone and places it on the pile.
        /// Rule 602.3.
        /// </summary>
        public bool RevealSecret(int cardID, int controllerID)
        {
            var card = _state.GetCard(cardID);
            if (card == null) return false;
            if (card.Location != CardLocation.SorceryZone) return false;
            if (card.FaceState != CardFaceState.FaceDown) return false;
            if (card.ControllerID != controllerID) return false;

            card.FaceState = CardFaceState.FaceUp;
            PlaceCard(cardID, controllerID, PlayType.Reveal);
            return true;
        }

        /// <summary>
        /// During a Conspiracy window, the controller Puts a face-down Sorcery
        /// from their hand onto their SorceryZone. The Put bypasses the pile
        /// and cannot be Negated. Rule 303.9, 604.4.
        /// </summary>
        public bool ConspiracyPut(int cardID, int controllerID)
        {
            if (!IsWaitingForConspiracyPut) return false;
            if (controllerID != _conspiracyController) return false;

            var card = _state.GetCard(cardID);
            if (card == null) return false;

            var controller = _state.GetPlayer(controllerID);
            if (controller == null) return false;
            if (!controller.Hand.Contains(cardID)) return false;
            if (card.CurrentSuperType != CardSuperType.Sorcery) return false;

            card.FaceState = CardFaceState.FaceDown;
            card.IsPlayed = true;
            card.LastPlayType = PlayType.Put;
            ZoneMover.MoveCard(_state, cardID, CardLocation.SorceryZone);

            ContinueConspiracyResolution();
            return true;
        }

        /// <summary>
        /// During a Conspiracy window, the controller skips the Put. Rule 303.9.
        /// </summary>
        public bool ConspiracySkip(int controllerID)
        {
            if (!IsWaitingForConspiracyPut) return false;
            if (controllerID != _conspiracyController) return false;

            ContinueConspiracyResolution();
            return true;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Builds the priority order array based on the last acting player ID.
        /// The acting player goes last, and opponents are ordered by ascending Spirit Rank.
        /// </summary>
        /// <param name="actingPlayerID"></param>
        private void BuildPriorityOrder(int actingPlayerID)
        {
            // Opponents first, ascending Spirit Rank. Acting player last. Rule 708.
            var opponents = new List<int>();
            for (int i = 0; i < _state.Players.Length; i++)
            {
                var p = _state.Players[i];
                if (p == null || p.PlayerID == actingPlayerID) continue;
                opponents.Add(p.PlayerID);
            }

            // Insertion sort by Spirit Rank ascending (lists are small).
            for (int i = 1; i < opponents.Count; i++)
            {
                var key = opponents[i];
                var keyRank = GetSpiritRank(key);
                var j = i - 1;
                while (j >= 0 && GetSpiritRank(opponents[j]) > keyRank)
                {
                    opponents[j + 1] = opponents[j];
                    j--;
                }
                opponents[j + 1] = key;
            }

            _priorityOrder = new int[opponents.Count + 1];
            for (int i = 0; i < opponents.Count; i++)
                _priorityOrder[i] = opponents[i];
            _priorityOrder[opponents.Count] = actingPlayerID;
        }

        /// <summary>
        /// Helper method to get a player's Spirit Rank for priority ordering.
        /// </summary>
        /// <param name="playerID"></param>
        /// <returns></returns>
        private int GetSpiritRank(int playerID)
        {
            var player = _state.GetPlayer(playerID);
            if (player == null) return 0;
            var spirit = _state.GetCard(player.SpiritZone.SpiritID);
            return spirit != null ? spirit.SpiritRank : 0;
        }

        /// <summary>
        /// Opens the priority window for the current top item on the pile, starting with the last acting player.
        /// </summary>
        private void OpenPriorityWindow()
        {
            BuildPriorityOrder(_lastActingPlayerID);
            _priorityIndex = 0;
            IsWaitingForPriority = true;
            OnPriorityOpened?.Invoke(_priorityOrder[0]);
        }

        /// <summary>
        /// Resolves the top item on the pile.
        /// If it's a card, resolves its effects in order.
        /// If it's an effect object, resolves it directly.
        /// </summary>
        private void ResolveTop()
        {
            IsWaitingForPriority = false;

            if (_state.ResolutionPile.IsEmpty)
            {
                OnPileEmpty?.Invoke();
                return;
            }

            var topID = _state.ResolutionPile.Peek();

            // Determine whether the top item is a card or an effect object.
            RuntimeCard card;
            if (_state.CardRegistry.TryGetValue(topID, out card)
                && card.Location == CardLocation.ResolutionPile)
            {
                ResolveCard(card);
            }
            else if (_state.PileObjectRegistry.ContainsKey(topID))
            {
                ResolvePileObject(_state.PileObjectRegistry[topID]);
            }
        }

        /// <summary>
        /// Resolves a card's effects in order. If skipConspiracy is false and an un-silenced Conspiracy effect is found,
        /// the window opens and resolution suspends; ContinueConspiracyResolution will call this again with skipConspiracy = true.
        /// </summary>
        /// <param name="card"></param>
        private void ResolveCard(RuntimeCard card) => ResolveEffects(card, skipConspiracy: false);
    

        /// <summary>
        /// Iterates the card's effects and resolves them.
        /// If skipConspiracy is false and an un-silenced Conspiracy effect is found,
        /// the window opens and resolution suspends; ContinueConspiracyResolution
        /// will call this again with skipConspiracy = true.
        /// </summary>
        private void ResolveEffects(RuntimeCard card, bool skipConspiracy)
        {
            if (!card.IsSilenced)
            {
                var fizzledIndices = new HashSet<int>();

                foreach (var effect in card.Effects)
                {
                    if (effect is ConspiracyEffectData)
                    {
                        if (!skipConspiracy && effect.Status != EffectStatus.Silenced)
                        {
                            _conspiracySecretID = card.ID;
                            _conspiracyController = card.ControllerID;
                            IsWaitingForConspiracyPut = true;
                            OnConspiracyWindowOpened?.Invoke(_conspiracyController);
                            return;
                        }
                        continue;
                    }
                    
                    // Linked dependency check
                    if (effect.Dependency == EffectDependency.Linked
                        && fizzledIndices.Contains(effect.LinkedToPrecedingEffectIndex))
                    {
                        fizzledIndices.Add(effect.PrintedEffectIndex);
                        OnItemResolved?.Invoke(card.ID);  // fires Fizzled signal
                        continue;
                    }

                    var result = EffectResolver.Resolve(effect, _state);
                    
                    if (result == EffectResolutionResult.Fizzled || result == EffectResolutionResult.Blocked)
                        fizzledIndices.Add(effect.PrintedEffectIndex);
                    
                    if (effect.EffectType == EffectType.Negate
                        && result == EffectResolutionResult.Resolved)
                    {
                        var negate = (NegateEffectData)effect;
                        OnCardNegated?.Invoke(negate.TargetOnPileID);
                    }
                }
            }

            // Effects resolved (or card silenced — all Fizzle implicitly).
            CardResolutionHandler.HandleAfterResolution(_state, card);

            if (card.HasEnteredField)
                OnCardEnteredField?.Invoke(card.ID);

            OnItemResolved?.Invoke(card.ID);
            AfterItemResolved();
        }

        /// <summary>
        /// Resolves an effect object directly. If it's a Negate effect that successfully resolves, raises OnCardNegated for the target.
        /// </summary>
        /// <param name="pileObject"></param>
        private void ResolvePileObject(PileObject pileObject)
        {
            _state.ResolutionPile.Pop();
            _state.PileObjectRegistry.Remove(pileObject.ID);

            var result = EffectResolver.Resolve(pileObject.Effect, _state);
            if (pileObject.Effect.EffectType == EffectType.Negate
                && result == EffectResolutionResult.Resolved)
            {
                var negate = (NegateEffectData)pileObject.Effect;
                OnCardNegated?.Invoke(negate.TargetOnPileID);
            }

            OnItemResolved?.Invoke(pileObject.ID);
            AfterItemResolved();
        }

        /// <summary>
        /// After an item resolves, resets consecutive passes and either opens a new priority window for the next item
        /// or raises OnPileEmpty if the pile is now empty.
        /// </summary>
        private void ContinueConspiracyResolution()
        {
            var secretID = _conspiracySecretID;
            _conspiracySecretID = -1;
            _conspiracyController = -1;
            IsWaitingForConspiracyPut = false;

            var card = _state.GetCard(secretID);
            if (card == null || card.Location != CardLocation.ResolutionPile)
            {
                AfterItemResolved();
                return;
            }

            ResolveEffects(card, skipConspiracy: true);
        }

        /// <summary>
        /// Resets consecutive passes and opens the next priority window if there are more items on the pile,
        /// or raises OnPileEmpty if the pile is now empty.
        /// </summary>
        private void AfterItemResolved()
        {
            _consecutivePasses = 0;

            if (_state.ResolutionPile.IsEmpty)
            {
                OnPileEmpty?.Invoke();
            }
            else
            {
                OpenPriorityWindow();
            }
        }
        #endregion

        #region IDisposable Support
        /// <summary>
        /// Disposes of the ResolutionStack by clearing all event subscriptions.
        /// Should be called when the stack is no longer needed to prevent memory leaks.
        /// </summary>
        public void Dispose()
        {
            OnCardPlaced = null;
            OnEffectObjectPlaced = null;
            OnPriorityOpened = null;
            OnPriorityPassed = null;
            OnItemResolved = null;
            OnPileEmpty = null;
            OnCardEnteredField = null;
            OnCardNegated = null;
            OnConspiracyWindowOpened = null;
        }
        #endregion
    }
}
