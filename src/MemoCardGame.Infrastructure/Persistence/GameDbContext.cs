using Microsoft.EntityFrameworkCore;

namespace MemoCardGame.Infrastructure.Persistence;

public class GameDbContext : DbContext
{
    public GameDbContext(DbContextOptions<GameDbContext> options) : base(options) { }

    public DbSet<GameEntity> Games => Set<GameEntity>();
    public DbSet<LeaderboardEntryEntity> LeaderboardEntries => Set<LeaderboardEntryEntity>();
    public DbSet<LeaderboardSubmissionEntity> LeaderboardSubmissions => Set<LeaderboardSubmissionEntity>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<GameEntity>(e =>
        {
            e.ToTable("Games");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasMaxLength(36);
        });

        builder.Entity<LeaderboardSubmissionEntity>(e =>
        {
            e.ToTable("LeaderboardSubmissions");
            e.HasKey(x => x.GameId);
            e.Property(x => x.GameId).HasMaxLength(36);
        });

        builder.Entity<LeaderboardEntryEntity>(e =>
        {
            e.ToTable("LeaderboardEntries");
            e.HasKey(x => x.Id);
            e.Property(x => x.DisplayName).HasMaxLength(64);
            e.Property(x => x.NormalizedDisplayName).HasMaxLength(64);
            e.Property(x => x.Mode).HasMaxLength(16);
            e.HasIndex(x => new { x.NormalizedDisplayName, x.Mode, x.BoardSize }).IsUnique();
        });
    }
}
