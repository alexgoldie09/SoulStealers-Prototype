namespace SSR.Logic
{
    /// <summary>
    /// Determines where a card goes after it resolves from the Resolution Pile
    /// and moves it there via ZoneMover. Rule 404, 604.5.
    ///
    /// Called by ResolutionStack after a card's effects have all resolved.
    /// No-op if the card already left the pile during effect resolution
    /// (e.g. Merge moved it to Attached, Negate sent it to Discard).
    /// </summary>
    public static class CardResolutionHandler
    {
        /// <summary>
        /// Moves the card to its post-resolution destination if it is still
        /// on the Resolution Pile, then clears IsPlayed.
        /// </summary>
        public static void HandleAfterResolution(GameState state, RuntimeCard card)
        {
            if (card.Location != CardLocation.ResolutionPile) return;

            var destination = GetDestination(state, card);
            ZoneMover.MoveCard(state, card.ID, destination);
            card.IsPlayed = false;
        }

        /// <summary>
        /// Determines the appropriate destination for a card after resolution based on its type and play context.
        /// </summary>
        /// <param name="state"></param>
        /// <param name="card"></param>
        /// <returns></returns>
        private static CardLocation GetDestination(GameState state, RuntimeCard card)
        {
            switch (card.CurrentType)
            {
                case CardType.Spell:
                case CardType.Secret:
                    return CardLocation.DiscardPile;

                case CardType.Ritual:
                case CardType.Curse:
                case CardType.Prayer:
                    if (card.LastPlayType == PlayType.MergePlay)
                        return CardLocation.DiscardPile;

                    var controller = state.GetPlayer(card.ControllerID);
                    if (controller != null && !controller.IncantationZone.IsFull)
                        return CardLocation.IncantationZone;

                    return CardLocation.DiscardPile;

                case CardType.Spirit:
                    return CardLocation.SpiritZone;

                default:
                    return CardLocation.DiscardPile;
            }
        }
    }
}