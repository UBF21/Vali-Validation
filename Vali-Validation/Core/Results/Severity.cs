using System.Text.Json.Serialization;

namespace Vali_Validation.Core.Results;

/// <summary>
/// How strongly a validation failure should be treated. <see cref="Error"/> is the default for
/// every rule that doesn't explicitly call <c>.WithSeverity(...)</c> — only <see cref="Error"/>
/// failures make <see cref="ValidationResult.IsValid"/> false. <see cref="Warning"/> and
/// <see cref="Info"/> failures still appear in <see cref="ValidationResult.Failures"/>, but don't
/// block validation.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Severity
{
    Error = 0,
    Warning = 1,
    Info = 2
}
