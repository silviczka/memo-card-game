namespace MemoCardGame.Application;

public sealed class LeaderboardEntryDto
{
    public string DisplayName { get; set; } = "";
    public int Score { get; set; }
    public int MoveCount { get; set; }
    public DateTime SubmittedAt { get; set; }
}

public sealed class LeaderboardPercentileDto
{
    /// <summary>0–100 when <see cref="SampleSize"/> &gt; 0; otherwise null (still HTTP 200 so clients avoid false &quot;errors&quot;).</summary>
    public decimal? Percentile { get; set; }
    public int SampleSize { get; set; }
    public string CohortLabel { get; set; } = "";
}

public sealed class LeaderboardSubmitResponseDto
{
    public bool Ok { get; set; }
    public string? Error { get; set; }
    public string? Message { get; set; }
    public bool EntryUpdated { get; set; }
}
