using System.Linq.Expressions;
using Vali_Validation.Core.Results;
using Vali_Validation.Core.Validators;

namespace Vali_Validation.Core.Rules;

public partial class RuleBuilder<T, TProperty> where T : class
{
    public IRuleBuilder<T, TProperty> MustAsync(Func<TProperty, Task<bool>> predicateAsync)
    {
        if (predicateAsync == null) throw new ArgumentNullException(nameof(predicateAsync));

        string message = _currentMessage ?? $"The {_propertyName} field does not meet the specified condition.";
        _currentMessage = null;

        AddAsyncRule(async (instance, _) =>
        {
            var result = new ValidationResult();
            TProperty value = _propertyFunc != null ? _propertyFunc(instance) : default!;
            bool isValid = await predicateAsync(value).ConfigureAwait(false);
            if (!isValid) result.AddError(_effectivePropertyName, message);
            return result;
        });

        return this;
    }

    public IRuleBuilder<T, TProperty> MustAsync(Func<TProperty, CancellationToken, Task<bool>> predicateAsync)
    {
        if (predicateAsync == null) throw new ArgumentNullException(nameof(predicateAsync));

        string message = _currentMessage ?? $"The {_propertyName} field does not meet the specified condition.";
        _currentMessage = null;

        AddAsyncRule(async (instance, ct) =>
        {
            var result = new ValidationResult();
            TProperty value = _propertyFunc != null ? _propertyFunc(instance) : default!;
            bool isValid = await predicateAsync(value, ct).ConfigureAwait(false);
            if (!isValid) result.AddError(_effectivePropertyName, message);
            return result;
        });

        return this;
    }

    public IRuleBuilder<T, TProperty> DependentRuleAsync<TDependent>(
        Expression<Func<T, TProperty>> propertyExpression,
        Expression<Func<T, TDependent>> dependentPropertyExpression,
        Func<TProperty, TDependent, Task<bool>> predicateAsync)
    {
        if (predicateAsync == null) throw new ArgumentNullException(nameof(predicateAsync));
        if (propertyExpression == null) throw new ArgumentNullException(nameof(propertyExpression));
        if (dependentPropertyExpression == null) throw new ArgumentNullException(nameof(dependentPropertyExpression));

        var propertyName = AbstractValidator<T>.GetPropertyName(propertyExpression.Body);
        var propertyFunc = propertyExpression.Compile();

        var dependentPropertyName = AbstractValidator<T>.GetPropertyName(dependentPropertyExpression.Body);
        var dependentFunc = dependentPropertyExpression.Compile();

        string message = _currentMessage ?? $"The field {propertyName} does not meet the dependent condition of {dependentPropertyName}.";
        _currentMessage = null;

        AddAsyncRule(async (instance, _) =>
        {
            var result = new ValidationResult();
            TProperty value = propertyFunc(instance);
            TDependent dependentValue = dependentFunc(instance);

            bool isValid = await predicateAsync(value, dependentValue).ConfigureAwait(false);
            if (!isValid) result.AddError(propertyName, message);
            return result;
        });

        return this;
    }

    public IRuleBuilder<T, TProperty> WhenAsync(Func<T, CancellationToken, Task<bool>> condition)
        => When(instance => Task.Run(() => condition(instance, CancellationToken.None)).GetAwaiter().GetResult());

    public IRuleBuilder<T, TProperty> UnlessAsync(Func<T, CancellationToken, Task<bool>> condition)
        => Unless(instance => Task.Run(() => condition(instance, CancellationToken.None)).GetAwaiter().GetResult());
}
