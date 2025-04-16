using System.Linq.Expressions;
using Vali_Validation.Core.Results;
using Vali_Validation.Core.Rules;

namespace Vali_Validation.Core.Validators;

public class AbstractValidator<T> : IValidator<T> where T : class
{
    private readonly List<Func<T, ValidationResult>> _rules = new();

    public IRuleBuilder<T, TProperty> RuleFor<TProperty>(Expression<Func<T, TProperty>> expression)
    {
        var propertyName = ((MemberExpression)expression.Body).Member.Name;
        var propertyFunc = expression.Compile();
        return new RuleBuilder<T, TProperty>(this, propertyFunc, propertyName);
    }

    internal void AddRule(Func<T, ValidationResult> rule) => _rules.Add(rule);
    

    public ValidationResult Validate(T instance)
    {
        var result = new ValidationResult();
        foreach (var rule in _rules)
        {
            ValidationResult partial = rule(instance);
            foreach (var error in partial.Errors)
            {
                foreach (string? message in error.Value)
                {
                    result.AddError(error.Key, message);
                }
            }
        }

        return result;
    }
}