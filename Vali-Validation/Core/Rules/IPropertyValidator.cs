namespace Vali_Validation.Core.Rules;

/// <summary>
/// Contract for a reusable, testable custom validation rule — the first-class alternative to
/// writing an <see cref="IRuleBuilder{T,TProperty}"/> extension method (see e.g.
/// <c>PasswordPolicy()</c> in <c>RuleBuilder.FormatRules.Password.cs</c> for that pattern), useful
/// when the rule needs to be unit-tested in isolation or shared without exposing the package's
/// internal <c>_rules</c> tuple machinery. Attach via <see cref="IRuleBuilder{T,TProperty}.SetPropertyValidator"/>.
/// </summary>
/// <typeparam name="TProperty">The property type this validator checks.</typeparam>
public interface IPropertyValidator<TProperty>
{
    /// <summary>Returns <c>true</c> if <paramref name="value"/> is valid.</summary>
    bool IsValid(TProperty value);

    /// <summary>
    /// Language code ("en", "es", ...) → default message text used when this validator fails and
    /// no <c>.WithMessage()</c> override is set on the rule. Must contain at least one entry —
    /// resolved against <see cref="System.Globalization.CultureInfo.CurrentUICulture"/> (or an
    /// explicit <c>ValidationOptions.WithLanguage(...)</c> override), falling back to this
    /// dictionary's FIRST entry (not the package's built-in English catalog) if neither language is
    /// present here. Only <c>{PropertyName}</c>/<c>{PropertyValue}</c> are substituted into this
    /// text (every <see cref="Vali_Validation.Core.Localization.MessageSpec"/> resolves those
    /// regardless of source) — there is no <c>Args</c> mechanism for custom <c>{token}</c>
    /// placeholders in an <see cref="IPropertyValidator{TProperty}"/>'s own message text.
    /// </summary>
    IReadOnlyDictionary<string, string> Messages { get; }
}
