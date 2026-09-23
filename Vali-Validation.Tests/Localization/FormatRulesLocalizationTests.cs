using Vali_Validation.Core.Results;
using Vali_Validation.Core.Rules;
using Vali_Validation.Core.Validators;
using Xunit;

namespace Vali_Validation.Tests.Localization;

public class FormatRulesLocalizationDto
{
    public string? Value { get; set; }
}

public class FormatRulesLocalizationEnumDto
{
    public DayOfWeek Value { get; set; }
}

public class FormatRulesLocalizationTests
{
    private static ValidationResult ValidateWith(Action<IRuleBuilder<FormatRulesLocalizationDto, string?>> configure, string? value)
    {
        var v = new InlineValidator(configure);
        return v.Validate(new FormatRulesLocalizationDto { Value = value }, opts => opts.WithLanguage("en"));
    }

    private class InlineValidator : AbstractValidator<FormatRulesLocalizationDto>
    {
        public InlineValidator(Action<IRuleBuilder<FormatRulesLocalizationDto, string?>> configure)
            => configure(RuleFor(x => x.Value));
    }

    [Fact] public void Email_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must be a valid email address.",
            ValidateWith(r => r.Email(), "not-an-email").Errors["Value"][0]);

    [Fact] public void Url_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must be a valid URL.",
            ValidateWith(r => r.Url(), "not-a-url").Errors["Value"][0]);

    [Fact] public void IsAlpha_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must only contain alphabetic characters.",
            ValidateWith(r => r.IsAlpha(), "abc123").Errors["Value"][0]);

    [Fact] public void IsAlphanumeric_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must only contain alphanumeric characters.",
            ValidateWith(r => r.IsAlphanumeric(), "abc!23").Errors["Value"][0]);

    [Fact] public void IsNumeric_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must only contain numbers.",
            ValidateWith(r => r.IsNumeric(), "abc").Errors["Value"][0]);

    [Fact] public void IsEnum_Default_MatchesOriginalEnglishText()
    {
        var v = new EnumInlineValidator();
        var result = v.Validate(new FormatRulesLocalizationEnumDto { Value = (DayOfWeek)99 }, opts => opts.WithLanguage("en"));
        Assert.Equal("The Value field must be a valid DayOfWeek value.", result.Errors["Value"][0]);
    }

    private class EnumInlineValidator : AbstractValidator<FormatRulesLocalizationEnumDto>
    {
        public EnumInlineValidator() => RuleFor(x => x.Value).IsEnum<DayOfWeek>();
    }

    [Fact] public void Guid_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must be a valid GUID.",
            ValidateWith(r => r.Guid(), "not-a-guid").Errors["Value"][0]);

    [Fact] public void NotEmptyGuid_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must not be an empty GUID.",
            ValidateWith(r => r.NotEmptyGuid(), Guid.Empty.ToString()).Errors["Value"][0]);

    [Fact] public void PhoneNumber_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must be a valid phone number.",
            ValidateWith(r => r.PhoneNumber(), "abc").Errors["Value"][0]);

    [Fact] public void IPv4_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must be a valid IPv4 address.",
            ValidateWith(r => r.IPv4(), "999.999.999.999").Errors["Value"][0]);

    [Fact] public void IPv6_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must be a valid IPv6 address.",
            ValidateWith(r => r.IPv6(), "not-an-ipv6").Errors["Value"][0]);

    [Fact] public void MacAddress_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must be a valid MAC address.",
            ValidateWith(r => r.MacAddress(), "not-a-mac").Errors["Value"][0]);

    [Fact] public void CreditCard_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must be a valid credit card number.",
            ValidateWith(r => r.CreditCard(), "1234").Errors["Value"][0]);

    [Fact] public void Latitude_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must be a valid latitude (-90 to 90).",
            ValidateWith(r => r.Latitude(), "200").Errors["Value"][0]);

    [Fact] public void Longitude_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must be a valid longitude (-180 to 180).",
            ValidateWith(r => r.Longitude(), "200").Errors["Value"][0]);

    [Fact] public void CountryCode_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must be a valid ISO 3166-1 alpha-2 country code (e.g. US, PE, ES).",
            ValidateWith(r => r.CountryCode(), "123").Errors["Value"][0]);

    [Fact] public void CurrencyCode_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must be a valid ISO 4217 currency code (e.g. USD, EUR, PEN).",
            ValidateWith(r => r.CurrencyCode(), "123").Errors["Value"][0]);

    // Spanish spot-checks
    [Fact] public void Email_Spanish_MatchesSpanishCatalog()
    {
        var v = new InlineValidator(r => r.Email());
        var result = v.Validate(new FormatRulesLocalizationDto { Value = "not-an-email" }, opts => opts.WithLanguage("es"));
        Assert.Equal("El campo Value debe ser una dirección de correo electrónico válida.", result.Errors["Value"][0]);
    }

    [Fact] public void IPv4_Spanish_MatchesSpanishCatalog()
    {
        var v = new InlineValidator(r => r.IPv4());
        var result = v.Validate(new FormatRulesLocalizationDto { Value = "999.999.999.999" }, opts => opts.WithLanguage("es"));
        Assert.Equal("El campo Value debe ser una dirección IPv4 válida.", result.Errors["Value"][0]);
    }
}
