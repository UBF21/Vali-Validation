# Vali-Validation Documentation (English)

Welcome to the English documentation for **Vali-Validation**, the fluent validation library for .NET 7/8/9 from the Vali ecosystem.

---

## Table of Contents

### Fundamentals

| # | Document | Description |
|---|---|---|
| 01 | [Introduction](01-introduction.md) | What Vali-Validation is, comparison with FluentValidation, package ecosystem and compatibility table |
| 02 | [Installation](02-installation.md) | How to install each NuGet package, when to use each one, recommended project structure |
| 03 | [Quick Start](03-quick-start.md) | Complete example from scratch: model, validator, DI, endpoint and test in 10 minutes |

### Validators and Rules

| # | Document | Description |
|---|---|---|
| 04 | [Validators](04-validators.md) | `AbstractValidator<T>`, `RuleFor`, `RuleForEach`, `Include`, `RuleSwitch`, validation methods (`ValidateAsync`, `ValidateParallelAsync`, `ValidateAndThrow`) |
| 05 | [Basic Rules](05-basic-rules.md) | Complete catalog of synchronous rules: nullability, equality, length, numeric ranges, strings, format, dates and collections |
| 06 | [Advanced Rules](06-advanced-rules.md) | `Must`, `MustAsync`, `DependentRuleAsync`, `Custom` with context, `Transform`, `SetValidator`, `SwitchOn`, advanced `RuleForEach` |
| 07 | [Modifiers](07-modifiers.md) | `WithMessage` (with placeholders), `WithErrorCode`, `OverridePropertyName`, `StopOnFirstFailure`, `When`/`Unless`, `WhenAsync`/`UnlessAsync` |
| 08 | [CascadeMode](08-cascade-mode.md) | Controlling validation flow per property vs global, when to use each mode |

### Results and Errors

| # | Document | Description |
|---|---|---|
| 09 | [Validation Result](09-validation-result.md) | Complete `ValidationResult`: `Errors`, `ErrorCodes`, `IsValid`, `ErrorCount`, `ToFlatList`, `Merge`, `HasErrorFor`, usage in APIs and testing |
| 10 | [Exceptions](10-exceptions.md) | `ValidationException`, `ValidateAndThrow` vs value result, when to use each approach, middleware capture |

### Integration and DI

| # | Document | Description |
|---|---|---|
| 11 | [Dependency Injection](11-dependency-injection.md) | `AddValidationsFromAssembly`, lifetimes, `IValidator<T>` in constructors, complete `Program.cs` example, testing with mocks |
| 12 | [ASP.NET Core](12-aspnetcore-integration.md) | `UseValiValidationExceptionHandler` middleware, `WithValiValidation<T>` for Minimal API, `[ValiValidate]` for MVC, when to use each |
| 13 | [MediatR](13-mediatr-integration.md) | `Vali-Validation.MediatR`: setup, behavior pipeline, complete command/validator/handler example |
| 14 | [Vali-Mediator](14-valimediator-integration.md) | `Vali-Validation.ValiMediator`: behavior with `Result<T>` vs simple types, complete example |

### Advanced Patterns

| # | Document | Description |
|---|---|---|
| 15 | [Advanced Patterns](15-advanced-patterns.md) | Nested validators with `SetValidator`, `Include` for inheritance, complex conditional validation, `RuleSwitch`/`SwitchOn` polymorphic validation, passwords, nested collections, `IRuleBuilder` extensions |
| 16 | [Switch / Case](16-switch-case.md) | Complete `RuleSwitch` and `SwitchOn` reference: syntax, 12+ real-world examples (e-commerce, multi-tenant, loans, notifications, scientific measurements), decision tree, xUnit tests, anti-patterns |

---

## Quick Reading Guide

### I am new to Vali-Validation

1. Read [Introduction](01-introduction.md) to understand the purpose and ecosystem
2. Follow the [Quick Start](03-quick-start.md) to get something working
3. Refer to [Basic Rules](05-basic-rules.md) when you need a specific rule

### I want to integrate with ASP.NET Core

1. [Installation](02-installation.md) — packages `Vali-Validation` + `Vali-Validation.AspNetCore`
2. [Dependency Injection](11-dependency-injection.md) — registration in `Program.cs`
3. [ASP.NET Core](12-aspnetcore-integration.md) — middleware, filters and attributes

### I want to integrate with MediatR

1. [Installation](02-installation.md) — package `Vali-Validation.MediatR`
2. [MediatR](13-mediatr-integration.md) — setup and complete example

