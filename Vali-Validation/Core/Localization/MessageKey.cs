namespace Vali_Validation.Core.Localization;

/// <summary>
/// Stable identifier for a built-in rule's default message template, used as the lookup key
/// into <see cref="LanguageManager"/>'s per-language catalogs. One value per built-in rule.
/// </summary>
public enum MessageKey
{
    // Generic fallback (RuleBuilder.cs: AddCurrentCondition's null-message case, WithMessage(null))
    RuleBuilderDefault,

    // String rules (RuleBuilder.StringRules.cs)
    NotEmpty, MustContain, MinimumLength, MaximumLength, Matches, StartsWith, EndsWith,
    NotContains, NoWhitespace, Lowercase, Uppercase, MinWords, MaxWords,

    // Comparison rules (RuleBuilder.ComparisonRules.cs)
    Must, EqualTo, NotEqual, GreaterThan, LessThan, GreaterThanOrEqualTo, LessThanOrEqualTo,
    Between, ExclusiveBetween, LengthBetween, NotNull, Null, Empty,
    EqualToProperty, GreaterThanProperty, GreaterThanOrEqualToProperty,
    LessThanProperty, LessThanOrEqualToProperty, NotEqualToProperty,
    RequiredIf, RequiredUnless,

    // Numeric rules (RuleBuilder.NumericRules.cs)
    Positive, Negative, NotZero, NonNegative, Percentage, Precision, MultipleOf,
    MultipleOfProperty, Odd, Even, MaxDecimalPlaces,

    // Date rules (RuleBuilder.DateRules.cs)
    FutureDate, PastDate, Today, MinAge, MaxAge, DateBetween, NotExpired,
    WithinNext, WithinLast, IsWeekday, IsWeekend,

    // Collection rules (RuleBuilder.CollectionRules.cs)
    HasCount, NotEmptyCollection, MinCount, MaxCount, Unique, AllSatisfy, AnySatisfy, In, NotIn,

    // Format rules (RuleBuilder.FormatRules.cs)
    Email, Url, IsAlpha, IsAlphanumeric, IsNumeric, IsEnum, Guid, NotEmptyGuid, PhoneNumber,
    IPv4, IPv6, MacAddress, CreditCard, Latitude, Longitude, CountryCode, CurrencyCode,

    // Format rules part 2 / password (RuleBuilder.FormatRules.Password.cs)
    IsValidJson, IsValidBase64, Iban, HasUppercase, HasLowercase, HasDigit, HasSpecialChar,
    Slug, NoHtmlTags, NoSqlInjectionPatterns,

    // Async rules (RuleBuilder.AsyncRules.cs)
    MustAsync, DependentRuleAsync
}
