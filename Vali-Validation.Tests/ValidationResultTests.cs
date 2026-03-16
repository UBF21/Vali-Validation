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
}
