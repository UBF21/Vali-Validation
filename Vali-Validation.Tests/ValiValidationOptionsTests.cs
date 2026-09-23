using Vali_Validation.Core.Configuration;
using Vali_Validation.Core.Validators;
using Xunit;

namespace Vali_Validation.Tests;

public class GlobalConfigDto
{
    public string? FirstName { get; set; }
}

public class GlobalConfigDefaultValidator : AbstractValidator<GlobalConfigDto>
{
    public GlobalConfigDefaultValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty();
        RuleFor(x => x.FirstName).MinimumLength(3);
    }
}

public class GlobalConfigOverridingValidator : AbstractValidator<GlobalConfigDto>
{
    protected override CascadeMode GlobalCascadeMode => CascadeMode.StopOnFirstFailure;

    public GlobalConfigOverridingValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty();
        RuleFor(x => x.FirstName).MinimumLength(3);
    }
}

public class CrossPropertyDto
{
    public int Value { get; set; }
    public int OtherValue { get; set; }
}

public class CrossPropertyValidator : AbstractValidator<CrossPropertyDto>
{
    public CrossPropertyValidator()
    {
        RuleFor(x => x.Value).GreaterThanProperty(x => x.OtherValue);
    }
}

public class MultipleOfPropertyValidator : AbstractValidator<CrossPropertyDto>
{
    public MultipleOfPropertyValidator()
    {
        RuleFor(x => x.Value).MultipleOfProperty(x => x.OtherValue);
    }
}

public class ValiValidationOptionsTests : IDisposable
{
    // Reset global state after every test — these are process-wide statics, and leaking a
    // changed value into an unrelated test elsewhere in the suite would be a real, hard-to-diagnose flake.
    public void Dispose()
    {
        ValiValidationOptions.Global.DefaultCascadeMode = CascadeMode.Continue;
        ValiValidationOptions.Global.DefaultLanguage = "en";
        ValiValidationOptions.Global.DisplayNameResolver = name => name;
        ValiValidationOptions.Global.PropertyNameResolver = name => name;
    }

    [Fact]
    public void DefaultCascadeMode_AppliesToValidatorsThatDoNotOverrideGlobalCascadeMode()
    {
        ValiValidationOptions.Global.DefaultCascadeMode = CascadeMode.StopOnFirstFailure;

        // Pin CurrentUICulture — this test asserts English message text and must not depend on
        // the executing machine's ambient UI culture (same convention as LanguageResolutionTests).
        var previousCulture = System.Globalization.CultureInfo.CurrentUICulture;
        System.Globalization.CultureInfo.CurrentUICulture = new System.Globalization.CultureInfo("en");
        try
        {
            var result = new GlobalConfigDefaultValidator().Validate(new GlobalConfigDto { FirstName = "" });

            // StopOnFirstFailure: only the first failing rule (NotEmpty) should produce an error —
            // MinimumLength never runs.
            Assert.Single(result.Errors["FirstName"]);
            Assert.Equal("The FirstName field cannot be empty.", result.Errors["FirstName"][0]);
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentUICulture = previousCulture;
        }
    }

    [Fact]
    public void DefaultCascadeMode_DoesNotOverrideAValidatorThatExplicitlySetsGlobalCascadeMode()
    {
        ValiValidationOptions.Global.DefaultCascadeMode = CascadeMode.Continue;

        // This validator overrides GlobalCascadeMode => StopOnFirstFailure regardless of the
        // global default set above — the per-class override must still win.
        var result = new GlobalConfigOverridingValidator().Validate(new GlobalConfigDto { FirstName = "" });

        Assert.Single(result.Errors["FirstName"]);
    }

    [Fact]
    public void DefaultCascadeMode_UnsetDefaultsToContinue_PreservesTodaysBehavior()
    {
        // No test above has run first (or Dispose already reset it) — confirm the OOTB default,
        // with no configuration at all, is still Continue (today's hardcoded behavior).
        var result = new GlobalConfigDefaultValidator().Validate(new GlobalConfigDto { FirstName = "" });

        // Continue: BOTH NotEmpty and MinimumLength fail and both errors are present.
        Assert.Equal(2, result.Errors["FirstName"].Count);
    }

    [Fact]
    public void DefaultLanguage_UsedWhenCurrentUICultureHasNoCatalogEntry()
    {
        ValiValidationOptions.Global.DefaultLanguage = "es";
        var previous = System.Globalization.CultureInfo.CurrentUICulture;
        System.Globalization.CultureInfo.CurrentUICulture = new System.Globalization.CultureInfo("fr"); // not in catalog
        try
        {
            var result = new GlobalConfigDefaultValidator().Validate(new GlobalConfigDto { FirstName = "" });
            Assert.Equal("El campo FirstName no puede estar vacío.", result.Errors["FirstName"][0]);
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public void DisplayNameResolver_TransformsMessageTextButNotErrorsKey()
    {
        ValiValidationOptions.Global.DisplayNameResolver = name => name == "FirstName" ? "First Name" : name;

        var result = new GlobalConfigDefaultValidator().Validate(new GlobalConfigDto { FirstName = "" });

        Assert.Contains("First Name", result.Errors["FirstName"][0]);
        // The Errors dictionary KEY is still the raw property name — DisplayNameResolver only
        // affects message TEXT, never the key used to look failures up.
        Assert.True(result.HasErrorFor("FirstName"));
    }

    [Fact]
    public void PropertyNameResolver_TransformsErrorsKeyAndMessageText()
    {
        ValiValidationOptions.Global.PropertyNameResolver = name => name == "FirstName" ? "first_name" : name;

        var result = new GlobalConfigDefaultValidator().Validate(new GlobalConfigDto { FirstName = "" });

        Assert.True(result.HasErrorFor("first_name"));
        Assert.False(result.HasErrorFor("FirstName"));
    }

    [Fact]
    public void DisplayNameResolver_AppliesToCrossPropertyReferences()
    {
        // Both resolvers configured simultaneously — the cross-property "otherName" substitution
        // must go through PropertyNameResolver first (as GetPropertyName always does), then
        // DisplayNameResolver on top, exactly like the primary property's own {PropertyName}.
        ValiValidationOptions.Global.PropertyNameResolver = name => name == "OtherValue" ? "other_value" : name;
        ValiValidationOptions.Global.DisplayNameResolver = name => name == "other_value" ? "Other Value" : name;

        var result = new CrossPropertyValidator().Validate(new CrossPropertyDto { Value = 1, OtherValue = 5 });

        Assert.Contains("Other Value", result.Errors["Value"][0]);
        Assert.DoesNotContain("other_value", result.Errors["Value"][0]);
        Assert.DoesNotContain("OtherValue", result.Errors["Value"][0]);
    }

    [Fact]
    public void DisplayNameResolver_AppliesToMultipleOfPropertyReference()
    {
        // Same gap, different cross-property rule — MultipleOfProperty builds its "otherName"
        // Args entry independently of ComparisonRules' methods, so it needs its own regression check.
        ValiValidationOptions.Global.PropertyNameResolver = name => name == "OtherValue" ? "other_value" : name;
        ValiValidationOptions.Global.DisplayNameResolver = name => name == "other_value" ? "Other Value" : name;

        var result = new MultipleOfPropertyValidator().Validate(new CrossPropertyDto { Value = 5, OtherValue = 2 });

        Assert.Contains("Other Value", result.Errors["Value"][0]);
        Assert.DoesNotContain("other_value", result.Errors["Value"][0]);
        Assert.DoesNotContain("OtherValue", result.Errors["Value"][0]);
    }
}
