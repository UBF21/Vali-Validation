namespace Vali_Validation.Core.Rules;

/// <summary>
/// Fluent builder for property-level switch/case validation.
/// Applies different rules to the same property based on a discriminator property value.
/// </summary>
public interface ISwitchOnBuilder<T, TProperty, TKey> where T : class
{
    /// <summary>Applies the given rules to the property when the key equals <paramref name="value"/>.</summary>
    ISwitchOnBuilder<T, TProperty, TKey> Case(TKey value, Action<IRuleBuilder<T, TProperty>> configure);

    /// <summary>Applies the given rules to the property when no case matches.</summary>
    ISwitchOnBuilder<T, TProperty, TKey> Default(Action<IRuleBuilder<T, TProperty>> configure);
}
