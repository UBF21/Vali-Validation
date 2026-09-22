using Vali_Validation.Core.Validators;
using Xunit;

namespace Vali_Validation.Tests;

public class NumConvDto
{
    public object? Value { get; set; }
    public string? Text { get; set; }
}

public class NumConvPositiveValidator : AbstractValidator<NumConvDto>
{
    public NumConvPositiveValidator() => RuleFor(x => x.Value).Positive();
}

public class NumConvPrecisionValidator : AbstractValidator<NumConvDto>
{
    public NumConvPrecisionValidator() => RuleFor(x => x.Value).Precision(5, 2);
}

public class NumConvOddValidator : AbstractValidator<NumConvDto>
{
    public NumConvOddValidator() => RuleFor(x => x.Text).Odd();
}

public class NumConvBase64Validator : AbstractValidator<NumConvDto>
{
    public NumConvBase64Validator() => RuleFor(x => x.Text).IsValidBase64();
}

public class NumericConversionTests
{
    [Theory]
    [InlineData(5)]
    [InlineData(5L)]
    [InlineData(5.5)]
    [InlineData("5.5")]
    public void Positive_AcceptsValidNumericTypesAndNumericStrings(object value)
    {
        var result = new NumConvPositiveValidator().Validate(new NumConvDto { Value = value });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Positive_OnNonConvertibleValue_FailsWithoutThrowing()
    {
        var exception = Record.Exception(() =>
            new NumConvPositiveValidator().Validate(new NumConvDto { Value = new object() }));

        Assert.Null(exception);
        var result = new NumConvPositiveValidator().Validate(new NumConvDto { Value = new object() });
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Precision_OnNonConvertibleValue_FailsInsteadOfPassing()
    {
        // Regression test for the bug: the old catch block returned true (valid) when the
        // value could not be converted to decimal. It must now return false (invalid).
        var result = new NumConvPrecisionValidator().Validate(new NumConvDto { Value = new object() });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Precision_OnValidDecimalWithinBounds_Passes()
    {
        var result = new NumConvPrecisionValidator().Validate(new NumConvDto { Value = 123.45m });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Precision_OnNullValue_Passes()
    {
        // Unchanged behavior: null means "not present", distinct from "not convertible".
        var result = new NumConvPrecisionValidator().Validate(new NumConvDto { Value = null });
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("3", true)]
    [InlineData("4", false)]
    [InlineData("not-a-number", false)]
    public void Odd_WorksOnNumericStringsAndFailsCleanlyOnNonNumeric(string text, bool expectedValid)
    {
        var result = new NumConvOddValidator().Validate(new NumConvDto { Text = text });
        Assert.Equal(expectedValid, result.IsValid);
    }

    [Theory]
    [InlineData("aGVsbG8=", true)]
    [InlineData("not valid base64!!", false)]
    public void IsValidBase64_WorksWithoutThrowing(string text, bool expectedValid)
    {
        var exception = Record.Exception(() => new NumConvBase64Validator().Validate(new NumConvDto { Text = text }));
        Assert.Null(exception);

        var result = new NumConvBase64Validator().Validate(new NumConvDto { Text = text });
        Assert.Equal(expectedValid, result.IsValid);
    }
}
