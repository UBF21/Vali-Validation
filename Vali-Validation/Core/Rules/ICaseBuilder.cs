using Vali_Validation.Core.Validators;

namespace Vali_Validation.Core.Rules;

/// <summary>
/// Fluent builder for validator-level switch/case validation.
/// Allows applying different sets of rules based on a discriminator property value.
/// </summary>
public interface ICaseBuilder<T, TKey> where T : class
{
    /// <summary>Applies the given rules when the key property equals <paramref name="value"/>.</summary>
    ICaseBuilder<T, TKey> Case(TKey value, Action<AbstractValidator<T>> configure);

    /// <summary>Applies the given rules when no case matches.</summary>
    ICaseBuilder<T, TKey> Default(Action<AbstractValidator<T>> configure);
}
