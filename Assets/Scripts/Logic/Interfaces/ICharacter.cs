namespace SSR.Logic
{
    /// <summary>
    /// Represents a character in the game, which has a certain number of souls and can die when its souls reach zero.
    /// </summary>
    public interface ICharacter : IIdentifiable
    {
        int Souls { get; set; }
        void Die();
    }
}
