# Basic Rules

This document covers all synchronous rules available in `IRuleBuilder<T, TProperty>`. Async rules (`MustAsync`, `DependentRuleAsync`, `Custom`, `Transform`, `SetValidator`) are described in [Advanced Rules](06-advanced-rules.md).

---

## Nullability and Emptiness

### `NotNull()`

Verifies that the value is not `null`.

```csharp
public class ProductValidator : AbstractValidator<Product>
{
    public ProductValidator()
    {
        RuleFor(x => x.Category).NotNull();
        RuleFor(x => x.Tags).NotNull();
    }
}
```

Difference from `NotEmpty()`: `NotNull()` only rejects `null`. An empty string `""` would pass `NotNull()` but fail `NotEmpty()`.

### `Null()`

Verifies that the value is `null`. Useful for validating that certain fields must NOT be sent in specific requests.

```csharp
public class UpdatePasswordRequestValidator : AbstractValidator<UpdatePasswordRequest>
{
    public UpdatePasswordRequestValidator()
    {
        // The Id field should not be sent in the body (it is taken from the JWT)
        RuleFor(x => x.UserId).Null()
            .WithMessage("Do not send the user ID in the body; it is taken from the token.");
    }
}
```

### `NotEmpty()`

Verifies that the value is not `null`, not an empty string (`""`), and not only whitespace. For collections, also verifies they are not empty.

```csharp
public class ArticleValidator : AbstractValidator<Article>
{
    public ArticleValidator()
    {
        RuleFor(x => x.Title).NotEmpty();
        RuleFor(x => x.Content).NotEmpty();
        RuleFor(x => x.Author).NotEmpty();

        // For strings with spaces: "   " fails NotEmpty()
        RuleFor(x => x.Slug)
            .NotEmpty()
            .NoWhitespace();
    }
}
```

### `Empty()`

Verifies that the value is `null` or an empty string. Rarely used, but useful for validating fields that must be blank in certain contexts.

```csharp
RuleFor(x => x.InternalNotes)
    .Empty()
        .WithMessage("Internal notes must not be sent from the public API.")
    .When(x => x.Source == RequestSource.PublicApi);
```

---

## Equality

### `EqualTo(TProperty value)`

Verifies that the value equals the given value.

```csharp
public class AcceptTermsValidator : AbstractValidator<RegistrationRequest>
{
    public AcceptTermsValidator()
    {
        RuleFor(x => x.AcceptTerms)
            .EqualTo(true)
                .WithMessage("You must accept the terms and conditions.");
    }
}
```

### `NotEqual(TProperty value)`

Verifies that the value is different from the given value.

```csharp
RuleFor(x => x.NewPassword)
    .NotEqual("password")
        .WithMessage("Do not use 'password' as your password.")
    .NotEqual("12345678")
        .WithMessage("Choose a more secure password.");
```

### `EqualToProperty(Expression<Func<T, TProperty>> otherProp)`

Verifies that the value equals the value of another property of the same object. Ideal for password or email confirmation.

```csharp
public class ChangePasswordValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(8);

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty()
            .EqualToProperty(x => x.NewPassword)
                .WithMessage("The passwords do not match.");

        // The new password must not equal the current one
        RuleFor(x => x.NewPassword)
            .NotEqual("") // Already covered by NotEmpty, but illustrates usage
            .Must((request, newPwd) => newPwd != request.CurrentPassword)
                .WithMessage("The new password must be different from the current one.");
    }
}
```

---

## String Length

### `MinimumLength(int n)`

Verifies that the string length is at least `n` characters.

```csharp
RuleFor(x => x.Username).MinimumLength(3);
RuleFor(x => x.Password).MinimumLength(8);
```

### `MaximumLength(int n)`

Verifies that the string length does not exceed `n` characters.

```csharp
RuleFor(x => x.Name).MaximumLength(200);
RuleFor(x => x.Bio).MaximumLength(500);
RuleFor(x => x.Email).MaximumLength(320); // RFC 5321 limit
```

### `LengthBetween(int min, int max)`

Verifies that the length is between `min` and `max` (both inclusive).

```csharp
RuleFor(x => x.PhoneNumber)
    .LengthBetween(7, 15)
        .WithMessage("The phone number must have between 7 and 15 digits.");

RuleFor(x => x.PostalCode)
    .LengthBetween(5, 10)
        .WithMessage("The postal code must have between 5 and 10 characters.");
```

---

## Numeric Range

These rules work with `IComparable`. They work with `int`, `decimal`, `double`, `DateTime`, `long`, etc.

### `GreaterThan(IComparable threshold)`

