using Vali_Validation.Core.Results;
using Vali_Validation.Core.Rules;
using Vali_Validation.Core.Validators;
using Xunit;

namespace Vali_Validation.Tests;

// IPropertyValidator implemented directly (minimal contract path).
public class EvenNumberValidator : IPropertyValidator<int>
{
    public bool IsValid(int value) => value % 2 == 0;
    public IReadOnlyDictionary<string, string> Messages { get; } = new Dictionary<string, string>
    {
        ["en"] = "The {PropertyName} field must be an even number (custom validator).",
        ["es"] = "El campo {PropertyName} debe ser un número par (validador custom)."
    };
}

// PropertyValidator<TProperty> base class path (single-language default).
public class NotSpaceValidator : PropertyValidator<string?>
{
    public override bool IsValid(string? value) => value != null && !value.Contains(' ');
}

public class PropertyValidatorDto
{
    public int Age { get; set; }
    public string? Slug { get; set; }
}

public class EvenAgeValidator : AbstractValidator<PropertyValidatorDto>
{
    public EvenAgeValidator() => RuleFor(x => x.Age).SetPropertyValidator(new EvenNumberValidator());
}

public class NotSpaceSlugValidator : AbstractValidator<PropertyValidatorDto>
{
    public NotSpaceSlugValidator() => RuleFor(x => x.Slug).SetPropertyValidator(new NotSpaceValidator());
}

public class PropertyValidatorTests
{
    [Fact]
    public void SetPropertyValidator_FailingValue_ProducesDefaultEnglishMessage()
    {
        // Pin CurrentUICulture — this test asserts English message text and must not depend on
        // the host OS/CI machine's ambient culture (see ValiValidationOptionsTests.cs for the
        // same convention elsewhere in this suite).
        var previous = System.Globalization.CultureInfo.CurrentUICulture;
        System.Globalization.CultureInfo.CurrentUICulture = new System.Globalization.CultureInfo("en");
        try
        {
            var result = new EvenAgeValidator().Validate(new PropertyValidatorDto { Age = 3 });
            Assert.Equal("The Age field must be an even number (custom validator).", result.Errors["Age"][0]);
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public void SetPropertyValidator_PassingValue_ProducesNoError()
    {
        var result = new EvenAgeValidator().Validate(new PropertyValidatorDto { Age = 4 });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void SetPropertyValidator_SpanishCulture_UsesSpanishEntryFromMessagesDictionary()
    {
        var previous = System.Globalization.CultureInfo.CurrentUICulture;
        System.Globalization.CultureInfo.CurrentUICulture = new System.Globalization.CultureInfo("es");
        try
        {
            var result = new EvenAgeValidator().Validate(new PropertyValidatorDto { Age = 3 });
            Assert.Equal("El campo Age debe ser un número par (validador custom).", result.Errors["Age"][0]);
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public void SetPropertyValidator_UnknownCulture_FallsBackToFirstAvailableEntry()
    {
        var previous = System.Globalization.CultureInfo.CurrentUICulture;
        System.Globalization.CultureInfo.CurrentUICulture = new System.Globalization.CultureInfo("fr");
        try
        {
            var result = new EvenAgeValidator().Validate(new PropertyValidatorDto { Age = 3 });
            // "fr" is in neither the validator's own Messages dict nor relevant here — falls back
            // to the FIRST entry in Messages (insertion order: "en"), not LanguageManager's catalog
            // (SetPropertyValidator resolves against the validator's OWN dictionary, never the
            // package's built-in catalog).
            Assert.Equal("The Age field must be an even number (custom validator).", result.Errors["Age"][0]);
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public void SetPropertyValidator_WithSeverityWarning_DoesNotFailIsValidButAppearsInFailures()
    {
        var validator = new EvenAgeValidatorWithSeverity();
        var result = validator.Validate(new PropertyValidatorDto { Age = 3 });

        Assert.True(result.IsValid);
        Assert.Contains(result.Failures, f => f.PropertyName == "Age" && f.Severity == Severity.Warning);
    }

    private class EvenAgeValidatorWithSeverity : AbstractValidator<PropertyValidatorDto>
    {
        public EvenAgeValidatorWithSeverity() =>
            RuleFor(x => x.Age).SetPropertyValidator(new EvenNumberValidator()).WithSeverity(Severity.Warning);
    }

    [Fact]
    public void SetPropertyValidator_WithMessageOverride_ReplacesDefaultMessage()
    {
        var validator = new EvenAgeValidatorWithCustomMessage();
        var result = validator.Validate(new PropertyValidatorDto { Age = 3 });
        Assert.Equal("Age must be divisible by 2.", result.Errors["Age"][0]);
    }

    private class EvenAgeValidatorWithCustomMessage : AbstractValidator<PropertyValidatorDto>
    {
        public EvenAgeValidatorWithCustomMessage() =>
            RuleFor(x => x.Age).SetPropertyValidator(new EvenNumberValidator()).WithMessage("Age must be divisible by 2.");
    }

    [Fact]
    public void PropertyValidatorBaseClass_UsesDefaultMessageWhenNotOverridden()
    {
        var previous = System.Globalization.CultureInfo.CurrentUICulture;
        System.Globalization.CultureInfo.CurrentUICulture = new System.Globalization.CultureInfo("en");
        try
        {
            var result = new NotSpaceSlugValidator().Validate(new PropertyValidatorDto { Slug = "has space" });
            Assert.Equal("The Slug field is invalid.", result.Errors["Slug"][0]);
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public void SetPropertyValidator_NullValidator_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new NullValidatorHarness());
    }

    [Fact]
    public void SetPropertyValidator_SameInstanceReused_ReResolvesLanguagePerValidateCall()
    {
        // Regression test: a Singleton/reused validator instance must re-resolve the custom
        // validator's message against WHICHEVER language override is active on EACH Validate()
        // call — not the language that happened to be active when SetPropertyValidator was first
        // invoked (rule-registration time, i.e. the AbstractValidator's constructor).
        var validator = new EvenAgeValidator();
        var dto = new PropertyValidatorDto { Age = 3 };

        var spanishResult = validator.Validate(dto, opts => opts.WithLanguage("es"));
        Assert.Equal("El campo Age debe ser un número par (validador custom).", spanishResult.Errors["Age"][0]);

        var englishResult = validator.Validate(dto, opts => opts.WithLanguage("en"));
        Assert.Equal("The Age field must be an even number (custom validator).", englishResult.Errors["Age"][0]);
    }

    private class NullValidatorHarness : AbstractValidator<PropertyValidatorDto>
    {
        public NullValidatorHarness() => RuleFor(x => x.Age).SetPropertyValidator(null!);
    }
}
