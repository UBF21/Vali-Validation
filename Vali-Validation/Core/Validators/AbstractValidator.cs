using System.Collections.Concurrent;
using System.Linq.Expressions;
using Vali_Validation.Core.Exceptions;
using Vali_Validation.Core.Results;
using Vali_Validation.Core.Rules;

namespace Vali_Validation.Core.Validators;

/// <summary>
/// Base class for all validators. Subclass this and call <see cref="RuleFor"/> in the constructor
/// to define validation rules.
/// </summary>
public abstract partial class AbstractValidator<T> : IValidator<T> where T : class
{
    private readonly List<Func<T, ValidationResult>> _syncRules = new();
    private readonly List<Func<T, CancellationToken, Task<ValidationResult>>> _asyncRules = new();

    private static readonly ConcurrentDictionary<string, Delegate> _compiledExpressionCache = new();

    private static Func<T, TResult> CompileCached<TResult>(Expression<Func<T, TResult>> expression)
    {
        string key = typeof(TResult).FullName + ":" + expression.ToString();
        return (Func<T, TResult>)_compiledExpressionCache.GetOrAdd(key, _ => expression.Compile());
    }

    protected virtual CascadeMode GlobalCascadeMode => CascadeMode.Continue;

    /// <summary>
    /// Runs before any rules are evaluated. Override to short-circuit validation entirely —
    /// return <c>false</c> and add errors directly to <paramref name="result"/> to skip all
    /// registered rules (useful for guard clauses like "instance must not be null" that would
    /// otherwise require every rule to handle a bad instance defensively).
    /// </summary>
    /// <param name="instance">The object about to be validated.</param>
    /// <param name="result">The result being built. Add errors here if returning <c>false</c>.</param>
    /// <returns>
    /// <c>true</c> (the default) to proceed with normal rule evaluation; <c>false</c> to skip it.
    /// <strong>WARNING:</strong> Returning <c>false</c> without adding at least one error to
    /// <paramref name="result"/> produces a silent pass — <c>Validate()</c> will return an empty,
    /// <c>IsValid == true</c> result even though rule evaluation was skipped. This is the most
    /// dangerous misuse of this hook. Always add at least one error before returning <c>false</c>.
    /// </returns>
    protected virtual bool PreValidate(T instance, ValidationResult result) => true;

    /// <inheritdoc/>
    /// <remarks>
    /// Calling <c>RuleFor</c> more than once for the same property is supported and additive —
    /// each call returns an independent rule builder, and both sets of rules run. Earlier calls
    /// are never replaced or discarded.
    /// </remarks>
    public IRuleBuilder<T, TProperty> RuleFor<TProperty>(Expression<Func<T, TProperty>> expression)
    {
        var propertyName = GetPropertyName(expression.Body);
        var propertyFunc = CompileCached(expression);
        return new RuleBuilder<T, TProperty>(this, propertyFunc, propertyName, _ambientCondition);
    }

    /// <summary>
    /// Begins a validation rule for each element of a collection property.
    /// Errors are reported with indexed keys, e.g. <c>Items[0]</c>.
    /// </summary>
    public IRuleBuilder<T, TElement> RuleForEach<TElement>(
        Expression<Func<T, IEnumerable<TElement>>> expression)
    {
        var collectionName = GetPropertyName(expression.Body);
        var collectionFunc = CompileCached(expression);
        return new RuleBuilder<T, TElement>(this, collectionFunc, collectionName, _ambientCondition);
    }

    /// <summary>
    /// Begins a validation rule for each element of a collection property that satisfies
    /// <paramref name="filter"/>. Elements that don't satisfy the filter are excluded from
    /// validation entirely — they are never checked against the rules and never produce errors,
    /// even if they would otherwise be invalid.
    /// </summary>
    /// <remarks>
    /// Error keys are indexed by the FILTERED sequence position (e.g. <c>Items[0]</c> refers to
    /// the first element that passed <paramref name="filter"/>, not its position in the original
    /// collection).
    /// </remarks>
    public IRuleBuilder<T, TElement> RuleForEach<TElement>(
        Expression<Func<T, IEnumerable<TElement>>> expression,
        Func<TElement, bool> filter)
    {
        var collectionName = GetPropertyName(expression.Body);
        var collectionFunc = CompileCached(expression);
        Func<T, IEnumerable<TElement>> filteredFunc =
            instance => collectionFunc(instance)?.Where(filter).ToList() ?? new List<TElement>();
        return new RuleBuilder<T, TElement>(this, filteredFunc, collectionName, _ambientCondition);
    }