```csharp
RuleFor(x => x.Age).GreaterThan(0);
RuleFor(x => x.Price).GreaterThan(0m);
RuleFor(x => x.EventDate).GreaterThan(DateTime.Today);
```

### `GreaterThanOrEqualTo(IComparable threshold)`

```csharp
RuleFor(x => x.Stock).GreaterThanOrEqualTo(0);
RuleFor(x => x.Quantity).GreaterThanOrEqualTo(1);
```

### `LessThan(IComparable threshold)`

```csharp
RuleFor(x => x.Age).LessThan(150);
RuleFor(x => x.Discount).LessThan(100m);
```

### `LessThanOrEqualTo(IComparable threshold)`

```csharp
RuleFor(x => x.MaxRetries).LessThanOrEqualTo(10);
RuleFor(x => x.Percentage).LessThanOrEqualTo(100m);
```

### `Between<TComparable>(TComparable min, TComparable max)`

Includes the extremes (min and max are valid values).

```csharp
RuleFor(x => x.Month).Between(1, 12)
    .WithMessage("The month must be between 1 and 12.");

RuleFor(x => x.Rating).Between(1, 5)
    .WithMessage("The rating must be between 1 and 5.");
```

### `ExclusiveBetween<TComparable>(TComparable min, TComparable max)`

Excludes the extremes (min and max are not valid).

```csharp
RuleFor(x => x.Probability)
    .ExclusiveBetween(0m, 1m)
        .WithMessage("The probability must be a value between 0 and 1 (extremes excluded).");
```

### `Positive()`

Verifies that the number is strictly greater than 0. Zero is not positive.

```csharp
RuleFor(x => x.Price).Positive();
RuleFor(x => x.Quantity).Positive();
```

### `NonNegative()`

Verifies that the number is greater than or equal to 0 (zero is allowed). Use this instead of `Positive()` when a zero value is a meaningful, valid state — for example, a balance that has been fully spent or a free-tier item with zero cost.

```csharp
RuleFor(x => x.Balance).NonNegative()
    .WithMessage("The balance cannot be negative.");

RuleFor(x => x.Stock).NonNegative()
    .WithMessage("The stock level cannot be negative.");
```

### `Percentage()`

Verifies that the numeric value is between 0 and 100 inclusive. Ideal for percentage fields such as tax rates, discount rates and completion scores.

```csharp
RuleFor(x => x.DiscountRate)
    .Percentage()
        .WithMessage("The discount rate must be a value between 0 and 100.");

RuleFor(x => x.TaxRate)
    .Percentage()
        .WithMessage("The tax rate must be between 0% and 100%.");
```

### `Precision(int totalDigits, int decimalPlaces)`

Verifies that the decimal value fits within a `DECIMAL(totalDigits, decimalPlaces)` column — at most `totalDigits` significant digits in total and at most `decimalPlaces` digits after the decimal point. This maps directly to SQL DECIMAL/NUMERIC column constraints.

```csharp
// Equivalent to SQL DECIMAL(10, 2): up to 10 digits total, 2 after the point
RuleFor(x => x.Price)
    .Precision(10, 2)
        .WithMessage("The price cannot exceed 10 significant digits with 2 decimal places.");

// Financial amounts stored as DECIMAL(18, 4)
RuleFor(x => x.ExchangeRate)
    .Precision(18, 4)
        .WithMessage("The exchange rate must fit within DECIMAL(18, 4).");
```

### `Negative()`

Verifies that the number is strictly less than 0.

```csharp
RuleFor(x => x.TemperatureOffset).Negative()
    .WithMessage("The temperature offset must be negative for this mode.");
```

### `NotZero()`

Verifies that the number is not 0. Useful when both positive and negative values are valid, but zero has no meaning.

```csharp
RuleFor(x => x.ScaleFactor).NotZero()
    .WithMessage("The scale factor cannot be 0.");
```

### `Odd()`

Verifies that the number is odd.

```csharp
RuleFor(x => x.ThreadCount).Odd()
    .WithMessage("The number of threads must be odd for this algorithm.");
```

### `Even()`

Verifies that the number is even.

```csharp
RuleFor(x => x.BatchSize)
    .Even()
        .WithMessage("The batch size must be even.")
    .GreaterThan(0);
```

### `MultipleOf(decimal factor)`

Verifies that the number is a multiple of the given factor.

```csharp
RuleFor(x => x.Amount)
    .MultipleOf(0.01m)
        .WithMessage("The amount must have at most 2 decimal places.");

RuleFor(x => x.PageSize)
    .MultipleOf(10)
        .WithMessage("The page size must be a multiple of 10 (10, 20, 30...).");
```

