namespace SSR.Logic
{
    public interface ISaveSystem
    {
        void Save<T>(string key, T data);
        T Load<T>(string key);
        bool Exists(string key);
        void Delete(string key);
    }
}
