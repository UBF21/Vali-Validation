using Vali_Validation.Core.Results;
using Vali_Validation.Core.Rules;
using Vali_Validation.Core.Validators;
using Xunit;

namespace Vali_Validation.Tests.Localization;

public class CollectionRulesLocalizationDto
{
    public List<int>? Value { get; set; }
}

public class CollectionRulesLocalizationScalarDto
{
    public int Value { get; set; }
}

public class CollectionRulesLocalizationTests
{
    private static ValidationResult ValidateWith(Action<IRuleBuilder<CollectionRulesLocalizationDto, List<int>?>> configure, List<int>? value)
    {
        var v = new InlineValidator(configure);
        return v.Validate(new CollectionRulesLocalizationDto { Value = value }, opts => opts.WithLanguage("en"));
    }

    private class InlineValidator : AbstractValidator<CollectionRulesLocalizationDto>
    {
        public InlineValidator(Action<IRuleBuilder<CollectionRulesLocalizationDto, List<int>?>> configure)
            => configure(RuleFor(x => x.Value));
    }

    private static ValidationResult ValidateScalarWith(Action<IRuleBuilder<CollectionRulesLocalizationScalarDto, int>> configure, int value)
    {
        var v = new ScalarInlineValidator(configure);
        return v.Validate(new CollectionRulesLocalizationScalarDto { Value = value }, opts => opts.WithLanguage("en"));
    }

    private class ScalarInlineValidator : AbstractValidator<CollectionRulesLocalizationScalarDto>
    {
        public ScalarInlineValidator(Action<IRuleBuilder<CollectionRulesLocalizationScalarDto, int>> configure)
            => configure(RuleFor(x => x.Value));
    }

    [Fact] public void HasCount_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must contain exactly 3 items.",
            ValidateWith(r => r.HasCount(3), new List<int> { 1, 2 }).Errors["Value"][0]);

    [Fact] public void NotEmptyCollection_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must not be an empty collection.",
            ValidateWith(r => r.NotEmptyCollection(), new List<int>()).Errors["Value"][0]);

    [Fact] public void MinCount_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must contain at least 3 items.",
            ValidateWith(r => r.MinCount(3), new List<int> { 1 }).Errors["Value"][0]);

    [Fact] public void MaxCount_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must contain at most 1 items.",
            ValidateWith(r => r.MaxCount(1), new List<int> { 1, 2 }).Errors["Value"][0]);

    [Fact] public void Unique_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must not contain duplicate values.",
            ValidateWith(r => r.Unique(), new List<int> { 1, 1 }).Errors["Value"][0]);

    [Fact] public void AllSatisfy_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field: all items must satisfy the condition.",
            ValidateWith(r => r.AllSatisfy(o => (int)o > 0), new List<int> { 1, -1 }).Errors["Value"][0]);

    [Fact] public void AnySatisfy_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field: at least one item must satisfy the condition.",
            ValidateWith(r => r.AnySatisfy(o => (int)o > 10), new List<int> { 1, 2 }).Errors["Value"][0]);

    [Fact] public void In_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must be in the list of allowed values.",
            ValidateScalarWith(r => r.In(new List<int> { 1, 2 }), 3).Errors["Value"][0]);

    [Fact] public void NotIn_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must not be in the list of disallowed values.",
            ValidateScalarWith(r => r.NotIn(new List<int> { 1, 2 }), 1).Errors["Value"][0]);

    // Spanish spot-checks
    [Fact] public void MinCount_Spanish_MatchesSpanishCatalog()
    {
        var v = new InlineValidator(r => r.MinCount(3));
        var result = v.Validate(new CollectionRulesLocalizationDto { Value = new List<int> { 1 } }, opts => opts.WithLanguage("es"));
        Assert.Equal("El campo Value debe contener al menos 3 elementos.", result.Errors["Value"][0]);
    }

    [Fact] public void Unique_Spanish_MatchesSpanishCatalog()
    {
        var v = new InlineValidator(r => r.Unique());
        var result = v.Validate(new CollectionRulesLocalizationDto { Value = new List<int> { 1, 1 } }, opts => opts.WithLanguage("es"));
        Assert.Equal("El campo Value no debe contener valores duplicados.", result.Errors["Value"][0]);
    }
}
