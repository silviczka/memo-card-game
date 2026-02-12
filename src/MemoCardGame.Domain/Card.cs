namespace MemoCardGame.Domain;

/// <summary>
/// A single card on the board. Same PairId = same symbol (match).
/// </summary>
public class Card
{
    public Guid Id { get; private set; }
    public int PairId { get; private set; }
    public bool IsFlipped { get; private set; }
    public bool IsMatched { get; private set; }
    public int Position { get; private set; }

    public Card(Guid id, int pairId, int position)
    {
        Id = id;
        PairId = pairId;
        Position = position;
        IsFlipped = false;
        IsMatched = false;
    }

    public void Flip()
    {
        if (IsMatched) return;
        IsFlipped = true;
    }

    public void Unflip()
    {
        if (IsMatched) return;
        IsFlipped = false;
    }

    public void MarkMatched()
    {
        IsMatched = true;
        IsFlipped = true;
    }

    public bool CanBeFlipped => !IsMatched && !IsFlipped;

    internal static Card FromSnapshot(CardSnapshot s)
    {
        var c = new Card(s.Id, s.PairId, s.Position);
        if (s.IsFlipped) c.Flip();
        if (s.IsMatched) c.MarkMatched();
        return c;
    }
}
