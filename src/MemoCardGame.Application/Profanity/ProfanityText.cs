using System.Globalization;
using System.Text;

namespace MemoCardGame.Application.Profanity;

internal static class ProfanityText
{
    public static string FoldDiacriticsToLower(string s)
    {
        if (string.IsNullOrEmpty(s))
            return s;

        var formD = s.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(formD.Length);
        foreach (var ch in formD)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
                continue;
            sb.Append(char.ToLowerInvariant(ch));
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
