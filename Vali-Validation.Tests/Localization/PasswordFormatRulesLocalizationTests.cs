using Vali_Validation.Core.Results;
using Vali_Validation.Core.Rules;
using Vali_Validation.Core.Validators;
using Xunit;

namespace Vali_Validation.Tests.Localization;

public class PasswordFormatRulesLocalizationDto
{
    public string? Value { get; set; }
}

public class PasswordFormatRulesLocalizationTests
{
    private static ValidationResult ValidateWith(Action<IRuleBuilder<PasswordFormatRulesLocalizationDto, string?>> configure, string? value)
    {
        var v = new InlineValidator(configure);
        return v.Validate(new PasswordFormatRulesLocalizationDto { Value = value }, opts => opts.WithLanguage("en"));
    }

    private class InlineValidator : AbstractValidator<PasswordFormatRulesLocalizationDto>
    {
        public InlineValidator(Action<IRuleBuilder<PasswordFormatRulesLocalizationDto, string?>> configure)
            => configure(RuleFor(x => x.Value));
    }

    [Fact] public void IsValidJson_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must be a valid JSON string.",
            ValidateWith(r => r.IsValidJson(), "{not json").Errors["Value"][0]);

    [Fact] public void IsValidBase64_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must be a valid Base64 encoded string.",
            ValidateWith(r => r.IsValidBase64(), "not base64!!!").Errors["Value"][0]);

    [Fact] public void Iban_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must be a valid IBAN.",
            ValidateWith(r => r.Iban(), "invalid").Errors["Value"][0]);

    [Fact] public void HasUppercase_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must contain at least one uppercase letter.",
            ValidateWith(r => r.HasUppercase(), "abc").Errors["Value"][0]);

    [Fact] public void HasLowercase_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must contain at least one lowercase letter.",
            ValidateWith(r => r.HasLowercase(), "ABC").Errors["Value"][0]);

    [Fact] public void HasDigit_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must contain at least one digit.",
            ValidateWith(r => r.HasDigit(), "abc").Errors["Value"][0]);

    [Fact] public void HasSpecialChar_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must contain at least one special character.",
            ValidateWith(r => r.HasSpecialChar(), "abc123").Errors["Value"][0]);

    [Fact] public void Slug_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must be a valid URL slug (lowercase letters, numbers, and hyphens only).",
            ValidateWith(r => r.Slug(), "Not A Slug!").Errors["Value"][0]);

    [Fact] public void NoHtmlTags_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must not contain HTML tags.",
            ValidateWith(r => r.NoHtmlTags(), "<b>bold</b>").Errors["Value"][0]);

    [Fact] public void NoSqlInjectionPatterns_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field contains potentially unsafe content.",
            ValidateWith(r => r.NoSqlInjectionPatterns(), "'; DROP TABLE users; --").Errors["Value"][0]);

    [Fact] public void PasswordPolicy_ComposesUnderlyingRuleMessages()
    {
        var result = ValidateWith(r => r.PasswordPolicy(8), "abc");
        var messages = result.Errors["Value"];
        Assert.Contains("The Value field must be at least 8 characters long.", messages);
        Assert.Contains("The Value field must contain at least one uppercase letter.", messages);
        Assert.Contains("The Value field must contain at least one digit.", messages);
        Assert.Contains("The Value field must contain at least one special character.", messages);
    }

    // Spanish spot-checks
    [Fact] public void HasUppercase_Spanish_MatchesSpanishCatalog()
    {
        var v = new InlineValidator(r => r.HasUppercase());
        var result = v.Validate(new PasswordFormatRulesLocalizationDto { Value = "abc" }, opts => opts.WithLanguage("es"));
        Assert.Equal("El campo Value debe contener al menos una letra mayúscula.", result.Errors["Value"][0]);
    }

    [Fact] public void Iban_Spanish_MatchesSpanishCatalog()
    {
        var v = new InlineValidator(r => r.Iban());
        var result = v.Validate(new PasswordFormatRulesLocalizationDto { Value = "invalid" }, opts => opts.WithLanguage("es"));
        Assert.Equal("El campo Value debe ser un IBAN válido.", result.Errors["Value"][0]);
    }
}
