using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace MemoCardGame.Application;

public static class DisplayNameValidation
{
    private static readonly Regex AllowedChars = new(
        @"^[\p{L}\p{M}\p{N} ._'\-]{2,24}$",
        RegexOptions.CultureInvariant);

    private static readonly HashSet<string> ProfanityTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "fuck", "shit", "cunt", "nazi", "nigger", "rape", "porn", "slut", "whore"
    };

    public static DisplayNameResult Validate(string? raw)
    {
        if (raw is null)
            return DisplayNameResult.Failure("Name is required.");

        var trimmed = raw.Trim();
        if (trimmed.Length < 2)
            return DisplayNameResult.Failure("Use at least 2 characters.");

        if (trimmed.Length > 24)
            return DisplayNameResult.Failure("Use at most 24 characters.");

        if (!AllowedChars.IsMatch(trimmed))
            return DisplayNameResult.Failure("Use letters, numbers, spaces, or . _ ' - only.");

        if (ContainsProfanity(trimmed))
            return DisplayNameResult.Failure("That name is not allowed. Please choose another.");

        var normalized = NormalizeKey(trimmed);
        if (normalized.Length < 2)
            return DisplayNameResult.Failure("Use at least 2 letters or numbers.");

        return DisplayNameResult.Success(trimmed, normalized);
    }

    private static bool ContainsProfanity(string text)
    {
        foreach (var token in Tokenize(text))
        {
            if (ProfanityTokens.Contains(token))
                return true;
        }

        return false;
    }

    private static IEnumerable<string> Tokenize(string text)
    {
        var sb = new StringBuilder();
        foreach (var ch in text)
        {
            if (char.IsLetter(ch))
                sb.Append(ch);
            else if (sb.Length > 0)
            {
                yield return sb.ToString();
                sb.Clear();
            }
        }

        if (sb.Length > 0)
            yield return sb.ToString();
    }

    private static string NormalizeKey(string displayName)
    {
        var formD = displayName.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(formD.Length);
        foreach (var ch in formD)
        {
            var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (uc != UnicodeCategory.NonSpacingMark)
                sb.Append(char.ToLowerInvariant(ch));
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}

public readonly record struct DisplayNameResult(bool Ok, string? DisplayName, string? NormalizedKey, string? ErrorMessage)
{
    public static DisplayNameResult Success(string display, string normalized) =>
        new(true, display, normalized, null);

    public static DisplayNameResult Failure(string message) =>
        new(false, null, null, message);
}
