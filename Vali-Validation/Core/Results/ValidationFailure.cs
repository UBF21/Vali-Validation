using System.Text.Json.Serialization;

namespace Vali_Validation.Core.Results;

/// <summary>
/// A single validation failure — one property, one message, one severity. See
/// <see cref="ValidationResult.Failures"/> for the full set produced by a validation run.
/// </summary>
public sealed class ValidationFailure
{
    [JsonPropertyName("property")]
    public string PropertyName { get; }

    [JsonPropertyName("message")]
    public string Message { get; }

    [JsonPropertyName("severity")]
    public Severity Severity { get; }

    [JsonPropertyName("errorCode")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ErrorCode { get; }

    public ValidationFailure(string propertyName, string message, Severity severity = Severity.Error, string? errorCode = null)
    {
        PropertyName = propertyName;
        Message = message;
        Severity = severity;
        ErrorCode = errorCode;
    }
}