### `MultipleOfProperty(Expression<Func<T, TProperty>> otherExpression)`

Verifies that the value is a multiple of another property on the same object. Useful when the allowed granularity itself comes from the request, for example ensuring that a transfer amount is an exact multiple of the minimum lot size for a financial instrument.

```csharp
public class TradeOrderValidator : AbstractValidator<TradeOrderRequest>
{
    public TradeOrderValidator()
    {
        RuleFor(x => x.Quantity)
            .MultipleOfProperty(x => x.MinLotSize)
                .WithMessage("The order quantity must be a multiple of the minimum lot size.");
    }
}
```

### `MaxDecimalPlaces(int places)`

Verifies that the decimal number has no more than `places` decimal places.

```csharp
RuleFor(x => x.Price)
    .MaxDecimalPlaces(2)
        .WithMessage("The price cannot have more than 2 decimal places.");

RuleFor(x => x.ExchangeRate)
    .MaxDecimalPlaces(6)
        .WithMessage("The exchange rate cannot have more than 6 decimal places.");
```

---

## Strings

### `Matches(string pattern)`

Verifies that the string matches the given regular expression.

```csharp
RuleFor(x => x.PostalCode)
    .Matches(@"^\d{5}(-\d{4})?$")
        .WithMessage("The postal code must be a ZIP (XXXXX or XXXXX-XXXX).");

RuleFor(x => x.Slug)
    .Matches(@"^[a-z0-9-]+$")
        .WithMessage("The slug can only contain lowercase letters, numbers and hyphens.");

RuleFor(x => x.InvoiceNumber)
    .Matches(@"^INV-\d{4}-\d{6}$")
        .WithMessage("The invoice number must have the format INV-YYYY-XXXXXX.");
```

### `MustContain(string substring, StringComparison comparison)`

Verifies that the string contains the given substring. Comparison is `OrdinalIgnoreCase` by default.

```csharp
RuleFor(x => x.Password)
    .MustContain("@")
        .WithMessage("The password must contain the @ symbol.");

// With case-sensitive comparison
RuleFor(x => x.ApiKey)
    .MustContain("sk-", StringComparison.Ordinal);
```

### `NotContains(string substring, StringComparison comparison)`

Verifies that the string does NOT contain the given substring.

```csharp
RuleFor(x => x.Username)
    .NotContains("admin", StringComparison.OrdinalIgnoreCase)
        .WithMessage("The username cannot contain 'admin'.")
    .NotContains("root")
        .WithMessage("The username cannot contain 'root'.");
```

### `StartsWith(string prefix)`

```csharp
RuleFor(x => x.Iban)
    .StartsWith("US")
        .WithMessage("The IBAN must start with 'US' for US accounts.");

RuleFor(x => x.OrderId)
    .StartsWith("ORD-")
        .WithMessage("The order ID must start with 'ORD-'.");
```

### `EndsWith(string suffix)`

```csharp
RuleFor(x => x.Email)
    .EndsWith("@company.com", StringComparison.OrdinalIgnoreCase)
        .WithMessage("Only corporate emails are accepted (@company.com).");
```

### `IsAlpha()`

Verifies that the string contains only letters. Includes accented characters (á, é, ñ, ü, etc.).

```csharp
RuleFor(x => x.FirstName)
    .IsAlpha()
        .WithMessage("The first name can only contain letters.");

RuleFor(x => x.LastName)
    .IsAlpha()
        .WithMessage("The last name can only contain letters.");
```

### `IsAlphanumeric()`

Verifies that the string contains only letters, numbers and underscore (`_`).

```csharp
RuleFor(x => x.Username)
    .IsAlphanumeric()
        .WithMessage("The username can only contain letters, numbers and underscores.");

RuleFor(x => x.Identifier)
    .IsAlphanumeric();
```

### `IsNumeric()`

Verifies that the string contains only digits (0-9). Does not accept signs or decimals.

```csharp
RuleFor(x => x.PostalCode)
    .IsNumeric()
        .WithMessage("The postal code can only contain digits.");

RuleFor(x => x.PinCode)
    .IsNumeric()
    .LengthBetween(4, 6);
```

### `Lowercase()`

Verifies that the entire string is lowercase.

```csharp
RuleFor(x => x.Slug)
    .Lowercase()
        .WithMessage("The slug must be lowercase.");
```

### `Uppercase()`

Verifies that the entire string is uppercase.

```csharp
RuleFor(x => x.CountryCode)
    .Uppercase()
    .LengthBetween(2, 3)
        .WithMessage("The country code must be uppercase (e.g.: US, GBR).");
```

### `NoWhitespace()`

Verifies that the string contains no whitespace (no spaces, tabs or line breaks).

