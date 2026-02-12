using MemoCardGame.Domain;

namespace MemoCardGame.Application;

public interface IMoveValidator
{
    (bool IsValid, string? Error) CanFlip(Game game, Guid cardId);
}

public class MoveValidator : IMoveValidator
{
    public (bool IsValid, string? Error) CanFlip(Game game, Guid cardId)
    {
        if (game is null)
            return (false, "Game not found.");
        if (game.IsFinished)
            return (false, "Game is already finished.");
        if (!game.HasAttemptsLeft)
            return (false, "No attempts left.");
        if (!game.CurrentTurn.CanFlipAnother)
            return (false, "Already flipped 2 cards this turn.");
        var card = game.GetCard(cardId);
        if (card == null)
            return (false, "Card not found.");
        if (game.CurrentTurn.FlippedCardIds.Contains(cardId))
            return (false, "Cannot flip the same card twice in one turn.");
        if (!card.CanBeFlipped)
            return (false, "Card cannot be flipped (already flipped or matched).");
        return (true, null);
    }
}
