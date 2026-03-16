using System.Linq.Expressions;

namespace Vali_Validation.Core.Rules;

/// <summary>
/// Provides a fluent interface for defining validation rules on a specific property of type <typeparamref name="T"/>.
/// </summary>
public interface IRuleBuilder<T, TProperty> where T : class
{
    /// <summary>Sets a custom error message for the last defined rule.</summary>
    IRuleBuilder<T, TProperty> WithMessage(string? message);

    /// <summary>Overrides the property name used in error keys.</summary>
    IRuleBuilder<T, TProperty> OverridePropertyName(string name);

    /// <summary>Stops evaluating further rules for this property after the first failure.</summary>
    IRuleBuilder<T, TProperty> StopOnFirstFailure();

    /// <summary>Only executes the preceding rules when <paramref name="condition"/> is true.</summary>
    IRuleBuilder<T, TProperty> When(Func<T, bool> condition);

    /// <summary>Only executes the preceding rules when <paramref name="condition"/> is false.</summary>
    IRuleBuilder<T, TProperty> Unless(Func<T, bool> condition);

    /// <summary>Sets an error code for the last defined rule.</summary>
    IRuleBuilder<T, TProperty> WithErrorCode(string code);

    // -------------------------------------------------------------------------
    // Built-in rules
    // -------------------------------------------------------------------------

    IRuleBuilder<T, TProperty> NotEmpty();
    IRuleBuilder<T, TProperty> Must(Func<TProperty, bool>? predicate);
    IRuleBuilder<T, TProperty> MustContain(string substring,
        StringComparison comparison = StringComparison.OrdinalIgnoreCase);
    IRuleBuilder<T, TProperty> MinimumLength(int length);
    IRuleBuilder<T, TProperty> MaximumLength(int length);
    IRuleBuilder<T, TProperty> Matches(string pattern);
    IRuleBuilder<T, TProperty> EqualTo(TProperty other);
    IRuleBuilder<T, TProperty> GreaterThan(IComparable threshold);
    IRuleBuilder<T, TProperty> LessThan(IComparable threshold);
    IRuleBuilder<T, TProperty> NotNull();
    IRuleBuilder<T, TProperty> Null();
    IRuleBuilder<T, TProperty> Empty();
    IRuleBuilder<T, TProperty> Email();
    IRuleBuilder<T, TProperty> Url();
    IRuleBuilder<T, TProperty> IsAlpha();
    IRuleBuilder<T, TProperty> IsAlphanumeric();
    IRuleBuilder<T, TProperty> IsNumeric();
    IRuleBuilder<T, TProperty> Between<TComparable>(TComparable min, TComparable max)
        where TComparable : IComparable;
    IRuleBuilder<T, TProperty> StartsWith(string prefix);
    IRuleBuilder<T, TProperty> EndsWith(string suffix);
    IRuleBuilder<T, TProperty> In(IEnumerable<TProperty> values);
    IRuleBuilder<T, TProperty> Positive();
    IRuleBuilder<T, TProperty> Negative();
    IRuleBuilder<T, TProperty> NotZero();
    IRuleBuilder<T, TProperty> FutureDate();
    IRuleBuilder<T, TProperty> PastDate();
    IRuleBuilder<T, TProperty> Today();
    IRuleBuilder<T, TProperty> HasCount(int count);
    IRuleBuilder<T, TProperty> NotEmptyCollection();
    IRuleBuilder<T, TProperty> NotEqual(TProperty other);
    IRuleBuilder<T, TProperty> LengthBetween(int min, int max);
    IRuleBuilder<T, TProperty> ExclusiveBetween<TComparable>(TComparable min, TComparable max)
        where TComparable : IComparable;
    IRuleBuilder<T, TProperty> IsEnum<TEnum>() where TEnum : struct, Enum;
    IRuleBuilder<T, TProperty> Guid();
    IRuleBuilder<T, TProperty> GreaterThanOrEqualTo(IComparable threshold);
    IRuleBuilder<T, TProperty> LessThanOrEqualTo(IComparable threshold);
    IRuleBuilder<T, TProperty> EqualToProperty(Expression<Func<T, TProperty>> otherExpression);
    IRuleBuilder<T, TProperty> NotContains(string substring, StringComparison comparison = StringComparison.OrdinalIgnoreCase);
    IRuleBuilder<T, TProperty> NoWhitespace();
    IRuleBuilder<T, TProperty> PhoneNumber();
    IRuleBuilder<T, TProperty> IPv4();
    IRuleBuilder<T, TProperty> CreditCard();
    IRuleBuilder<T, TProperty> MaxDecimalPlaces(int decimalPlaces);
    IRuleBuilder<T, TProperty> MultipleOf(decimal factor);
    IRuleBuilder<T, TProperty> Odd();
    IRuleBuilder<T, TProperty> Even();
    IRuleBuilder<T, TProperty> NotEmptyGuid();