### I want to integrate with Vali-Mediator

1. [Installation](02-installation.md) — package `Vali-Validation.ValiMediator`
2. [Vali-Mediator](14-valimediator-integration.md) — setup and behavior with `Result<T>`

### I have a complex use case

1. [Advanced Rules](06-advanced-rules.md) — `MustAsync`, `Custom`, `Transform`, `SetValidator`, `SwitchOn`
2. [Modifiers](07-modifiers.md) — `When`/`Unless`, `WhenAsync`/`UnlessAsync`
3. [Advanced Patterns](15-advanced-patterns.md) — composition, inheritance, nested collections, `RuleSwitch`/`SwitchOn`

---

## Quick Rule Reference

### By Category

| Category | Main Rules |
|---|---|
| Null/empty | `NotNull`, `Null`, `NotEmpty`, `Empty` |
| Equality | `EqualTo`, `NotEqual`, `EqualToProperty` |
| Length | `MinimumLength`, `MaximumLength`, `LengthBetween` |
| Numeric range | `GreaterThan`, `LessThan`, `Between`, `Positive`, `NonNegative`, `Negative`, `NotZero`, `Odd`, `Even`, `MultipleOf`, `MultipleOfProperty`, `MaxDecimalPlaces`, `Percentage`, `Precision` |
| Strings | `Matches`, `MustContain`, `StartsWith`, `EndsWith`, `IsAlpha`, `IsAlphanumeric`, `IsNumeric`, `Lowercase`, `Uppercase`, `NoWhitespace`, `MinWords`, `MaxWords`, `Slug`, `NoHtmlTags`, `NoSqlInjectionPatterns` |
| Format | `Email`, `Url`, `PhoneNumber`, `IPv4`, `IPv6`, `MacAddress`, `CreditCard`, `Guid`, `NotEmptyGuid`, `IsEnum<T>`, `CountryCode`, `CurrencyCode`, `Latitude`, `Longitude`, `IsValidJson`, `IsValidBase64`, `Iban` |
| Password | `HasUppercase`, `HasLowercase`, `HasDigit`, `HasSpecialChar`, `PasswordPolicy` |
| Dates | `FutureDate`, `PastDate`, `Today`, `MinAge`, `MaxAge`, `DateBetween`, `NotExpired`, `WithinNext`, `WithinLast`, `IsWeekday`, `IsWeekend` |
| Collections | `NotEmptyCollection`, `HasCount`, `MinCount`, `MaxCount`, `Unique`, `AllSatisfy`, `AnySatisfy`, `In`, `NotIn` |
| Cross-property | `GreaterThanProperty`, `GreaterThanOrEqualToProperty`, `LessThanProperty`, `LessThanOrEqualToProperty`, `NotEqualToProperty`, `MultipleOfProperty` |
| Conditional required | `RequiredIf`, `RequiredUnless` |
| Conditional switch | `RuleSwitch` (validator-level), `SwitchOn` (property-level) |
| Custom | `Must`, `MustAsync`, `DependentRuleAsync`, `Custom`, `Transform`, `SetValidator` |

### Available Modifiers

| Modifier | Effect |
|---|---|
| `.WithMessage(msg)` | Replaces the last rule's message |
| `.WithErrorCode(code)` | Adds code to `ErrorCodes` for the last rule |
| `.OverridePropertyName(name)` | Changes the key in `Errors` for the entire builder |
| `.StopOnFirstFailure()` | Stops property evaluation on first failure |
| `.When(condition)` | Applies rules only if condition is true |
| `.Unless(condition)` | Applies rules only if condition is false |
| `.WhenAsync(condition)` | `When` with async condition |
| `.UnlessAsync(condition)` | `Unless` with async condition |

---

## NuGet Packages

| Package | Installation |
|---|---|
| Core | `dotnet add package Vali-Validation` |
| MediatR | `dotnet add package Vali-Validation.MediatR` |
| Vali-Mediator | `dotnet add package Vali-Validation.ValiMediator` |
| ASP.NET Core | `dotnet add package Vali-Validation.AspNetCore` |

---

## Additional Resources

- **GitHub Repository:** [Vali-Validation](https://github.com/feliperafaelmontenegro/Vali-Validation)
- **Vali-Mediator Repository:** [Vali-Mediator](https://github.com/feliperafaelmontenegro/Vali-Mediator)
- **NuGet:** [nuget.org/profiles/feliperafaelmontenegro](https://nuget.org)
