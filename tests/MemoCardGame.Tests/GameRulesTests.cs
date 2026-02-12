using MemoCardGame.Application;
using MemoCardGame.Domain;
using MemoCardGame.Tests;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MemoCardGame.Tests;

public class GameRulesTests
{
    private static IGameService CreateService()
    {
        var services = new ServiceCollection();
        services.AddApplication();
        services.AddScoped<IGameRepository, InMemoryGameRepository>();
        return services.BuildServiceProvider().GetRequiredService<IGameService>();
    }

    [Fact]
    public void Cannot_flip_same_card_twice_in_same_turn()
    {
        var svc = CreateService();
        var game = svc.StartNewGame(2, maxAttempts: 10);
        var cardId = game.Cards.First().Id;

        svc.FlipCard(game.Id, cardId);
        var (success, error) = svc.FlipCard(game.Id, cardId);

        Assert.False(success);
        Assert.Contains("same card twice", error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Third_flip_in_same_turn_fails()
    {
        var game = Game.Create(Guid.NewGuid(), 2, maxAttempts: 10);
        var twoCards = game.Cards.Take(2).Select(c => c.Id).ToList();
        game.FlipCard(twoCards[0]);
        game.FlipCard(twoCards[1]);
        var thirdCard = game.Cards[2].Id;
        Assert.Throws<InvalidOperationException>(() => game.FlipCard(thirdCard));
    }

    [Fact]
    public void Cannot_flip_already_matched_card()
    {
        var game = Game.Create(Guid.NewGuid(), 2, null);
        var pair = game.Cards.GroupBy(c => c.PairId).First().ToList();
        game.FlipCard(pair[0].Id);
        game.FlipCard(pair[1].Id);
        game.ResolveTurn();

        var ex = Assert.Throws<InvalidOperationException>(() => game.FlipCard(pair[0].Id));
        Assert.Contains("cannot be flipped", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Score_updates_correctly_on_match_and_mismatch()
    {
        var game = Game.Create(Guid.NewGuid(), 4, null);
        Assert.Equal(0, game.Score);

        var pairs = game.Cards.GroupBy(c => c.PairId).ToList();
        var matchPair = pairs[0].ToList();
        var mismatchCard1 = pairs[1].First().Id;
        var mismatchCard2 = pairs[2].First().Id;

        game.FlipCard(matchPair[0].Id);
        game.FlipCard(matchPair[1].Id);
        game.ResolveTurn();
        Assert.Equal(ScoreRules.PointsPerMatch, game.Score);

        game.FlipCard(mismatchCard1);
        game.FlipCard(mismatchCard2);
        game.ResolveTurn();
        Assert.Equal(ScoreRules.PointsPerMatch + ScoreRules.PenaltyPerFailedAttempt, game.Score);
    }

    [Fact]
    public void Game_ends_only_when_all_pairs_found()
    {
        var game = Game.Create(Guid.NewGuid(), 2, null);
        Assert.False(game.IsFinished);

        foreach (var pair in game.Cards.GroupBy(c => c.PairId))
        {
            var list = pair.ToList();
            game.FlipCard(list[0].Id);
            game.FlipCard(list[1].Id);
            game.ResolveTurn();
        }

        Assert.True(game.AllPairsMatched);
        Assert.True(game.IsFinished);
    }

    [Fact]
    public void Game_ends_when_max_attempts_reached()
    {
        var game = Game.Create(Guid.NewGuid(), 2, maxAttempts: 1);
        var cards = game.Cards.Take(2).ToList();
        game.FlipCard(cards[0].Id);
        game.FlipCard(cards[1].Id);
        game.ResolveTurn();

        Assert.True(game.IsFinished);
        Assert.False(game.AllPairsMatched);
    }

    [Fact]
    public void Cannot_flip_after_game_finished()
    {
        var game = Game.Create(Guid.NewGuid(), 2, null);
        foreach (var pair in game.Cards.GroupBy(c => c.PairId))
        {
            var list = pair.ToList();
            game.FlipCard(list[0].Id);
            game.FlipCard(list[1].Id);
            game.ResolveTurn();
        }

        var anyCard = game.Cards.First().Id;
        Assert.Throws<InvalidOperationException>(() => game.FlipCard(anyCard));
    }

    [Fact]
    public void MoveValidator_rejects_third_flip()
    {
        var validator = new MoveValidator();
        var game = Game.Create(Guid.NewGuid(), 2, null);
        var ids = game.Cards.Take(2).Select(c => c.Id).ToList();
        game.FlipCard(ids[0]);
        game.FlipCard(ids[1]);

        var (valid, _) = validator.CanFlip(game, game.Cards[2].Id);
        Assert.False(valid);
    }

    [Fact]
    public void GetState_returns_masked_cards_for_unflipped()
    {
        var svc = CreateService();
        var game = svc.StartNewGame(2);
        var state = svc.GetState(game.Id);

        Assert.NotNull(state);
        var hidden = state!.Cards.Where(c => !c.IsFlipped && !c.IsMatched).ToList();
        Assert.All(hidden, c => Assert.Null(c.PairId));
    }
}
