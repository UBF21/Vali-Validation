using System.Text.Json.Serialization;

namespace Vali_Validation.Core.Results;

public class ValidationResult
{
    [JsonPropertyName("isValid")]
    public bool IsValid => !Failures.Any(f => f.Severity == Severity.Error);

    [JsonPropertyName("failures")]
    public List<ValidationFailure> Failures { get; } = new();

    /// <summary>
    /// Adds a failure with an explicit <see cref="Severity"/>. <see cref="AddError"/> is a
    /// shorthand for the common case (<see cref="Severity.Error"/>, no error code).
    /// </summary>
    public void AddFailure(string propertyName, string message, Severity severity = Severity.Error, string? errorCode = null)
        => Failures.Add(new ValidationFailure(propertyName, message, severity, errorCode));

    /// <summary>
    /// Adds an <see cref="Severity.Error"/>-severity failure. Equivalent to
    /// <c>AddFailure(property, message, Severity.Error, errorCode)</c>.
    /// </summary>
    public void AddError(string property, string message, string? errorCode = null)
        => AddFailure(property, message, Severity.Error, errorCode);

    /// <summary>
    /// Back-compat view over <see cref="Failures"/>, filtered to <see cref="Severity.Error"/> —
    /// a <see cref="Severity.Warning"/>/<see cref="Severity.Info"/> failure never appears here.
    /// Computed fresh on each access; mutating the returned dictionary has no effect on this
    /// <see cref="ValidationResult"/> — use <see cref="AddFailure"/> to add failures instead.
    /// </summary>
    [JsonIgnore]
    public IReadOnlyDictionary<string, List<string>> Errors
        => Failures.Where(f => f.Severity == Severity.Error)
            .GroupBy(f => f.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(f => f.Message).ToList());

    /// <summary>
    /// Back-compat view over <see cref="Failures"/>, filtered to <see cref="Severity.Error"/>
    /// failures that have a non-null error code. See <see cref="Errors"/> for the same caveats.
    /// </summary>
    [JsonIgnore]
    public IReadOnlyDictionary<string, List<string>> ErrorCodes
        => Failures.Where(f => f.Severity == Severity.Error && f.ErrorCode != null)
            .GroupBy(f => f.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(f => f.ErrorCode!).ToList());

    public List<string> ErrorsFor(string property)
        => Failures.Where(f => f.PropertyName == property && f.Severity == Severity.Error)
            .Select(f => f.Message)
            .ToList();

    public bool HasErrorFor(string property)
        => Failures.Any(f => f.PropertyName == property && f.Severity == Severity.Error);

    public string? FirstError(string property)
        => Failures.FirstOrDefault(f => f.PropertyName == property && f.Severity == Severity.Error)?.Message;

    public List<string> ToFlatList()
        => Errors.SelectMany(e => e.Value.Select(msg => $"{e.Key}: {msg}")).ToList();

    public int ErrorCount => Failures.Count(f => f.Severity == Severity.Error);

    public IReadOnlyList<string> PropertyNames
        => Failures.Where(f => f.Severity == Severity.Error)
            .Select(f => f.PropertyName)
            .Distinct()
            .ToList();

    /// <summary>
    /// Copies every failure from <paramref name="other"/> into this result, preserving each
    /// failure's severity and error code exactly.
    /// </summary>
    public void Merge(ValidationResult other)
    {
        foreach (var failure in other.Failures)
            AddFailure(failure.PropertyName, failure.Message, failure.Severity, failure.ErrorCode);
    }
}
