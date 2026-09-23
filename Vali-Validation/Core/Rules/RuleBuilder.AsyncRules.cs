using System.Linq.Expressions;
using Vali_Validation.Core.Localization;
using Vali_Validation.Core.Results;
using Vali_Validation.Core.Validators;

namespace Vali_Validation.Core.Rules;

public partial class RuleBuilder<T, TProperty> where T : class
{
    public IRuleBuilder<T, TProperty> MustAsync(Func<TProperty, Task<bool>> predicateAsync)
    {
        if (predicateAsync == null) throw new ArgumentNullException(nameof(predicateAsync));

        var state = new AsyncRuleState { Spec = _currentMessageSpec ?? MessageSpec.Localized(MessageKey.MustAsync) };
        _currentMessageSpec = null;
        _asyncRuleStates.Add(state);

        AddAsyncRule(async (instance, _) =>
        {
            var result = new ValidationResult();
            if (state.When != null && !state.When(instance)) return result;
            TProperty value = _propertyFunc != null ? _propertyFunc(instance) : default!;
            bool isValid = await predicateAsync(value).ConfigureAwait(false);
            if (!isValid) result.AddFailure(_effectivePropertyName, ResolveMessage(state.Spec, _effectivePropertyName, value), state.Severity, state.ErrorCode);
            return result;
        });
        // AddAsyncRule defaults _lastAdditionWasAsync to false (it's the shared choke point every
        // direct rule registration funnels through); flip it back to true now that this state is in place.
        _lastAdditionWasAsync = true;

        return this;
    }

    public IRuleBuilder<T, TProperty> MustAsync(Func<TProperty, CancellationToken, Task<bool>> predicateAsync)
    {
        if (predicateAsync == null) throw new ArgumentNullException(nameof(predicateAsync));

        var state = new AsyncRuleState { Spec = _currentMessageSpec ?? MessageSpec.Localized(MessageKey.MustAsync) };
        _currentMessageSpec = null;
        _asyncRuleStates.Add(state);

        AddAsyncRule(async (instance, ct) =>
        {
            var result = new ValidationResult();
            if (state.When != null && !state.When(instance)) return result;
            TProperty value = _propertyFunc != null ? _propertyFunc(instance) : default!;
            bool isValid = await predicateAsync(value, ct).ConfigureAwait(false);
            if (!isValid) result.AddFailure(_effectivePropertyName, ResolveMessage(state.Spec, _effectivePropertyName, value), state.Severity, state.ErrorCode);
            return result;
        });
        _lastAdditionWasAsync = true;

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

        var state = new AsyncRuleState
        {
            Spec = _currentMessageSpec ?? MessageSpec.Localized(MessageKey.DependentRuleAsync,
                new Dictionary<string, object> { ["dependentPropertyName"] = Configuration.ValiValidationOptions.Global.DisplayNameResolver(dependentPropertyName) })
        };
        _currentMessageSpec = null;
        _asyncRuleStates.Add(state);

        AddAsyncRule(async (instance, _) =>
        {
            var result = new ValidationResult();
            if (state.When != null && !state.When(instance)) return result;
            TProperty value = propertyFunc(instance);
            TDependent dependentValue = dependentFunc(instance);

            bool isValid = await predicateAsync(value, dependentValue).ConfigureAwait(false);
            if (!isValid) result.AddFailure(propertyName, ResolveMessage(state.Spec, propertyName, value), state.Severity, state.ErrorCode);
            return result;
        });
        _lastAdditionWasAsync = true;

        return this;
    }

    public IRuleBuilder<T, TProperty> WhenAsync(Func<T, CancellationToken, Task<bool>> condition)
        => When(instance => Task.Run(() => condition(instance, _validator.CurrentCancellationToken)).GetAwaiter().GetResult());

    public IRuleBuilder<T, TProperty> UnlessAsync(Func<T, CancellationToken, Task<bool>> condition)
        => Unless(instance => Task.Run(() => condition(instance, _validator.CurrentCancellationToken)).GetAwaiter().GetResult());
}