```csharp
RuleFor(x => x.ApiKey)
    .NoWhitespace()
        .WithMessage("The API key cannot contain spaces.");

RuleFor(x => x.Username)
    .NoWhitespace()
        .WithMessage("The username cannot contain spaces.");
```

### `MinWords(int n)`

Verifies that the string has at least `n` words (separated by spaces).

```csharp
RuleFor(x => x.FullName)
    .MinWords(2)
        .WithMessage("Please enter your full name (first and last name).");

RuleFor(x => x.ProductDescription)
    .MinWords(10)
        .WithMessage("The description must have at least 10 words.");
```

### `MaxWords(int n)`

Verifies that the string does not have more than `n` words.

```csharp
RuleFor(x => x.TagLine)
    .MaxWords(10)
        .WithMessage("The tagline cannot exceed 10 words.");
```

---

## Format and Data Validation

### `Email()`

Verifies that the string has a valid email format.

```csharp
RuleFor(x => x.Email)
    .NotEmpty()
    .Email()
        .WithMessage("The email does not have a valid format.");

// Multiple recipients are not valid with this rule:
// "user@example.com, other@example.com" FAILS
```

### `Url()`

Verifies that the string is a valid HTTP or HTTPS URL.

```csharp
RuleFor(x => x.Website)
    .Url()
        .WithMessage("The URL must start with http:// or https://.");

RuleFor(x => x.CallbackUrl)
    .NotEmpty()
    .Url()
    .StartsWith("https://")
        .WithMessage("The callback URL must use HTTPS.");
```

### `PhoneNumber()`

Verifies that the string is a phone number in E.164 format (e.g. `+12025551234`). Must always start with `+` followed by the country code.

```csharp
RuleFor(x => x.PhoneNumber)
    .PhoneNumber()
        .WithMessage("The phone number must be in E.164 format (e.g. +12025551234).");
```

### `IPv4()`

Verifies that the string is a valid IPv4 address.

```csharp
RuleFor(x => x.ServerIp)
    .IPv4()
        .WithMessage("The server IP is not a valid IPv4 address.");

RuleFor(x => x.AllowedIp)
    .IPv4()
    .When(x => x.AllowedIp != null);
```

### `CreditCard()`

Verifies that the string is a valid credit card number according to the Luhn algorithm. Accepts strings with or without spaces/hyphens.

```csharp
RuleFor(x => x.CardNumber)
    .NotEmpty()
    .CreditCard()
        .WithMessage("The card number is not valid.");
```

> **Security note:** This rule only validates the mathematical format (Luhn), it does not verify that the card exists or has funds. Card numbers should never be stored without tokenization.

### `Guid()`

Verifies that the string is a valid GUID in any standard format.

```csharp
RuleFor(x => x.ExternalId)
    .Guid()
        .WithMessage("The external ID must be a valid GUID.");

// Accepts: "6ba7b810-9dad-11d1-80b4-00c04fd430c8"
// Accepts: "{6ba7b810-9dad-11d1-80b4-00c04fd430c8}"
// Accepts: "6ba7b8109dad11d180b400c04fd430c8"
```

### `NotEmptyGuid()`

Verifies that the string is a valid GUID and different from `Guid.Empty` (`00000000-0000-0000-0000-000000000000`).

```csharp
RuleFor(x => x.UserId)
    .NotEmptyGuid()
        .WithMessage("The user ID cannot be empty.");
```

### `IsEnum<TEnum>()`

Verifies that the string is a valid member name of the `TEnum` enum.

```csharp
public enum OrderStatus { Pending, Processing, Shipped, Delivered, Cancelled }

public class UpdateOrderStatusValidator : AbstractValidator<UpdateOrderStatusRequest>
{
    public UpdateOrderStatusValidator()
    {
        RuleFor(x => x.Status)
            .IsEnum<OrderStatus>()
                .WithMessage("The status must be one of: Pending, Processing, Shipped, Delivered, Cancelled.");
    }
}
```

### `IPv6()`

Verifies that the string is a valid IPv6 address. Use alongside `IPv4()` when your API must accept both address families, or when your infrastructure is IPv6-only.

```csharp
RuleFor(x => x.ServerAddress)
    .IPv6()
        .WithMessage("The server address is not a valid IPv6 address.");

// Accept either IPv4 or IPv6 with conditional rules
RuleFor(x => x.RemoteAddress)
    .IPv4()
    .When(x => x.AddressFamily == "ipv4");

RuleFor(x => x.RemoteAddress)
    .IPv6()
    .When(x => x.AddressFamily == "ipv6");
```

### `MacAddress()`

