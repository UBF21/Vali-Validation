using Vali_Validation.Core.Results;
using Vali_Validation.Core.Validators;

namespace Vali_Validation.Core.Rules;

/// <summary>
/// Extension methods for <see cref="IRuleBuilder{T,TProperty}"/> that require type constraints
/// not available on the interface itself.
/// </summary>
public static class RuleBuilderValidatorExtensions
{
    /// <summary>
    /// Delegates validation of the current property to a nested validator.
    /// Errors are prefixed with the property name, e.g. <c>Address.Street</c>.
    /// </summary>
    /// <typeparam name="T">The root object type.</typeparam>
    /// <typeparam name="TProperty">The nested object type (must be a class).</typeparam>
    /// <param name="builder">The rule builder for the nested property.</param>
    /// <param name="nestedValidator">Validator to apply to the nested object.</param>
    public static IRuleBuilder<T, TProperty> SetValidator<T, TProperty>(
        this IRuleBuilder<T, TProperty> builder,
        AbstractValidator<TProperty> nestedValidator)
        where T : class
        where TProperty : class
    {
        if (builder is not RuleBuilder<T, TProperty> rb)
            return builder;

        string prefix = rb.EffectivePropertyName;

        rb.AddAsyncRule(async instance =>
        {
            TProperty? value = rb.PropertyFunc?.Invoke(instance);
            if (value == null) return new ValidationResult();

            var nestedResult = await nestedValidator.ValidateAsync(value).ConfigureAwait(false);
            var merged = new ValidationResult();
            foreach (var error in nestedResult.Errors)
                foreach (var message in error.Value)
                    merged.AddError($"{prefix}.{error.Key}", message);
            return merged;
        });

        return builder;
    }
}
