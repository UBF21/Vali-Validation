using Vali_Validation.Core.Results;
using Vali_Validation.Core.Rules;
using Vali_Validation.Core.Validators;
using Xunit;

namespace Vali_Validation.Tests.Localization;

public class ComparisonRulesLocalizationDto
{
    public string? Value { get; set; }
}

public class ComparisonRulesLocalizationNumericDto
{
    public int Value { get; set; }
}

public class ComparisonRulesLocalizationPairDto
{
    public int A { get; set; }
    public int B { get; set; }
}

public class ComparisonRulesLocalizationTests
{
    private static ValidationResult ValidateWith(Action<IRuleBuilder<ComparisonRulesLocalizationDto, string?>> configure, string? value)
    {
        var v = new InlineValidator(configure);
        return v.Validate(new ComparisonRulesLocalizationDto { Value = value }, opts => opts.WithLanguage("en"));
    }

    private class InlineValidator : AbstractValidator<ComparisonRulesLocalizationDto>
    {
        public InlineValidator(Action<IRuleBuilder<ComparisonRulesLocalizationDto, string?>> configure)
            => configure(RuleFor(x => x.Value));
    }

    private static ValidationResult ValidateNumericWith(Action<IRuleBuilder<ComparisonRulesLocalizationNumericDto, int>> configure, int value)
    {
        var v = new NumericInlineValidator(configure);
        return v.Validate(new ComparisonRulesLocalizationNumericDto { Value = value }, opts => opts.WithLanguage("en"));
    }

    private class NumericInlineValidator : AbstractValidator<ComparisonRulesLocalizationNumericDto>
    {
        public NumericInlineValidator(Action<IRuleBuilder<ComparisonRulesLocalizationNumericDto, int>> configure)
            => configure(RuleFor(x => x.Value));
    }

    private static ValidationResult ValidatePairWith(Action<IRuleBuilder<ComparisonRulesLocalizationPairDto, int>> configure, int a, int b)
    {
        var v = new PairInlineValidator(configure);
        return v.Validate(new ComparisonRulesLocalizationPairDto { A = a, B = b }, opts => opts.WithLanguage("en"));
    }

    private class PairInlineValidator : AbstractValidator<ComparisonRulesLocalizationPairDto>
    {
        public PairInlineValidator(Action<IRuleBuilder<ComparisonRulesLocalizationPairDto, int>> configure)
            => configure(RuleFor(x => x.A));
    }

    [Fact] public void Must_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field does not meet the specified condition.",
            ValidateWith(r => r.Must(v => v == "ok"), "no").Errors["Value"][0]);

    [Fact] public void EqualTo_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must be equal to 'x'.",
            ValidateWith(r => r.EqualTo("x"), "y").Errors["Value"][0]);

    [Fact] public void NotEqual_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must not be equal to 'x'.",
            ValidateWith(r => r.NotEqual("x"), "x").Errors["Value"][0]);

    [Fact] public void GreaterThan_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must be greater than 10.",
            ValidateNumericWith(r => r.GreaterThan(10), 5).Errors["Value"][0]);

    [Fact] public void LessThan_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must be less than 10.",
            ValidateNumericWith(r => r.LessThan(10), 15).Errors["Value"][0]);

    [Fact] public void GreaterThanOrEqualTo_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must be greater than or equal to 10.",
            ValidateNumericWith(r => r.GreaterThanOrEqualTo(10), 5).Errors["Value"][0]);

    [Fact] public void LessThanOrEqualTo_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must be less than or equal to 10.",
            ValidateNumericWith(r => r.LessThanOrEqualTo(10), 15).Errors["Value"][0]);

    [Fact] public void Between_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must be between 1 and 5.",
            ValidateNumericWith(r => r.Between(1, 5), 10).Errors["Value"][0]);

    [Fact] public void ExclusiveBetween_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must be exclusively between 1 and 5.",
            ValidateNumericWith(r => r.ExclusiveBetween(1, 5), 10).Errors["Value"][0]);

    [Fact] public void LengthBetween_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must be between 2 and 4 characters long.",
            ValidateWith(r => r.LengthBetween(2, 4), "abcdef").Errors["Value"][0]);

    [Fact] public void NotNull_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field cannot be null.",
            ValidateWith(r => r.NotNull(), null).Errors["Value"][0]);

    [Fact] public void Null_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must be null.",
            ValidateWith(r => r.Null(), "not null").Errors["Value"][0]);

    [Fact] public void Empty_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must be empty.",
            ValidateWith(r => r.Empty(), "not empty").Errors["Value"][0]);

    [Fact] public void EqualToProperty_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The A field must equal B.",
            ValidatePairWith(r => r.EqualToProperty(x => x.B), 1, 2).Errors["A"][0]);

    [Fact] public void GreaterThanProperty_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The A field must be greater than B.",
            ValidatePairWith(r => r.GreaterThanProperty(x => x.B), 1, 2).Errors["A"][0]);

    [Fact] public void GreaterThanOrEqualToProperty_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The A field must be greater than or equal to B.",
            ValidatePairWith(r => r.GreaterThanOrEqualToProperty(x => x.B), 1, 2).Errors["A"][0]);

    [Fact] public void LessThanProperty_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The A field must be less than B.",
            ValidatePairWith(r => r.LessThanProperty(x => x.B), 3, 2).Errors["A"][0]);

    [Fact] public void LessThanOrEqualToProperty_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The A field must be less than or equal to B.",
            ValidatePairWith(r => r.LessThanOrEqualToProperty(x => x.B), 3, 2).Errors["A"][0]);

    [Fact] public void NotEqualToProperty_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The A field must not be equal to B.",
            ValidatePairWith(r => r.NotEqualToProperty(x => x.B), 2, 2).Errors["A"][0]);

    [Fact] public void RequiredIf_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field is required.",
            ValidateWith(r => r.RequiredIf(_ => true), null).Errors["Value"][0]);

    // Spanish spot-checks
    [Fact] public void GreaterThan_Spanish_MatchesSpanishCatalog()
    {
        var v = new NumericInlineValidator(r => r.GreaterThan(10));
        var result = v.Validate(new ComparisonRulesLocalizationNumericDto { Value = 5 }, opts => opts.WithLanguage("es"));
        Assert.Equal("El campo Value debe ser mayor que 10.", result.Errors["Value"][0]);
    }

    [Fact] public void EqualToProperty_Spanish_MatchesSpanishCatalog()
    {
        var v = new PairInlineValidator(r => r.EqualToProperty(x => x.B));
        var result = v.Validate(new ComparisonRulesLocalizationPairDto { A = 1, B = 2 }, opts => opts.WithLanguage("es"));
        Assert.Equal("El campo A debe ser igual a B.", result.Errors["A"][0]);
    }
}
