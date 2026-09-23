using System.Globalization;
using Vali_Validation.Core.Validators;
using Xunit;

namespace Vali_Validation.Tests;

public class LanguageResolutionDto
{
    public string? Name { get; set; }
}

public class LanguageResolutionValidator : AbstractValidator<LanguageResolutionDto>
{
    public LanguageResolutionValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
    }
}

public class LanguageResolutionTests
{
    [Fact]
    public void Validate_DefaultCulture_ProducesEnglishMessage()
    {
        var previous = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = new CultureInfo("en");
        try
        {
            var result = new LanguageResolutionValidator().Validate(new LanguageResolutionDto { Name = "" });
            Assert.Equal("The Name field cannot be empty.", result.Errors["Name"][0]);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public void Validate_SpanishCulture_ProducesSpanishMessage()
    {
        var previous = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = new CultureInfo("es");
        try
        {
            var result = new LanguageResolutionValidator().Validate(new LanguageResolutionDto { Name = "" });
            Assert.Equal("El campo Name no puede estar vacío.", result.Errors["Name"][0]);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public void Validate_WithExplicitLanguageOverride_WinsOverCurrentUICulture()
    {
        var previous = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = new CultureInfo("en");
        try
        {
            var result = new LanguageResolutionValidator().Validate(
                new LanguageResolutionDto { Name = "" },
                opts => opts.WithLanguage("es"));
            Assert.Equal("El campo Name no puede estar vacío.", result.Errors["Name"][0]);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public void Validate_UnknownCulture_FallsBackToEnglish()
    {
        var previous = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = new CultureInfo("fr");
        try
        {
            var result = new LanguageResolutionValidator().Validate(new LanguageResolutionDto { Name = "" });
            Assert.Equal("The Name field cannot be empty.", result.Errors["Name"][0]);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }
}
