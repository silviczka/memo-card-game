using MemoCardGame.Application;
using Microsoft.AspNetCore.Mvc;

namespace MemoCardGame.Api;

public static class LeaderboardEndpoints
{
    public static void MapLeaderboard(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/leaderboard");

        group.MapGet("/", async (
            [FromQuery] string? mode,
            [FromQuery] int boardSize,
            [FromQuery] int take,
            ILeaderboardService leaderboard,
            CancellationToken ct) =>
        {
            if (boardSize < 2 || boardSize > 10 || boardSize % 2 != 0)
                return Results.BadRequest(ApiResponses.Error("Invalid board size."));
            var list = await leaderboard.ListAsync(mode ?? "image", boardSize, take <= 0 ? 50 : take, ct);
            return Results.Ok(list);
        });

        group.MapGet("/percentile/{gameId:guid}", async (Guid gameId, ILeaderboardService leaderboard, CancellationToken ct) =>
        {
            var dto = await leaderboard.GetPercentileAsync(gameId, ct);
            // Return 404 only when the game cannot be compared; empty cohorts still return SampleSize 0.
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        });

        group.MapPost("/submit", async ([FromBody] LeaderboardSubmitRequest? body, ILeaderboardService leaderboard, CancellationToken ct) =>
        {
            if (body?.GameId is null || body.GameId == Guid.Empty)
                return Results.BadRequest(ApiResponses.Error("GameId is required."));
            var name = body.DisplayName ?? "";
            var result = await leaderboard.SubmitAsync(body.GameId.Value, name, ct);
            return result.Ok
                ? Results.Ok(result)
                : Results.BadRequest(result);
        });
    }
}

public class LeaderboardSubmitRequest
{
    public Guid? GameId { get; set; }
    public string? DisplayName { get; set; }
}
