namespace SSR.Logic
{
    /// <summary>
    /// Represents a character in the game, which has a certain number of souls and can die when its souls reach zero.
    /// </summary>
    public interface ICharacter : IIdentifiable
    {
        /// <summary>
        /// The number of souls the character currently has. When this reaches zero, the character dies.
        /// </summary>
        int Souls { get; set; }
        /// <summary>
        /// Indicates whether the character is currently alive.
        /// A character is considered alive if it has more than zero souls.
        /// </summary>
        void Die();
    }
}
