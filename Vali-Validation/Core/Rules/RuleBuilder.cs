using System.Linq.Expressions;
using Vali_Validation.Core.Results;
using Vali_Validation.Core.Validators;

namespace Vali_Validation.Core.Rules;

public partial class RuleBuilder<T, TProperty> : IRuleBuilder<T, TProperty> where T : class
{
    private readonly AbstractValidator<T> _validator;

    // Single-value mode: exactly one of these is non-null
    private readonly Func<T, TProperty>? _propertyFunc;
    private readonly Func<T, IEnumerable<TProperty>>? _collectionFunc;

    private readonly List<(Func<TProperty, bool> condition, string message, Func<T, bool>? when, string? code)> _rules = new();
    private readonly List<(Func<T, bool> instanceCondition, string message, Func<T, bool>? when)> _instanceRules = new();
    private readonly string _propertyName;

    private string _effectivePropertyName;
    private string? _currentMessage;
    private Func<TProperty, bool>? _currentCondition;
    private bool _isRuleAdded;
    private bool _stopOnFirstFailure;

    // -------------------------------------------------------------------------
    // Constructors
    // -------------------------------------------------------------------------

    // Single-value mode
    public RuleBuilder(AbstractValidator<T> validator, Func<T, TProperty> propertyFunc, string propertyName)
    {
        _validator = validator;
        _propertyFunc = propertyFunc;
        _propertyName = propertyName;
        _effectivePropertyName = propertyName;
    }

    // Collection mode (used by AbstractValidator.RuleForEach)
    internal RuleBuilder(AbstractValidator<T> validator, Func<T, IEnumerable<TProperty>> collectionFunc, string propertyName)
    {
        _validator = validator;
        _collectionFunc = collectionFunc;
        _propertyName = propertyName;
        _effectivePropertyName = propertyName;
    }

    // -------------------------------------------------------------------------
    // Internal helpers for extensions (e.g. SetValidator)
    // -------------------------------------------------------------------------

    internal string EffectivePropertyName => _effectivePropertyName;
    internal Func<T, TProperty>? PropertyFunc => _propertyFunc;

    internal void AddAsyncRule(Func<T, CancellationToken, Task<ValidationResult>> rule) => _validator.AddRule(rule);

    // -------------------------------------------------------------------------
    // Core registration
    // -------------------------------------------------------------------------

    private void EnsureRegistered()
    {
        if (_isRuleAdded) return;

        if (_collectionFunc != null)
            _validator.AddRule(RunCollectionRules);
        else
            _validator.AddRule(RunSingleValueRules);

        _isRuleAdded = true;
    }

    // Collection mode: apply every property rule to each element, keyed as "{Property}[{index}]".
    private ValidationResult RunCollectionRules(T instance)
    {
        var result = new ValidationResult();
        var collection = _collectionFunc!(instance);
        if (collection == null) return result;

        int index = 0;
        foreach (var element in collection)
        {
            string key = $"{_effectivePropertyName}[{index}]";
            ApplyElementRules(instance, element, key, result);
            index++;
        }
        return result;
    }

    private void ApplyElementRules(T instance, TProperty element, string key, ValidationResult result)
    {
        foreach (var (condition, message, when, code) in _rules)
        {
            if (when != null && !when(instance)) continue;
            if (element == null || condition(element)) continue;

            string resolved = message
                .Replace("{PropertyName}", key)
                .Replace("{PropertyValue}", element?.ToString() ?? "null");
            result.AddError(key, resolved, code);
            if (_stopOnFirstFailure) break;
        }
    }

    // Single-value mode: apply property rules against the resolved value, then instance-level rules.
    private ValidationResult RunSingleValueRules(T instance)
    {
        var result = new ValidationResult();
        TProperty value = _propertyFunc!(instance);
        ApplyPropertyRules(instance, value, result);
        ApplyInstanceRules(instance, result);
        return result;
    }

    private void ApplyPropertyRules(T instance, TProperty value, ValidationResult result)
    {
        foreach (var (condition, message, when, code) in _rules)
        {
            if (when != null && !when(instance)) continue;
            if (condition(value)) continue;

            string resolved = message
                .Replace("{PropertyName}", _effectivePropertyName)
                .Replace("{PropertyValue}", value?.ToString() ?? "null");
            result.AddError(_effectivePropertyName, resolved, code);
            if (_stopOnFirstFailure) break;
        }
    }

