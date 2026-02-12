using MemoCardGame.Application;
using MemoCardGame.Domain;

namespace MemoCardGame.Tests;

public class InMemoryGameRepository : IGameRepository
{
    private readonly Dictionary<Guid, Game> _games = new();

    public void Save(Game game) => _games[game.Id] = game;
    public Game? GetById(Guid id) => _games.TryGetValue(id, out var g) ? g : null;
}
