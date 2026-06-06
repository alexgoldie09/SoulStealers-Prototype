namespace SSR.Logic
{
    /// <summary>
    /// Represents a system responsible for saving and loading game data, such as player progress, unlocked cards, and game settings.
    /// </summary>
    public interface ISaveSystem
    {
        void Save<T>(string key, T data);
        T Load<T>(string key);
        bool Exists(string key);
        void Delete(string key);
    }
}
