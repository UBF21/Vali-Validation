using Vali_Validation.Core.Results;
using Xunit;

namespace Vali_Validation.Tests;

public class ValidationResultTests
{
    [Fact]
    public void IsValid_WhenNoErrors_ReturnsTrue()
    {
        var result = new ValidationResult();
        Assert.True(result.IsValid);
    }

    [Fact]
    public void IsValid_WhenHasErrors_ReturnsFalse()
    {
        var result = new ValidationResult();
        result.AddError("Name", "Required");
        Assert.False(result.IsValid);
    }

    [Fact]
    public void ErrorsFor_WhenPropertyHasErrors_ReturnsErrors()
    {
        var result = new ValidationResult();
        result.AddError("Name", "Too short");
        result.AddError("Name", "Required");

        var errors = result.ErrorsFor("Name");
        Assert.Equal(2, errors.Count);
        Assert.Contains("Too short", errors);
        Assert.Contains("Required", errors);
    }

    [Fact]
    public void ErrorsFor_WhenPropertyHasNoErrors_ReturnsEmptyList()
    {
        var result = new ValidationResult();
        var errors = result.ErrorsFor("Name");
        Assert.NotNull(errors);
        Assert.Empty(errors);
    }

    [Fact]
    public void HasErrorFor_WhenPropertyHasErrors_ReturnsTrue()
    {
        var result = new ValidationResult();
        result.AddError("Email", "Invalid");
        Assert.True(result.HasErrorFor("Email"));
    }

    [Fact]
    public void HasErrorFor_WhenPropertyHasNoErrors_ReturnsFalse()
    {
        var result = new ValidationResult();
        Assert.False(result.HasErrorFor("Email"));
    }

    [Fact]
    public void FirstError_WhenPropertyHasErrors_ReturnsFirst()
    {
        var result = new ValidationResult();
        result.AddError("Name", "First error");
        result.AddError("Name", "Second error");

        Assert.Equal("First error", result.FirstError("Name"));
    }

    [Fact]
    public void FirstError_WhenPropertyHasNoErrors_ReturnsNull()
    {
        var result = new ValidationResult();
        Assert.Null(result.FirstError("Name"));
    }

    [Fact]
    public void ToFlatList_ReturnsAllErrorsFormatted()
    {
        var result = new ValidationResult();
        result.AddError("Name", "Required");
        result.AddError("Email", "Invalid");

        var flat = result.ToFlatList();
        Assert.Equal(2, flat.Count);
        Assert.Contains("Name: Required", flat);
        Assert.Contains("Email: Invalid", flat);
    }

    [Fact]
    public void ToFlatList_WhenNoErrors_ReturnsEmptyList()
    {
        var result = new ValidationResult();
        Assert.Empty(result.ToFlatList());
    }

    [Fact]
    public void AddError_AccumulatesMultipleErrorsPerProperty()
    {
        var result = new ValidationResult();
        result.AddError("Name", "Error 1");
        result.AddError("Name", "Error 2");
        result.AddError("Email", "Error 3");

        Assert.Equal(2, result.Errors["Name"].Count);
        Assert.Single(result.Errors["Email"]);
    }

    [Fact]
    public void AddFailure_WithWarningSeverity_DoesNotAffectIsValid()
    {
        var result = new ValidationResult();

        result.AddFailure("Discount", "Discount above 50% requires manager approval.", Severity.Warning);

        Assert.True(result.IsValid);
        Assert.Single(result.Failures);
        Assert.Equal(Severity.Warning, result.Failures[0].Severity);
    }

    [Fact]
    public void AddFailure_WithErrorSeverity_MakesIsValidFalse()
    {
        var result = new ValidationResult();

        result.AddFailure("Email", "The Email field must be a valid email address.", Severity.Error);

        Assert.False(result.IsValid);
        Assert.Single(result.Failures);
        Assert.Equal(Severity.Error, result.Failures[0].Severity);
    }

