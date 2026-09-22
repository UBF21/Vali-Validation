using Vali_Validation.Core.Results;

namespace Vali_Validation.TestHelpers;

/// <summary>
/// Fluent assertion extensions for <see cref="ValidationResult"/>, for use in test suites.
/// </summary>
public static class ValidationResultAssertions
{
    public static void ShouldHaveValidationErrorFor(this ValidationResult result, string propertyName)
    {
        if (!result.HasErrorFor(propertyName))
        {
            throw new ValidationAssertionException(
                $"Expected a validation error for property '{propertyName}', but none was found. " +
                $"Properties with errors: [{string.Join(", ", result.PropertyNames)}].");
        }
    }

    public static void ShouldHaveValidationErrorFor(this ValidationResult result, string propertyName, string expectedMessage)
    {
        ShouldHaveValidationErrorFor(result, propertyName);

        var messages = result.ErrorsFor(propertyName);
        if (!messages.Contains(expectedMessage))
        {
            throw new ValidationAssertionException(
                $"Expected a validation error for property '{propertyName}' with message '{expectedMessage}', " +
                $"but found: [{string.Join(", ", messages)}].");
        }
    }

    public static void ShouldNotHaveValidationErrorFor(this ValidationResult result, string propertyName)
    {
        if (result.HasErrorFor(propertyName))
        {
            var messages = result.ErrorsFor(propertyName);
            throw new ValidationAssertionException(
                $"Expected no validation error for property '{propertyName}', but found: [{string.Join(", ", messages)}].");
        }
    }
}
