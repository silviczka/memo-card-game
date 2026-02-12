using Microsoft.EntityFrameworkCore;

namespace MemoCardGame.Infrastructure.Persistence;

public class GameDbContext : DbContext
{
    public GameDbContext(DbContextOptions<GameDbContext> options) : base(options) { }

    public DbSet<GameEntity> Games => Set<GameEntity>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<GameEntity>(e =>
        {
            e.ToTable("Games");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasMaxLength(36);
        });
    }
}
