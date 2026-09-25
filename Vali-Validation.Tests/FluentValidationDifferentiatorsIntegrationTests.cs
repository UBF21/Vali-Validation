using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Vali_Validation.Core.Configuration;
using Vali_Validation.Core.Localization;
using Vali_Validation.Core.Results;
using Vali_Validation.Core.Rules;
using Vali_Validation.Core.Validators;
using Xunit;

namespace Vali_Validation.Tests;

/// <summary>
/// Combined-feature coverage for the capabilities Vali-Validation has that FluentValidation
/// doesn't: Severity, RuleSets, block-level When/Unless, InjectValidator, Localization, global
/// config, and PropertyValidator. Individual feature files already cover each in isolation — these
/// tests exercise realistic combinations that only surface bugs at the seams between features.
/// </summary>
[Collection("Global state")]
public class FluentValidationDifferentiatorsIntegrationTests
{
    // -------------------------------------------------------------------------
    // Severity + RuleSets + async
    // -------------------------------------------------------------------------

    public class SeverityRuleSetDto
    {
        public string? Email { get; set; }
        public string? PromoCode { get; set; }
    }

    private class SeverityRuleSetValidator : AbstractValidator<SeverityRuleSetDto>
    {
        public SeverityRuleSetValidator()
        {
            RuleFor(x => x.Email).NotEmpty().InRuleSet("registration");

            RuleFor(x => x.PromoCode)
                .MustAsync(async code => { await Task.Yield(); return code != "EXPIRED"; })
                .WithSeverity(Severity.Warning)
                .WithErrorCode("PROMO_EXPIRED")
                .InRuleSet("checkout");
        }
    }

    [Fact]
    public async Task Severity_And_RuleSets_And_AsyncFix_Compose_Correctly()
    {
        var validator = new SeverityRuleSetValidator();
        var dto = new SeverityRuleSetDto { Email = "", PromoCode = "EXPIRED" };

        // No rule-set filter: everything runs. Email failure (Error) blocks IsValid;
        // PromoCode failure is a Warning (thanks to the async-modifiers fix) and does not.
        var full = await validator.ValidateAsync(dto, _ => { });
        Assert.False(full.IsValid);
        Assert.Equal(2, full.Failures.Count);
        var promoFailure = Assert.Single(full.Failures, f => f.PropertyName == "PromoCode");
        Assert.Equal(Severity.Warning, promoFailure.Severity);
        Assert.Equal("PROMO_EXPIRED", promoFailure.ErrorCode);

        // Filtered to "checkout" only: Email's rule (tagged "registration") is excluded, so the
        // only failure is the Warning-severity PromoCode rule — IsValid becomes true.
        var checkoutOnly = await validator.ValidateAsync(dto, o => o.IncludeRuleSets("checkout"));
        Assert.True(checkoutOnly.IsValid);
        var onlyFailure = Assert.Single(checkoutOnly.Failures);
        Assert.Equal("PromoCode", onlyFailure.PropertyName);
    }

    // -------------------------------------------------------------------------
    // Block-level When + InjectValidator (DI-resolved nested validator)
    // -------------------------------------------------------------------------

    public class AddressDto
    {
        public string? Line1 { get; set; }
    }

    public class OrderDto
    {
        public bool RequiresShipping { get; set; }
        public AddressDto? ShippingAddress { get; set; }
    }

    private class AddressValidator : AbstractValidator<AddressDto>
    {
        public AddressValidator() => RuleFor(x => x.Line1).NotEmpty();
    }

    private class OrderValidator : AbstractValidator<OrderDto>
    {
        public OrderValidator(IServiceProvider serviceProvider)
        {
            When(x => x.RequiresShipping, () =>
            {
                RuleFor(x => x.ShippingAddress).InjectValidator(serviceProvider);
            });
        }
    }

