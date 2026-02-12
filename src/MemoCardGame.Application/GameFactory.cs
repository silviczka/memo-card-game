using MemoCardGame.Domain;

namespace MemoCardGame.Application;

public interface IGameFactory
{
    Game Create(int boardSize = 4, int? maxAttempts = null);
}

public class GameFactory : IGameFactory
{
    public Game Create(int boardSize = 4, int? maxAttempts = null)
    {
        if (boardSize < 2 || boardSize > 10 || boardSize % 2 != 0)
            throw new ArgumentOutOfRangeException(nameof(boardSize), "Board size must be an even number between 2 and 10.");
        return Game.Create(Guid.NewGuid(), boardSize, maxAttempts);
    }
}