Verifies that the string is a valid MAC address in colon-separated (`AA:BB:CC:DD:EE:FF`) or hyphen-separated (`AA-BB-CC-DD-EE-FF`) format.

```csharp
RuleFor(x => x.DeviceMacAddress)
    .MacAddress()
        .WithMessage("The MAC address is not valid (expected format: AA:BB:CC:DD:EE:FF).");
```

### `Latitude()`

Verifies that the numeric value is a valid latitude coordinate (between -90 and 90 inclusive).

```csharp
RuleFor(x => x.Latitude)
    .Latitude()
        .WithMessage("The latitude must be between -90 and 90.");
```

### `Longitude()`

Verifies that the numeric value is a valid longitude coordinate (between -180 and 180 inclusive).

```csharp
public class LocationValidator : AbstractValidator<LocationRequest>
{
    public LocationValidator()
    {
        RuleFor(x => x.Latitude).Latitude();
        RuleFor(x => x.Longitude).Longitude();
    }
}
```

### `CountryCode()`

Verifies that the string is a valid ISO 3166-1 alpha-2 country code: exactly 2 uppercase letters (e.g. `US`, `PE`, `ES`, `GB`).

```csharp
RuleFor(x => x.CountryCode)
    .CountryCode()
        .WithMessage("The country code must be a valid ISO 3166-1 alpha-2 code (e.g. US, GB, ES).");

RuleFor(x => x.ShippingCountry)
    .NotEmpty()
    .CountryCode();
```

### `CurrencyCode()`

Verifies that the string is a valid ISO 4217 currency code: exactly 3 uppercase letters (e.g. `USD`, `EUR`, `PEN`).

```csharp
RuleFor(x => x.Currency)
    .CurrencyCode()
        .WithMessage("The currency must be a valid ISO 4217 code (e.g. USD, EUR, GBP).");

RuleFor(x => x.InvoiceCurrency)
    .NotEmpty()
    .CurrencyCode();
```

### `Slug()`

Verifies that the string is a valid URL slug: lowercase letters, digits and hyphens only, with no leading or trailing hyphens (e.g. `my-article-2024`). A slug is the human-readable part of a URL.

```csharp
RuleFor(x => x.UrlSlug)
    .NotEmpty()
    .Slug()
        .WithMessage("The slug can only contain lowercase letters, numbers and hyphens (e.g. my-article-2024).");

RuleFor(x => x.CategorySlug)
    .Slug()
    .MaximumLength(100);
```

### `IsValidJson()`

Verifies that the string is syntactically valid JSON. Useful when accepting serialized metadata, configuration payloads or webhook bodies as strings.

```csharp
RuleFor(x => x.Metadata)
    .IsValidJson()
        .WithMessage("The metadata field must contain valid JSON.");

RuleFor(x => x.ConfigurationPayload)
    .NotEmpty()
    .IsValidJson()
        .WithMessage("The configuration must be a valid JSON object.");
```

### `IsValidBase64()`

Verifies that the string is a valid Base64-encoded value. Accepts standard Base64 with optional padding.

```csharp
RuleFor(x => x.FileContent)
    .IsValidBase64()
        .WithMessage("The file content must be encoded in Base64.");

RuleFor(x => x.PublicKey)
    .NotEmpty()
    .IsValidBase64()
        .WithMessage("The public key must be a valid Base64 string.");
```

### `NoHtmlTags()`

Verifies that the string contains no HTML tags (no `<` ... `>` sequences). Prevents stored XSS attacks in user-supplied text fields.

```csharp
RuleFor(x => x.Comment)
    .NoHtmlTags()
        .WithMessage("The comment cannot contain HTML tags.");

RuleFor(x => x.DisplayName)
    .NoHtmlTags()
        .WithMessage("The display name cannot contain HTML.");
```

### `NoSqlInjectionPatterns()`

Verifies that the string does not contain common SQL injection patterns such as `--`, `; DROP`, `' OR`, `UNION SELECT`, and similar sequences. This is an extra defence-in-depth layer and does not replace proper parameterised queries.

```csharp
RuleFor(x => x.SearchQuery)
    .NoSqlInjectionPatterns()
        .WithMessage("The search query contains forbidden characters.");
```

> **Security note:** `NoSqlInjectionPatterns()` is a heuristic guard, not a replacement for parameterised queries or an ORM. Always use parameterised queries when executing user input against a database.

### `Iban()`

Verifies that the string is a valid IBAN (International Bank Account Number) using the mod-97 checksum algorithm defined in ISO 13616. Accepts IBANs from all countries (15–34 characters).

