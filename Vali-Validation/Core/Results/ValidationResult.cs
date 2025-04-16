namespace Vali_Validation.Core.Results;

public class ValidationResult
{
    public Dictionary<string, List<string?>> Errors { get; } = new();
    public bool IsValid => !Errors.Any();
    public void AddError(string property, string? message)
    {
        if (!Errors.ContainsKey(property))
            Errors[property] = new List<string?>();

        Errors[property].Add(message);
    }
}