    internal void AddRule(Func<T, ValidationResult> rule, Func<IReadOnlySet<string>>? ruleSetsProvider = null)
    {
        _syncRules.Add(rule);
        _syncRuleSets[rule] = ruleSetsProvider ?? (() => DefaultRuleSet);
    }

    internal void AddRule(Func<T, CancellationToken, Task<ValidationResult>> rule, Func<IReadOnlySet<string>>? ruleSetsProvider = null)
    {
        _asyncRules.Add(rule);
        _asyncRuleSets[rule] = ruleSetsProvider ?? (() => DefaultRuleSet);
    }

    internal IReadOnlyList<Func<T, ValidationResult>> SyncRules => _syncRules;
    internal IReadOnlyList<Func<T, CancellationToken, Task<ValidationResult>>> AsyncRules => _asyncRules;

    /// <summary>
    /// Begins a switch/case validation block keyed on <paramref name="keyExpression"/>.
    /// Different rules are applied to the object depending on the key value.
    /// </summary>
    protected ICaseBuilder<T, TKey> RuleSwitch<TKey>(Expression<Func<T, TKey>> keyExpression)
    {
        var keyFunc = CompileCached(keyExpression);
        return new SwitchCaseBuilder<T, TKey>(this, keyFunc, _ambientCondition);
    }

    protected void Include(AbstractValidator<T> other)
    {
        foreach (var rule in other.SyncRules)
        {
            _syncRules.Add(rule);
            _syncRuleSets[rule] = other._syncRuleSets.TryGetValue(rule, out var provider) ? provider : (() => DefaultRuleSet);
        }
        foreach (var rule in other.AsyncRules)
        {
            _asyncRules.Add(rule);
            _asyncRuleSets[rule] = other._asyncRuleSets.TryGetValue(rule, out var provider) ? provider : (() => DefaultRuleSet);
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Nested validators attached via <c>SetValidator</c> whose own validator has any asynchronous
    /// rules are only evaluated by <see cref="ValidateAsync"/>. If the nested validator is fully
    /// synchronous, its errors ARE included in the result of this method.
    /// </remarks>
    public ValidationResult Validate(T instance)
    {
        var result = new ValidationResult();
        if (!PreValidate(instance, result)) return result;
        foreach (var rule in _syncRules)
        {
            result.Merge(rule(instance));
            if (GlobalCascadeMode == CascadeMode.StopOnFirstFailure && !result.IsValid) break;
        }
        return result;
    }

    /// <inheritdoc/>
    public async Task<ValidationResult> ValidateAsync(T instance, CancellationToken cancellationToken = default)
    {
        var result = new ValidationResult();
        if (!PreValidate(instance, result)) return result;
        foreach (var rule in _syncRules)
        {
            result.Merge(rule(instance));
            if (GlobalCascadeMode == CascadeMode.StopOnFirstFailure && !result.IsValid) return result;
        }
        foreach (var rule in _asyncRules)
        {
            result.Merge(await rule(instance, cancellationToken).ConfigureAwait(false));
            if (GlobalCascadeMode == CascadeMode.StopOnFirstFailure && !result.IsValid) return result;
        }
        return result;
    }

    /// <inheritdoc/>
    public void ValidateAndThrow(T instance)
    {
        var result = Validate(instance);
        if (!result.IsValid) throw new ValidationException(result);
    }

    /// <inheritdoc/>
    public async Task ValidateAndThrowAsync(T instance, CancellationToken cancellationToken = default)
    {
        var result = await ValidateAsync(instance, cancellationToken).ConfigureAwait(false);
        if (!result.IsValid) throw new ValidationException(result);
    }

    /// <inheritdoc/>
    public async Task<ValidationResult> ValidateParallelAsync(T instance, CancellationToken cancellationToken = default)
    {
        var result = new ValidationResult();
        if (!PreValidate(instance, result)) return result;
        foreach (var rule in _syncRules)
            result.Merge(rule(instance));

        if (_asyncRules.Count > 0)
        {
            var tasks = _asyncRules.Select(rule => rule(instance, cancellationToken));
            var results = await Task.WhenAll(tasks).ConfigureAwait(false);
            foreach (var partial in results)
                result.Merge(partial);
        }

        return result;
    }

    internal static string GetPropertyName(Expression expression)
    {
        if (expression is MemberExpression member) return member.Member.Name;
        if (expression is UnaryExpression unary) return GetPropertyName(unary.Operand);
        throw new ArgumentException($"Cannot extract property name from expression: {expression}");
    }
}
