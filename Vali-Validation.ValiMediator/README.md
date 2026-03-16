# Vali-Validation.ValiMediator

[Vali-Mediator](https://github.com/UBF21/Vali-Mediator) pipeline behavior integration for [Vali-Validation](https://github.com/UBF21/Vali-Validation). Automatically validates `IRequest<T>` objects before they reach their handler, with first-class support for `Result<T>`-based flows — no exceptions needed.

## Installation

```bash
dotnet add package Vali-Validation.ValiMediator
```

## Registration

### One-call setup (recommended)

```csharp
builder.Services.AddValiMediatorWithValidation(
    config => config.RegisterServicesFromAssembly(typeof(Program).Assembly),
    typeof(Program).Assembly);
```

### Manual setup

```csharp
builder.Services.AddValiMediator(config =>
{
    config.RegisterServicesFromAssembly(typeof(Program).Assembly);
    config.AddValiValidationBehavior();
});
builder.Services.AddValidationsFromAssembly(typeof(Program).Assembly);
```

## How It Works

`ValidationBehavior<TRequest, TResponse>` is registered as an `IPipelineBehavior`. Before the handler executes:

1. Resolves `IValidator<TRequest>` from DI. If not registered, the pipeline continues normally.
2. Calls `ValidateAsync` on the validator.
3. If validation passes, calls the next step in the pipeline.
4. If validation fails:
   - When `TResponse` is `Result<T>` → returns `Result<T>.Fail(errors, ErrorType.Validation)` (no throw).
   - When `TResponse` is `Result` (void) → returns `Result.Fail(errors, ErrorType.Validation)` (no throw).
   - Otherwise → throws `ValidationException`.

## Usage Example

```csharp
// Command
public record CreateUserCommand(string Name, string Email) : IRequest<Result<UserDto>>;

// Validator
public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaxLength(100);
        RuleFor(x => x.Email).NotEmpty().Email();
    }
}

// Handler — only reached if validation passes
public class CreateUserHandler : IRequestHandler<CreateUserCommand, Result<UserDto>>
{
    public Task<Result<UserDto>> Handle(CreateUserCommand request, CancellationToken ct)
        => Task.FromResult(Result<UserDto>.Ok(new UserDto(request.Name, request.Email)));
}
```

```csharp
// Call site — no try/catch needed
var result = await mediator.Send(new CreateUserCommand("", "not-an-email"));

result.Match(
    onSuccess: dto => Console.WriteLine($"Created: {dto.Name}"),
    onFailure: (errors, type) => Console.WriteLine($"Validation failed: {errors}")
);
```

## Validation Errors Response Format

When `TResponse` is `Result<T>`, errors are returned as `Dictionary<string, List<string>>` inside the `Result`:

```json
{
  "Name": ["'Name' must not be empty."],
  "Email": ["'Email' is not a valid email address."]
}
```

## Tip: Exception-based Flow

If you use MediatR instead of Vali-Mediator, use **Vali-Validation.MediatR** — it throws `ValidationException` on failure and integrates with the standard `IPipelineBehavior`.

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
