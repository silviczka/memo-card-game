using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using MemoCardGame.Application.Profanity;

namespace MemoCardGame.Application;

public static class DisplayNameValidation
{
    private static readonly Regex AllowedChars = new(
        @"^[\p{L}\p{M}\p{N} ._'\-]{2,24}$",
        RegexOptions.CultureInvariant);

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

        if (ProfanityChecker.ContainsProfanity(trimmed))
        {
            var matches = ProfanityChecker.GetMatches(trimmed);
            return DisplayNameResult.Failure(ProfanityMessages.ForMatches(matches));
        }

        var normalized = NormalizeKey(trimmed);
        if (normalized.Length < 2)
            return DisplayNameResult.Failure("Use at least 2 letters or numbers.");

        return DisplayNameResult.Success(trimmed, normalized);
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
