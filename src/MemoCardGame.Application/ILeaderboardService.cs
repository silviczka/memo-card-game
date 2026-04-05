namespace MemoCardGame.Application;

public interface ILeaderboardService
{
    Task<LeaderboardPercentileDto?> GetPercentileAsync(Guid gameId, CancellationToken cancellationToken = default);

    Task<LeaderboardSubmitResponseDto> SubmitAsync(Guid gameId, string displayName, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LeaderboardEntryDto>> ListAsync(string mode, int boardSize, int take = 50, CancellationToken cancellationToken = default);
}
