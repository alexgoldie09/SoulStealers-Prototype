using System;
using System.Collections.Generic;

namespace SSR.Logic
{
    /// <summary>
    /// Executes EffectData objects against GameState.
    /// The single entry point is Resolve(). Every call:
    ///   1. Checks the Cannot rule (rule 103.2).
    ///   2. Validates targets are still legal (rule 707 Step 3).
    ///   3. Dispatches to the typed resolver.
    ///   4. Fires events for subscribing systems.
    ///
    /// Does NOT manage the Resolution Pile — that is the Resolution
    /// Stack's responsibility (next chunk).
    /// Does NOT check win/loss — that is the Win/Loss Checker.
    /// Does NOT expire durations — that is the Duration Expiry System.
    /// Rule 700–814.
    /// </summary>
    public static class EffectResolver
    {
        // ── Events ────────────────────────────────────────────────
        // Fired after every resolution attempt.
        public static event Action<EffectData, EffectResolutionResult> OnEffectProcessed;

        // Fired when a player's soul total changes.
        // Args: playerID, oldValue, newValue.
        public static event Action<int, int, int> OnSoulsChanged;

        #region Entry Point
        /// <summary>
        /// Resolves an effect against the current game state.
        /// Returns Resolved, Fizzled, Blocked, or NotImplemented.
        /// </summary>
        public static EffectResolutionResult Resolve(
            EffectData effect, GameState state)
        {
            if (effect == null)
                return EffectResolutionResult.Fizzled;

            if (effect.Status == EffectStatus.Silenced)
                return Fire(effect, EffectResolutionResult.Fizzled);

            if (IsBlockedByCannot(effect, state))
                return Fire(effect, EffectResolutionResult.Blocked);

            if (!ValidateTargets(effect, state))
                return Fire(effect, EffectResolutionResult.Fizzled);

            var result = effect.EffectType switch
            {
                EffectType.Steal => ResolveSteal((StealEffectData)effect, state),
                EffectType.Banish => ResolveBanish((BanishEffectData)effect, state),
                EffectType.GiveSouls => ResolveGiveSouls((GiveSoulsEffectData)effect, state),
                EffectType.Destroy => ResolveDestroy((DestroyEffectData)effect, state),
                EffectType.Discard => ResolveDiscard((DiscardEffectData)effect, state),
                EffectType.Recall => ResolveRecall((RecallEffectData)effect, state),
                EffectType.Silence => ResolveSilence((SilenceEffectData)effect, state),
                EffectType.Counter => ResolveCounter((CounterEffectData)effect, state),

                // Static effects are always-active; no pile resolution needed.
                EffectType.Defense => EffectResolutionResult.Resolved,
                EffectType.Indestructible => EffectResolutionResult.Resolved,
                EffectType.Ignore => EffectResolutionResult.Resolved,

                // Pact is a play condition — checked before playing, not resolved.
                EffectType.Pact => EffectResolutionResult.Resolved,

                // Modifiers are evaluated inline by ApplyModifiers(); they do not
                // independently enter the pile in the current card set.
                EffectType.Modifier => EffectResolutionResult.Resolved,
                
                EffectType.Negate => ResolveNegate((NegateEffectData)effect, state),
                EffectType.Merge => ResolveMerge((MergeEffectData)effect, state),

                // Conspiracy window is opened by ResolutionStack before effects run;
                // if this branch is reached the effect is silenced or skipped.
                EffectType.Conspiracy => EffectResolutionResult.Resolved,
                EffectType.SpecialPlay => EffectResolutionResult.NotImplemented,
                EffectType.Copy => EffectResolutionResult.NotImplemented,
                _ => EffectResolutionResult.NotImplemented
            };

            return Fire(effect, result);
        }
        #endregion

