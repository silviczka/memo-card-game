using MemoCardGame.Client.Shared;

namespace MemoCardGame.Client;

/// <summary>
/// Single source of truth for supported mode + board-size combinations.
/// Used by the game start screen, leaderboard carousel, and query/cookie validation.
/// </summary>
public static class BoardRules
{
    public static readonly IReadOnlyList<BoardCombo> All =
    [
        new("image", 4, "Pictures · 4×4", "4×4 — classic"),
        new("image", 6, "Pictures · 6×6", "6×6 — challenge"),
        new("image", 8, "Pictures · 8×8", "8×8 — marathon"),
        new("audio", 4, "Sound · 4×4", "4×4 — animals"),
        new("audio", 6, "Sound · 6×6", "6×6 — FX"),
    ];

    public static bool IsValidCombo(string mode, int size) =>
        All.Any(b => b.Mode == mode && b.Size == size);

    public static int MaxSizeForMode(string mode) =>
        All.Where(b => b.Mode == mode).Select(b => b.Size).DefaultIfEmpty(4).Max();

    public static IReadOnlyList<IntSelectOption> StartOptionsForMode(string mode) =>
        All.Where(b => b.Mode == mode)
           .Select(b => new IntSelectOption(b.Size, b.StartLabel))
           .ToArray();

    public readonly record struct BoardCombo(string Mode, int Size, string LeaderboardTitle, string StartLabel);
}
