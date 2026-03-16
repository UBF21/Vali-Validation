using Vali_Validation.Core.Results;
using Vali_Validation.Core.Validators;

namespace Vali_Validation.Core.Rules;

internal sealed class SwitchOnBuilder<T, TProperty, TKey> : ISwitchOnBuilder<T, TProperty, TKey>
    where T : class
{
    private readonly AbstractValidator<T> _parentValidator;
    private readonly Func<T, TProperty> _propertyFunc;
    private readonly string _propertyName;
    private readonly Func<T, TKey> _keyFunc;

    private readonly List<(TKey Value, List<Func<T, ValidationResult>> SyncRules, List<Func<T, Task<ValidationResult>>> AsyncRules)> _cases
        = new List<(TKey, List<Func<T, ValidationResult>>, List<Func<T, Task<ValidationResult>>>)>();

    private List<Func<T, ValidationResult>>? _defaultSyncRules;
    private List<Func<T, Task<ValidationResult>>>? _defaultAsyncRules;

    private bool _registered;

    public SwitchOnBuilder(
        AbstractValidator<T> parentValidator,
        Func<T, TProperty> propertyFunc,
        string propertyName,
        Func<T, TKey> keyFunc)
    {
        _parentValidator = parentValidator;
        _propertyFunc = propertyFunc;
        _propertyName = propertyName;
        _keyFunc = keyFunc;
    }

    public ISwitchOnBuilder<T, TProperty, TKey> Case(TKey value, Action<IRuleBuilder<T, TProperty>> configure)
    {
        var tempValidator = new InlineSwitchValidator<T>();
        var tempBuilder = new RuleBuilder<T, TProperty>(tempValidator, _propertyFunc, _propertyName);
        configure(tempBuilder);

        _cases.Add((
            value,
            new List<Func<T, ValidationResult>>(tempValidator.SyncRules),
            new List<Func<T, Task<ValidationResult>>>(tempValidator.AsyncRules)
        ));

        EnsureRegistered();
        return this;
    }

    public ISwitchOnBuilder<T, TProperty, TKey> Default(Action<IRuleBuilder<T, TProperty>> configure)
    {
        var tempValidator = new InlineSwitchValidator<T>();
        var tempBuilder = new RuleBuilder<T, TProperty>(tempValidator, _propertyFunc, _propertyName);
        configure(tempBuilder);

        _defaultSyncRules = new List<Func<T, ValidationResult>>(tempValidator.SyncRules);
        _defaultAsyncRules = new List<Func<T, Task<ValidationResult>>>(tempValidator.AsyncRules);
        EnsureRegistered();
        return this;
    }

    private void EnsureRegistered()
    {
        if (_registered) return;
        _registered = true;

        _parentValidator.AddRule(instance =>
        {
            var result = new ValidationResult();
            var key = _keyFunc(instance);

            foreach (var entry in _cases)
            {
                if (!Equals(key, entry.Value)) continue;
                foreach (var rule in entry.SyncRules)
                    MergeInto(result, rule(instance));
                return result;
            }

            if (_defaultSyncRules != null)
                foreach (var rule in _defaultSyncRules)
                    MergeInto(result, rule(instance));

            return result;
        });

        _parentValidator.AddRule(async instance =>
        {
            var result = new ValidationResult();
            var key = _keyFunc(instance);

            foreach (var entry in _cases)
            {
                if (!Equals(key, entry.Value)) continue;
                foreach (var rule in entry.AsyncRules)
                    MergeInto(result, await rule(instance).ConfigureAwait(false));
                return result;
            }

            if (_defaultAsyncRules != null)
                foreach (var rule in _defaultAsyncRules)
                    MergeInto(result, await rule(instance).ConfigureAwait(false));

            return result;
        });
    }

    private static void MergeInto(ValidationResult target, ValidationResult source)
    {
        foreach (var kvp in source.Errors)
            foreach (var msg in kvp.Value)
                target.AddError(kvp.Key, msg);
    }
}
