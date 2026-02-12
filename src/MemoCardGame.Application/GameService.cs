using MemoCardGame.Domain;

namespace MemoCardGame.Application;

public interface IGameService
{
    Game StartNewGame(int boardSize = 4, int? maxAttempts = null);
    Game? GetGame(Guid gameId);
    (bool Success, string? Error) FlipCard(Guid gameId, Guid cardId);
    (bool Success, string? Error) ResolveTurn(Guid gameId);
    GameStateDto? GetState(Guid gameId);
}

public class GameService : IGameService
{
    private readonly IGameFactory _gameFactory;
    private readonly IMoveValidator _moveValidator;
    private readonly IGameRepository _repository;

    public GameService(IGameFactory gameFactory, IMoveValidator moveValidator, IGameRepository repository)
    {
        _gameFactory = gameFactory;
        _moveValidator = moveValidator;
        _repository = repository;
    }

    public Game StartNewGame(int boardSize = 4, int? maxAttempts = null)
    {
        if (boardSize < 2 || boardSize > 10 || boardSize % 2 != 0)
            throw new ArgumentOutOfRangeException(nameof(boardSize), "Board size must be an even number between 2 and 10.");
        var game = _gameFactory.Create(boardSize, maxAttempts);
        _repository.Save(game);
        return game;
    }

    public Game? GetGame(Guid gameId) => _repository.GetById(gameId);

    /// <summary>Flips one card. Does not resolve the turn — client calls ResolveTurn after showing both cards.</summary>
    public (bool Success, string? Error) FlipCard(Guid gameId, Guid cardId)
    {
        var game = _repository.GetById(gameId);
        if (game is null) return (false, "Game not found.");
        var (isValid, error) = _moveValidator.CanFlip(game, cardId);
        if (!isValid) return (false, error);
        try
        {
            game.FlipCard(cardId);
            _repository.Save(game);
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    /// <summary>Resolves the current turn (2 flipped cards): match → stay open, no match → flip back. Call after user has seen both cards.</summary>
    public (bool Success, string? Error) ResolveTurn(Guid gameId)
    {
        var game = _repository.GetById(gameId);
        if (game is null) return (false, "Game not found.");
        try
        {
            game.ResolveTurn();
            _repository.Save(game);
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public GameStateDto? GetState(Guid gameId)
    {
        var game = _repository.GetById(gameId);
        if (game is null) return null;
        return ToStateDto(game);
    }

    public static GameStateDto ToStateDto(Game game)
    {
        var cards = game.Cards
            .OrderBy(c => c.Position)
            .Select(c => new CardStateDto
            {
                Id = c.Id,
                Position = c.Position,
                IsFlipped = c.IsFlipped,
                IsMatched = c.IsMatched,
                PairId = c.IsMatched || c.IsFlipped ? c.PairId : (int?)null
            })
            .ToList();
        return new GameStateDto
        {
            Id = game.Id,
            BoardSize = game.BoardSize,
            Score = game.Score,
            MoveCount = game.MoveCount,
            MaxAttempts = game.MaxAttempts,
            IsFinished = game.IsFinished,
            AllPairsMatched = game.AllPairsMatched,
            HasAttemptsLeft = game.HasAttemptsLeft,
            FlippedCountThisTurn = game.CurrentTurn.FlippedCount,
            StartedAt = game.StartedAt,
            FinishedAt = game.FinishedAt,
            Cards = cards
        };
    }
}

public class GameStateDto
{
    public Guid Id { get; set; }
    public int BoardSize { get; set; }
    public int Score { get; set; }
    public int MoveCount { get; set; }
    public int? MaxAttempts { get; set; }
    public bool IsFinished { get; set; }
    public bool AllPairsMatched { get; set; }
    public bool HasAttemptsLeft { get; set; }
    public int FlippedCountThisTurn { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public List<CardStateDto> Cards { get; set; } = new();
}

public class CardStateDto
{
    public Guid Id { get; set; }
    public int Position { get; set; }
    public bool IsFlipped { get; set; }
    public bool IsMatched { get; set; }
    public int? PairId { get; set; }
}

public interface IGameRepository
{
    void Save(Game game);
    Game? GetById(Guid id);
}
