namespace MemoCardGame.Domain;

/// <summary>
/// Scoring rules: points per match, optional penalty for failed attempts.
/// </summary>
public static class ScoreRules
{
    public const int PointsPerMatch = 10;
    public const int PenaltyPerFailedAttempt = -2;

    public static int ScoreMatch() => PointsPerMatch;
    public static int ScoreMismatch() => PenaltyPerFailedAttempt;
}
