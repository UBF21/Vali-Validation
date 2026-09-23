using Vali_Validation.Core.Results;
using Vali_Validation.Core.Rules;
using Vali_Validation.Core.Validators;
using Xunit;

namespace Vali_Validation.Tests.Localization;

public class AsyncRulesLocalizationDto
{
    public string? Value { get; set; }
}

public class AsyncRulesLocalizationPairDto
{
    public string? A { get; set; }
    public string? B { get; set; }
}

public class AsyncRulesLocalizationTests
{
    private class MustAsyncValidator : AbstractValidator<AsyncRulesLocalizationDto>
    {
        public MustAsyncValidator() => RuleFor(x => x.Value).MustAsync(_ => Task.FromResult(false));
    }

    private class DependentRuleAsyncValidator : AbstractValidator<AsyncRulesLocalizationPairDto>
    {
        public DependentRuleAsyncValidator() =>
            RuleFor(x => x.A).DependentRuleAsync(x => x.A, x => x.B, (a, b) => Task.FromResult(false));
    }

    [Fact]
    public async Task MustAsync_Default_MatchesOriginalEnglishText()
    {
        var result = await new MustAsyncValidator().ValidateAsync(
            new AsyncRulesLocalizationDto { Value = "x" }, opts => opts.WithLanguage("en"));
        Assert.Equal("The Value field does not meet the specified condition.", result.Errors["Value"][0]);
    }

    [Fact]
    public async Task DependentRuleAsync_Default_MatchesOriginalEnglishText()
    {
        var result = await new DependentRuleAsyncValidator().ValidateAsync(
            new AsyncRulesLocalizationPairDto { A = "a", B = "b" }, opts => opts.WithLanguage("en"));
        Assert.Equal("The field A does not meet the dependent condition of B.", result.Errors["A"][0]);
    }

    [Fact]
    public async Task MustAsync_Spanish_MatchesSpanishCatalog()
    {
        var result = await new MustAsyncValidator().ValidateAsync(
            new AsyncRulesLocalizationDto { Value = "x" }, opts => opts.WithLanguage("es"));
        Assert.Equal("El campo Value no cumple con la condición especificada.", result.Errors["Value"][0]);
    }

    [Fact]
    public async Task DependentRuleAsync_Spanish_MatchesSpanishCatalog()
    {
        var result = await new DependentRuleAsyncValidator().ValidateAsync(
            new AsyncRulesLocalizationPairDto { A = "a", B = "b" }, opts => opts.WithLanguage("es"));
        Assert.Equal("El campo A no cumple con la condición dependiente de B.", result.Errors["A"][0]);
    }
}