```csharp
RuleFor(x => x.BankAccount)
    .NotEmpty()
    .Iban()
        .WithMessage("The bank account number is not a valid IBAN.");

public class PaymentInfoValidator : AbstractValidator<PaymentInfo>
{
    public PaymentInfoValidator()
    {
        RuleFor(x => x.Iban)
            .NotEmpty()
                .WithMessage("The IBAN is required for bank transfers.")
            .Iban()
                .WithMessage("The IBAN does not have a valid format.")
            .When(x => x.Method == "transfer");
    }
}
```

---

## Password and Security

These rules apply to strings and verify the presence of certain types of characters.

### `HasUppercase()`

```csharp
RuleFor(x => x.Password).HasUppercase()
    .WithMessage("The password must contain at least one uppercase letter.");
```

### `HasLowercase()`

```csharp
RuleFor(x => x.Password).HasLowercase()
    .WithMessage("The password must contain at least one lowercase letter.");
```

### `HasDigit()`

```csharp
RuleFor(x => x.Password).HasDigit()
    .WithMessage("The password must contain at least one number.");
```

### `HasSpecialChar()`

Verifies that the string contains at least one non-alphanumeric character (e.g. `!@#$%^&*`).

```csharp
RuleFor(x => x.Password).HasSpecialChar()
    .WithMessage("The password must contain at least one special character (!@#$%...).");
```

### `PasswordPolicy(int minLength, bool requireUppercase, bool requireLowercase, bool requireDigit, bool requireSpecialChar)`

A single all-in-one rule that combines length, uppercase, lowercase, digit and special-character checks into one fluent call. All parameters have sensible defaults (`minLength = 8`, all character requirements `true`). When a requirement fails, the error message clearly states which constraint was violated.

```csharp
// Default policy: 8+ chars, upper, lower, digit, special char
RuleFor(x => x.Password)
    .PasswordPolicy();

// Stricter enterprise policy
RuleFor(x => x.Password)
    .PasswordPolicy(minLength: 12, requireSpecialChar: true)
        .WithMessage("The password does not meet the security policy.");

// Relaxed policy: 6+ chars, only lowercase and digit required
RuleFor(x => x.Pin)
    .PasswordPolicy(minLength: 6, requireUppercase: false, requireSpecialChar: false)
        .WithMessage("The PIN must have at least 6 characters with at least one digit.");
```

`PasswordPolicy` is equivalent to chaining `MinimumLength`, `HasUppercase`, `HasLowercase`, `HasDigit` and `HasSpecialChar`, but produces a single rule entry in `ValidationResult.Errors`, making it easier to surface a concise error message to the user.

### Complete Password Validator

```csharp
public class PasswordValidator : AbstractValidator<SetPasswordRequest>
{
    public PasswordValidator()
    {
        RuleFor(x => x.Password)
            .NotEmpty()
                .WithMessage("The password is required.")
            .MinimumLength(12)
                .WithMessage("The password must have at least 12 characters.")
            .MaximumLength(128)
                .WithMessage("The password cannot exceed 128 characters.")
            .HasUppercase()
                .WithMessage("Must contain at least one uppercase letter.")
            .HasLowercase()
                .WithMessage("Must contain at least one lowercase letter.")
            .HasDigit()
                .WithMessage("Must contain at least one number.")
            .HasSpecialChar()
                .WithMessage("Must contain at least one special character.")
            .NotContains("password", StringComparison.OrdinalIgnoreCase)
                .WithMessage("The password cannot contain the word 'password'.")
            .NotContains("123456")
                .WithMessage("The password cannot contain the sequence '123456'.");

        RuleFor(x => x.ConfirmPassword)
            .EqualToProperty(x => x.Password)
                .WithMessage("The passwords do not match.");
    }
}
```

---

## Dates

### `FutureDate()`

Verifies that the date is after `DateTime.Now` (or `DateTime.UtcNow`).

```csharp
public class CreateEventValidator : AbstractValidator<CreateEventRequest>
{
    public CreateEventValidator()
    {
        RuleFor(x => x.EventDate)
            .FutureDate()
                .WithMessage("The event date must be in the future.");

        RuleFor(x => x.RegistrationDeadline)
            .FutureDate()
                .WithMessage("The registration deadline must be in the future.")
            .LessThan(x => x.EventDate)
                .WithMessage("The registration deadline must be before the event.");
    }
}
```

### `PastDate()`

Verifies that the date is before `DateTime.Now`.

```csharp
RuleFor(x => x.BirthDate)
    .PastDate()
        .WithMessage("The birth date must be in the past.");

RuleFor(x => x.DocumentIssueDate)
    .PastDate()
        .WithMessage("The document issue date must be in the past.");
```

### `Today()`

