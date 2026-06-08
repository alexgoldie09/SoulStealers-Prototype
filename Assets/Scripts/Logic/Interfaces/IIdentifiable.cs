namespace SSR.Logic
{
    /// <summary>
    /// Represents an object that has a unique identifier, such as a card, character, or zone.
    /// This allows us to reference objects by their ID instead of by reference, which is important for saving and loading game state.
    /// </summary>
    public interface IIdentifiable
    {
        /// <summary>
        /// Gets the unique identifier for this object.
        /// This ID should be assigned when the object is created and should never change.
        /// </summary>
        int ID { get; }
    }
}
