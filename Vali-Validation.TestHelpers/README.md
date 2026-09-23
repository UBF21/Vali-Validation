# Vali-Validation.TestHelpers

Fluent test-assertion extensions for [Vali-Validation](https://www.nuget.org/packages/Vali-Validation)'s `ValidationResult`. Framework-agnostic — works with xUnit, NUnit, MSTest, or any runner that fails a test on an unhandled exception.

## Usage

```csharp
var result = validator.Validate(order);

result.ShouldHaveValidationErrorFor("Email");
result.ShouldHaveValidationErrorFor("Email", "The Email field must be a valid email address.");
result.ShouldNotHaveValidationErrorFor("ShippingAddress");
```

Each assertion throws `ValidationAssertionException` with a descriptive message when it fails — your test runner reports it as a normal test failure, no adapter or dependency on any specific runner required.

## Installation

```
dotnet add package Vali-Validation.TestHelpers
```

See the main [Vali-Validation README](https://github.com/UBF21/Vali-Validation) for the full validation library documentation.
