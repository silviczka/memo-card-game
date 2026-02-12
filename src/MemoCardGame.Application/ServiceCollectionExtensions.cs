using Microsoft.Extensions.DependencyInjection;

namespace MemoCardGame.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IGameFactory, GameFactory>();
        services.AddScoped<IMoveValidator, MoveValidator>();
        services.AddScoped<IGameService, GameService>();
        return services;
    }
}
