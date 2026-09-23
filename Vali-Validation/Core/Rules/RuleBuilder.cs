using System.Linq;
using System.Linq.Expressions;
using Vali_Validation.Core.Localization;
using Vali_Validation.Core.Results;
using Vali_Validation.Core.Validators;

namespace Vali_Validation.Core.Rules;

public partial class RuleBuilder<T, TProperty> : IRuleBuilder<T, TProperty> where T : class
{
    private readonly AbstractValidator<T> _validator;

    // Single-value mode: exactly one of these is non-null
    private readonly Func<T, TProperty>? _propertyFunc;
    private readonly Func<T, IEnumerable<TProperty>>? _collectionFunc;

    private readonly List<(Func<TProperty, bool> condition, MessageSpec message, Func<T, bool>? when, string? code, Severity severity)> _rules = new();
    private readonly List<(Func<T, bool> instanceCondition, MessageSpec message, Func<T, bool>? when)> _instanceRules = new();
    private readonly string _propertyName;

    private string _effectivePropertyName;
    private MessageSpec? _currentMessageSpec;
    private Func<TProperty, bool>? _currentCondition;
    private bool _isRuleAdded;
    private bool _stopOnFirstFailure;
    private readonly HashSet<string> _ruleSets = new() { "default" };
    private bool _ruleSetsExplicit;
    private readonly Func<T, bool>? _ambientCondition;

    // -------------------------------------------------------------------------
    // Constructors
    // -------------------------------------------------------------------------

    // Single-value mode
    public RuleBuilder(AbstractValidator<T> validator, Func<T, TProperty> propertyFunc, string propertyName, Func<T, bool>? ambientCondition = null)
    {
        _validator = validator;
        _propertyFunc = propertyFunc;
        _propertyName = propertyName;
        _effectivePropertyName = propertyName;
        _ambientCondition = ambientCondition;
    }

    // Collection mode (used by AbstractValidator.RuleForEach)
    internal RuleBuilder(AbstractValidator<T> validator, Func<T, IEnumerable<TProperty>> collectionFunc, string propertyName, Func<T, bool>? ambientCondition = null)
    {
        _validator = validator;
        _collectionFunc = collectionFunc;
        _propertyName = propertyName;
        _effectivePropertyName = propertyName;
        _ambientCondition = ambientCondition;
    }

    // -------------------------------------------------------------------------
    // Internal helpers for extensions (e.g. SetValidator)
    // -------------------------------------------------------------------------

    internal string EffectivePropertyName => _effectivePropertyName;
    internal Func<T, TProperty>? PropertyFunc => _propertyFunc;
    internal AbstractValidator<T> Validator => _validator;

    internal void AddAsyncRule(Func<T, CancellationToken, Task<ValidationResult>> rule) => _validator.AddRule(WrapWithAmbientCondition(rule), () => _ruleSets);

    internal void AddSyncRule(Func<T, ValidationResult> rule) => _validator.AddRule(WrapWithAmbientCondition(rule), () => _ruleSets);

    private Func<T, ValidationResult> WrapWithAmbientCondition(Func<T, ValidationResult> rule)
    {
        if (_ambientCondition == null) return rule;
        var condition = _ambientCondition;
        return instance => condition(instance) ? rule(instance) : new ValidationResult();
    }

    private Func<T, CancellationToken, Task<ValidationResult>> WrapWithAmbientCondition(Func<T, CancellationToken, Task<ValidationResult>> rule)
    {
        if (_ambientCondition == null) return rule;
        var condition = _ambientCondition;
        return async (instance, ct) => condition(instance) ? await rule(instance, ct).ConfigureAwait(false) : new ValidationResult();
    }

    // -------------------------------------------------------------------------
    // Core registration
    // -------------------------------------------------------------------------

    private void EnsureRegistered()
    {
        if (_isRuleAdded) return;

        if (_collectionFunc != null)
            _validator.AddRule(RunCollectionRules, () => _ruleSets);
        else
            _validator.AddRule(RunSingleValueRules, () => _ruleSets);

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
        foreach (var (condition, messageSpec, when, code, severity) in _rules)
        {
            if (when != null && !when(instance)) continue;
            if (element == null || condition(element)) continue;

            string resolved = ResolveMessage(messageSpec, key, element);
            result.AddFailure(key, resolved, severity, code);
            if (_stopOnFirstFailure) break;
        }
    }

    private string ResolveMessage(MessageSpec spec, string propertyNameForPlaceholder, object? valueForPlaceholder)
    {
        string template = spec.ResolveTemplate(_validator.ActiveLanguage);
        string displayName = Configuration.ValiValidationOptions.Global.DisplayNameResolver(propertyNameForPlaceholder);
        string resolved = template
            .Replace("{PropertyName}", displayName)
            .Replace("{PropertyValue}", FormatPropertyValue(valueForPlaceholder));
        if (spec.Args != null)
            foreach (var (argKey, argValue) in spec.Args)
                resolved = resolved.Replace($"{{{argKey}}}", argValue?.ToString() ?? "");
        return resolved;
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
        foreach (var (condition, messageSpec, when, code, severity) in _rules)
        {
            if (when != null && !when(instance)) continue;
            if (condition(value)) continue;

            string resolved = ResolveMessage(messageSpec, _effectivePropertyName, value);
            result.AddFailure(_effectivePropertyName, resolved, severity, code);
            if (_stopOnFirstFailure) break;
        }
    }

