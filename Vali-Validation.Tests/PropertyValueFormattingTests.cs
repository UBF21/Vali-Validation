using Vali_Validation.Core.Validators;
using Xunit;

namespace Vali_Validation.Tests;

public class PropertyValueFormattingDto
{
    public string? Name { get; set; }
}

public class PropertyValueFormattingValidator : AbstractValidator<PropertyValueFormattingDto>
{
    public PropertyValueFormattingValidator()
    {
        RuleFor(x => x.Name).Must(_ => false).WithMessage("Value was: {PropertyValue}");
    }
}

public class PropertyValueFormattingTests
{
    [Fact]
    public void PropertyValue_WithEmbeddedNewline_StripsControlCharacters()
    {
        var validator = new PropertyValueFormattingValidator();

        var result = validator.Validate(new PropertyValueFormattingDto { Name = "line1\nline2\r\nline3" });

        var message = result.FirstError("Name");
        Assert.NotNull(message);
        Assert.DoesNotContain('\n', message);
        Assert.DoesNotContain('\r', message);
        Assert.Contains("line1line2line3", message);
    }

    [Fact]
    public void PropertyValue_LongerThan200Characters_IsTruncatedWithSuffix()
    {
        var validator = new PropertyValueFormattingValidator();
        string longValue = new string('a', 500);

        var result = validator.Validate(new PropertyValueFormattingDto { Name = longValue });

        var message = result.FirstError("Name");
        Assert.NotNull(message);
        Assert.Contains("…(truncated)", message);
        Assert.True(message.Length < longValue.Length);
    }

    [Fact]
    public void PropertyValue_ShortNormalValue_IsUnaffected()
    {
        var validator = new PropertyValueFormattingValidator();

        var result = validator.Validate(new PropertyValueFormattingDto { Name = "Ana" });

        Assert.Equal("Value was: Ana", result.FirstError("Name"));
    }

    [Fact]
    public void PropertyValue_Null_StillRendersAsNullLiteral()
    {
        var validator = new PropertyValueFormattingValidator();

        var result = validator.Validate(new PropertyValueFormattingDto { Name = null });

        Assert.Equal("Value was: null", result.FirstError("Name"));
    }
}
