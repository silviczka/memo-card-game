using MemoCardGame.Domain;

namespace MemoCardGame.Application;

public interface IGameFactory
{
    Game Create(int boardSize = 4, int? maxAttempts = null, string playMode = "image");
}

public class GameFactory : IGameFactory
{
    public Game Create(int boardSize = 4, int? maxAttempts = null, string playMode = "image")
    {
        return Game.Create(Guid.NewGuid(), boardSize, maxAttempts, playMode);
    }
}
