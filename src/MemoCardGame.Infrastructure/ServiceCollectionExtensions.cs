using MemoCardGame.Application;
using MemoCardGame.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MemoCardGame.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string? sqlitePath = null)
    {
        var path = sqlitePath ?? "memo.db";
        services.AddDbContext<GameDbContext>(o => o.UseSqlite($"Data Source={path}"));
        services.AddScoped<IGameRepository, GameRepository>();
        return services;
    }
}
