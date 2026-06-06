namespace SSR.Logic
{
    /// <summary>
    /// A simple factory for generating unique IDs for game entities.
    /// This ensures that each entity can be uniquely identified throughout the game.
    /// </summary>
    public static class IDFactory
    {
        private static int _count;

        /// <summary>
        /// Generates and returns a unique ID. Each call to this method will increment the internal counter,
        /// ensuring that the returned ID is unique.
        /// </summary>
        /// <returns></returns>
        public static int GetUniqueID()
        {
            _count++;
            return _count;
        }

        /// <summary>
        /// Resets the ID counter to zero. This can be useful for testing purposes or when starting a new game session
        /// to ensure that IDs start from a known state.
        /// </summary>
        public static void ResetIDs()
        {
            _count = 0;
        }
    }
}