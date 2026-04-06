using MemoCardGame.Application;
using Xunit;

namespace MemoCardGame.Tests;

public class DisplayNameValidationTests
{
    [Fact]
    public void Validate_accepts_plain_unicode_name()
    {
        var r = DisplayNameValidation.Validate("Jana");
        Assert.True(r.Ok);
        Assert.Equal("Jana", r.DisplayName);
    }

    [Fact]
    public void Validate_rejects_too_short()
    {
        var r = DisplayNameValidation.Validate("a");
        Assert.False(r.Ok);
    }

    [Fact]
    public void Validate_rejects_common_profanity_with_explanation()
    {
        var r = DisplayNameValidation.Validate("player shit here");
        Assert.False(r.Ok);
        Assert.NotNull(r.ErrorMessage);
        Assert.Contains("leaderboard", r.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("profanity", r.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("Pica")]
    [InlineData("píča")]
    [InlineData("Player Piča")]
    [InlineData("Blablapica")]
    [InlineData("pojeb")]
    [InlineData("Vyjebaný")]
    [InlineData("buzerant")]
    public void Validate_rejects_czech_and_slovak_profanity_with_or_without_diacritics(string name)
    {
        var r = DisplayNameValidation.Validate(name);
        Assert.False(r.Ok);
    }

    [Theory]
    [InlineData("Pikachu")]
    [InlineData("picachu")]
    [InlineData("Grape")]
    [InlineData("cepice")]
    [InlineData("čepice")]
    [InlineData("capica")]
    [InlineData("Class")]
    public void Validate_accepts_allowlisted_safe_words_even_if_similar_to_blockers(string name)
    {
        var r = DisplayNameValidation.Validate(name);
        Assert.True(r.Ok, r.ErrorMessage);
    }
}