        #region Cannot Rule
        /// <summary>
        /// Returns true if a static "cannot" effect blocks this event.
        /// Cannot effects take precedence over all other effects. Rule 103.2.
        /// Events that cannot happen cannot be replaced. Rule 103.2.
        /// </summary>
        private static bool IsBlockedByCannot(EffectData effect, GameState state)
        {
            switch (effect.EffectType)
            {
                case EffectType.Destroy:
                {
                    var e = (DestroyEffectData)effect;
                    var target = state.GetCard(e.TargetCardID);
                    if (target == null) return false;
                    // Spirits cannot be Destroyed. Rule 301.3.
                    if (target.CurrentType == CardType.Spirit) return true;
                    // INDESTRUCTIBLE prevents Destruction unless Silenced. Rule 810.
                    if (target.IsIndestructible && !target.IsSilenced) return true;
                    return false;
                }

                case EffectType.Silence:
                {
                    var e = (SilenceEffectData)effect;
                    var target = state.GetCard(e.TargetCardID);
                    if (target == null) return false;
                    // Spirits cannot be Silenced. Rule 301.3.
                    return target.CurrentType == CardType.Spirit;
                }

                case EffectType.Steal:
                case EffectType.Banish:
                {
                    // No explicit "cannot be Stolen/Banished" effects exist in the
                    // current card set. Reserved for future expansion.
                    return false;
                }

                // All other effects have no "cannot" conditions in the current card set. Rule 103.2.
                case EffectType.GiveSouls:
                case EffectType.Defense:
                case EffectType.Modifier:
                case EffectType.Merge:
                case EffectType.Negate:
                case EffectType.Ignore:
                case EffectType.Conspiracy:
                case EffectType.SpecialPlay:
                case EffectType.Pact:
                case EffectType.Recall:
                case EffectType.Discard:
                case EffectType.Indestructible:
                case EffectType.Copy:
                case EffectType.Counter:
                default:
                    return false;
            }
        }
        #endregion

        #region Target Validation
        /// <summary>
        /// Verifies all declared targets are still legal at resolution time.
        /// A target is illegal if it left the zone it was in when targeted,
        /// or a game-state change made it ineligible. Rule 707 Step 3.
        /// If all targets are illegal, the effect Fizzles. Rule 707.5.
        /// </summary>
        private static bool ValidateTargets(EffectData effect, GameState state)
        {
            switch (effect.EffectType)
            {
                case EffectType.Destroy:
                {
                    var e = (DestroyEffectData)effect;
                    if (e.TargetCardID == -1) return false;
                    var target = state.GetCard(e.TargetCardID);
                    // Target must still be on the Field. Rule 813.
                    return target != null
                        && (target.Location == CardLocation.IncantationZone
                         || target.Location == CardLocation.SorceryZone
                         || target.Location == CardLocation.SpiritZone);
                }

                case EffectType.Counter:
                {
                    var e = (CounterEffectData)effect;
                    if (e.TargetCardID == -1) return false;
                    var target = state.GetCard(e.TargetCardID);
                    // Counters can only be on Field cards. Rule 107.2.
                    return target != null
                        && (target.Location == CardLocation.IncantationZone
                         || target.Location == CardLocation.SorceryZone);
                }

                case EffectType.Silence:
                {
                    var e = (SilenceEffectData)effect;
                    if (e.TargetCardID == -1) return false;
                    var target = state.GetCard(e.TargetCardID);
                    // Target must be on the Field. Rule 808.
                    return target != null
                        && (target.Location == CardLocation.IncantationZone
                         || target.Location == CardLocation.SorceryZone);
                }

                case EffectType.Steal:
                case EffectType.Banish:
                case EffectType.GiveSouls:
                {
                    // Target must be a valid player.
                    int targetPlayerID = effect.TargetIDs.Count > 0
                        ? effect.TargetIDs[0] : -1;
                    return targetPlayerID != -1
                        && state.GetPlayer(targetPlayerID) != null;
                }

                case EffectType.Discard:
                {
                    var e = (DiscardEffectData)effect;
                    if (e.TargetCardID == -1) return false;
                    var target = state.GetCard(e.TargetCardID);
                    return target != null
                        && target.Location == (e.SourceZone switch
                        {
                            ZoneType.Hand            => CardLocation.Hand,
                            ZoneType.IncantationZone => CardLocation.IncantationZone,
                            ZoneType.SorceryZone     => CardLocation.SorceryZone,
                            _                        => CardLocation.Hand
                        });
                }

                case EffectType.Recall:
                {
                    var e = (RecallEffectData)effect;
                    var player = state.GetPlayer(e.TargetPlayerID);
                    return player != null;
                }
                
                case EffectType.Negate:
                {
                    var e = (NegateEffectData)effect;
                    if (e.TargetOnPileID == -1) return false;
                    if (e.TargetsCard)
                    {
                        var target = state.GetCard(e.TargetOnPileID);
                        return target != null
                               && target.Location == CardLocation.ResolutionPile;
                    }
                    return state.PileObjectRegistry.ContainsKey(e.TargetOnPileID);
                }

                case EffectType.Merge:
                {
                    var e = (MergeEffectData)effect;
                    if (e.TargetIncantationID == -1) return false;
                    var host = state.GetCard(e.TargetIncantationID);
                    return host != null
                           && host.Location == CardLocation.IncantationZone;
                }

                // Static effects and pile-window effects have no targets to validate here.
                case EffectType.Defense:
                case EffectType.Modifier:
                case EffectType.Ignore:
                case EffectType.Conspiracy:
                case EffectType.SpecialPlay:
                case EffectType.Pact:
                case EffectType.Indestructible:
                case EffectType.Copy:
                default:
                    return true;
            }
        }
        #endregion

