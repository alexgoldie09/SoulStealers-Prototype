namespace SSR.Logic
{
    public static class IDFactory
    {
        private static int _count;

        public static int GetUniqueID()
        {
            _count++;
            return _count;
        }

        public static void ResetIDs()
        {
            _count = 0;
        }
    }
}