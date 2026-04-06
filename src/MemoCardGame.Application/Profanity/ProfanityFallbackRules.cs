namespace MemoCardGame.Application.Profanity;

/// <summary>
/// Curated fallback rules when BogaNet is unavailable or misses ASCII/diacritic variants.
/// BogaNet ships Czech (<c>cs.txt</c>) in LTR packs; Slovak is mostly covered by the same roots — these entries add SK-heavy morphology and shared CZ/SK gaps.
/// - <see cref="SafeWholeWordsFolded"/>: known harmless names that may contain vulgar-like substrings.
/// - <see cref="SubstringBlockersFolded"/>: match inside a longer token (e.g. Blablapica → pica); longest first.
/// - <see cref="ExactTokenBlockersFolded"/>: match only the whole token (avoids grape/rape, bass/ass, epistle/piss, …).
/// </summary>
internal static class ProfanityFallbackRules
{
    /// <summary>Folded tokens that are always allowed as a single word (diacritics stripped, lowercased).</summary>
    public static readonly HashSet<string> SafeWholeWordsFolded = new(StringComparer.Ordinal)
    {
        // Pokémon / names often typo’d
        "pikachu", "picachu",
        "picasso",
        // Czech / Slovak: čepice (hat), user “capica”; extend as needed
        "cepice", "capica",
        // English examples containing ass / similar
        "class", "glass", "bass", "brass", "grass", "mass", "pass", "compass", "surpass",
        "grape", "grapes", "drape", "scrap", "therapy",
        "epistle",
        "switch", "twitch",
        "benedick",
        "woodcock", "hancock",
        "canal", "banal",
        "accumulate", "document", "incumbent",
        "picnic", "pickle", "pickles", "picky", "pico", "piccolo", "picot", "picture", "pictures", "pictorial",
        "special", "especially", "species", "specific",
        "retardant", "retardation",
    };

    /// <summary>Folded strings matched with <see cref="string.Contains(string)"/> inside a token (min length enforced in checker).</summary>
    public static readonly string[] SubstringBlockersFolded =
        BuildSortedByLengthDesc(
        [
            // Longer / compound insults first (Czech, Slovak, English)
            "asshole", "buzerant", "vyjeban", "zkurvysyn", "kokotina", "picovina", "vypicenec", "prdelka", "kurva",
            "kokot", "kunda", "prdel", "hovno", "jebat", "jebnut", "mamrd", "mrdat", "mrdka", "bordel", "debil", "chuj",
            "zmrd", "prcat", "zkurvit", "pojeb", "vyjeb", "prejeb", "dojeb", "nasrat", "zasrat", "posrat", "osukat",
            "buzna", "hajzl", "pica", "pizda", "curak", "sracka", "picus", "soustat", "sulin", "stetka",
            "nigger", "faggot", "fuck", "shit", "cunt", "bitch", "whore", "slut", "porn", "nazi",
        ]);

    /// <summary>Folded token must equal exactly (substring matching disabled — too many false positives).</summary>
    public static readonly HashSet<string> ExactTokenBlockersFolded = new(StringComparer.Ordinal)
    {
        "ass", "cum", "rape", "dick", "cock", "anal", "tit", "piss", "retard",
    };

    private static string[] BuildSortedByLengthDesc(IEnumerable<string> raw)
    {
        return raw
            .Select(static s => ProfanityText.FoldDiacriticsToLower(s))
            .Where(static s => s.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .OrderByDescending(static s => s.Length)
            .ThenBy(static s => s, StringComparer.Ordinal)
            .ToArray();
    }
}
