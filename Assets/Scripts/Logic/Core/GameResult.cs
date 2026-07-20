using System;

namespace SSR.Logic
{
    /// <summary>
    /// Represents the result of a game, including the type of result, the reason for the game over, and the ID of the winner (if applicable).
    /// </summary>
    [Serializable]
    public class GameResult
    {
        public GameResultType ResultType;
        public GameOverReason Reason;
        public int WinnerID; // -1 for Draw

        public GameResult(GameResultType resultType, GameOverReason reason, int winnerID = -1)
        {
            ResultType = resultType;
            Reason = reason;
            WinnerID = winnerID;
        }
    }
}