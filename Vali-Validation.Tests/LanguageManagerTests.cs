using Vali_Validation.Core.Localization;
using Xunit;

namespace Vali_Validation.Tests;

public class LanguageManagerTests
{
    [Fact]
    public void GetTemplate_KnownKeyEnglish_ReturnsEnglishTemplate()
    {
        string template = LanguageManager.GetTemplate(MessageKey.NotEmpty, "en");
        Assert.Equal("The {PropertyName} field cannot be empty.", template);
    }

    [Fact]
    public void GetTemplate_KnownKeySpanish_ReturnsSpanishTemplate()
    {
        string template = LanguageManager.GetTemplate(MessageKey.NotEmpty, "es");
        Assert.Equal("El campo {PropertyName} no puede estar vacío.", template);
    }

    [Fact]
    public void GetTemplate_UnknownLanguage_FallsBackToEnglish()
    {
        string template = LanguageManager.GetTemplate(MessageKey.NotEmpty, "fr");
        Assert.Equal("The {PropertyName} field cannot be empty.", template);
    }

    [Fact]
    public void RegisterLanguage_NewLanguage_IsThenResolvable()
    {
        LanguageManager.RegisterLanguage("xx-test", new Dictionary<MessageKey, string>
        {
            [MessageKey.NotEmpty] = "XX-TEST cannot be empty: {PropertyName}"
        });

        string template = LanguageManager.GetTemplate(MessageKey.NotEmpty, "xx-test");
        Assert.Equal("XX-TEST cannot be empty: {PropertyName}", template);
    }

    [Fact]
    public void RegisterLanguage_PartialCatalog_MissingKeyFallsBackToEnglish()
    {
        LanguageManager.RegisterLanguage("yy-test", new Dictionary<MessageKey, string>
        {
            [MessageKey.NotEmpty] = "YY-TEST: {PropertyName}"
            // MinimumLength deliberately omitted
        });

        string template = LanguageManager.GetTemplate(MessageKey.MinimumLength, "yy-test");
        Assert.Equal("The {PropertyName} field must be at least {length} characters long.", template);
    }
}
