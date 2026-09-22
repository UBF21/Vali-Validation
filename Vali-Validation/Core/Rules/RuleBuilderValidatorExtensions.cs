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
    /// <remarks>
    /// If <paramref name="nestedValidator"/> has no asynchronous rules, the nested validation runs
    /// synchronously and its errors are visible to both <see cref="AbstractValidator{T}.Validate"/> and
    /// <see cref="AbstractValidator{T}.ValidateAsync"/>. If <paramref name="nestedValidator"/> has any
    /// asynchronous rules, the nested validation can only run via <see cref="AbstractValidator{T}.ValidateAsync"/> —
    /// <see cref="AbstractValidator{T}.Validate"/> will not evaluate it, consistent with <c>Validate()</c>
    /// only ever executing synchronous rules.
    /// </remarks>
    /// <typeparam name="T">The root object type.</typeparam>
    /// <typeparam name="TProperty">The nested object type (must be a class).</typeparam>
    /// <param name="builder">The rule builder for the nested property.</param>
    /// <param name="nestedValidator">Validator to apply to the nested object.</param>
    public static IRuleBuilder<T, TProperty?> SetValidator<T, TProperty>(
        this IRuleBuilder<T, TProperty?> builder,
        AbstractValidator<TProperty> nestedValidator)
        where T : class
        where TProperty : class
    {
        if (builder is not RuleBuilder<T, TProperty> rb)
            return builder;

        string prefix = rb.EffectivePropertyName;

        if (nestedValidator.AsyncRules.Count == 0)
        {
            rb.AddSyncRule(instance =>
            {
                TProperty? value = rb.PropertyFunc?.Invoke(instance);
                return value == null ? new ValidationResult() : MergeNested(nestedValidator.Validate(value), prefix);
            });
        }
        else
        {
            rb.AddAsyncRule(async (instance, ct) =>
            {
                TProperty? value = rb.PropertyFunc?.Invoke(instance);
                if (value == null) return new ValidationResult();

                var nestedResult = await nestedValidator.ValidateAsync(value, ct).ConfigureAwait(false);
                return MergeNested(nestedResult, prefix);
            });
        }

        return builder;
    }

    private static ValidationResult MergeNested(ValidationResult nestedResult, string prefix)
    {
        var merged = new ValidationResult();
        foreach (var error in nestedResult.Errors)
            foreach (var message in error.Value)
                merged.AddError($"{prefix}.{error.Key}", message);
        return merged;
    }
}
