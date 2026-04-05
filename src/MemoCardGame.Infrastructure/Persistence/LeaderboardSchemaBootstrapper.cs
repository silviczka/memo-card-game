using Microsoft.EntityFrameworkCore;

namespace MemoCardGame.Infrastructure.Persistence;

/// <summary>
/// <see cref="GameDbContext"/> uses EnsureCreated, which does not add tables to an existing database.
/// This applies the leaderboard DDL when those tables are missing (SQLite or PostgreSQL).
/// </summary>
public static class LeaderboardSchemaBootstrapper
{
    public static async Task EnsureLeaderboardTablesAsync(GameDbContext db, CancellationToken cancellationToken = default)
    {
        var provider = db.Database.ProviderName ?? "";
        if (provider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
            await EnsureSqliteAsync(db, cancellationToken);
        else if (provider.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
            await EnsurePostgresAsync(db, cancellationToken);
    }

    private static async Task EnsureSqliteAsync(GameDbContext db, CancellationToken cancellationToken)
    {
        await using var connection = db.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken);
        await using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT 1 FROM sqlite_master WHERE type='table' AND name='LeaderboardEntries' LIMIT 1;";
            var exists = await cmd.ExecuteScalarAsync(cancellationToken) is not null;
            if (exists)
                return;
        }

        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE LeaderboardSubmissions (
                GameId TEXT NOT NULL CONSTRAINT PK_LeaderboardSubmissions PRIMARY KEY,
                SubmittedAt TEXT NOT NULL
            );
            """,
            cancellationToken);

        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE LeaderboardEntries (
                Id INTEGER NOT NULL CONSTRAINT PK_LeaderboardEntries PRIMARY KEY AUTOINCREMENT,
                DisplayName TEXT NOT NULL,
                NormalizedDisplayName TEXT NOT NULL,
                Mode TEXT NOT NULL,
                BoardSize INTEGER NOT NULL,
                Score INTEGER NOT NULL,
                MoveCount INTEGER NOT NULL,
                SubmittedAt TEXT NOT NULL
            );
            """,
            cancellationToken);

        await db.Database.ExecuteSqlRawAsync(
            "CREATE UNIQUE INDEX IX_LeaderboardEntries_CohortName ON LeaderboardEntries (NormalizedDisplayName, Mode, BoardSize);",
            cancellationToken);
    }

    private static async Task EnsurePostgresAsync(GameDbContext db, CancellationToken cancellationToken)
    {
        await using var connection = db.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken);
        await using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = """
                SELECT 1 FROM information_schema.tables
                WHERE table_schema = current_schema() AND table_name = 'LeaderboardEntries' LIMIT 1;
                """;
            var exists = await cmd.ExecuteScalarAsync(cancellationToken) is not null;
            if (exists)
                return;
        }

        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE "LeaderboardSubmissions" (
                "GameId" character varying(36) NOT NULL CONSTRAINT "PK_LeaderboardSubmissions" PRIMARY KEY,
                "SubmittedAt" timestamp with time zone NOT NULL
            );
            """,
            cancellationToken);

        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE "LeaderboardEntries" (
                "Id" bigserial NOT NULL CONSTRAINT "PK_LeaderboardEntries" PRIMARY KEY,
                "DisplayName" character varying(64) NOT NULL,
                "NormalizedDisplayName" character varying(64) NOT NULL,
                "Mode" character varying(16) NOT NULL,
                "BoardSize" integer NOT NULL,
                "Score" integer NOT NULL,
                "MoveCount" integer NOT NULL,
                "SubmittedAt" timestamp with time zone NOT NULL
            );
            """,
            cancellationToken);

        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE UNIQUE INDEX "IX_LeaderboardEntries_CohortName"
                ON "LeaderboardEntries" ("NormalizedDisplayName", "Mode", "BoardSize");
            """,
            cancellationToken);
    }
}