Verifies that the date is today's date (without time).

```csharp
RuleFor(x => x.ReportDate)
    .Today()
        .WithMessage("The report date must be today.");
```

### `MinAge(int years)`

Verifies that a birth date implies a minimum age in full years. The calculation uses today's date, correctly accounting for leap years and months.

```csharp
RuleFor(x => x.BirthDate)
    .PastDate()
    .MinAge(18)
        .WithMessage("You must be at least 18 years old to register.");

RuleFor(x => x.DriverBirthDate)
    .MinAge(16)
        .WithMessage("The driver must be at least 16 years old.");
```

### `MaxAge(int years)`

Verifies that a birth date implies an age no greater than the given number of years. Useful for youth programs, age-restricted promotions, or pediatric applications.

```csharp
RuleFor(x => x.BirthDate)
    .MaxAge(120)
        .WithMessage("Please enter a realistic birth date.");

// Pediatric patient portal: must be under 18
RuleFor(x => x.PatientBirthDate)
    .MinAge(0)
    .MaxAge(17)
        .WithMessage("This portal is for patients under 18 years old.");
```

### `DateBetween(DateTime from, DateTime to)`

Verifies that the date falls within an inclusive range. Both endpoints are included.

```csharp
RuleFor(x => x.AppointmentDate)
    .DateBetween(DateTime.Today, DateTime.Today.AddMonths(3))
        .WithMessage("Appointments can be booked up to 3 months in advance.");

RuleFor(x => x.ReservationDate)
    .DateBetween(DateTime.Today, DateTime.Today.AddDays(60))
        .WithMessage("Reservations must be within the next 60 days.");
```

### `NotExpired()`

Verifies that the date has not already passed (`>= DateTime.Now`). Ideal for document expiry dates, subscription end dates and promotional codes.

```csharp
RuleFor(x => x.PassportExpiryDate)
    .NotExpired()
        .WithMessage("The passport has expired. Please use a valid document.");

RuleFor(x => x.PromoCodeExpiry)
    .NotExpired()
        .WithMessage("This promotional code has expired.");
```

### `WithinNext(TimeSpan span)`

Verifies that the date is in the future but no further ahead than the given time span. Combines an implicit future-date check with an upper bound.

```csharp
// Appointment must be within the next 30 days
RuleFor(x => x.AppointmentDate)
    .WithinNext(TimeSpan.FromDays(30))
        .WithMessage("The appointment must be within the next 30 days.");

// Delivery date: within the next 2 weeks
RuleFor(x => x.RequestedDeliveryDate)
    .WithinNext(TimeSpan.FromDays(14))
        .WithMessage("The requested delivery date cannot be more than 2 weeks away.");
```

### `WithinLast(TimeSpan span)`

Verifies that the date is in the past but no older than the given time span. Useful for verifying recent documents or recent activity timestamps.

```csharp
// Medical test must have been taken within the last 6 months
RuleFor(x => x.TestDate)
    .WithinLast(TimeSpan.FromDays(180))
        .WithMessage("The medical test must have been performed within the last 6 months.");

// Confirm the event happened within the last 24 hours
RuleFor(x => x.EventTimestamp)
    .WithinLast(TimeSpan.FromHours(24))
        .WithMessage("The event timestamp must be within the last 24 hours.");
```

### `IsWeekday()`

Verifies that the date falls on a weekday (Monday through Friday). Use this for business-hours scheduling where weekend dates are not accepted.

```csharp
RuleFor(x => x.AppointmentDate)
    .IsWeekday()
        .WithMessage("Appointments can only be scheduled on weekdays (Monday–Friday).");

RuleFor(x => x.PickupDate)
    .IsWeekday()
        .WithMessage("Pickups are only available Monday to Friday.");
```

### `IsWeekend()`

Verifies that the date falls on a weekend (Saturday or Sunday). Useful for weekend-only events or promotions.

```csharp
RuleFor(x => x.EventDate)
    .IsWeekend()
        .WithMessage("This event is only available on weekends.");
```

---

## Collections

### `NotEmptyCollection()`

Verifies that the collection is not `null` and not empty.

```csharp
RuleFor(x => x.Tags)
    .NotEmptyCollection()
        .WithMessage("The article must have at least one tag.");

RuleFor(x => x.OrderLines)
    .NotEmptyCollection()
        .WithMessage("The order must have at least one product.");
```

### `HasCount(int n)`

Verifies that the collection has exactly `n` elements.

```csharp
RuleFor(x => x.SecurityQuestions)
    .HasCount(3)
        .WithMessage("You must provide exactly 3 security questions.");
```

### `MinCount(int n)`