    private void ApplyInstanceRules(T instance, ValidationResult result)
    {
        foreach (var (instanceCondition, messageSpec, when) in _instanceRules)
        {
            if (when != null && !when(instance)) continue;
            if (instanceCondition(instance)) continue;

            // IMPORTANT: pass _propertyName (not _effectivePropertyName) here — this preserves
            // today's exact (pre-existing, unrelated-to-this-task) behavior where cross-property
            // rules (GreaterThanProperty, EqualToProperty, etc.) resolve {PropertyName} from the
            // value captured at RuleFor(...) time, not from any later OverridePropertyName() call.
            string resolved = ResolveMessage(messageSpec, _propertyName, null);
            result.AddFailure(_effectivePropertyName, resolved, Severity.Error);
            if (_stopOnFirstFailure) break;
        }
    }

    private void AddCurrentCondition()
    {
        if (_currentCondition != null)
        {
            MessageSpec spec = _currentMessageSpec ?? MessageSpec.Localized(MessageKey.RuleBuilderDefault);
            _rules.Add((_currentCondition, spec, _ambientCondition, null, Severity.Error));
            _currentCondition = null;
            _currentMessageSpec = null;
        }

        EnsureRegistered();
    }

    private void AddInstanceCondition(Func<T, bool> condition, MessageSpec message)
    {
        _instanceRules.Add((condition, message, _ambientCondition));
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
            var (condition, existingSpec, when, code, severity) = _rules[last];
            MessageSpec newSpec = message != null
                ? MessageSpec.Raw(message, existingSpec.Args)
                : MessageSpec.Localized(MessageKey.RuleBuilderDefault);
            _rules[last] = (condition, newSpec, when, code, severity);
        }
        return this;
    }

    public IRuleBuilder<T, TProperty> WithErrorCode(string code)
    {
        if (_rules.Count > 0)
        {
            int last = _rules.Count - 1;
            var (cond, msg, when, _, severity) = _rules[last];
            _rules[last] = (cond, msg, when, code, severity);
        }
        return this;
    }

    public IRuleBuilder<T, TProperty> WithSeverity(Severity severity)
    {
        if (_rules.Count > 0)
        {
            int last = _rules.Count - 1;
            var (cond, msg, when, code, _) = _rules[last];
            _rules[last] = (cond, msg, when, code, severity);
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

    public IRuleBuilder<T, TProperty> InRuleSet(params string[] ruleSetNames)
    {
        if (ruleSetNames == null || ruleSetNames.Length == 0)
            throw new ArgumentException("At least one rule set name must be provided.", nameof(ruleSetNames));

        if (!_ruleSetsExplicit)
        {
            _ruleSets.Clear();
            _ruleSetsExplicit = true;
        }

        foreach (var name in ruleSetNames)
            _ruleSets.Add(name);

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
            var (cond, msg, existingWhen, code, severity) = _rules[i];
            _rules[i] = (cond, msg, CombineWhen(existingWhen, condition), code, severity);
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

    private const int MaxPropertyValueLength = 200;

    private static string FormatPropertyValue(object? value)
    {
        if (value == null) return "null";
        string text = value.ToString() ?? "null";
        if (text.Length > MaxPropertyValueLength)
            text = text.Substring(0, MaxPropertyValueLength) + "…(truncated)";
        return new string(text.Where(c => !char.IsControl(c)).ToArray());
    }

    // -------------------------------------------------------------------------
    // Custom rule
    // -------------------------------------------------------------------------

    public IRuleBuilder<T, TProperty> Custom(Action<TProperty, CustomValidationContext<T>> action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));
        AddSyncRule(instance =>
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
    // Reusable custom validator
    // -------------------------------------------------------------------------

    public IRuleBuilder<T, TProperty> SetPropertyValidator(IPropertyValidator<TProperty> validator)
    {
        if (validator == null) throw new ArgumentNullException(nameof(validator));

        _currentCondition = validator.IsValid;
        _currentMessageSpec = MessageSpec.FromDictionary(validator.Messages);
        AddCurrentCondition();
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
        return new RuleBuilder<T, TNew>(_validator, newFunc, _propertyName, _ambientCondition);
    }

    // -------------------------------------------------------------------------
    // Property-level switch/case
    // -------------------------------------------------------------------------

    public ISwitchOnBuilder<T, TProperty, TKey> SwitchOn<TKey>(Expression<Func<T, TKey>> keyExpression)
    {
        var keyFunc = keyExpression.Compile();
        return new SwitchOnBuilder<T, TProperty, TKey>(_validator, _propertyFunc!, _effectivePropertyName, keyFunc, _ambientCondition);
    }
}
