using Vali_Validation.Core.Results;

namespace Vali_Validation.Core.Validators;

public abstract partial class AbstractValidator<T> where T : class
{
    private static readonly IReadOnlySet<string> DefaultRuleSet = new HashSet<string> { "default" };

    private readonly Dictionary<Func<T, ValidationResult>, Func<IReadOnlySet<string>>> _syncRuleSets = new();
    private readonly Dictionary<Func<T, CancellationToken, Task<ValidationResult>>, Func<IReadOnlySet<string>>> _asyncRuleSets = new();

    /// <summary>
    /// Runs only the synchronous rules whose rule set tags overlap what <paramref name="configureOptions"/>
    /// includes via <see cref="ValidationOptions.IncludeRuleSets"/>. Rules with no <c>InRuleSet</c> call
    /// carry the implicit tag <c>"default"</c> and are excluded unless <c>"default"</c> is itself included.
    /// Unlike <see cref="Validate(T)"/>, this overload always applies a filter — there is no way to pass
    /// an empty filter and get "everything" here; use <see cref="Validate(T)"/> for that.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="configureOptions"/> is null.</exception>
    public ValidationResult Validate(T instance, Action<ValidationOptions> configureOptions)
    {
        if (configureOptions == null) throw new ArgumentNullException(nameof(configureOptions));

        var options = new ValidationOptions();
        configureOptions(options);

        var previousLanguage = _explicitLanguage.Value;
        _explicitLanguage.Value = options.ExplicitLanguage;
        try
        {
            var result = new ValidationResult();
            if (!PreValidate(instance, result)) return result;

            foreach (var rule in SyncRules)
            {
                if (!MatchesFilter(_syncRuleSets, rule, options)) continue;
                MergeResultInto(result, rule(instance));
                if (GlobalCascadeMode == CascadeMode.StopOnFirstFailure && !result.IsValid) break;
            }
            return result;
        }
        finally
        {
            _explicitLanguage.Value = previousLanguage;
        }
    }

    /// <summary>
    /// Runs only the rules (sync and async) whose rule set tags overlap what
    /// <paramref name="configureOptions"/> includes. See <see cref="Validate(T, Action{ValidationOptions})"/>
    /// for the filtering semantics.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="configureOptions"/> is null.</exception>
    public async Task<ValidationResult> ValidateAsync(T instance, Action<ValidationOptions> configureOptions, CancellationToken cancellationToken = default)
    {
        if (configureOptions == null) throw new ArgumentNullException(nameof(configureOptions));

        var options = new ValidationOptions();
        configureOptions(options);

        var previousLanguage = _explicitLanguage.Value;
        _explicitLanguage.Value = options.ExplicitLanguage;
        try
        {
            var result = new ValidationResult();
            if (!PreValidate(instance, result)) return result;

            foreach (var rule in SyncRules)
            {
                if (!MatchesFilter(_syncRuleSets, rule, options)) continue;
                MergeResultInto(result, rule(instance));
                if (GlobalCascadeMode == CascadeMode.StopOnFirstFailure && !result.IsValid) return result;
            }
            foreach (var rule in AsyncRules)
            {
                if (!MatchesFilter(_asyncRuleSets, rule, options)) continue;
                MergeResultInto(result, await rule(instance, cancellationToken).ConfigureAwait(false));
                if (GlobalCascadeMode == CascadeMode.StopOnFirstFailure && !result.IsValid) return result;
            }
            return result;
        }
        finally
        {
            _explicitLanguage.Value = previousLanguage;
        }
    }

    private static bool MatchesFilter<TRule>(
        Dictionary<TRule, Func<IReadOnlySet<string>>> ruleSetsByRule,
        TRule rule,
        ValidationOptions options) where TRule : notnull
    {
        if (!options.HasRuleSetFilter) return true;
        var ruleSets = ruleSetsByRule.TryGetValue(rule, out var provider) ? provider() : DefaultRuleSet;
        return ruleSets.Overlaps(options.IncludedRuleSets);
    }

    private static void MergeResultInto(ValidationResult target, ValidationResult source) => target.Merge(source);
}
