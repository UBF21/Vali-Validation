# Vali-Validation

Vali-Validation is a lightweight, zero-dependency fluent validation library for .NET 7, 8, and 9. It provides a clean, expressive API for defining validation rules on your models, with full support for async validation, conditional rules, nested object validation, collection validation, cascade mode, error codes, custom rules, and seamless dependency injection — all without requiring any external dependencies beyond `Microsoft.Extensions.DependencyInjection.Abstractions`.

## Installation

```bash
dotnet add package Vali-Validation
```

## Quick Start

Define a validator by subclassing `AbstractValidator<T>` and configuring rules in the constructor:

```csharp
using Vali_Validation.Core.Validators;

public class CreateUserDto
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
    public int Age { get; set; }
}

public class CreateUserValidator : AbstractValidator<CreateUserDto>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(100);

        RuleFor(x => x.Email)
            .NotEmpty()
            .Email();

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .HasUppercase()
            .HasLowercase()
            .HasDigit()
            .HasSpecialChar();

        RuleFor(x => x.Age)
            .GreaterThanOrEqualTo(18)
            .LessThan(120);
    }
}
```

Use the validator:

```csharp
var validator = new CreateUserValidator();
var result = validator.Validate(dto);

if (!result.IsValid)
{
    foreach (var error in result.ToFlatList())
        Console.WriteLine(error);
}

// Async
var result = await validator.ValidateAsync(dto, cancellationToken);

// Throw on failure
validator.ValidateAndThrow(dto);
await validator.ValidateAndThrowAsync(dto, cancellationToken);
```

## Available Rules

| Rule | Description |
|------|-------------|
| `NotEmpty()` | Value must not be null or whitespace |
| `NotNull()` | Value must not be null |
| `Null()` | Value must be null |
| `Empty()` | Value must be null or empty string |
| `Must(predicate)` | Custom sync predicate |
| `MustAsync(predicate)` | Custom async predicate |
| `MustAsync(predicate, ct)` | Custom async predicate with CancellationToken |
| `Custom(action)` | Custom validation action with full context |
| `MinimumLength(n)` | String length >= n |
| `MaximumLength(n)` | String length <= n |
| `LengthBetween(min, max)` | String length between min and max |
| `Matches(pattern)` | Must match regex pattern |
| `Email()` | Must be a valid email address |
| `Url()` | Must be a valid HTTP/HTTPS URL |
| `PhoneNumber()` | Must be a valid E.164 phone number |
| `IPv4()` | Must be a valid IPv4 address |
| `CreditCard()` | Must pass Luhn check |
| `Guid()` | Must be a valid GUID string |
| `NotEmptyGuid()` | Must be a non-empty GUID |
| `IsAlpha()` | Only alphabetic characters |
| `IsAlphanumeric()` | Only alphanumeric characters |
| `IsNumeric()` | Only numeric characters |
| `NoWhitespace()` | No whitespace characters |
| `EqualTo(value)` | Must equal the given value |
| `NotEqual(value)` | Must not equal the given value |
| `EqualToProperty(expr)` | Must equal another property |
| `GreaterThan(n)` | Must be greater than n |
| `LessThan(n)` | Must be less than n |
| `GreaterThanOrEqualTo(n)` | Must be >= n |
| `LessThanOrEqualTo(n)` | Must be <= n |
| `Between(min, max)` | Inclusive range |
| `ExclusiveBetween(min, max)` | Exclusive range |
| `Positive()` | Must be > 0 |
| `Negative()` | Must be < 0 |
| `NotZero()` | Must not be 0 |
| `Odd()` | Must be an odd integer |
| `Even()` | Must be an even integer |
| `MultipleOf(factor)` | Must be a multiple of factor |
| `MaxDecimalPlaces(n)` | At most n decimal places |
| `In(values)` | Must be in the allowed list |
| `NotIn(values)` | Must not be in the disallowed list |
| `StartsWith(prefix)` | String must start with prefix |
| `EndsWith(suffix)` | String must end with suffix |
| `MustContain(substring)` | String must contain substring |
| `NotContains(substring)` | String must not contain substring |
| `FutureDate()` | DateTime must be in the future |
| `PastDate()` | DateTime must be in the past |
| `Today()` | DateTime must be today |
| `IsEnum<TEnum>()` | Must be a valid enum value |
| `HasCount(n)` | Collection must have exactly n items |
| `MinCount(n)` | Collection must have at least n items |
| `MaxCount(n)` | Collection must have at most n items |
| `NotEmptyCollection()` | Collection must not be empty |
| `Unique()` | Collection must have no duplicates |
| `AllSatisfy(predicate)` | All collection items must satisfy predicate |
| `AnySatisfy(predicate)` | At least one item must satisfy predicate |
| `HasUppercase()` | String must contain an uppercase letter |
| `HasLowercase()` | String must contain a lowercase letter |
| `HasDigit()` | String must contain a digit |
| `HasSpecialChar()` | String must contain a special character |
| `Lowercase()` | String must be all lowercase |
| `Uppercase()` | String must be all uppercase |
| `MinWords(n)` | String must have at least n words |
| `MaxWords(n)` | String must have at most n words |

## Modifiers

| Modifier | Description |
|----------|-------------|
| `WithMessage(msg)` | Override the error message; supports `{PropertyName}` and `{PropertyValue}` |
| `WithErrorCode(code)` | Attach an error code to the rule |
| `OverridePropertyName(name)` | Use a custom key in error dictionaries |
| `StopOnFirstFailure()` | Stop evaluating rules for this property after first failure |
| `When(condition)` | Only run preceding rules when condition is true |
| `Unless(condition)` | Only run preceding rules when condition is false |
| `WhenAsync(condition)` | Async version of `When` |
| `UnlessAsync(condition)` | Async version of `Unless` |

## Message Templates

Error messages support `{PropertyName}` and `{PropertyValue}` placeholders:

```csharp
RuleFor(x => x.Age)
    .GreaterThan(0)
    .WithMessage("'{PropertyName}' value '{PropertyValue}' must be positive.");
```

## CascadeMode

Stop validation after the first property failure across all properties:

```csharp
public class MyValidator : AbstractValidator<MyDto>
{
    protected override CascadeMode GlobalCascadeMode => CascadeMode.StopOnFirstFailure;

    public MyValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Email).Email(); // Skipped if Name already fails
    }
}
```

## Nested Validators

```csharp
RuleFor(x => x.Address).SetValidator(new AddressValidator());
```

## Collection Validation

```csharp
RuleForEach(x => x.Tags).NotEmpty().MinimumLength(2);
```

## DI Registration

```csharp
// Register all validators from an assembly
services.AddValidationsFromAssembly(typeof(CreateUserValidator).Assembly);

// Or register individually
services.AddScoped<IValidator<CreateUserDto>, CreateUserValidator>();
```

## Links

- [GitHub Repository](https://github.com/UBF21/Vali-Validation)
- [NuGet Package](https://www.nuget.org/packages/Vali-Validation)

---

## Donations

If Vali-Validation is useful to you, consider supporting its development:

- **Latin America** — [MercadoPago](https://link.mercadopago.com.pe/felipermm)
- **International** — [PayPal](https://paypal.me/felipeRMM?country.x=PE&locale.x=es_XC)

---

## License

[Apache License 2.0](https://www.apache.org/licenses/LICENSE-2.0)

## Contributions

Issues and pull requests are welcome on [GitHub](https://github.com/UBF21/Vali-Validation).