    [Fact]
    public void BlockLevelWhen_Gates_InjectValidator_NestedRule()
    {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<AddressDto>, AddressValidator>();
        var provider = services.BuildServiceProvider();
        var validator = new OrderValidator(provider);

        // Gate false: nested InjectValidator rule never runs, even though the nested address is invalid.
        var noShipping = validator.Validate(new OrderDto { RequiresShipping = false, ShippingAddress = new AddressDto { Line1 = "" } });
        Assert.True(noShipping.IsValid);

        // Gate true: nested validator runs and surfaces the nested failure under the prefixed property path.
        var withShipping = validator.Validate(new OrderDto { RequiresShipping = true, ShippingAddress = new AddressDto { Line1 = "" } });
        Assert.False(withShipping.IsValid);
        Assert.Contains(withShipping.Failures, f => f.PropertyName == "ShippingAddress.Line1");
    }

    // -------------------------------------------------------------------------
    // PropertyValidator + Localization + Global Config DisplayNameResolver
    // -------------------------------------------------------------------------

    private class EvenNumberValidator : PropertyValidator<int>
    {
        public override bool IsValid(int value) => value % 2 == 0;

        public override IReadOnlyDictionary<string, string> Messages { get; } = new Dictionary<string, string>
        {
            ["en"] = "{PropertyName} must be an even number.",
            ["es"] = "{PropertyName} debe ser un número par.",
        };
    }

    public class StockDto
    {
        public int UnitsPerBox { get; set; }
    }

    private class StockValidator : AbstractValidator<StockDto>
    {
        public StockValidator() => RuleFor(x => x.UnitsPerBox).SetPropertyValidator(new EvenNumberValidator());
    }

    [Fact]
    public void PropertyValidator_Honors_ExplicitLanguage_And_GlobalDisplayNameResolver()
    {
        var originalResolver = ValiValidationOptions.Global.DisplayNameResolver;
        try
        {
            ValiValidationOptions.Global.DisplayNameResolver = name => name == "UnitsPerBox" ? "Unidades por caja" : name;

            var validator = new StockValidator();
            var result = validator.Validate(new StockDto { UnitsPerBox = 3 }, o => o.WithLanguage("es"));

            var failure = Assert.Single(result.Failures);
            Assert.Equal("Unidades por caja debe ser un número par.", failure.Message);
        }
        finally
        {
            ValiValidationOptions.Global.DisplayNameResolver = originalResolver;
        }
    }

    [Fact]
    public void PropertyValidator_FallsBackToCurrentUICulture_WhenNoExplicitLanguage()
    {
        var previousCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("es");
            var validator = new StockValidator();
            var result = validator.Validate(new StockDto { UnitsPerBox = 3 });

            var failure = Assert.Single(result.Failures);
            Assert.Contains("número par", failure.Message);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previousCulture;
        }
    }

    // -------------------------------------------------------------------------
    // Global Config DefaultCascadeMode + Severity aggregation
    // -------------------------------------------------------------------------

    public class MixedSeverityDto
    {
        public string? Username { get; set; }
    }

    private class MixedSeverityValidator : AbstractValidator<MixedSeverityDto>
    {
        public MixedSeverityValidator()
        {
            RuleFor(x => x.Username).MinimumLength(8).WithSeverity(Severity.Warning);
            RuleFor(x => x.Username).NotEmpty(); // stays Error
        }
    }

    [Fact]
    public void DefaultCascadeMode_Continue_Runs_All_Rules_And_Severity_Aggregates_Correctly()
    {
        Assert.Equal(CascadeMode.Continue, ValiValidationOptions.Global.DefaultCascadeMode);

        var validator = new MixedSeverityValidator();
        var result = validator.Validate(new MixedSeverityDto { Username = "short" });

        // Only the Warning-severity MinimumLength rule fails (NotEmpty passes) — IsValid stays true.
        var failure = Assert.Single(result.Failures);
        Assert.Equal(Severity.Warning, failure.Severity);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void DefaultCascadeMode_Continue_BlocksIsValid_WhenAnyErrorSeverityFailurePresent()
    {
        var validator = new MixedSeverityValidator();
        var result = validator.Validate(new MixedSeverityDto { Username = "" });

        // Both rules fail: MinimumLength (Warning) and NotEmpty (Error). Continue mode runs both;
        // IsValid is false because at least one Error-severity failure exists.
        Assert.Equal(2, result.Failures.Count);
        Assert.False(result.IsValid);
        Assert.Contains(result.Failures, f => f.Severity == Severity.Error);
        Assert.Contains(result.Failures, f => f.Severity == Severity.Warning);
    }
}
