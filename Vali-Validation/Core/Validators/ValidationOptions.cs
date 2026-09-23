namespace Vali_Validation.Core.Validators;

/// <summary>
/// Configures which rule sets a single <see cref="AbstractValidator{T}.Validate(T, Action{ValidationOptions})"/>
/// call should run. With no rule sets included, every rule runs regardless of tag — the same as
/// calling <c>Validate(instance)</c> without options.
/// </summary>
public sealed class ValidationOptions
{
    private readonly HashSet<string> _includedRuleSets = new();

    internal bool HasRuleSetFilter => _includedRuleSets.Count > 0;
    internal IReadOnlySet<string> IncludedRuleSets => _includedRuleSets;

    /// <summary>
    /// Restricts this validation run to rules tagged with any of <paramref name="ruleSetNames"/>
    /// (via <see cref="Rules.IRuleBuilder{T,TProperty}.InRuleSet"/>). Rules with no explicit
    /// <c>InRuleSet</c> call carry the implicit tag <c>"default"</c> and are excluded unless
    /// <c>"default"</c> is itself passed here.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="ruleSetNames"/> is empty.</exception>
    public ValidationOptions IncludeRuleSets(params string[] ruleSetNames)
    {
        if (ruleSetNames == null || ruleSetNames.Length == 0)
            throw new ArgumentException("At least one rule set name must be provided.", nameof(ruleSetNames));

        foreach (var name in ruleSetNames)
            _includedRuleSets.Add(name);

        return this;
    }

    private string? _explicitLanguage;

    internal string? ExplicitLanguage => _explicitLanguage;

    /// <summary>
    /// Overrides the language used to resolve built-in rule messages for this validation call,
    /// taking precedence over <see cref="System.Globalization.CultureInfo.CurrentUICulture"/> and
    /// any app-wide default. Pass a code registered via <see cref="Vali_Validation.Core.Localization.LanguageManager.RegisterLanguage"/>
    /// (e.g. <c>"en"</c>, <c>"es"</c>) — an unregistered code falls back to English per-message, same
    /// as an unregistered <see cref="System.Globalization.CultureInfo.CurrentUICulture"/> would.
    /// </summary>
    public ValidationOptions WithLanguage(string languageCode)
    {
        _explicitLanguage = languageCode;
        return this;
    }
}
