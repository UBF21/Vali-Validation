using Vali_Validation.Core.Results;
using Vali_Validation.Core.Validators;

namespace Vali_Validation.Core.Rules;

/// <summary>
/// Shared case/default registration and dispatch logic for <see cref="SwitchOnBuilder{T,TProperty,TKey}"/>
/// and <see cref="SwitchCaseBuilder{T,TKey}"/>. Both builders collect per-case sync/async rule lists keyed
/// by <typeparamref name="TKey"/>, register a sync dispatch delegate always and an async dispatch delegate
/// only if at least one case (or the default) registered an async rule, and at evaluation time run the
/// rules for the matching case (or the default case, if any).
/// </summary>
internal abstract class SwitchRegistry<T, TKey> where T : class
{
    private readonly AbstractValidator<T> _validator;
    private readonly Func<T, TKey> _keyFunc;

    private readonly List<(TKey Value, List<Func<T, ValidationResult>> SyncRules, List<Func<T, CancellationToken, Task<ValidationResult>>> AsyncRules)> _cases
        = new List<(TKey, List<Func<T, ValidationResult>>, List<Func<T, CancellationToken, Task<ValidationResult>>>)>();

    private List<Func<T, ValidationResult>>? _defaultSyncRules;
    private List<Func<T, CancellationToken, Task<ValidationResult>>>? _defaultAsyncRules;

    private bool _syncRegistered;
    private bool _asyncRegistered;

    protected SwitchRegistry(AbstractValidator<T> validator, Func<T, TKey> keyFunc)
    {
        _validator = validator;
        _keyFunc = keyFunc;
    }

    protected void AddCase(TKey value, InlineSwitchValidator<T> configured)
    {
        _cases.Add((
            value,
            new List<Func<T, ValidationResult>>(configured.SyncRules),
            new List<Func<T, CancellationToken, Task<ValidationResult>>>(configured.AsyncRules)
        ));
        EnsureSyncRegistered();
        if (configured.AsyncRules.Count > 0) EnsureAsyncRegistered();
    }

    protected void SetDefault(InlineSwitchValidator<T> configured)
    {
        _defaultSyncRules = new List<Func<T, ValidationResult>>(configured.SyncRules);
        _defaultAsyncRules = new List<Func<T, CancellationToken, Task<ValidationResult>>>(configured.AsyncRules);
        EnsureSyncRegistered();
        if (configured.AsyncRules.Count > 0) EnsureAsyncRegistered();
    }

    private void EnsureSyncRegistered()
    {
        if (_syncRegistered) return;
        _syncRegistered = true;
        _validator.AddRule(RunSyncCase);
    }

    private void EnsureAsyncRegistered()
    {
        if (_asyncRegistered) return;
        _asyncRegistered = true;
        _validator.AddRule(RunAsyncCase);
    }

    private ValidationResult RunSyncCase(T instance)
    {
        var result = new ValidationResult();
        var key = _keyFunc(instance);

        var matched = FindMatchingCase(key);
        var rulesToRun = matched?.SyncRules ?? _defaultSyncRules;
        if (rulesToRun == null) return result;

        foreach (var rule in rulesToRun)
            MergeInto(result, rule(instance));
        return result;
    }

    private async Task<ValidationResult> RunAsyncCase(T instance, CancellationToken ct)
    {
        var result = new ValidationResult();
        var key = _keyFunc(instance);

        var matched = FindMatchingCase(key);
        var rulesToRun = matched?.AsyncRules ?? _defaultAsyncRules;
        if (rulesToRun == null) return result;

        foreach (var rule in rulesToRun)
            MergeInto(result, await rule(instance, ct).ConfigureAwait(false));
        return result;
    }

    private (List<Func<T, ValidationResult>> SyncRules, List<Func<T, CancellationToken, Task<ValidationResult>>> AsyncRules)? FindMatchingCase(TKey key)
    {
        foreach (var entry in _cases)
        {
            if (Equals(key, entry.Value))
                return (entry.SyncRules, entry.AsyncRules);
        }
        return null;
    }

    private static void MergeInto(ValidationResult target, ValidationResult source)
    {
        foreach (var kvp in source.Errors)
            foreach (var msg in kvp.Value)
                target.AddError(kvp.Key, msg);
    }
}
