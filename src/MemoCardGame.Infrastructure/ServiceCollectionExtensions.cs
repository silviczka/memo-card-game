using MemoCardGame.Application;
using MemoCardGame.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MemoCardGame.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string? sqlitePath = null,
        string? defaultConnectionString = null)
    {
        if (!string.IsNullOrWhiteSpace(defaultConnectionString))
        {
            services.AddDbContext<GameDbContext>(o => o.UseNpgsql(defaultConnectionString));
        }
        else
        {
            var path = sqlitePath ?? "memo.db";
            services.AddDbContext<GameDbContext>(o => o.UseSqlite($"Data Source={path}"));
        }

        services.AddScoped<IGameRepository, GameRepository>();
        services.AddScoped<ILeaderboardService, LeaderboardService>();
        return services;
    }
}
