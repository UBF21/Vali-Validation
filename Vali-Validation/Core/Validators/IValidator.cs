using System.Linq.Expressions;
using Vali_Validation.Core.Results;
using Vali_Validation.Core.Rules;

namespace Vali_Validation.Core.Validators;

/// <summary>
/// Defines a validator for a specific type. Allows configuration of validation rules
/// and execution of validations against instances of the type.
/// </summary>
/// <typeparam name="T">The type of object to validate. Must be a reference type.</typeparam>
public interface IValidator<T> where T : class
{
    /// <summary>
    /// Begins the definition of validation rules for a specified property of the object.
    /// </summary>
    /// <typeparam name="TProperty">The type of the property to validate.</typeparam>
    /// <param name="expression">An expression that selects the property to validate (x => x.Name).</param>
    /// <returns>An <see cref="IRuleBuilder{T, TProperty}"/> to define fluent validation rules.</returns>
    IRuleBuilder<T, TProperty> RuleFor<TProperty>(Expression<Func<T, TProperty>> expression);

    /// <summary>
    /// Executes all synchronous validation rules against the given instance.
    /// </summary>
    /// <param name="instance">The object to validate.</param>
    /// <returns>A <see cref="ValidationResult"/> containing any validation errors.</returns>
    ValidationResult Validate(T instance);

    /// <summary>
    /// Executes all validation rules (sync and async) against the given instance.
    /// </summary>
    /// <param name="instance">The object to validate.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="ValidationResult"/> containing any validation errors.</returns>
    Task<ValidationResult> ValidateAsync(T instance, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes all synchronous validation rules and throws <see cref="Exceptions.ValidationException"/> if validation fails.
    /// </summary>
    void ValidateAndThrow(T instance);

    /// <summary>
    /// Executes all validation rules and throws <see cref="Exceptions.ValidationException"/> if validation fails.
    /// </summary>
    Task ValidateAndThrowAsync(T instance, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes sync rules sequentially and async rules in parallel, then merges results.
    /// </summary>
    Task<ValidationResult> ValidateParallelAsync(T instance, CancellationToken cancellationToken = default);
}
