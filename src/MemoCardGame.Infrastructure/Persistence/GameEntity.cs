namespace MemoCardGame.Infrastructure.Persistence;

public class GameEntity
{
    public string Id { get; set; } = string.Empty;
    public string StateJson { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public int Score { get; set; }
    public int MoveCount { get; set; }
}
