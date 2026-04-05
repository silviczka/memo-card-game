namespace MemoCardGame.Infrastructure.Persistence;

/// <summary>Each finished game can contribute at most one leaderboard submission (prevents replay).</summary>
public class LeaderboardSubmissionEntity
{
    public string GameId { get; set; } = "";
    public DateTime SubmittedAt { get; set; }
}
