using System.Collections.Generic;

namespace SSR.Logic
{
    public interface IZone
    {
        ZoneType ZoneType { get; }
        IReadOnlyList<int> CardIDs { get; }
        void Add(int cardID);
        void Remove(int cardID);
        bool Contains(int cardID);
    }
}
