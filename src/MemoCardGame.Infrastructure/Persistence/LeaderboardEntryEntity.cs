namespace MemoCardGame.Infrastructure.Persistence;

public class LeaderboardEntryEntity
{
    public long Id { get; set; }
    public string DisplayName { get; set; } = "";
    public string NormalizedDisplayName { get; set; } = "";
    public string Mode { get; set; } = "";
    public int BoardSize { get; set; }
    public int Score { get; set; }
    public int MoveCount { get; set; }
    public DateTime SubmittedAt { get; set; }
}
