using System.Reflection;
using BogaNet.BWF;
using BogaNet.BWF.Filter;

namespace MemoCardGame.Application.Profanity;

/// <summary>
/// BogaNet.BadWordFilter (includes Czech <c>cs.txt</c> in the LTR bundle; no separate Slovak file) plus a curated fallback:
/// allowlisted safe words, substring blockers (e.g. pica in Blablapica), exact-token-only blockers (e.g. ass, rape — avoids bass, grape),
/// and extra CZ/SK morphology in <see cref="ProfanityFallbackRules"/>.
/// </summary>
internal static class ProfanityChecker
{
    private const int MinSubstringBlockerLength = 4;

    private static readonly object Gate = new();
    private static bool _initialized;
    private static bool _bogaNetReady;

    public static bool ContainsProfanity(string text)
    {
        EnsureInitialized();

        // BogaNet can false-positive on innocent names (e.g. Pikachu). Skip it when every letter-token is allowlisted.
        if (IsEntirelyAllowlistedTokens(text))
            return false;

        if (_bogaNetReady)
        {
            try
            {
                if (BadWordFilter.Instance.Contains(text))
                    return true;
            }
            catch
            {
                // Fall through to fallback only.
            }
        }

        return FallbackContains(text);
    }

    public static IReadOnlyList<string> GetMatches(string text)
    {
        EnsureInitialized();

        if (IsEntirelyAllowlistedTokens(text))
            return Array.Empty<string>();

        var matches = new List<string>();
        if (_bogaNetReady)
        {
            try
            {
                var list = BadWordFilter.Instance.GetAll(text);
                if (list is { Count: > 0 })
                    matches.AddRange(list.Where(static s => !string.IsNullOrWhiteSpace(s)));
            }
            catch
            {
                // ignore
            }
        }

        foreach (var m in FallbackGetMatches(text))
        {
            if (!matches.Contains(m, StringComparer.OrdinalIgnoreCase))
                matches.Add(m);
        }

        return matches;
    }

    private static void EnsureInitialized()
    {
        if (_initialized)
            return;

        lock (Gate)
        {
            if (_initialized)
                return;

            try
            {
                var asmDir = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
                if (!string.IsNullOrEmpty(asmDir))
                    Directory.SetCurrentDirectory(asmDir);

                BadWordFilter.Instance.LoadFiles(true, BWFConstants.BWF_LTR);
                BadWordFilter.Instance.LoadFiles(false, BWFConstants.BWF_RTL);
                _bogaNetReady = true;
            }
            catch
            {
                _bogaNetReady = false;
            }

            _initialized = true;
        }
    }

    private static bool IsEntirelyAllowlistedTokens(string text)
    {
        var any = false;
        foreach (var token in Tokenize(text))
        {
            any = true;
            var folded = ProfanityText.FoldDiacriticsToLower(token);
            if (!ProfanityFallbackRules.SafeWholeWordsFolded.Contains(folded))
                return false;
        }

        return any;
    }

    private static bool FallbackContains(string text)
    {
        foreach (var token in Tokenize(text))
        {
            if (FallbackMatchToken(token) is not null)
                return true;
        }

        return false;
    }

    private static IReadOnlyList<string> FallbackGetMatches(string text)
    {
        var matches = new List<string>();
        foreach (var token in Tokenize(text))
        {
            var hit = FallbackMatchToken(token);
            if (hit is not null && !matches.Contains(hit, StringComparer.OrdinalIgnoreCase))
                matches.Add(hit);
        }

        return matches;
    }

    /// <returns>Display snippet for error message, or null if token is OK.</returns>
    private static string? FallbackMatchToken(string token)
    {
        var folded = ProfanityText.FoldDiacriticsToLower(token);

        if (ProfanityFallbackRules.SafeWholeWordsFolded.Contains(folded))
            return null;

        if (ProfanityFallbackRules.ExactTokenBlockersFolded.Contains(folded))
            return token;

        foreach (var sub in ProfanityFallbackRules.SubstringBlockersFolded)
        {
            if (sub.Length < MinSubstringBlockerLength)
                continue;
            if (folded.Contains(sub, StringComparison.Ordinal))
                return sub;
        }

        return null;
    }

    private static IEnumerable<string> Tokenize(string text)
    {
        var start = -1;
        for (var i = 0; i <= text.Length; i++)
        {
            if (i < text.Length && char.IsLetter(text[i]))
            {
                if (start < 0) start = i;
            }
            else if (start >= 0)
            {
                yield return text[start..i];
                start = -1;
            }
        }
    }
}