    // -------------------------------------------------------------------------
    // Password strength rules
    // -------------------------------------------------------------------------

    IRuleBuilder<T, TProperty> HasUppercase();
    IRuleBuilder<T, TProperty> HasLowercase();
    IRuleBuilder<T, TProperty> HasDigit();
    IRuleBuilder<T, TProperty> HasSpecialChar();

    // -------------------------------------------------------------------------
    // Collection and string rules
    // -------------------------------------------------------------------------

    IRuleBuilder<T, TProperty> NotIn(IEnumerable<TProperty> values);
    IRuleBuilder<T, TProperty> MinCount(int min);
    IRuleBuilder<T, TProperty> MaxCount(int max);
    IRuleBuilder<T, TProperty> Unique();
    IRuleBuilder<T, TProperty> AllSatisfy(Func<object, bool> predicate);
    IRuleBuilder<T, TProperty> AnySatisfy(Func<object, bool> predicate);
    IRuleBuilder<T, TProperty> Lowercase();
    IRuleBuilder<T, TProperty> Uppercase();
    IRuleBuilder<T, TProperty> MinWords(int min);
    IRuleBuilder<T, TProperty> MaxWords(int max);

    // -------------------------------------------------------------------------
    // Custom rule
    // -------------------------------------------------------------------------

    IRuleBuilder<T, TProperty> Custom(Action<TProperty, CustomValidationContext<T>> action);

    // -------------------------------------------------------------------------
    // Async rules
    // -------------------------------------------------------------------------

    IRuleBuilder<T, TProperty> MustAsync(Func<TProperty, Task<bool>> predicateAsync);
    IRuleBuilder<T, TProperty> MustAsync(Func<TProperty, CancellationToken, Task<bool>> predicateAsync);

    IRuleBuilder<T, TProperty> DependentRuleAsync<TDependent>(
        Expression<Func<T, TProperty>> propertyExpression,
        Expression<Func<T, TDependent>> dependentPropertyExpression,
        Func<TProperty, TDependent, Task<bool>> predicateAsync);

    IRuleBuilder<T, TProperty> WhenAsync(Func<T, CancellationToken, Task<bool>> condition);
    IRuleBuilder<T, TProperty> UnlessAsync(Func<T, CancellationToken, Task<bool>> condition);

    // -------------------------------------------------------------------------
    // Cross-property comparison rules
    // -------------------------------------------------------------------------

    /// <summary>Validates that this property is greater than another property on the same instance.</summary>
    IRuleBuilder<T, TProperty> GreaterThanProperty(Expression<Func<T, TProperty>> otherExpression);

    /// <summary>Validates that this property is greater than or equal to another property on the same instance.</summary>
    IRuleBuilder<T, TProperty> GreaterThanOrEqualToProperty(Expression<Func<T, TProperty>> otherExpression);

    /// <summary>Validates that this property is less than another property on the same instance.</summary>
    IRuleBuilder<T, TProperty> LessThanProperty(Expression<Func<T, TProperty>> otherExpression);

    /// <summary>Validates that this property is less than or equal to another property on the same instance.</summary>
    IRuleBuilder<T, TProperty> LessThanOrEqualToProperty(Expression<Func<T, TProperty>> otherExpression);

    /// <summary>Validates that this property is not equal to another property on the same instance.</summary>
    IRuleBuilder<T, TProperty> NotEqualToProperty(Expression<Func<T, TProperty>> otherExpression);

    // -------------------------------------------------------------------------
    // RequiredIf / RequiredUnless
    // -------------------------------------------------------------------------

    /// <summary>The field is required (non-null and non-empty) when the given condition is true.</summary>
    IRuleBuilder<T, TProperty> RequiredIf(Func<T, bool> condition);

    /// <summary>The field is required when another property equals the expected value.</summary>
    IRuleBuilder<T, TProperty> RequiredIf<TOther>(Expression<Func<T, TOther>> otherProperty, TOther expectedValue);

    /// <summary>The field is required unless the given condition is true.</summary>
    IRuleBuilder<T, TProperty> RequiredUnless(Func<T, bool> condition);

    // -------------------------------------------------------------------------
    // Date rules
    // -------------------------------------------------------------------------

    /// <summary>Validates that the DateTime value corresponds to a minimum age in years.</summary>
    IRuleBuilder<T, TProperty> MinAge(int years);

