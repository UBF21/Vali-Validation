using Vali_Validation.Core.Results;
using Vali_Validation.Core.Validators;

namespace Vali_Validation.Core.Rules;

internal sealed class SwitchCaseBuilder<T, TKey> : ICaseBuilder<T, TKey> where T : class
{
    private readonly AbstractValidator<T> _validator;
    private readonly Func<T, TKey> _keyFunc;

    private readonly List<(TKey Value, List<Func<T, ValidationResult>> SyncRules, List<Func<T, Task<ValidationResult>>> AsyncRules)> _cases
        = new List<(TKey, List<Func<T, ValidationResult>>, List<Func<T, Task<ValidationResult>>>)>();

    private List<Func<T, ValidationResult>>? _defaultSyncRules;
    private List<Func<T, Task<ValidationResult>>>? _defaultAsyncRules;

    private bool _registered;

    public SwitchCaseBuilder(AbstractValidator<T> validator, Func<T, TKey> keyFunc)
    {
        _validator = validator;
        _keyFunc = keyFunc;
    }

    public ICaseBuilder<T, TKey> Case(TKey value, Action<AbstractValidator<T>> configure)
    {
        var sub = new InlineSwitchValidator<T>();
        configure(sub);

        _cases.Add((
            value,
            new List<Func<T, ValidationResult>>(sub.SyncRules),
            new List<Func<T, Task<ValidationResult>>>(sub.AsyncRules)
        ));

        EnsureRegistered();
        return this;
    }

    public ICaseBuilder<T, TKey> Default(Action<AbstractValidator<T>> configure)
    {
        var sub = new InlineSwitchValidator<T>();
        configure(sub);
        _defaultSyncRules = new List<Func<T, ValidationResult>>(sub.SyncRules);
        _defaultAsyncRules = new List<Func<T, Task<ValidationResult>>>(sub.AsyncRules);
        EnsureRegistered();
        return this;
    }

    private void EnsureRegistered()
    {
        if (_registered) return;
        _registered = true;

        // Register one sync delegate that evaluates the matching case
        _validator.AddRule(instance =>
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

        // Register one async delegate that evaluates the matching case
        _validator.AddRule(async instance =>
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
