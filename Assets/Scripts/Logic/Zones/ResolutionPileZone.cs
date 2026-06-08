using System;

namespace SSR.Logic
{
    /// <summary>
    /// The shared Resolution Pile. LIFO — last in, first out.
    /// Cards and effects resolve top-to-bottom, one at a time. Rule 405.
    /// Activated and Triggered effects are independent objects on this pile,
    /// separate from their source card. Rule 405.3.
    /// </summary>
    [Serializable]
    public class ResolutionPileZone : ZoneBase
    {
        public ResolutionPileZone()
        {
            ZoneType = ZoneType.ResolutionPile;
            IsPublic = true;
        }

        /// <summary>
        /// Pushes a card or effect ID to the top of the pile.
        /// </summary>
        public void Push(int id)
        {
            _cardIDs.Insert(0, id);
        }

        /// <summary>
        /// Removes and returns the top item.
        /// Returns -1 if empty.
        /// </summary>
        public int Pop()
        {
            if (_cardIDs.Count == 0) 
                return -1;
            
            var top = _cardIDs[0];
            _cardIDs.RemoveAt(0);
            return top;
        }

        /// <summary>
        /// Returns the top item without removing it.
        /// Returns -1 if empty.
        /// </summary>
        public int Peek() => _cardIDs.Count > 0 ? _cardIDs[0] : -1;

        public bool IsEmpty => _cardIDs.Count == 0;

        /// <summary>
        /// Default Add pushes to the top (LIFO behaviour).
        /// </summary>
        public override void Add(int id) => Push(id);
    }
}