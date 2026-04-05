namespace MemoCardGame.Domain;

/// <summary>
/// Snapshot of game state for persistence and reconstitution.
/// </summary>
public class GameStateSnapshot
{
    public Guid Id { get; set; }
    public int BoardSize { get; set; }
    public int? MaxAttempts { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public int Score { get; set; }
    public int MoveCount { get; set; }
    /// <summary>Client-facing mode: "image" or "audio" (stored for leaderboard cohort).</summary>
    public string? PlayMode { get; set; }
    public List<Guid> FlippedCardIdsThisTurn { get; set; } = new();
    public List<CardSnapshot> Cards { get; set; } = new();
}

public class CardSnapshot
{
    public Guid Id { get; set; }
    public int PairId { get; set; }
    public int Position { get; set; }
    public bool IsFlipped { get; set; }
    public bool IsMatched { get; set; }
}
