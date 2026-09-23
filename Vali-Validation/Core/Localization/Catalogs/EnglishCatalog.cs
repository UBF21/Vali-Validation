using Vali_Validation.Core.Localization;

namespace Vali_Validation.Core.Localization.Catalogs;

internal static class EnglishCatalog
{
    public static readonly IReadOnlyDictionary<MessageKey, string> Messages = new Dictionary<MessageKey, string>
    {
        [MessageKey.RuleBuilderDefault] = "The {PropertyName} field is invalid.",
        [MessageKey.NotEmpty] = "The {PropertyName} field cannot be empty.",
        [MessageKey.MinimumLength] = "The {PropertyName} field must be at least {length} characters long.",

        // String rules (RuleBuilder.StringRules.cs)
        [MessageKey.MustContain] = "The {PropertyName} field must contain '{substring}'.",
        [MessageKey.MaximumLength] = "The {PropertyName} field must be no longer than {length} characters.",
        [MessageKey.Matches] = "The {PropertyName} field is not in the correct format.",
        [MessageKey.StartsWith] = "The {PropertyName} field must begin with '{prefix}'.",
        [MessageKey.EndsWith] = "The {PropertyName} field must end with '{suffix}'.",
        [MessageKey.NotContains] = "The {PropertyName} field must not contain '{substring}'.",
        [MessageKey.NoWhitespace] = "The {PropertyName} field must not contain whitespace.",
        [MessageKey.Lowercase] = "The {PropertyName} field must be all lowercase.",
        [MessageKey.Uppercase] = "The {PropertyName} field must be all uppercase.",
        [MessageKey.MinWords] = "The {PropertyName} field must contain at least {min} words.",
        [MessageKey.MaxWords] = "The {PropertyName} field must contain at most {max} words.",

        // Comparison rules (RuleBuilder.ComparisonRules.cs)
        [MessageKey.Must] = "The {PropertyName} field does not meet the specified condition.",
        [MessageKey.EqualTo] = "The {PropertyName} field must be equal to '{other}'.",
        [MessageKey.NotEqual] = "The {PropertyName} field must not be equal to '{other}'.",
        [MessageKey.GreaterThan] = "The {PropertyName} field must be greater than {threshold}.",
        [MessageKey.LessThan] = "The {PropertyName} field must be less than {threshold}.",
        [MessageKey.GreaterThanOrEqualTo] = "The {PropertyName} field must be greater than or equal to {threshold}.",
        [MessageKey.LessThanOrEqualTo] = "The {PropertyName} field must be less than or equal to {threshold}.",
        [MessageKey.Between] = "The {PropertyName} field must be between {min} and {max}.",
        [MessageKey.ExclusiveBetween] = "The {PropertyName} field must be exclusively between {min} and {max}.",
        [MessageKey.LengthBetween] = "The {PropertyName} field must be between {min} and {max} characters long.",
        [MessageKey.NotNull] = "The {PropertyName} field cannot be null.",
        [MessageKey.Null] = "The {PropertyName} field must be null.",
        [MessageKey.Empty] = "The {PropertyName} field must be empty.",
        [MessageKey.EqualToProperty] = "The {PropertyName} field must equal {otherName}.",
        [MessageKey.GreaterThanProperty] = "The {PropertyName} field must be greater than {otherName}.",
        [MessageKey.GreaterThanOrEqualToProperty] = "The {PropertyName} field must be greater than or equal to {otherName}.",
        [MessageKey.LessThanProperty] = "The {PropertyName} field must be less than {otherName}.",
        [MessageKey.LessThanOrEqualToProperty] = "The {PropertyName} field must be less than or equal to {otherName}.",
        [MessageKey.NotEqualToProperty] = "The {PropertyName} field must not be equal to {otherName}.",
        [MessageKey.RequiredIf] = "The {PropertyName} field is required.",
        [MessageKey.RequiredUnless] = "The {PropertyName} field is required.",

        // Numeric rules (RuleBuilder.NumericRules.cs)
        [MessageKey.Positive] = "The {PropertyName} field must be a positive number.",
        [MessageKey.Negative] = "The {PropertyName} field must be a negative number.",
        [MessageKey.NotZero] = "The {PropertyName} field must not be zero.",
        [MessageKey.NonNegative] = "The {PropertyName} field must be non-negative (zero or greater).",
        [MessageKey.Percentage] = "The {PropertyName} field must be a valid percentage between 0 and 100.",
        [MessageKey.Precision] = "The {PropertyName} field must have at most {totalDigits} total digits and {decimalPlaces} decimal places.",
        [MessageKey.MultipleOf] = "The {PropertyName} field must be a multiple of {factor}.",
        [MessageKey.MultipleOfProperty] = "The {PropertyName} field must be a multiple of {otherName}.",
        [MessageKey.Odd] = "The {PropertyName} field must be an odd number.",
        [MessageKey.Even] = "The {PropertyName} field must be an even number.",
        [MessageKey.MaxDecimalPlaces] = "The {PropertyName} field must have at most {decimalPlaces} decimal places.",

        // Date rules (RuleBuilder.DateRules.cs)
        [MessageKey.FutureDate] = "The {PropertyName} field must be a future date.",
        [MessageKey.PastDate] = "The {PropertyName} field must be a past date.",
        [MessageKey.Today] = "The {PropertyName} field must be today's date.",
        [MessageKey.MinAge] = "The {PropertyName} field requires a minimum age of {years} years.",
        [MessageKey.MaxAge] = "The {PropertyName} field must correspond to a maximum age of {years} years.",
        [MessageKey.DateBetween] = "The {PropertyName} field must be between {from} and {to}.",
        [MessageKey.NotExpired] = "The {PropertyName} field must not be expired.",
        [MessageKey.WithinNext] = "The {PropertyName} field must be within the next {days} days.",
        [MessageKey.WithinLast] = "The {PropertyName} field must be within the last {days} days.",
        [MessageKey.IsWeekday] = "The {PropertyName} field must be a weekday.",
        [MessageKey.IsWeekend] = "The {PropertyName} field must be a weekend day.",

        // Collection rules (RuleBuilder.CollectionRules.cs)
        [MessageKey.HasCount] = "The {PropertyName} field must contain exactly {count} items.",
        [MessageKey.NotEmptyCollection] = "The {PropertyName} field must not be an empty collection.",
        [MessageKey.MinCount] = "The {PropertyName} field must contain at least {min} items.",
        [MessageKey.MaxCount] = "The {PropertyName} field must contain at most {max} items.",
        [MessageKey.Unique] = "The {PropertyName} field must not contain duplicate values.",
        [MessageKey.AllSatisfy] = "The {PropertyName} field: all items must satisfy the condition.",
        [MessageKey.AnySatisfy] = "The {PropertyName} field: at least one item must satisfy the condition.",
        [MessageKey.In] = "The {PropertyName} field must be in the list of allowed values.",
        [MessageKey.NotIn] = "The {PropertyName} field must not be in the list of disallowed values.",

        // Format rules (RuleBuilder.FormatRules.cs)
        [MessageKey.Email] = "The {PropertyName} field must be a valid email address.",
        [MessageKey.Url] = "The {PropertyName} field must be a valid URL.",
        [MessageKey.IsAlpha] = "The {PropertyName} field must only contain alphabetic characters.",
        [MessageKey.IsAlphanumeric] = "The {PropertyName} field must only contain alphanumeric characters.",
        [MessageKey.IsNumeric] = "The {PropertyName} field must only contain numbers.",
        [MessageKey.IsEnum] = "The {PropertyName} field must be a valid {enumType} value.",
        [MessageKey.Guid] = "The {PropertyName} field must be a valid GUID.",
        [MessageKey.NotEmptyGuid] = "The {PropertyName} field must not be an empty GUID.",
        [MessageKey.PhoneNumber] = "The {PropertyName} field must be a valid phone number.",
        [MessageKey.IPv4] = "The {PropertyName} field must be a valid IPv4 address.",
        [MessageKey.IPv6] = "The {PropertyName} field must be a valid IPv6 address.",
        [MessageKey.MacAddress] = "The {PropertyName} field must be a valid MAC address.",
        [MessageKey.CreditCard] = "The {PropertyName} field must be a valid credit card number.",
        [MessageKey.Latitude] = "The {PropertyName} field must be a valid latitude (-90 to 90).",
        [MessageKey.Longitude] = "The {PropertyName} field must be a valid longitude (-180 to 180).",
        [MessageKey.CountryCode] = "The {PropertyName} field must be a valid ISO 3166-1 alpha-2 country code (e.g. US, PE, ES).",
        [MessageKey.CurrencyCode] = "The {PropertyName} field must be a valid ISO 4217 currency code (e.g. USD, EUR, PEN).",

        // Format rules part 2 / password (RuleBuilder.FormatRules.Password.cs)
        [MessageKey.IsValidJson] = "The {PropertyName} field must be a valid JSON string.",
        [MessageKey.IsValidBase64] = "The {PropertyName} field must be a valid Base64 encoded string.",
        [MessageKey.Iban] = "The {PropertyName} field must be a valid IBAN.",
        [MessageKey.HasUppercase] = "The {PropertyName} field must contain at least one uppercase letter.",
        [MessageKey.HasLowercase] = "The {PropertyName} field must contain at least one lowercase letter.",
        [MessageKey.HasDigit] = "The {PropertyName} field must contain at least one digit.",
        [MessageKey.HasSpecialChar] = "The {PropertyName} field must contain at least one special character.",
        [MessageKey.Slug] = "The {PropertyName} field must be a valid URL slug (lowercase letters, numbers, and hyphens only).",
        [MessageKey.NoHtmlTags] = "The {PropertyName} field must not contain HTML tags.",
        [MessageKey.NoSqlInjectionPatterns] = "The {PropertyName} field contains potentially unsafe content.",

        // Async rules (RuleBuilder.AsyncRules.cs)
        [MessageKey.MustAsync] = "The {PropertyName} field does not meet the specified condition.",
        [MessageKey.DependentRuleAsync] = "The field {PropertyName} does not meet the dependent condition of {dependentPropertyName}.",
    };
}