    private void ApplyInstanceRules(T instance, ValidationResult result)
    {
        foreach (var (instanceCondition, message, when) in _instanceRules)
        {
            if (when != null && !when(instance)) continue;
            if (instanceCondition(instance)) continue;

            result.AddError(_effectivePropertyName, message);
            if (_stopOnFirstFailure) break;
        }
    }

    private void AddCurrentCondition()
    {
        if (_currentCondition != null)
        {
            string message = _currentMessage ?? $"The {_effectivePropertyName} field is invalid.";
            _rules.Add((_currentCondition, message, null, null));
            _currentCondition = null;
            _currentMessage = null;
        }

        EnsureRegistered();
    }

    private void AddInstanceCondition(Func<T, bool> condition, string message)
    {
        _instanceRules.Add((condition, message, null));
        EnsureRegistered();
    }

    // -------------------------------------------------------------------------
    // Modifiers
    // -------------------------------------------------------------------------

    public IRuleBuilder<T, TProperty> WithMessage(string? message)
    {
        if (_rules.Count > 0)
        {
            int last = _rules.Count - 1;
            var (condition, _, when, code) = _rules[last];
            _rules[last] = (condition, message ?? $"The {_effectivePropertyName} field is invalid.", when, code);
        }
        return this;
    }

    public IRuleBuilder<T, TProperty> WithErrorCode(string code)
    {
        if (_rules.Count > 0)
        {
            int last = _rules.Count - 1;
            var (cond, msg, when, _) = _rules[last];
            _rules[last] = (cond, msg, when, code);
        }
        return this;
    }

    public IRuleBuilder<T, TProperty> OverridePropertyName(string name)
    {
        _effectivePropertyName = name;
        return this;
    }

    public IRuleBuilder<T, TProperty> StopOnFirstFailure()
    {
        _stopOnFirstFailure = true;
        return this;
    }

    /// <summary>
    /// Applies a guard condition to ALL rules defined so far in this builder. Rules are skipped
    /// when <paramref name="condition"/> returns false. If a rule already has a guard from an
    /// earlier <see cref="When"/>/<see cref="Unless"/> call, the new condition is combined with it
    /// using logical AND — it does not replace the earlier guard.
    /// </summary>
    public IRuleBuilder<T, TProperty> When(Func<T, bool> condition)
    {
        for (int i = 0; i < _rules.Count; i++)
        {
            var (cond, msg, existingWhen, code) = _rules[i];
            _rules[i] = (cond, msg, CombineWhen(existingWhen, condition), code);
        }
        for (int i = 0; i < _instanceRules.Count; i++)
        {
            var (cond, msg, existingWhen) = _instanceRules[i];
            _instanceRules[i] = (cond, msg, CombineWhen(existingWhen, condition));
        }
        return this;
    }

    public IRuleBuilder<T, TProperty> Unless(Func<T, bool> condition)
        => When(instance => !condition(instance));

    private static Func<T, bool> CombineWhen(Func<T, bool>? existing, Func<T, bool> next)
        => existing == null ? next : instance => existing(instance) && next(instance);

    // -------------------------------------------------------------------------
    // Custom rule
    // -------------------------------------------------------------------------

    public IRuleBuilder<T, TProperty> Custom(Action<TProperty, CustomValidationContext<T>> action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));
        _validator.AddRule(instance =>
        {
            var result = new ValidationResult();
            TProperty value = _propertyFunc != null ? _propertyFunc(instance) : default!;
            var context = new CustomValidationContext<T>(instance, result, _effectivePropertyName);
            action(value, context);
            return result;
        });
        return this;
    }

    // -------------------------------------------------------------------------
    // Transform
    // -------------------------------------------------------------------------

    public RuleBuilder<T, TNew> Transform<TNew>(Func<TProperty, TNew> transform)
    {
        if (transform == null) throw new ArgumentNullException(nameof(transform));
        if (_propertyFunc == null)
            throw new InvalidOperationException("Transform is not supported in collection mode.");
        Func<T, TNew> newFunc = instance => transform(_propertyFunc(instance));
        return new RuleBuilder<T, TNew>(_validator, newFunc, _propertyName);
    }

    // -------------------------------------------------------------------------
    // Property-level switch/case
    // -------------------------------------------------------------------------

    public ISwitchOnBuilder<T, TProperty, TKey> SwitchOn<TKey>(Expression<Func<T, TKey>> keyExpression)
    {
        var keyFunc = keyExpression.Compile();
        return new SwitchOnBuilder<T, TProperty, TKey>(_validator, _propertyFunc!, _effectivePropertyName, keyFunc);
    }
}
