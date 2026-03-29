using MemoCardGame.Application;
using Microsoft.AspNetCore.Mvc;

namespace MemoCardGame.Api;

public static class ApiEndpoints
{
    public static void MapApi(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/games");

        group.MapPost("/", ([FromServices] IGameService svc, [FromBody] StartGameRequest? body) =>
        {
            var size = body?.BoardSize ?? 4;
            var maxAttempts = body?.MaxAttempts;
            if (size < 2 || size > 10 || size % 2 != 0)
                return Results.BadRequest(ApiError("Board size must be an even number between 2 and 10."));
            try
            {
                var game = svc.StartNewGame(size, maxAttempts);
                return Results.Created($"/games/{game.Id}", GameService.ToStateDto(game));
            }
            catch (ArgumentOutOfRangeException ex)
            {
                return Results.BadRequest(ApiError(ex.Message));
            }
        });

        group.MapGet("/{id:guid}", (Guid id, IGameService svc) =>
        {
            var state = svc.GetState(id);
            return state is null ? Results.NotFound() : Results.Ok(state);
        });

        group.MapPost("/{id:guid}/flip", (Guid id, [FromBody] FlipRequest body, IGameService svc) =>
        {
            if (body?.CardId is null) return Results.BadRequest(ApiError("CardId is required."));
            var (success, state, error) = svc.FlipCardAndGetState(id, body.CardId.Value);
            if (!success) return Results.BadRequest(ApiError(error ?? "Invalid move."));
            return state is null ? Results.NotFound() : Results.Ok(state);
        });

        group.MapPost("/{id:guid}/resolve", (Guid id, IGameService svc) =>
        {
            var (success, state, error) = svc.ResolveTurnAndGetState(id);
            if (!success) return Results.BadRequest(ApiError(error ?? "Resolve failed."));
            return state is null ? Results.NotFound() : Results.Ok(state);
        });
    }

    private static object ApiError(string message) => new { error = message };
}

public class StartGameRequest
{
    public int BoardSize { get; set; } = 4;
    public int? MaxAttempts { get; set; }
}

public class FlipRequest
{
    public Guid? CardId { get; set; }
}
