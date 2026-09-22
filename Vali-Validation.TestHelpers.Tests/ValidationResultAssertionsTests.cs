using Vali_Validation.Core.Results;
using Xunit;

namespace Vali_Validation.TestHelpers.Tests;

public class ValidationResultAssertionsTests
{
    [Fact]
    public void ShouldHaveValidationErrorFor_WhenErrorExists_DoesNotThrow()
    {
        var result = new ValidationResult();
        result.AddError("Email", "must be valid");

        var exception = Record.Exception(() => result.ShouldHaveValidationErrorFor("Email"));

        Assert.Null(exception);
    }

    [Fact]
    public void ShouldHaveValidationErrorFor_WhenErrorMissing_ThrowsValidationAssertionException()
    {
        var result = new ValidationResult();

        var exception = Assert.Throws<ValidationAssertionException>(
            () => result.ShouldHaveValidationErrorFor("Email"));

        Assert.Contains("Email", exception.Message);
    }

    [Fact]
    public void ShouldHaveValidationErrorFor_WithExpectedMessage_WhenMessageMatches_DoesNotThrow()
    {
        var result = new ValidationResult();
        result.AddError("Email", "must be valid");

        var exception = Record.Exception(() => result.ShouldHaveValidationErrorFor("Email", "must be valid"));

        Assert.Null(exception);
    }

    [Fact]
    public void ShouldHaveValidationErrorFor_WithExpectedMessage_WhenMessageDiffers_ThrowsValidationAssertionException()
    {
        var result = new ValidationResult();
        result.AddError("Email", "must be valid");

        var exception = Assert.Throws<ValidationAssertionException>(
            () => result.ShouldHaveValidationErrorFor("Email", "must not be empty"));

        Assert.Contains("must be valid", exception.Message);
    }

    [Fact]
    public void ShouldNotHaveValidationErrorFor_WhenNoError_DoesNotThrow()
    {
        var result = new ValidationResult();

        var exception = Record.Exception(() => result.ShouldNotHaveValidationErrorFor("Email"));

        Assert.Null(exception);
    }

    [Fact]
    public void ShouldNotHaveValidationErrorFor_WhenErrorExists_ThrowsValidationAssertionException()
    {
        var result = new ValidationResult();
        result.AddError("Email", "must be valid");

        var exception = Assert.Throws<ValidationAssertionException>(
            () => result.ShouldNotHaveValidationErrorFor("Email"));

        Assert.Contains("Email", exception.Message);
    }
}
