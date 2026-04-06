namespace MemoCardGame.Application.Profanity;

internal static class ProfanityMessages
{
    /// <summary>
    /// User-facing explanation without repeating full slurs (still hints at what was caught when safe).
    /// </summary>
    public static string ForMatches(IReadOnlyList<string> matches)
    {
        if (matches.Count == 0)
        {
            return "That name contains language we don't allow on the public leaderboard (for example insults, slurs, or explicit words). Please choose a different name.";
        }

        var masked = matches
            .Take(3)
            .Select(MaskSnippet)
            .Where(static s => s.Length > 0)
            .ToList();

        if (masked.Count == 0)
        {
            return "That name contains language we don't allow on the public leaderboard. Please choose a different name.";
        }

        var parts = string.Join(", ", masked);
        var suffix = matches.Count > 3 ? " …" : "";
        return $"That name contains words we don't allow on the public leaderboard (flagged: {parts}{suffix}). Please choose a different name without profanity or hate-related language.";
    }

    /// <summary>Short mask for UI: avoids echoing full offensive terms.</summary>
    private static string MaskSnippet(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "";

        var s = raw.Trim();
        if (s.Length <= 1)
            return "*";

        if (s.Length == 2)
            return $"{s[0]}*";

        return $"{s[0]}{new string('*', Math.Min(s.Length - 2, 6))}{s[^1]}";
    }
}
