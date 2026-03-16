using System.Linq.Expressions;
using Vali_Validation.Core.Exceptions;
using Vali_Validation.Core.Results;
using Vali_Validation.Core.Rules;

namespace Vali_Validation.Core.Validators;

/// <summary>
/// Base class for all validators. Subclass this and call <see cref="RuleFor"/> in the constructor
/// to define validation rules.
/// </summary>
public abstract class AbstractValidator<T> : IValidator<T> where T : class
{
    private readonly List<Func<T, ValidationResult>> _syncRules = new();
    private readonly List<Func<T, Task<ValidationResult>>> _asyncRules = new();

    protected virtual CascadeMode GlobalCascadeMode => CascadeMode.Continue;

    /// <inheritdoc/>
    public IRuleBuilder<T, TProperty> RuleFor<TProperty>(Expression<Func<T, TProperty>> expression)
    {
        var propertyName = GetPropertyName(expression.Body);
        var propertyFunc = expression.Compile();
        return new RuleBuilder<T, TProperty>(this, propertyFunc, propertyName);
    }

    /// <summary>
    /// Begins a validation rule for each element of a collection property.
    /// Errors are reported with indexed keys, e.g. <c>Items[0]</c>.
    /// </summary>
    public IRuleBuilder<T, TElement> RuleForEach<TElement>(
        Expression<Func<T, IEnumerable<TElement>>> expression)
    {
        var collectionName = GetPropertyName(expression.Body);
        var collectionFunc = expression.Compile();
        return new RuleBuilder<T, TElement>(this, collectionFunc, collectionName);
    }

    internal void AddRule(Func<T, ValidationResult> rule) => _syncRules.Add(rule);
    internal void AddRule(Func<T, Task<ValidationResult>> rule) => _asyncRules.Add(rule);

    internal IReadOnlyList<Func<T, ValidationResult>> SyncRules => _syncRules;
    internal IReadOnlyList<Func<T, Task<ValidationResult>>> AsyncRules => _asyncRules;

    /// <summary>
    /// Begins a switch/case validation block keyed on <paramref name="keyExpression"/>.
    /// Different rules are applied to the object depending on the key value.
    /// </summary>
    protected ICaseBuilder<T, TKey> RuleSwitch<TKey>(Expression<Func<T, TKey>> keyExpression)
    {
        var keyFunc = keyExpression.Compile();
        return new SwitchCaseBuilder<T, TKey>(this, keyFunc);
    }

    protected void Include(AbstractValidator<T> other)
    {
        foreach (var rule in other.SyncRules) _syncRules.Add(rule);
        foreach (var rule in other.AsyncRules) _asyncRules.Add(rule);
    }

    /// <inheritdoc/>
    public ValidationResult Validate(T instance)
    {
        var result = new ValidationResult();
        foreach (var rule in _syncRules)
        {
            MergeErrors(result, rule(instance));
            if (GlobalCascadeMode == CascadeMode.StopOnFirstFailure && !result.IsValid) break;
        }
        return result;
    }

    /// <inheritdoc/>
    public async Task<ValidationResult> ValidateAsync(T instance, CancellationToken cancellationToken = default)
    {
        var result = new ValidationResult();
        foreach (var rule in _syncRules)
        {
            MergeErrors(result, rule(instance));
            if (GlobalCascadeMode == CascadeMode.StopOnFirstFailure && !result.IsValid) return result;
        }
        foreach (var rule in _asyncRules)
        {
            MergeErrors(result, await rule(instance).ConfigureAwait(false));
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
        foreach (var rule in _syncRules)
            MergeErrors(result, rule(instance));

        if (_asyncRules.Count > 0)
        {
            var tasks = _asyncRules.Select(rule => rule(instance));
            var results = await Task.WhenAll(tasks).ConfigureAwait(false);
            foreach (var partial in results)
                MergeErrors(result, partial);
        }

        return result;
    }

    private static void MergeErrors(ValidationResult target, ValidationResult source)
    {
        foreach (var error in source.Errors)
            foreach (var message in error.Value)
                target.AddError(error.Key, message);

        foreach (var code in source.ErrorCodes)
            foreach (var c in code.Value)
            {
                if (!target.ErrorCodes.ContainsKey(code.Key))
                    target.ErrorCodes[code.Key] = new List<string>();
                target.ErrorCodes[code.Key].Add(c);
            }
    }

    internal static string GetPropertyName(Expression expression)
    {
        if (expression is MemberExpression member) return member.Member.Name;
        if (expression is UnaryExpression unary) return GetPropertyName(unary.Operand);
        throw new ArgumentException($"Cannot extract property name from expression: {expression}");
    }
}