    /// <summary>Validates that the DateTime value corresponds to a maximum age in years.</summary>
    IRuleBuilder<T, TProperty> MaxAge(int years);

    /// <summary>Validates that the DateTime value falls between <paramref name="from"/> and <paramref name="to"/> (inclusive).</summary>
    IRuleBuilder<T, TProperty> DateBetween(DateTime from, DateTime to);

    /// <summary>Validates that the DateTime value is not in the past (i.e. not expired).</summary>
    IRuleBuilder<T, TProperty> NotExpired();

    /// <summary>Validates that the DateTime value is within the next <paramref name="span"/> from now.</summary>
    IRuleBuilder<T, TProperty> WithinNext(TimeSpan span);

    /// <summary>Validates that the DateTime value is within the last <paramref name="span"/> from now.</summary>
    IRuleBuilder<T, TProperty> WithinLast(TimeSpan span);

    /// <summary>Validates that the DateTime value falls on a weekday (Monday–Friday).</summary>
    IRuleBuilder<T, TProperty> IsWeekday();

    /// <summary>Validates that the DateTime value falls on a weekend (Saturday or Sunday).</summary>
    IRuleBuilder<T, TProperty> IsWeekend();

    // -------------------------------------------------------------------------
    // Numeric rules
    // -------------------------------------------------------------------------

    /// <summary>Validates that the value is zero or greater.</summary>
    IRuleBuilder<T, TProperty> NonNegative();

    /// <summary>Validates that the value is between 0 and 100 inclusive.</summary>
    IRuleBuilder<T, TProperty> Percentage();

    /// <summary>Validates that the decimal representation has at most <paramref name="totalDigits"/> total digits and <paramref name="decimalPlaces"/> fractional digits.</summary>
    IRuleBuilder<T, TProperty> Precision(int totalDigits, int decimalPlaces);

    /// <summary>Validates that this property is a multiple of another property on the same instance.</summary>
    IRuleBuilder<T, TProperty> MultipleOfProperty(Expression<Func<T, TProperty>> otherExpression);

    // -------------------------------------------------------------------------
    // String / format rules
    // -------------------------------------------------------------------------

    /// <summary>Validates that the string is a valid URL slug (lowercase letters, numbers, and hyphens).</summary>
    IRuleBuilder<T, TProperty> Slug();

    /// <summary>Validates that the string is a valid IPv6 address.</summary>
    IRuleBuilder<T, TProperty> IPv6();

    /// <summary>Validates that the string is a valid MAC address.</summary>
    IRuleBuilder<T, TProperty> MacAddress();

    /// <summary>Validates that the numeric value is a valid latitude (-90 to 90).</summary>
    IRuleBuilder<T, TProperty> Latitude();

    /// <summary>Validates that the numeric value is a valid longitude (-180 to 180).</summary>
    IRuleBuilder<T, TProperty> Longitude();

    /// <summary>Validates that the string is a valid ISO 3166-1 alpha-2 country code.</summary>
    IRuleBuilder<T, TProperty> CountryCode();

    /// <summary>Validates that the string is a valid ISO 4217 currency code.</summary>
    IRuleBuilder<T, TProperty> CurrencyCode();

    /// <summary>Validates that the string is valid JSON.</summary>
    IRuleBuilder<T, TProperty> IsValidJson();

    /// <summary>Validates that the string is a valid Base64-encoded string.</summary>
    IRuleBuilder<T, TProperty> IsValidBase64();

    /// <summary>Validates that the string contains no HTML tags.</summary>
    IRuleBuilder<T, TProperty> NoHtmlTags();

    /// <summary>Validates that the string contains no common SQL injection patterns.</summary>
    IRuleBuilder<T, TProperty> NoSqlInjectionPatterns();

    /// <summary>Validates that the string is a valid IBAN using the ISO 13616 mod-97 algorithm.</summary>
    IRuleBuilder<T, TProperty> Iban();

    /// <summary>
    /// Applies a password-strength policy by combining existing rules.
    /// Defaults: minLength=8, requireUppercase, requireLowercase, requireDigit, requireSpecialChar.
    /// </summary>
    IRuleBuilder<T, TProperty> PasswordPolicy(int minLength = 8, bool requireUppercase = true, bool requireLowercase = true, bool requireDigit = true, bool requireSpecialChar = true);

    // -------------------------------------------------------------------------
    // Property-level switch/case
    // -------------------------------------------------------------------------

    /// <summary>
    /// Applies different validation rules to this property depending on the value of another property.
    /// </summary>
    ISwitchOnBuilder<T, TProperty, TKey> SwitchOn<TKey>(Expression<Func<T, TKey>> keyExpression);
}
