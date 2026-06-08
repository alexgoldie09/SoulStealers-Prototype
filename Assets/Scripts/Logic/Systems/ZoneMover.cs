using System;

namespace SSR.Logic
{
    /// <summary>
    /// Moves RuntimeCards between zones in GameState.
    /// Updates the zone objects, the card's Location property,
    /// and calls the appropriate lifecycle hooks on the card.
    ///
    /// All card movement in the rules engine should go through
    /// ZoneMover rather than directly manipulating zone objects,
    /// to ensure Location and lifecycle state stay in sync.
    /// </summary>
    public static class ZoneMover
    {
        // Fired when a card successfully moves between locations.
        // Args: cardID, fromLocation, toLocation.
        public static event Action<int, CardLocation, CardLocation> OnCardMoved;

        /// <summary>
        /// Moves a card to the destination zone, updating all relevant
        /// state. Returns false if the card is not found. The destination
        /// zone is determined by the card's ControllerID for field zones
        /// and OwnerID for Discard and Deck zones per the rules.
        ///
        /// For Discard: always goes to owner's pile. Rule 404.
        /// For Hand: goes to the controller's hand unless overridden.
        /// </summary>
        public static bool MoveCard(
            GameState state,
            int cardID,
            CardLocation destination,
            int? overridePlayerID = null)
        {
            var card = state.GetCard(cardID);
            if (card == null) return false;

            var from = card.Location;

            RemoveFromCurrentZone(state, card);
            AddToDestinationZone(state, card, destination, overridePlayerID);

            card.Location = destination;
            OnCardMoved?.Invoke(cardID, from, destination);
            return true;
        }

        #region Remove
        /// <summary>
        /// Removes the card from its current zone, updating the appropriate player zone and card properties.
        /// </summary>
        /// <param name="state"></param>
        /// <param name="card"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private static void RemoveFromCurrentZone(GameState state, RuntimeCard card)
        {
            switch (card.Location)
            {
                case CardLocation.MainDeck:
                    state.GetPlayer(card.OwnerID)?.MainDeck.Remove(card.ID);
                    break;

                case CardLocation.Hand:
                    state.GetPlayer(card.ControllerID)?.Hand.Remove(card.ID);
                    break;

                case CardLocation.DiscardPile:
                    state.GetPlayer(card.OwnerID)?.DiscardPile.Remove(card.ID);
                    break;

                case CardLocation.SpiritZone:
                    state.GetPlayer(card.ControllerID)?.SpiritZone.Remove(card.ID);
                    break;

                case CardLocation.IncantationZone:
                    state.GetPlayer(card.ControllerID)?.IncantationZone.Remove(card.ID);
                    card.OnLeaveField();
                    break;

                case CardLocation.SorceryZone:
                    state.GetPlayer(card.ControllerID)?.SorceryZone.Remove(card.ID);
                    card.OnLeaveField();
                    break;

                case CardLocation.ResolutionPile:
                    if (state.ResolutionPile.Contains(card.ID))
                        state.ResolutionPile.Remove(card.ID);
                    break;

                case CardLocation.Attached:
                    // Remove from host's AttachedCardIDs list.
                    foreach (var kvp in state.CardRegistry)
                    {
                        if (kvp.Value.AttachedCardIDs.Contains(card.ID))
                        {
                            kvp.Value.AttachedCardIDs.Remove(card.ID);
                            break;
                        }
                    }
                    card.IsAttached = false;
                    card.AttachedHostID = -1;
                    break;

                case CardLocation.SideGame:
                    // Side game objects are managed externally.
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        #endregion

        #region Add
        /// <summary>
        /// Adds the card to the destination zone, updating the appropriate player zone and card properties.
        /// The destination zone is determined by the card's ControllerID for field zones and OwnerID for Discard and Deck zones per the rules.
        /// For Discard, always goes to owner's pile. Rule 404.
        /// For Hand, goes to the controller's hand unless overridden by overridePlayerID.
        /// For MainDeck, always goes to owner's deck. Rule 404.
        /// For IncantationZone, goes to the controller's incantation zone and sets the card as existing on the field.
        /// For SorceryZone, goes to the controller's sorcery zone and sets the card as existing on the field.
        /// For SpiritZone, goes to the controller's spirit zone.
        /// For ResolutionPile, goes to the shared resolution pile.
        /// </summary>
        /// <param name="state"></param>
        /// <param name="card"></param>
        /// <param name="destination"></param>
        /// <param name="overridePlayerID"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private static void AddToDestinationZone(
            GameState state,
            RuntimeCard card,
            CardLocation destination,
            int? overridePlayerID)
        {
            // Discard always goes to owner. Rule 404.
            var ownerID = card.OwnerID;
            // Hand and deck go to controller unless overridden.
            var controllerID = overridePlayerID ?? card.ControllerID;

            switch (destination)
            {
                case CardLocation.DiscardPile:
                    state.GetPlayer(ownerID)?.DiscardPile.AddToTop(card.ID);
                    break;

                case CardLocation.Hand:
                    state.GetPlayer(controllerID)?.Hand.Add(card.ID);
                    break;

                case CardLocation.MainDeck:
                    state.GetPlayer(ownerID)?.MainDeck.Add(card.ID);
                    break;

                case CardLocation.IncantationZone:
                    state.GetPlayer(controllerID)?.IncantationZone.Add(card.ID);
                    card.HasEnteredField = true;
                    break;

                case CardLocation.SorceryZone:
                    state.GetPlayer(controllerID)?.SorceryZone.Add(card.ID);
                    card.HasEnteredField = true;
                    break;

                case CardLocation.SpiritZone:
                    state.GetPlayer(controllerID)?.SpiritZone.Add(card.ID);
                    break;

                case CardLocation.ResolutionPile:
                    state.ResolutionPile.Push(card.ID);
                    break;
                case CardLocation.SideGame:
                    break;
                case CardLocation.Attached:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(destination), destination, null);
            }
        }
        #endregion
    }
}
