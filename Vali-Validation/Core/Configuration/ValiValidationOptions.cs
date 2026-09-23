using Vali_Validation.Core.Validators;

namespace Vali_Validation.Core.Configuration;

/// <summary>
/// App-wide configuration for Vali-Validation, mirroring FluentValidation's
/// <c>ValidatorOptions.Global</c>. Configure once at startup (e.g. in <c>Program.cs</c>), before
/// any concurrent validation runs — these are plain mutable statics with no internal locking,
/// the same convention .NET uses for <c>JsonSerializerOptions.Default</c>.
/// </summary>
public static class ValiValidationOptions
{
    public static class Global
    {
        /// <summary>
        /// Default cascade mode for validators whose class does not override
        /// <see cref="AbstractValidator{T}.GlobalCascadeMode"/>. Default: <see cref="CascadeMode.Continue"/>
        /// (preserves today's behavior with no configuration).
        /// </summary>
        public static CascadeMode DefaultCascadeMode { get; set; } = CascadeMode.Continue;

        /// <summary>
        /// Fallback language code used to resolve built-in rule messages when
        /// <see cref="System.Globalization.CultureInfo.CurrentUICulture"/> has no matching catalog
        /// entry and no explicit <c>ValidationOptions.WithLanguage(...)</c> override is set for the
        /// call. Default: <c>"en"</c>.
        /// </summary>
        public static string DefaultLanguage { get; set; } = "en";

        /// <summary>
        /// Transforms the property name substituted into the <c>{PropertyName}</c> placeholder in
        /// message text (for the rule's own property, as well as any other property referenced by
        /// name in a cross-property rule, e.g. <c>otherName</c> in <c>EqualToProperty</c> or
        /// <c>dependentPropertyName</c> in <c>DependentRuleAsync</c>). Does NOT affect the key used
        /// in <c>ValidationResult.Errors</c>/failure entries — see <see cref="PropertyNameResolver"/>
        /// for that. When both resolvers are configured, the name passed into
        /// <see cref="DisplayNameResolver"/> is already the <see cref="PropertyNameResolver"/>-transformed
        /// name, never the raw untransformed one. Default: identity (no transform, preserves today's
        /// behavior).
        /// </summary>
        public static Func<string, string> DisplayNameResolver { get; set; } = name => name;

        /// <summary>
        /// Transforms the property name used as the key in <c>ValidationResult.Errors</c>/failure
        /// entries, applied once when a property expression is first resolved (e.g. inside
        /// <c>RuleFor(x => x.Foo)</c>). Default: identity (no transform, preserves today's behavior
        /// exactly — existing consumers reading <c>Errors["Foo"]</c> are unaffected unless this is
        /// explicitly configured).
        /// </summary>
        public static Func<string, string> PropertyNameResolver { get; set; } = name => name;
    }
}