Verifies that the collection has at least `n` elements.

```csharp
RuleFor(x => x.Photos)
    .MinCount(1)
        .WithMessage("You must upload at least one photo.")
    .MaxCount(10)
        .WithMessage("You cannot upload more than 10 photos.");
```

### `MaxCount(int n)`

Verifies that the collection has no more than `n` elements.

```csharp
RuleFor(x => x.Tags)
    .MaxCount(5)
        .WithMessage("You cannot add more than 5 tags.");
```

### `Unique()`

Verifies that all elements of the collection are distinct (no duplicates).

```csharp
RuleFor(x => x.SelectedRoleIds)
    .Unique()
        .WithMessage("The selected roles cannot be repeated.");

RuleFor(x => x.EmailAddresses)
    .Unique()
        .WithMessage("The email list cannot have duplicates.");
```

### `AllSatisfy(Func<object, bool> predicate)`

Verifies that all elements of the collection satisfy the predicate.

```csharp
RuleFor(x => x.FileNames)
    .AllSatisfy(name => ((string)name).EndsWith(".pdf"))
        .WithMessage("All files must be PDF.");

RuleFor(x => x.Prices)
    .AllSatisfy(p => (decimal)p > 0)
        .WithMessage("All prices must be positive.");
```

### `AnySatisfy(Func<object, bool> predicate)`

Verifies that at least one element satisfies the predicate.

```csharp
RuleFor(x => x.ContactMethods)
    .AnySatisfy(method => (string)method == "email")
        .WithMessage("There must be at least one email contact method.");
```

### `In(IEnumerable<TProperty> values)`

Verifies that the value is within the list of allowed values.

```csharp
private static readonly string[] AllowedCurrencies = new[] { "EUR", "USD", "GBP", "JPY" };

RuleFor(x => x.Currency)
    .In(AllowedCurrencies)
        .WithMessage("The currency must be EUR, USD, GBP or JPY.");

RuleFor(x => x.Priority)
    .In(new[] { 1, 2, 3, 4, 5 })
        .WithMessage("The priority must be a value from 1 to 5.");
```

### `NotIn(IEnumerable<TProperty> values)`

Verifies that the value is NOT in the list of prohibited values.

```csharp
private static readonly string[] ReservedUsernames = new[] { "admin", "root", "system", "api" };

RuleFor(x => x.Username)
    .NotIn(ReservedUsernames)
        .WithMessage("That username is reserved.");
```

---

## Summary of Available Rules

| Category | Rules |
|---|---|
| Null/empty | `NotNull`, `Null`, `NotEmpty`, `Empty` |
| Equality | `EqualTo`, `NotEqual`, `EqualToProperty` |
| Length | `MinimumLength`, `MaximumLength`, `LengthBetween` |
| Numeric range | `GreaterThan`, `GreaterThanOrEqualTo`, `LessThan`, `LessThanOrEqualTo`, `Between`, `ExclusiveBetween`, `Positive`, `NonNegative`, `Negative`, `NotZero`, `Odd`, `Even`, `MultipleOf`, `MultipleOfProperty`, `MaxDecimalPlaces`, `Percentage`, `Precision` |
| String | `Matches`, `MustContain`, `NotContains`, `StartsWith`, `EndsWith`, `IsAlpha`, `IsAlphanumeric`, `IsNumeric`, `Lowercase`, `Uppercase`, `NoWhitespace`, `MinWords`, `MaxWords`, `Slug`, `NoHtmlTags`, `NoSqlInjectionPatterns` |
| Format | `Email`, `Url`, `PhoneNumber`, `IPv4`, `IPv6`, `MacAddress`, `CreditCard`, `Guid`, `NotEmptyGuid`, `IsEnum<T>`, `CountryCode`, `CurrencyCode`, `Latitude`, `Longitude`, `IsValidJson`, `IsValidBase64`, `Iban` |
| Password | `HasUppercase`, `HasLowercase`, `HasDigit`, `HasSpecialChar`, `PasswordPolicy` |
| Dates | `FutureDate`, `PastDate`, `Today`, `MinAge`, `MaxAge`, `DateBetween`, `NotExpired`, `WithinNext`, `WithinLast`, `IsWeekday`, `IsWeekend` |
| Collections | `NotEmptyCollection`, `HasCount`, `MinCount`, `MaxCount`, `Unique`, `AllSatisfy`, `AnySatisfy`, `In`, `NotIn` |

## Next Steps

- **[Advanced Rules](06-advanced-rules.md)** — Must, MustAsync, Custom, Transform, SetValidator
- **[Modifiers](07-modifiers.md)** — WithMessage, WithErrorCode, When/Unless and more
