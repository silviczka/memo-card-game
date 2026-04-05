namespace MemoCardGame.Domain;

/// <summary>
/// Root aggregate: a single memory game with board, turn state, score, and completion.
/// </summary>
public class Game
{
    public Guid Id { get; private set; }
    public int BoardSize { get; private set; }
    public int TotalPairs => (BoardSize * BoardSize) / 2;
    private List<Card> _cards = new();
    public IReadOnlyList<Card> Cards => _cards;
    public Turn CurrentTurn { get; private set; } = new();
    public int Score { get; private set; }
    public int MoveCount { get; private set; }
    public int? MaxAttempts { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime? FinishedAt { get; private set; }
    /// <summary>"image" or "audio". Persisted for leaderboard cohort.</summary>
    public string PlayMode { get; private set; } = "image";

    public bool IsFinished => FinishedAt.HasValue;
    public int MatchedPairs => _cards.Count(c => c.IsMatched) / 2;
    public bool AllPairsMatched => MatchedPairs >= TotalPairs;
    public int AttemptsUsed => MoveCount;
    public bool HasAttemptsLeft => !MaxAttempts.HasValue || AttemptsUsed < MaxAttempts.Value;

    private Game() { }

    public static Game Create(Guid id, int boardSize, int? maxAttempts = null, string playMode = "image")
    {
        if (boardSize < 2 || boardSize > 10 || boardSize % 2 != 0)
            throw new ArgumentException("Board size must be an even number between 2 and 10.", nameof(boardSize));
        var totalCells = boardSize * boardSize;
        if (totalCells % 2 != 0)
            throw new ArgumentException("Board must have even number of cells.", nameof(boardSize));

        var mode = NormalizePlayMode(playMode);
        var game = new Game
        {
            Id = id,
            BoardSize = boardSize,
            MaxAttempts = maxAttempts,
            StartedAt = DateTime.UtcNow,
            PlayMode = mode
        };
        game.InitializeCards();
        return game;
    }

    public static string NormalizePlayMode(string playMode)
    {
        if (string.IsNullOrWhiteSpace(playMode))
            return "image";
        var m = playMode.Trim().ToLowerInvariant();
        return m == "audio" ? "audio" : "image";
    }

    private void InitializeCards()
    {
        var pairCount = TotalPairs;
        var positions = Enumerable.Range(0, BoardSize * BoardSize).ToList();
        var random = new Random();
        for (var i = 0; i < positions.Count; i++)
        {
            var j = random.Next(i, positions.Count);
            (positions[i], positions[j]) = (positions[j], positions[i]);
        }
        _cards = new List<Card>();
        for (var pairId = 0; pairId < pairCount; pairId++)
        {
            _cards.Add(new Card(Guid.NewGuid(), pairId, positions[pairId * 2]));
            _cards.Add(new Card(Guid.NewGuid(), pairId, positions[pairId * 2 + 1]));
        }
    }

    public Card? GetCard(Guid cardId) => _cards.FirstOrDefault(c => c.Id == cardId);
    public Card? GetCardByPosition(int position) => _cards.FirstOrDefault(c => c.Position == position);

    public void FlipCard(Guid cardId)
    {
        if (IsFinished) throw new InvalidOperationException("Game is already finished.");
        if (!HasAttemptsLeft) throw new InvalidOperationException("No attempts left.");
        if (!CurrentTurn.CanFlipAnother) throw new InvalidOperationException("Already flipped 2 cards this turn.");
        var card = GetCard(cardId) ?? throw new ArgumentException("Card not found.", nameof(cardId));
        if (!card.CanBeFlipped) throw new InvalidOperationException("Card cannot be flipped (already flipped or matched).");

        card.Flip();
        CurrentTurn.RecordFlip(cardId);
    }

    public void ResolveTurn()
    {
        if (CurrentTurn.FlippedCount != 2) return;
        MoveCount++;
        var id1 = CurrentTurn.FlippedCardIds[0];
        var id2 = CurrentTurn.FlippedCardIds[1];
        var card1 = GetCard(id1)!;
        var card2 = GetCard(id2)!;
        if (card1.PairId == card2.PairId)
        {
            card1.MarkMatched();
            card2.MarkMatched();
            Score += ScoreRules.ScoreMatch();
        }
        else
        {
            card1.Unflip();
            card2.Unflip();
            Score += ScoreRules.ScoreMismatch();
        }
        CurrentTurn.Clear();
        if (AllPairsMatched || (MaxAttempts.HasValue && AttemptsUsed >= MaxAttempts.Value))
            FinishedAt = DateTime.UtcNow;
    }

    public GameStateSnapshot ExportState()
    {
        return new GameStateSnapshot
        {
            Id = Id,
            BoardSize = BoardSize,
            MaxAttempts = MaxAttempts,
            StartedAt = StartedAt,
            FinishedAt = FinishedAt,
            Score = Score,
            MoveCount = MoveCount,
            PlayMode = PlayMode,
            FlippedCardIdsThisTurn = CurrentTurn.FlippedCardIds.ToList(),
            Cards = _cards.Select(c => new CardSnapshot
            {
                Id = c.Id,
                PairId = c.PairId,
                Position = c.Position,
                IsFlipped = c.IsFlipped,
                IsMatched = c.IsMatched
            }).ToList()
        };
    }

    public static Game FromState(GameStateSnapshot s)
    {
        var game = new Game
        {
            Id = s.Id,
            BoardSize = s.BoardSize,
            MaxAttempts = s.MaxAttempts,
            StartedAt = s.StartedAt,
            FinishedAt = s.FinishedAt,
            Score = s.Score,
            MoveCount = s.MoveCount,
            PlayMode = string.IsNullOrWhiteSpace(s.PlayMode) ? "image" : NormalizePlayMode(s.PlayMode),
            _cards = s.Cards.Select(Card.FromSnapshot).ToList()
        };
        game.CurrentTurn.SetFlippedForReconstitution(s.FlippedCardIdsThisTurn);
        return game;
    }
}