        #region Defense Calculator
        /// <summary>
        /// Sums the total DEFENSE value for the target player from all
        /// non-Silenced Rituals, Prayers, and Spirits currently on their
        /// Field. Multiple DEFENSE values are cumulative. Rule 804.
        /// Returns 0 if no Defense is active.
        /// </summary>
        public static int CalculateTotalDefense(
            GameState state, int targetPlayerID)
        {
            var total = 0;
            var player = state.GetPlayer(targetPlayerID);
            if (player == null) return 0;

            // Spirit zone (e.g. Uzilda with DEFENSE 2).
            var spirit = state.GetCard(player.SpiritZone.SpiritID);
            if (spirit != null && !spirit.IsSilenced)
                total += SumDefenseFromCard(spirit);

            // Incantation zone (Rituals/Prayers with DEFENSE keyword).
            foreach (int id in player.IncantationZone.CardIDs)
            {
                var card = state.GetCard(id);
                if (card == null || card.IsSilenced) continue;
                total += SumDefenseFromCard(card);

                // Attached cards contribute their effects to the host. Rule 805.5.
                foreach (int attachedID in card.AttachedCardIDs)
                {
                    var attached = state.GetCard(attachedID);
                    if (attached != null)
                        total += SumDefenseFromCard(attached);
                }
            }

            return total;
        }

        /// <summary>
        /// Sums the DEFENSE values from a single card's active, non-Silenced effects.
        /// </summary>
        /// <param name="card"></param>
        /// <returns></returns>
        private static int SumDefenseFromCard(RuntimeCard card)
        {
            var sum = 0;
            foreach (var e in card.Effects)
                if (e is DefenseEffectData def && e.Status != EffectStatus.Silenced)
                    sum += def.BaseValue;
            return sum;
        }
        #endregion

