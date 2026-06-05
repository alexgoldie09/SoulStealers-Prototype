namespace SSR.Logic
{
    public interface ICharacter : IIdentifiable
    {
        int Souls { get; set; }
        void Die();
    }
}
