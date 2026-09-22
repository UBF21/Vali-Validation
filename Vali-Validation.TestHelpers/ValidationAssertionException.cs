namespace Vali_Validation.TestHelpers;

/// <summary>
/// Thrown by <see cref="ValidationResultAssertions"/> when a validation assertion fails.
/// Framework-agnostic: any test runner that fails a test on an unhandled exception (xUnit, NUnit,
/// MSTest) reports the assertion failure correctly without this library depending on that runner.
/// </summary>
public sealed class ValidationAssertionException : Exception
{
    public ValidationAssertionException(string message) : base(message)
    {
    }
}
