# Vali-Validation.TestHelpers

Fluent test-assertion extensions for [Vali-Validation](https://www.nuget.org/packages/Vali-Validation)'s `ValidationResult`. Framework-agnostic — works with xUnit, NUnit, MSTest, or any runner that fails a test on an unhandled exception.

- **Targets**: net7.0 / net8.0 / net9.0 / net10.0
- Depends on `Vali-Validation` 3.0.0+.

## Usage

```csharp
var result = validator.Validate(order);

result.ShouldHaveValidationErrorFor("Email");
result.ShouldHaveValidationErrorFor("Email", "The Email field must be a valid email address.");
result.ShouldNotHaveValidationErrorFor("ShippingAddress");
```

Each assertion throws `ValidationAssertionException` with a descriptive message when it fails — your test runner reports it as a normal test failure, no adapter or dependency on any specific runner required.

> **Severity note:** these assertions check `Severity.Error` failures only (via `ValidationResult.HasErrorFor`/`ErrorsFor`). A rule marked `.WithSeverity(Severity.Warning)` (or `Severity.Info`) never satisfies `ShouldHaveValidationErrorFor` and never trips `ShouldNotHaveValidationErrorFor` — assert against `result.Failures` directly if your test needs to check a non-Error severity.

## Installation

```
dotnet add package Vali-Validation.TestHelpers
```

See the main [Vali-Validation README](https://github.com/UBF21/Vali-Validation) for the full validation library documentation.
