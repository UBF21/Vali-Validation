using Vali_Validation.Core.Results;
using Vali_Validation.Core.Rules;
using Vali_Validation.Core.Validators;
using Xunit;

namespace Vali_Validation.Tests.Localization;

public class NumericRulesLocalizationDto
{
    public decimal? Value { get; set; }
}

public class NumericRulesLocalizationPairDto
{
    public decimal A { get; set; }
    public decimal B { get; set; }
}

public class NumericRulesLocalizationIntDto
{
    public int Value { get; set; }
}

public class NumericRulesLocalizationTests
{
    private static ValidationResult ValidateWith(Action<IRuleBuilder<NumericRulesLocalizationDto, decimal?>> configure, decimal? value)
    {
        var v = new InlineValidator(configure);
        return v.Validate(new NumericRulesLocalizationDto { Value = value }, opts => opts.WithLanguage("en"));
    }

    private class InlineValidator : AbstractValidator<NumericRulesLocalizationDto>
    {
        public InlineValidator(Action<IRuleBuilder<NumericRulesLocalizationDto, decimal?>> configure)
            => configure(RuleFor(x => x.Value));
    }

    private static ValidationResult ValidatePairWith(Action<IRuleBuilder<NumericRulesLocalizationPairDto, decimal>> configure, decimal a, decimal b)
    {
        var v = new PairInlineValidator(configure);
        return v.Validate(new NumericRulesLocalizationPairDto { A = a, B = b }, opts => opts.WithLanguage("en"));
    }

    private class PairInlineValidator : AbstractValidator<NumericRulesLocalizationPairDto>
    {
        public PairInlineValidator(Action<IRuleBuilder<NumericRulesLocalizationPairDto, decimal>> configure)
            => configure(RuleFor(x => x.A));
    }

    // NotZero() compares via IComparable.CompareTo(0) (a boxed int), which throws for a
    // boxed decimal (Decimal.CompareTo requires the same runtime type) — a pre-existing
    // quirk unrelated to this task. Use an int-typed DTO for that one rule only.
    private static ValidationResult ValidateIntWith(Action<IRuleBuilder<NumericRulesLocalizationIntDto, int>> configure, int value)
    {
        var v = new IntInlineValidator(configure);
        return v.Validate(new NumericRulesLocalizationIntDto { Value = value }, opts => opts.WithLanguage("en"));
    }

    private class IntInlineValidator : AbstractValidator<NumericRulesLocalizationIntDto>
    {
        public IntInlineValidator(Action<IRuleBuilder<NumericRulesLocalizationIntDto, int>> configure)
            => configure(RuleFor(x => x.Value));
    }

    [Fact] public void Positive_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must be a positive number.",
            ValidateWith(r => r.Positive(), -1).Errors["Value"][0]);

    [Fact] public void Negative_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must be a negative number.",
            ValidateWith(r => r.Negative(), 1).Errors["Value"][0]);

    [Fact] public void NotZero_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must not be zero.",
            ValidateIntWith(r => r.NotZero(), 0).Errors["Value"][0]);

    [Fact] public void NonNegative_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must be non-negative (zero or greater).",
            ValidateWith(r => r.NonNegative(), -1).Errors["Value"][0]);

    [Fact] public void Percentage_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must be a valid percentage between 0 and 100.",
            ValidateWith(r => r.Percentage(), 150).Errors["Value"][0]);

    [Fact] public void Precision_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must have at most 4 total digits and 1 decimal places.",
            ValidateWith(r => r.Precision(4, 1), 123.45m).Errors["Value"][0]);

    [Fact] public void MultipleOf_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must be a multiple of 5.",
            ValidateWith(r => r.MultipleOf(5), 7).Errors["Value"][0]);

    [Fact] public void MultipleOfProperty_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The A field must be a multiple of B.",
            ValidatePairWith(r => r.MultipleOfProperty(x => x.B), 7, 5).Errors["A"][0]);

    [Fact] public void Odd_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must be an odd number.",
            ValidateWith(r => r.Odd(), 4).Errors["Value"][0]);

    [Fact] public void Even_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must be an even number.",
            ValidateWith(r => r.Even(), 3).Errors["Value"][0]);

    [Fact] public void MaxDecimalPlaces_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must have at most 2 decimal places.",
            ValidateWith(r => r.MaxDecimalPlaces(2), 1.234m).Errors["Value"][0]);

    // Spanish spot-checks
    [Fact] public void Precision_Spanish_MatchesSpanishCatalog()
    {
        var v = new InlineValidator(r => r.Precision(4, 1));
        var result = v.Validate(new NumericRulesLocalizationDto { Value = 123.45m }, opts => opts.WithLanguage("es"));
        Assert.Equal("El campo Value debe tener como máximo 4 dígitos totales y 1 decimales.", result.Errors["Value"][0]);
    }

    [Fact] public void MultipleOf_Spanish_MatchesSpanishCatalog()
    {
        var v = new InlineValidator(r => r.MultipleOf(5));
        var result = v.Validate(new NumericRulesLocalizationDto { Value = 7 }, opts => opts.WithLanguage("es"));
        Assert.Equal("El campo Value debe ser un múltiplo de 5.", result.Errors["Value"][0]);
    }
}
