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
            var playMode = body?.PlayMode ?? "image";
            if (size < 2 || size > 10 || size % 2 != 0)
                return Results.BadRequest(ApiResponses.Error("Board size must be an even number between 2 and 10."));
            try
            {
                var game = svc.StartNewGame(size, maxAttempts, playMode);
                return Results.Created($"/games/{game.Id}", GameService.ToStateDto(game));
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(ApiResponses.Error(ex.Message));
            }
        });

        group.MapGet("/{id:guid}", (Guid id, IGameService svc) =>
        {
            var state = svc.GetState(id);
            return state is null ? Results.NotFound() : Results.Ok(state);
        });

        group.MapPost("/{id:guid}/flip", (Guid id, [FromBody] FlipRequest body, IGameService svc) =>
        {
            if (body?.CardId is null) return Results.BadRequest(ApiResponses.Error("CardId is required."));
            var (success, state, error) = svc.FlipCardAndGetState(id, body.CardId.Value);
            if (!success) return Results.BadRequest(ApiResponses.Error(error ?? "Invalid move."));
            return state is null ? Results.NotFound() : Results.Ok(state);
        });

        group.MapPost("/{id:guid}/resolve", (Guid id, IGameService svc) =>
        {
            var (success, state, error) = svc.ResolveTurnAndGetState(id);
            if (!success) return Results.BadRequest(ApiResponses.Error(error ?? "Resolve failed."));
            return state is null ? Results.NotFound() : Results.Ok(state);
        });
    }

}

public class StartGameRequest
{
    public int BoardSize { get; set; } = 4;
    public int? MaxAttempts { get; set; }
    /// <summary>"image" or "audio".</summary>
    public string? PlayMode { get; set; }
}

public class FlipRequest
{
    public Guid? CardId { get; set; }
}
