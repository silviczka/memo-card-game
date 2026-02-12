namespace MemoCardGame.Domain;

/// <summary>
/// Represents the current turn state: which cards are flipped this turn.
/// Backend enforces max 2 flips per turn.
/// </summary>
public class Turn
{
    private readonly List<Guid> _flippedCardIds = new();
    public IReadOnlyList<Guid> FlippedCardIds => _flippedCardIds;

    public int FlippedCount => _flippedCardIds.Count;
    public bool IsComplete => FlippedCount >= 2;

    public bool CanFlipAnother => FlippedCount < 2;

    public void RecordFlip(Guid cardId)
    {
        if (_flippedCardIds.Count >= 2)
            throw new InvalidOperationException("Cannot flip more than 2 cards per turn.");
        if (_flippedCardIds.Contains(cardId))
            throw new InvalidOperationException("Cannot flip the same card twice in one turn.");
        _flippedCardIds.Add(cardId);
    }

    public void Clear()
    {
        _flippedCardIds.Clear();
    }

    internal void SetFlippedForReconstitution(IEnumerable<Guid> cardIds)
    {
        _flippedCardIds.Clear();
        _flippedCardIds.AddRange(cardIds);
    }
}
