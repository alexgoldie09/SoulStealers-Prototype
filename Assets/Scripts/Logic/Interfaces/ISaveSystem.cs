namespace SSR.Logic
{
    /// <summary>
    /// Represents a system responsible for saving and loading game data, such as player progress, unlocked cards, and game settings.
    /// </summary>
    public interface ISaveSystem
    {
        /// <summary>
        /// Saves the given data under the specified key.
        /// The data can be of any type, and the implementation should handle serialization and storage.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="data"></param>
        /// <typeparam name="T"></typeparam>
        void Save<T>(string key, T data);
        /// <summary>
        /// Loads the data of the specified type associated with the given key.
        /// If the key does not exist or the data cannot be loaded, the implementation should handle this gracefully,
        /// such as by returning a default value or throwing an exception.
        /// </summary>
        /// <param name="key"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        T Load<T>(string key);
        /// <summary>
        /// Checks if there is saved data associated with the given key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        bool Exists(string key);
        /// <summary>
        /// Deletes the saved data associated with the given key.
        /// </summary>
        /// <param name="key"></param>
        void Delete(string key);
    }
}