        #region Modifier Calculator
        /// <summary>
        /// Applies all active Modifier effects relevant to the given
        /// effect type and controller to a base value.
        /// Only Symbolic and X values can be modified. Rule 803.
        /// WordForm values are returned unchanged.
        /// Result is clamped to 0 minimum for non-Modifier values. Rule 104.3.
        /// </summary>
        public static int ApplyModifiers(
            GameState state,
            int baseValue,
            EffectType targetEffectType,
            int controllerID,
            int sourceCardID,
            NumericValueType valueType)
        {
            // WordForm values cannot be modified. Rule 803.
            if (valueType == NumericValueType.WordForm)
                return baseValue;

            var total = baseValue;
            var sourceCard = state.GetCard(sourceCardID);

            foreach (var player in state.Players)
            {
                if (player == null) continue;

                // Spirit zone.
                var spirit = state.GetCard(player.SpiritZone.SpiritID);
                if (spirit != null && !spirit.IsSilenced)
                    total = ApplyCardModifiers(
                        total, spirit, targetEffectType,
                        controllerID, sourceCard, state);

                // Incantation zone.
                foreach (int id in player.IncantationZone.CardIDs)
                {
                    var card = state.GetCard(id);
                    if (card == null || card.IsSilenced) continue;
                    total = ApplyCardModifiers(
                        total, card, targetEffectType,
                        controllerID, sourceCard, state);

                    foreach (int attachedID in card.AttachedCardIDs)
                    {
                        var attached = state.GetCard(attachedID);
                        if (attached != null)
                            total = ApplyCardModifiers(
                                total, attached, targetEffectType,
                                controllerID, sourceCard, state);
                    }
                }
            }

            // Clamp negative to 0 for numeric values. Rule 104.3.
            return Math.Max(0, total);
        }

        /// <summary>
        /// Applies relevant ModifierEffectData from a single card to the value.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="modifierCard"></param>
        /// <param name="targetEffectType"></param>
        /// <param name="controllerID"></param>
        /// <param name="sourceCard"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        private static int ApplyCardModifiers(
            int value,
            RuntimeCard modifierCard,
            EffectType targetEffectType,
            int controllerID,
            RuntimeCard sourceCard,
            GameState state)
        {
            foreach (var e in modifierCard.Effects)
            {
                if (e is not ModifierEffectData mod) continue;
                if (e.Status == EffectStatus.Silenced) continue;
                if (!mod.ModifiedEffectTypes.Contains(targetEffectType)) continue;

                // ControllerOnly: only applies to the controller's effects.
                if (mod.ControllerOnly && mod.ControllerID != controllerID)
                    continue;

                // Source card type restriction (e.g. "stealing spells get +1").
                if (mod.SourceCardTypeRestriction.HasValue
                    && sourceCard != null
                    && sourceCard.CurrentType != mod.SourceCardTypeRestriction.Value)
                    continue;

                value += mod.IsPositive ? mod.BaseValue : -mod.BaseValue;
            }
            return value;
        }
        #endregion

