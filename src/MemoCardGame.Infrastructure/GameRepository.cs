using System.Text.Json;
using MemoCardGame.Application;
using MemoCardGame.Domain;
using MemoCardGame.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MemoCardGame.Infrastructure;

public class GameRepository : IGameRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    private readonly GameDbContext _db;

    public GameRepository(GameDbContext db)
    {
        _db = db;
    }

    public void Save(Game game)
    {
        var snapshot = game.ExportState();
        var json = JsonSerializer.Serialize(snapshot, JsonOptions);
        var entity = new GameEntity
        {
            Id = snapshot.Id.ToString(),
            StateJson = json,
            StartedAt = snapshot.StartedAt,
            FinishedAt = snapshot.FinishedAt,
            Score = snapshot.Score,
            MoveCount = snapshot.MoveCount
        };
        var existing = _db.Games.AsNoTracking().FirstOrDefault(x => x.Id == entity.Id);
        if (existing is not null)
            _db.Games.Update(entity);
        else
            _db.Games.Add(entity);
        _db.SaveChanges();
    }

    public Game? GetById(Guid id)
    {
        var entity = _db.Games.AsNoTracking().FirstOrDefault(x => x.Id == id.ToString());
        if (entity is null) return null;
        var snapshot = JsonSerializer.Deserialize<GameStateSnapshot>(entity.StateJson, JsonOptions);
        if (snapshot is null) return null;
        return Game.FromState(snapshot);
    }
}
