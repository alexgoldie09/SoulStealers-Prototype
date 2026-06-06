using System;

namespace SSR.Logic
{
    /// <summary>
    /// A player's Hand. Hidden from opponents, but the count is public.
    /// Players may examine and reorder their own Hand freely. Rule 403.
    /// </summary>
    [Serializable]
    public class HandZone : ZoneBase
    {
        public HandZone()
        {
            ZoneType = ZoneType.Hand;
            IsPublic = false;
        }
    }
}