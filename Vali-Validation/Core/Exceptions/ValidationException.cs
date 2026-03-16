using Vali_Validation.Core.Results;

namespace Vali_Validation.Core.Exceptions;

/// <summary>
/// Exception thrown when validation fails and the caller wants an exception-based flow.
/// </summary>
public sealed class ValidationException : Exception
{
    /// <summary>
    /// The full validation result containing all errors.
    /// </summary>
    public ValidationResult ValidationResult { get; }

    public ValidationException(ValidationResult result)
        : base("Validation failed. See ValidationResult for details.")
    {
        ValidationResult = result;
    }
}
