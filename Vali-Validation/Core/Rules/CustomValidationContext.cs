using Vali_Validation.Core.Results;

namespace Vali_Validation.Core.Rules;

public sealed class CustomValidationContext<T>
{
    private readonly ValidationResult _result;
    private readonly string _defaultProperty;

    public T Instance { get; }

    internal CustomValidationContext(T instance, ValidationResult result, string defaultProperty)
    {
        Instance = instance;
        _result = result;
        _defaultProperty = defaultProperty;
    }

    public void AddFailure(string message) => _result.AddError(_defaultProperty, message);
    public void AddFailure(string property, string message) => _result.AddError(property, message);
    public void AddFailure(string property, string message, string errorCode) => _result.AddError(property, message, errorCode);

    public void AddFailure(string message, Severity severity, string? errorCode = null)
        => _result.AddFailure(_defaultProperty, message, severity, errorCode);

    public void AddFailure(string property, string message, Severity severity, string? errorCode = null)
        => _result.AddFailure(property, message, severity, errorCode);
}