        #region Soul Effect Resolvers
        /// <summary>
        /// Resolves a Steal effect, applying Modifiers and Defense as appropriate,
        /// and transferring Souls between the target and controller.
        /// </summary>
        /// <param name="effect"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        private static EffectResolutionResult ResolveSteal(
            StealEffectData effect, GameState state)
        {
            var controller = state.GetPlayer(effect.ControllerID);
            var target = state.GetPlayer(effect.TargetPlayerID);
            if (controller == null || target == null)
                return EffectResolutionResult.Fizzled;

            // Apply Modifiers to the base value. Rule 803.
            var modified = ApplyModifiers(
                state, effect.EffectiveBaseValue, EffectType.Steal,
                effect.ControllerID, effect.SourceCardID, effect.ValueType);

            // Apply Defense (unless source card Ignores Defense). Rule 804, 807.
            var sourceCard = state.GetCard(effect.SourceCardID);
            var ignoresDef = sourceCard != null && sourceCard.HasIgnoreDefense();
            var defense = ignoresDef ? 0
                : CalculateTotalDefense(state, effect.TargetPlayerID);

            var netValue = Math.Max(0, modified - defense);
            if (netValue <= 0) return EffectResolutionResult.Resolved;

            var oldTargetSouls = target.Souls;
            var oldControllerSouls = controller.Souls;

            target.Souls = Math.Max(0, target.Souls - netValue);
            controller.Souls += netValue;

            OnSoulsChanged?.Invoke(target.PlayerID, oldTargetSouls, target.Souls);
            OnSoulsChanged?.Invoke(controller.PlayerID, oldControllerSouls, controller.Souls);

            return EffectResolutionResult.Resolved;
        }

        /// <summary>
        /// Resolves a Banish effect, applying Modifiers and Defense as appropriate,
        /// and reducing the target's Souls.
        /// </summary>
        /// <param name="effect"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        private static EffectResolutionResult ResolveBanish(
            BanishEffectData effect, GameState state)
        {
            var target = state.GetPlayer(effect.TargetPlayerID);
            if (target == null) return EffectResolutionResult.Fizzled;

            var modified = ApplyModifiers(
                state, effect.EffectiveBaseValue, EffectType.Banish,
                effect.ControllerID, effect.SourceCardID, effect.ValueType);

            var sourceCard = state.GetCard(effect.SourceCardID);
            var ignoresDef = sourceCard != null && sourceCard.HasIgnoreDefense();
            var defense = ignoresDef ? 0
                : CalculateTotalDefense(state, effect.TargetPlayerID);

            var netValue = Math.Max(0, modified - defense);
            if (netValue <= 0) return EffectResolutionResult.Resolved;

            var oldSouls = target.Souls;
            target.Souls = Math.Max(0, target.Souls - netValue);

            OnSoulsChanged?.Invoke(target.PlayerID, oldSouls, target.Souls);
            return EffectResolutionResult.Resolved;
        }

        /// <summary>
        /// Resolves a Give Souls effect, applying Modifiers but ignoring Defense,
        /// and transferring Souls between the target and controller according to the effect's IsImposed flag.
        /// </summary>
        /// <param name="effect"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        private static EffectResolutionResult ResolveGiveSouls(
            GiveSoulsEffectData effect, GameState state)
        {
            var controller = state.GetPlayer(effect.ControllerID);
            var target = state.GetPlayer(effect.TargetPlayerID);
            if (controller == null || target == null)
                return EffectResolutionResult.Fizzled;

            // Give Souls ignores Defense and has no dedicated Modifiers. Rule 802.
            var value = Math.Max(0, effect.EffectiveBaseValue);
            if (value <= 0) return EffectResolutionResult.Resolved;

            if (effect.IsImposed)
            {
                // "Give X of your Souls" — controller loses, target gains.
                var oldController = controller.Souls;
                var oldTarget = target.Souls;
                controller.Souls = Math.Max(0, controller.Souls - value);
                target.Souls += value;
                OnSoulsChanged?.Invoke(controller.PlayerID, oldController, controller.Souls);
                OnSoulsChanged?.Invoke(target.PlayerID, oldTarget, target.Souls);
            }
            else
            {
                // "An opponent gives you X Souls" — target loses, controller gains.
                var oldTarget = target.Souls;
                var oldController = controller.Souls;
                target.Souls = Math.Max(0, target.Souls - value);
                controller.Souls += value;
                OnSoulsChanged?.Invoke(target.PlayerID, oldTarget, target.Souls);
                OnSoulsChanged?.Invoke(controller.PlayerID, oldController, controller.Souls);
            }

            return EffectResolutionResult.Resolved;
        }
        #endregion

        #region Card Movement Resolvers
        /// <summary>
        /// Resolves a Destroy effect by moving the target card to its owner's Discard pile,
        /// along with any cards attached to it. Rule 805.6, 813.
        /// </summary>
        /// <param name="effect"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        private static EffectResolutionResult ResolveDestroy(
            DestroyEffectData effect, GameState state)
        {
            var target = state.GetCard(effect.TargetCardID);
            if (target == null) return EffectResolutionResult.Fizzled;

            // Handle attached cards first — they go to their owners' Discards.
            // Rule 805.6, 813.
            var attachedIDs = new List<int>(target.AttachedCardIDs);
            foreach (int attachedID in attachedIDs)
            {
                var attached = state.GetCard(attachedID);
                if (attached == null) continue;
                ZoneMover.MoveCard(state, attachedID, CardLocation.DiscardPile);
            }

            ZoneMover.MoveCard(state, effect.TargetCardID, CardLocation.DiscardPile);
            return EffectResolutionResult.Resolved;
        }
        
        /// <summary>
        /// Resolves a Discard effect by moving the target card from the specified source zone to its owner's Discard pile.
        /// </summary>
        /// <param name="effect"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        private static EffectResolutionResult ResolveDiscard(
            DiscardEffectData effect, GameState state)
        {
            var target = state.GetCard(effect.TargetCardID);
            if (target == null) return EffectResolutionResult.Fizzled;

            ZoneMover.MoveCard(state, effect.TargetCardID, CardLocation.DiscardPile);
            return EffectResolutionResult.Resolved;
        }

        /// <summary>
        /// Resolves a Recall effect by moving the specified number of cards from the source zone to the target player's hand,
        /// using specific card IDs if provided, or taking from the top of the source zone otherwise. Rule 808.
        /// </summary>
        /// <param name="effect"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        private static EffectResolutionResult ResolveRecall(
            RecallEffectData effect, GameState state)
        {
            var targetPlayer = state.GetPlayer(effect.TargetPlayerID);
            if (targetPlayer == null) return EffectResolutionResult.Fizzled;

            // Use specific card IDs if named by the effect.
            if (effect.SpecificCardIDs.Count > 0)
            {
                foreach (var id in effect.SpecificCardIDs)
                    ZoneMover.MoveCard(state, id, CardLocation.Hand, effect.TargetPlayerID);
                return EffectResolutionResult.Resolved;
            }

            // Otherwise take from the top of the source zone.
            var sourceZone = GetZoneForType(targetPlayer, effect.SourceZone);
            if (sourceZone == null || sourceZone.Count == 0)
                return EffectResolutionResult.Fizzled;

            var taken = 0;
            while (taken < effect.Count && sourceZone.Count > 0)
            {
                var topID = sourceZone.CardIDs[0];
                ZoneMover.MoveCard(state, topID, CardLocation.Hand,
                    effect.TargetPlayerID);
                taken++;
            }

            return taken > 0
                ? EffectResolutionResult.Resolved
                : EffectResolutionResult.Fizzled;
        }
        #endregion

        #region Status Effect Resolvers
        /// <summary>
        /// Resolves a Silence effect by setting the target card's IsSilenced flag to true,
        /// removing its text and preventing it from triggering or activating. Rule 808.
        /// </summary>
        /// <param name="effect"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        private static EffectResolutionResult ResolveSilence(
            SilenceEffectData effect, GameState state)
        {
            var target = state.GetCard(effect.TargetCardID);
            if (target == null) return EffectResolutionResult.Fizzled;

            target.IsSilenced = true;

            // Silencing removes INDESTRUCTIBLE. Rule 810.
            if (target.IsIndestructible)
                target.IsIndestructible = false;

            // Register duration for the Duration Expiry System. Rule 808.
            var duration = new ActiveDuration(
                effectOwnerID: effect.ControllerID,
                sourceCardID: effect.SourceCardID,
                affectedCardID: effect.TargetCardID,
                effectType: EffectType.Silence,
                timing: effect.Duration,
                appliedOnRound: state.RoundNumber,
                appliedOnTurnOfPlayerID: state.ActivePlayerID);

            state.ActiveDurations.Add(duration);

            return EffectResolutionResult.Resolved;
        }

        /// <summary>
        /// Resolves a Counter effect by modifying the target card's CounterCount according to the effect's IsAddition flag and BaseValue,
        /// ensuring that CounterCount does not drop below 0. Rule 107.2.
        /// </summary>
        /// <param name="effect"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        private static EffectResolutionResult ResolveCounter(
            CounterEffectData effect, GameState state)
        {
            var target = state.GetCard(effect.TargetCardID);
            if (target == null) return EffectResolutionResult.Fizzled;

            if (effect.IsAddition)
                target.CounterCount += effect.BaseValue;
            else
                target.CounterCount = Math.Max(0,
                    target.CounterCount - effect.BaseValue);

            return EffectResolutionResult.Resolved;
        }
        #endregion
        
        #region Pile Effect Resolvers
        /// <summary>
        /// Resolves a Negate effect by removing the target card or effect object
        /// from the Resolution Pile before it resolves.
        /// Negated cards go to their owner's Discard; negated effect objects cease
        /// to exist. Rule 806.
        /// </summary>
        private static EffectResolutionResult ResolveNegate(
            NegateEffectData effect, GameState state)
        {
            var targetID = effect.TargetOnPileID;
            if (targetID == -1) return EffectResolutionResult.Fizzled;

            if (effect.TargetsCard)
            {
                var target = state.GetCard(targetID);
                if (target == null || target.Location != CardLocation.ResolutionPile)
                    return EffectResolutionResult.Fizzled;

                // Type restriction: only cards of the specified type can be targeted. Rule 806.3.
                if (effect.TypeRestriction.HasValue
                    && target.CurrentType != effect.TypeRestriction.Value)
                    return EffectResolutionResult.Fizzled;

                ZoneMover.MoveCard(state, targetID, CardLocation.DiscardPile);
                return EffectResolutionResult.Resolved;
            }
            else
            {
                if (!state.PileObjectRegistry.ContainsKey(targetID))
                    return EffectResolutionResult.Fizzled;

                if (!state.ResolutionPile.Contains(targetID))
                    return EffectResolutionResult.Fizzled;

                // Effect object ceases to exist — remove from pile and registry.
                state.ResolutionPile.Remove(targetID);
                state.PileObjectRegistry.Remove(targetID);
                return EffectResolutionResult.Resolved;
            }
        }

        /// <summary>
        /// Resolves a Merge effect by attaching the source Incantation to a host
        /// Incantation already on the Field. The merged card loses its type and name;
        /// its effects are added to the host. Rule 805.
        /// </summary>
        private static EffectResolutionResult ResolveMerge(
            MergeEffectData effect, GameState state)
        {
            var sourceCard = state.GetCard(effect.SourceCardID);
            var host = state.GetCard(effect.TargetIncantationID);

            if (sourceCard == null || host == null)
                return EffectResolutionResult.Fizzled;

            if (host.Location != CardLocation.IncantationZone)
                return EffectResolutionResult.Fizzled;

            // Maximum two attached cards per host. Rule 805.3.
            if (host.AttachedCardIDs.Count >= 2)
                return EffectResolutionResult.Fizzled;

            // Ritual MERGE targets only cards under the controller's control. Rule 805.
            if (effect.SourceCardType == CardType.Ritual
                && host.ControllerID != sourceCard.ControllerID)
                return EffectResolutionResult.Fizzled;

            host.AttachedCardIDs.Add(sourceCard.ID);
            sourceCard.OnAttach(host.ID);
            ZoneMover.MoveCard(state, sourceCard.ID, CardLocation.Attached);
            return EffectResolutionResult.Resolved;
        }
        #endregion

        #region Helpers
        /// <summary>
        /// Fires the OnEffectProcessed event after an effect is processed, allowing subscribing systems to react to the resolution outcome.
        /// </summary>
        /// <param name="effect"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private static EffectResolutionResult Fire(
            EffectData effect, EffectResolutionResult result)
        {
            OnEffectProcessed?.Invoke(effect, result);
            return result;
        }

        /// <summary>
        /// Helper method to get the appropriate zone for a player based on the specified ZoneType.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="zoneType"></param>
        /// <returns></returns>
        private static ZoneBase GetZoneForType(
            PlayerState player, ZoneType zoneType)
        {
            return zoneType switch
            {
                ZoneType.DiscardPile => player.DiscardPile,
                ZoneType.Hand => player.Hand,
                ZoneType.IncantationZone => player.IncantationZone,
                ZoneType.SorceryZone => player.SorceryZone,
                ZoneType.MainDeck => player.MainDeck,
                _ => null
            };
        }
        #endregion
    }
}