    [Fact]
    public void AddError_DefaultsToErrorSeverity()
    {
        var result = new ValidationResult();

        result.AddError("Name", "The Name field is required.");

        Assert.Single(result.Failures);
        Assert.Equal(Severity.Error, result.Failures[0].Severity);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Failures_CanMixErrorAndWarningSeverity_InOneResult()
    {
        var result = new ValidationResult();
        result.AddFailure("Email", "The Email field must be a valid email address.", Severity.Error);
        result.AddFailure("Discount", "Discount above 50% requires manager approval.", Severity.Warning);

        Assert.False(result.IsValid);
        Assert.Equal(2, result.Failures.Count);
    }

    [Fact]
    public void BackCompat_Errors_OnlyIncludesErrorSeverityFailures()
    {
        var result = new ValidationResult();
        result.AddFailure("Email", "Invalid email.", Severity.Error);
        result.AddFailure("Discount", "Needs approval.", Severity.Warning);

        Assert.True(result.Errors.ContainsKey("Email"));
        Assert.False(result.Errors.ContainsKey("Discount"));
    }

    [Fact]
    public void BackCompat_HasErrorFor_ReturnsFalseForWarningOnlyProperty()
    {
        var result = new ValidationResult();
        result.AddFailure("Discount", "Needs approval.", Severity.Warning);

        Assert.False(result.HasErrorFor("Discount"));
    }

    [Fact]
    public void BackCompat_ErrorsFor_ReturnsEmptyForWarningOnlyProperty()
    {
        var result = new ValidationResult();
        result.AddFailure("Discount", "Needs approval.", Severity.Warning);

        Assert.Empty(result.ErrorsFor("Discount"));
    }

    [Fact]
    public void BackCompat_FirstError_ReturnsNullForWarningOnlyProperty()
    {
        var result = new ValidationResult();
        result.AddFailure("Discount", "Needs approval.", Severity.Warning);

        Assert.Null(result.FirstError("Discount"));
    }

    [Fact]
    public void BackCompat_PropertyNames_ExcludesWarningOnlyProperties()
    {
        var result = new ValidationResult();
        result.AddFailure("Email", "Invalid.", Severity.Error);
        result.AddFailure("Discount", "Needs approval.", Severity.Warning);

        Assert.Contains("Email", result.PropertyNames);
        Assert.DoesNotContain("Discount", result.PropertyNames);
    }

    [Fact]
    public void BackCompat_ErrorCount_CountsOnlyErrorSeverity()
    {
        var result = new ValidationResult();
        result.AddFailure("Email", "Invalid.", Severity.Error);
        result.AddFailure("Discount", "Needs approval.", Severity.Warning);
        result.AddFailure("Discount", "Also needs approval.", Severity.Warning);

        Assert.Equal(1, result.ErrorCount);
    }

    [Fact]
    public void BackCompat_ErrorCodes_OnlyIncludesErrorSeverityFailuresWithACode()
    {
        var result = new ValidationResult();
        result.AddFailure("Email", "Invalid.", Severity.Error, "EMAIL_INVALID");
        result.AddFailure("Discount", "Needs approval.", Severity.Warning, "DISCOUNT_HIGH");

        Assert.True(result.ErrorCodes.ContainsKey("Email"));
        Assert.False(result.ErrorCodes.ContainsKey("Discount"));
    }

    [Fact]
    public void Merge_PreservesSeverityAndErrorCodeOfMergedFailures()
    {
        var target = new ValidationResult();
        var source = new ValidationResult();
        source.AddFailure("Discount", "Needs approval.", Severity.Warning, "DISCOUNT_HIGH");

        target.Merge(source);

        Assert.Single(target.Failures);
        Assert.Equal(Severity.Warning, target.Failures[0].Severity);
        Assert.Equal("DISCOUNT_HIGH", target.Failures[0].ErrorCode);
        Assert.True(target.IsValid);
    }
}
