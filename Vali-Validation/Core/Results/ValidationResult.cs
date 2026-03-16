namespace Vali_Validation.Core.Results;

public class ValidationResult
{
    public Dictionary<string, List<string>> Errors { get; } = new();
    public Dictionary<string, List<string>> ErrorCodes { get; } = new();
    public bool IsValid => !Errors.Any();

    public void AddError(string property, string message, string? errorCode = null)
    {
        if (!Errors.ContainsKey(property))
            Errors[property] = new List<string>();

        Errors[property].Add(message);

        if (errorCode != null)
        {
            if (!ErrorCodes.ContainsKey(property))
                ErrorCodes[property] = new List<string>();
            ErrorCodes[property].Add(errorCode);
        }
    }

    public List<string> ErrorsFor(string property)
        => Errors.TryGetValue(property, out var list) ? list : new List<string>();

    public bool HasErrorFor(string property) => Errors.ContainsKey(property);

    public string? FirstError(string property)
        => Errors.TryGetValue(property, out var list) && list.Count > 0 ? list[0] : null;

    public List<string> ToFlatList()
        => Errors.SelectMany(e => e.Value.Select(msg => $"{e.Key}: {msg}")).ToList();

    public int ErrorCount => Errors.Values.Sum(list => list.Count);

    public IReadOnlyList<string> PropertyNames => Errors.Keys.ToList();

    public void Merge(ValidationResult other)
    {
        foreach (var error in other.Errors)
            foreach (var message in error.Value)
                AddError(error.Key, message);

        foreach (var code in other.ErrorCodes)
            foreach (var c in code.Value)
            {
                if (!ErrorCodes.ContainsKey(code.Key))
                    ErrorCodes[code.Key] = new List<string>();
                ErrorCodes[code.Key].Add(c);
            }
    }
}
