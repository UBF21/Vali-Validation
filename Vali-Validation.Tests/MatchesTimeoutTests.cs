using System.Diagnostics;
using Vali_Validation.Core.Validators;
using Xunit;

namespace Vali_Validation.Tests;

public class MatchesTimeoutDto
{
    public string? Value { get; set; }
}

public class MatchesTimeoutValidator : AbstractValidator<MatchesTimeoutDto>
{
    public MatchesTimeoutValidator()
    {
        // Classic catastrophic-backtracking pattern: nested quantifiers with no anchor,
        // matched against a long string with no valid match — exhibits exponential blowup
        // on pre-.NET-7 regex engines and can still be slow depending on input shape.
        RuleFor(x => x.Value).Matches("(a+)+$");
    }
}

public class MatchesTimeoutTests
{
    [Fact]
    public void Matches_WithCatastrophicBacktrackingPattern_DoesNotHangIndefinitely()
    {
        var validator = new MatchesTimeoutValidator();
        string adversarialInput = new string('a', 40) + "!";

        var stopwatch = Stopwatch.StartNew();
        var result = validator.Validate(new MatchesTimeoutDto { Value = adversarialInput });
        stopwatch.Stop();

        // The rule should complete (either matched=false due to timeout-as-failure, or a
        // clean false from a genuine non-match) well within a bounded time, not hang.
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(5), $"Matches() took {stopwatch.Elapsed}, expected it to be bounded by the regex timeout.");
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Matches_WithNormalPatternAndInput_StillWorksCorrectly()
    {
        var validator = new MatchesTimeoutValidator();

        var validResult = validator.Validate(new MatchesTimeoutDto { Value = "aaa" });
        var invalidResult = validator.Validate(new MatchesTimeoutDto { Value = "bbb" });

        Assert.True(validResult.IsValid);
        Assert.False(invalidResult.IsValid);
    }
}
