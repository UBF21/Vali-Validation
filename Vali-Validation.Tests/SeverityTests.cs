using System.Text.Json;
using Vali_Validation.Core.Results;
using Xunit;

namespace Vali_Validation.Tests;

public class SeverityTests
{
    [Fact]
    public void ValidationResult_SerializesToSingleStructuredFailuresArray_WithDefaultJsonOptions()
    {
        var result = new ValidationResult();
        result.AddFailure("Email", "The Email field must be a valid email address.", Severity.Error);
        result.AddFailure("Discount", "Discount above 50% requires manager approval.", Severity.Warning);

        string json = JsonSerializer.Serialize(result);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.False(root.GetProperty("isValid").GetBoolean());

        var failures = root.GetProperty("failures");
        Assert.Equal(2, failures.GetArrayLength());

        var first = failures[0];
        Assert.Equal("Email", first.GetProperty("property").GetString());
        Assert.Equal("The Email field must be a valid email address.", first.GetProperty("message").GetString());
        Assert.Equal("Error", first.GetProperty("severity").GetString());
        Assert.False(first.TryGetProperty("errorCode", out _));

        var second = failures[1];
        Assert.Equal("Discount", second.GetProperty("property").GetString());
        Assert.Equal("Warning", second.GetProperty("severity").GetString());
    }

    [Fact]
    public void ValidationResult_Serialization_IncludesErrorCodeWhenPresent()
    {
        var result = new ValidationResult();
        result.AddFailure("Email", "Invalid.", Severity.Error, "EMAIL_INVALID");

        string json = JsonSerializer.Serialize(result);
        using var doc = JsonDocument.Parse(json);

        Assert.Equal("EMAIL_INVALID", doc.RootElement.GetProperty("failures")[0].GetProperty("errorCode").GetString());
    }

    [Fact]
    public void ValidationResult_WithNoFailures_SerializesIsValidTrueAndEmptyFailuresArray()
    {
        var result = new ValidationResult();

        string json = JsonSerializer.Serialize(result);
        using var doc = JsonDocument.Parse(json);

        Assert.True(doc.RootElement.GetProperty("isValid").GetBoolean());
        Assert.Equal(0, doc.RootElement.GetProperty("failures").GetArrayLength());
    }
}
