# Introduction to Vali-Validation

## What is Vali-Validation?

Vali-Validation is a fluent validation library for .NET, designed to be lightweight, expressive and free of unnecessary external dependencies. It lets you define validation rules declaratively through a fluent API, separating the validation logic from the domain model and from controllers.

The library follows the same conceptual pattern as FluentValidation, but is built from scratch with a focus on:

- **Zero external dependencies** in the core package (only `Microsoft.Extensions.DependencyInjection.Abstractions`)
- **Native async/await support** without the deadlock issues present in other libraries
- **First-class integration** with the Vali ecosystem (Vali-Mediator)
- **Modern API** taking advantage of C# 11 and .NET 7+ features

## Why does it exist?

FluentValidation is excellent, but in projects that use Vali-Mediator or that seek to minimize transitive dependencies, Vali-Validation offers:

1. **Native integration with `Result<T>`** — Instead of throwing exceptions when using Vali-Mediator, the behavior returns `Result<T>.Fail(...)` directly, without try/catch in handlers.
2. **No third-party dependencies** in the core — No NuGet packages are dragged in that could cause version conflicts.
3. **ValidateParallelAsync** — Executes async rules in parallel natively, with no additional configuration.
4. **Rich collection API** — `RuleForEach`, `Unique()`, `AllSatisfy()`, `AnySatisfy()`, `In()`, `NotIn()` included out of the box.

## Comparison with FluentValidation

| Feature | Vali-Validation | FluentValidation |
|---|---|---|
| External dependencies (core) | DI Abstractions only | None |
| Native async support | Yes, with CT | Yes, with CT |
| Parallel validation | `ValidateParallelAsync` | Not native |
| Mediator integration | ValiMediator (Result<T>) | MediatR (exception) |
| `Transform<TNew>()` | Yes | Yes (`Transform`) |
| `Custom()` with context | Yes (`CustomContext<T>`) | Yes (`ValidationContext<T>`) |
| Nested `SetValidator` | Yes | Yes |
| `RuleForEach` | Yes | Yes |
| `Include` (inheritance) | Yes | Yes |
| `When` / `Unless` async | `WhenAsync` / `UnlessAsync` | Not native |
| Password rules | `HasUppercase`, `HasDigit`, etc. | Separate extensions |
| Collection rules | `Unique`, `AllSatisfy`, `In`, etc. | Partial |
| Credit cards (Luhn) | `CreditCard()` | Yes |
| License | MIT | Apache 2.0 |
| Targets | net7/8/9 | netstandard2.0+ |

> **Note:** If you already use FluentValidation in a mature project with many custom rules, migrating may not be worth it just for the change. Vali-Validation shines especially in new projects that use Vali-Mediator or that want a fully controlled stack.

## Package Ecosystem

Vali-Validation is split into separate packages so you only install what you need:

### `Vali-Validation` (core)

The main package. Contains:

- `AbstractValidator<T>` — base class for all validators
- `IValidator<T>` — interface for dependency injection
- `IRuleBuilder<T, TProperty>` — fluent interface with all rules
- `ValidationResult` — result with errors and codes
- `ValidationException` — typed exception
- DI registration (`AddValidationsFromAssembly`)

**Dependency:** `Microsoft.Extensions.DependencyInjection.Abstractions` (only the abstraction, not the full DI container)

```xml
<PackageReference Include="Vali-Validation" Version="*" />
```

### `Vali-Validation.MediatR`

Integration with **MediatR**. Registers an `IPipelineBehavior<TRequest, TResponse>` that automatically validates the request before it reaches the handler. If validation fails, it throws `ValidationException`.

```xml
<PackageReference Include="Vali-Validation.MediatR" Version="*" />
```

### `Vali-Validation.ValiMediator`

Integration with **Vali-Mediator**. Similar to the above, but when `TResponse` is `Result<T>`, it returns `Result<T>.Fail(errors, ErrorType.Validation)` instead of throwing an exception. This allows handling validation errors in the same flow as other domain errors.

```xml
<PackageReference Include="Vali-Validation.ValiMediator" Version="*" />
```

### `Vali-Validation.AspNetCore`

Integration with **ASP.NET Core**. Includes:

- Middleware that catches `ValidationException` and returns HTTP 400 in `application/problem+json` format
- `ValiValidationFilter<T>` for Minimal API (endpoint filter)
- `ValiValidateAttribute` for MVC controllers (action filter)

```xml
<PackageReference Include="Vali-Validation.AspNetCore" Version="*" />
```

## Compatibility Table

| Package | net7.0 | net8.0 | net9.0 |
|---|---|---|---|
| `Vali-Validation` | ✓ | ✓ | ✓ |
| `Vali-Validation.MediatR` | ✓ | ✓ | ✓ |
| `Vali-Validation.ValiMediator` | ✓ | ✓ | ✓ |
| `Vali-Validation.AspNetCore` | ✓ | ✓ | ✓ |

> The core source code uses C# 11. No C# 12+ features are used (collection expressions `[]`, primary constructors on classes, etc.), making it compatible with the .NET 7 SDK toolchain and above.

## Relationship with Vali-Mediator

Vali-Validation is part of the **Vali** ecosystem, alongside **Vali-Mediator**. Vali-Mediator is a lightweight mediator (similar to MediatR) that uses `Result<T>` as the standard return type to handle errors without exceptions.

The `Vali-Validation.ValiMediator` integration connects both worlds: the validator detects whether the response type is `Result<T>` and, if so, returns the failure instead of throwing an exception. This means handler code never needs a try/catch for validation errors:

```csharp
// Without Vali-Validation.ValiMediator:
public async Task<Result<OrderDto>> Handle(CreateOrderCommand command)
{
    try
    {
        // logic...
    }
    catch (ValidationException ex)
    {
        return Result<OrderDto>.Fail(ex.Message, ErrorType.Validation);
    }
}

// With Vali-Validation.ValiMediator:
public async Task<Result<OrderDto>> Handle(CreateOrderCommand command)
{
    // If validation fails, the behavior returns Result<OrderDto>.Fail(...) automatically.
    // This code never executes if the command is invalid.
    var order = await _orderService.CreateAsync(command);
    return Result<OrderDto>.Ok(new OrderDto(order));
}
```

## Core Concepts

Before diving into the details, there are three key concepts:

### 1. Validator/Model separation

Validators live in separate classes from the model. The model is unaware that a validator exists.

```csharp
// Clean model, no annotations
public class CreateUserRequest
{
    public string Name { get; set; }
    public string Email { get; set; }
    public int Age { get; set; }
}

// Separate validator
public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().Email();
        RuleFor(x => x.Age).GreaterThan(0).LessThan(150);
    }
}
```

### 2. Validation as a pipeline

In real applications, validation happens before a command/query handler processes the request. The integration packages implement this as a pipeline behavior:

```
Request → ValidationBehavior → Handler → Response
              ↓ (failure)
           ValidationException / Result<T>.Fail
```

### 3. ValidationResult as a value

`ValidationResult` is a value object that contains all errors found. Validation always completes (does not throw by default), allowing all errors to be collected in a single pass:

```csharp
var result = await validator.ValidateAsync(request);
if (!result.IsValid)
{
    // result.Errors has ALL errors, not just the first one
    foreach (var (property, errors) in result.Errors)
    {
        Console.WriteLine($"{property}: {string.Join(", ", errors)}");
    }
}
```

## Next Steps

- **[Installation](02-installation.md)** — How to install each package and when to use each one
- **[Quick Start](03-quick-start.md)** — A complete example in minutes
- **[Validators](04-validators.md)** — AbstractValidator in depth
