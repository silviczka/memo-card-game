using MemoCardGame.Application;
using MemoCardGame.Domain;
using MemoCardGame.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MemoCardGame.Infrastructure;

public class LeaderboardService : ILeaderboardService
{
    private readonly GameDbContext _db;
    private readonly IGameRepository _games;

    public LeaderboardService(GameDbContext db, IGameRepository games)
    {
        _db = db;
        _games = games;
    }

    public async Task<LeaderboardPercentileDto?> GetPercentileAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        var game = _games.GetById(gameId);
        if (game is null || !game.IsFinished || !game.AllPairsMatched)
            return null;

        var mode = Game.NormalizePlayMode(game.PlayMode);
        var boardSize = game.BoardSize;
        var score = game.Score;
        var moves = game.MoveCount;

        var cohort = _db.LeaderboardEntries.AsNoTracking()
            .Where(e => e.Mode == mode && e.BoardSize == boardSize);

        var label = FormatCohort(mode, boardSize);
        var total = await cohort.CountAsync(cancellationToken);
        if (total == 0)
        {
            return new LeaderboardPercentileDto
            {
                Percentile = null,
                SampleSize = 0,
                CohortLabel = label
            };
        }

        var beaten = await cohort.CountAsync(
            e => e.Score < score || (e.Score == score && e.MoveCount > moves),
            cancellationToken);

        var percentile = Math.Round(100m * beaten / total, 1, MidpointRounding.AwayFromZero);
        return new LeaderboardPercentileDto
        {
            Percentile = percentile,
            SampleSize = total,
            CohortLabel = label
        };
    }

    public async Task<LeaderboardSubmitResponseDto> SubmitAsync(Guid gameId, string displayName, CancellationToken cancellationToken = default)
    {
        if (await _db.LeaderboardSubmissions.AsNoTracking().AnyAsync(s => s.GameId == gameId.ToString(), cancellationToken))
        {
            return new LeaderboardSubmitResponseDto
            {
                Ok = false,
                Error = "already_submitted",
                Message = "This game was already submitted to the leaderboard."
            };
        }

        var game = _games.GetById(gameId);
        if (game is null)
        {
            return new LeaderboardSubmitResponseDto
            {
                Ok = false,
                Error = "game_not_found",
                Message = "Game not found."
            };
        }

        if (!game.IsFinished || !game.AllPairsMatched)
        {
            return new LeaderboardSubmitResponseDto
            {
                Ok = false,
                Error = "not_a_win",
                Message = "Only a completed win can be submitted."
            };
        }

        var nameCheck = DisplayNameValidation.Validate(displayName);
        if (!nameCheck.Ok)
        {
            return new LeaderboardSubmitResponseDto
            {
                Ok = false,
                Error = "invalid_name",
                Message = nameCheck.ErrorMessage ?? "Invalid name."
            };
        }

        var mode = Game.NormalizePlayMode(game.PlayMode);
        var boardSize = game.BoardSize;
        var score = game.Score;
        var moves = game.MoveCount;
        var normalized = nameCheck.NormalizedKey!;
        var display = nameCheck.DisplayName!;
        var now = DateTime.UtcNow;

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

        _db.LeaderboardSubmissions.Add(new LeaderboardSubmissionEntity
        {
            GameId = gameId.ToString(),
            SubmittedAt = now
        });

        var existing = await _db.LeaderboardEntries
            .FirstOrDefaultAsync(
                e => e.NormalizedDisplayName == normalized && e.Mode == mode && e.BoardSize == boardSize,
                cancellationToken);

        var entryUpdated = false;
        string message;

        if (existing is null)
        {
            _db.LeaderboardEntries.Add(new LeaderboardEntryEntity
            {
                DisplayName = display,
                NormalizedDisplayName = normalized,
                Mode = mode,
                BoardSize = boardSize,
                Score = score,
                MoveCount = moves,
                SubmittedAt = now
            });
            entryUpdated = true;
            message = "You're on the leaderboard!";
        }
        else if (score > existing.Score || (score == existing.Score && moves < existing.MoveCount))
        {
            existing.DisplayName = display;
            existing.Score = score;
            existing.MoveCount = moves;
            existing.SubmittedAt = now;
            entryUpdated = true;
            message = "Your best score for this name was updated!";
        }
        else
        {
            message = "Submitted. Your name already had a better or equal score on this board.";
        }

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            await tx.RollbackAsync(cancellationToken);
            return new LeaderboardSubmitResponseDto
            {
                Ok = false,
                Error = "conflict",
                Message = "Could not save. Try again."
            };
        }

        return new LeaderboardSubmitResponseDto
        {
            Ok = true,
            EntryUpdated = entryUpdated,
            Message = message
        };
    }

    public async Task<IReadOnlyList<LeaderboardEntryDto>> ListAsync(string mode, int boardSize, int take = 50, CancellationToken cancellationToken = default)
    {
        var m = Game.NormalizePlayMode(mode);
        take = Math.Clamp(take, 1, 200);

        var rows = await _db.LeaderboardEntries.AsNoTracking()
            .Where(e => e.Mode == m && e.BoardSize == boardSize)
            .OrderByDescending(e => e.Score)
            .ThenBy(e => e.MoveCount)
            .ThenBy(e => e.SubmittedAt)
            .Take(take)
            .Select(e => new LeaderboardEntryDto
            {
                DisplayName = e.DisplayName,
                Score = e.Score,
                MoveCount = e.MoveCount,
                SubmittedAt = e.SubmittedAt
            })
            .ToListAsync(cancellationToken);

        return rows;
    }

    private static string FormatCohort(string mode, int boardSize)
    {
        var label = mode == "audio" ? "sound" : "pictures";
        return $"{boardSize}×{boardSize} · {label}";
    }
}
