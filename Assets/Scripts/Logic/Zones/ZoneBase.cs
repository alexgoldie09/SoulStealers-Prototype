using System.Collections.Generic;
using System;

namespace SSR.Logic
{
    /// <summary>
    /// Base class for all game zones. Zones own lists of card IDs,
    /// not RuntimeCard references, to keep them serialisable and
    /// independent of the card registry. Rule 400.
    /// </summary>
    [Serializable]
    public abstract class ZoneBase : IZone
    {
        public ZoneType ZoneType { get; protected set; }
        public bool IsPublic { get; protected set; }

        protected List<int> _cardIDs = new List<int>();

        public IReadOnlyList<int> CardIDs => _cardIDs.AsReadOnly();
        public int Count => _cardIDs.Count;

        /// <summary>
        /// Checks if the specified card ID is present in this zone.
        /// </summary>
        /// <param name="cardID"></param>
        /// <returns></returns>
        public virtual bool Contains(int cardID) => _cardIDs.Contains(cardID);

        /// <summary>
        /// Adds a card ID to this zone.
        /// Subclasses should call CanAdd before calling this method to enforce zone-specific rules.
        /// </summary>
        /// <param name="cardID"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public virtual void Add(int cardID)
        {
            if (!CanAdd(cardID))
                throw new InvalidOperationException(
                    $"Cannot add card {cardID} to {ZoneType}: {GetAddFailReason()}");
            _cardIDs.Add(cardID);
        }

        /// <summary>
        /// Removes a card ID from this zone. Throws an exception if the card ID is not found.
        /// </summary>
        /// <param name="cardID"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public virtual void Remove(int cardID)
        {
            if (!_cardIDs.Remove(cardID))
                throw new InvalidOperationException(
                    $"Card {cardID} not found in {ZoneType}");
        }

        /// <summary>
        /// Removes all card IDs from this zone.
        /// Subclasses can override this to enforce rules about when a zone can be cleared.
        /// </summary>
        public virtual void Clear() => _cardIDs.Clear();

        /// <summary>
        /// Subclasses override this to enforce zone-specific capacity and
        /// entry rules before Add is called.
        /// </summary>
        public virtual bool CanAdd(int cardID) => true;
        
        protected virtual string GetAddFailReason() => "Unknown reason";
    }
}