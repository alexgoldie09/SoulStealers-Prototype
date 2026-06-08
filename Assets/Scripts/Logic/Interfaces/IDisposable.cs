namespace SSR.Logic
{
    /// <summary>
    /// Defines a contract for objects that require explicit cleanup of resources.
    /// </summary>
    public interface IDisposable
    {
        /// <summary>
        /// Disposes of the object, performing any necessary cleanup.
        /// This method should be called when the object is no longer needed
        /// to free up resources and prevent memory leaks.
        /// </summary>
        void Dispose();
    }
}
