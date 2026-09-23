namespace Vali_Validation.Core.Rules;

/// <summary>
/// Convenience base class for <see cref="IPropertyValidator{TProperty}"/> — override
/// <see cref="IsValid"/> and, optionally, <see cref="Messages"/> (defaults to a single English
/// entry: <c>"The {PropertyName} field is invalid."</c>).
/// </summary>
/// <typeparam name="TProperty">The property type this validator checks.</typeparam>
public abstract class PropertyValidator<TProperty> : IPropertyValidator<TProperty>
{
    /// <inheritdoc/>
    public abstract bool IsValid(TProperty value);

    /// <inheritdoc/>
    public virtual IReadOnlyDictionary<string, string> Messages { get; } = new Dictionary<string, string>
    {
        ["en"] = "The {PropertyName} field is invalid."
    };
}
