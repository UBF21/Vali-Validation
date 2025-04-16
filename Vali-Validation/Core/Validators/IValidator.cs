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
    /// <returns>An instance of <see cref="IRuleBuilder{T, TProperty}"/> to define fluent validation rules.</returns>
    public IRuleBuilder<T, TProperty> RuleFor<TProperty>(Expression<Func<T, TProperty>> expression);
    
    /// <summary>
    /// Executes all defined validation rules against the given instance.
    /// </summary>
    /// <param name="instance">The object to validate.</param>
    /// <returns>A <see cref="ValidationResult"/> that contains the outcome of the validation,
    /// including any validation errors.</returns>
    ValidationResult Validate(T instance);
}