using Vali_Validation.Core.Results;
using Vali_Validation.Core.Rules;
using Vali_Validation.Core.Validators;
using Xunit;

namespace Vali_Validation.Tests.Localization;

public class StringRulesLocalizationDto
{
    public string? Value { get; set; }
}

public class StringRulesLocalizationTests
{
    private static ValidationResult ValidateWith(Action<IRuleBuilder<StringRulesLocalizationDto, string?>> configure, string? value)
    {
        var v = new InlineValidator(configure);
        return v.Validate(new StringRulesLocalizationDto { Value = value }, opts => opts.WithLanguage("en"));
    }

    private class InlineValidator : AbstractValidator<StringRulesLocalizationDto>
    {
        public InlineValidator(Action<IRuleBuilder<StringRulesLocalizationDto, string?>> configure)
            => configure(RuleFor(x => x.Value));
    }

    [Fact] public void NotEmpty_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field cannot be empty.",
            ValidateWith(r => r.NotEmpty(), "").Errors["Value"][0]);

    [Fact] public void MustContain_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must contain 'abc'.",
            ValidateWith(r => r.MustContain("abc"), "xyz").Errors["Value"][0]);

    [Fact] public void MinimumLength_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must be at least 5 characters long.",
            ValidateWith(r => r.MinimumLength(5), "ab").Errors["Value"][0]);

    [Fact] public void MaximumLength_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must be no longer than 3 characters.",
            ValidateWith(r => r.MaximumLength(3), "abcdef").Errors["Value"][0]);

    [Fact] public void Matches_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field is not in the correct format.",
            ValidateWith(r => r.Matches("^[0-9]+$"), "abc").Errors["Value"][0]);

    [Fact] public void StartsWith_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must begin with 'ab'.",
            ValidateWith(r => r.StartsWith("ab"), "xy").Errors["Value"][0]);

    [Fact] public void EndsWith_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must end with 'xy'.",
            ValidateWith(r => r.EndsWith("xy"), "ab").Errors["Value"][0]);

    [Fact] public void NotContains_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must not contain 'ab'.",
            ValidateWith(r => r.NotContains("ab"), "abc").Errors["Value"][0]);

    [Fact] public void NoWhitespace_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must not contain whitespace.",
            ValidateWith(r => r.NoWhitespace(), "a b").Errors["Value"][0]);

    [Fact] public void Lowercase_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must be all lowercase.",
            ValidateWith(r => r.Lowercase(), "ABC").Errors["Value"][0]);

    [Fact] public void Uppercase_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must be all uppercase.",
            ValidateWith(r => r.Uppercase(), "abc").Errors["Value"][0]);

    [Fact] public void MinWords_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must contain at least 3 words.",
            ValidateWith(r => r.MinWords(3), "one two").Errors["Value"][0]);

    [Fact] public void MaxWords_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must contain at most 2 words.",
            ValidateWith(r => r.MaxWords(2), "one two three").Errors["Value"][0]);

    // Spanish spot-checks — a handful, not all 13.
    [Fact] public void MinimumLength_Spanish_MatchesSpanishCatalog()
    {
        var v = new InlineValidator(r => r.MinimumLength(5));
        var result = v.Validate(new StringRulesLocalizationDto { Value = "ab" }, opts => opts.WithLanguage("es"));
        Assert.Equal("El campo Value debe tener al menos 5 caracteres.", result.Errors["Value"][0]);
    }

    [Fact] public void MustContain_WithCustomMessage_OverridesAndStillSubstitutesArgs()
    {
        var v = new InlineValidator(r => r.MustContain("abc").WithMessage("Custom: needs '{substring}' in {PropertyName}"));
        var result = v.Validate(new StringRulesLocalizationDto { Value = "xyz" });
        Assert.Equal("Custom: needs 'abc' in Value", result.Errors["Value"][0]);
    }
}
