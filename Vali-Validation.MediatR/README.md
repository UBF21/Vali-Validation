# Vali-Validation.MediatR

[MediatR](https://github.com/jbogard/MediatR) pipeline behavior integration for [Vali-Validation](https://github.com/UBF21/Vali-Validation). Automatically validates `IRequest<T>` objects before they reach their handler.

## Installation

```bash
dotnet add package Vali-Validation.MediatR
```

## Registration

### One-call setup (recommended)

```csharp
builder.Services.AddMediatRWithValidation(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
}, typeof(Program).Assembly);
```

### Manual setup

```csharp
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
});
builder.Services.AddValiValidationBehavior(typeof(Program).Assembly);
```

## How It Works

`ValidationBehavior<TRequest, TResponse>` is registered as an `IPipelineBehavior`. Before the handler executes:

1. Resolves `IValidator<TRequest>` from DI. If not registered, the pipeline continues normally.
2. Calls `ValidateAsync` on the validator.
3. If validation passes, calls the next step in the pipeline.
4. If validation fails, throws `ValidationException` with structured errors (property → messages).

```csharp
// Handler
public class CreateUserHandler : IRequestHandler<CreateUserCommand, UserDto>
{
    public Task<UserDto> Handle(CreateUserCommand request, CancellationToken ct)
        => ...; // only reached if validation passes
}
```

```csharp
// Validator
public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaxLength(100);
        RuleFor(x => x.Email).NotEmpty().Email();
    }
}
```

## Catching Validation Errors

Catch `ValidationException` from the call site or use `ValiValidationMiddleware` from `Vali-Validation.AspNetCore`:

```csharp
try
{
    var result = await mediator.Send(new CreateUserCommand(...));
}
catch (ValidationException ex)
{
    // ex.Errors: Dictionary<string, List<string>>
}
```

## Tip: Result\<T\>-based Flow

If you prefer returning errors as values instead of throwing exceptions, use **Vali-Mediator.Validation** instead. It integrates with `Vali-Mediator` and returns `Result<T>.Fail(ValidationErrors, ErrorType.Validation)` when `TResponse` is `Result<T>`.

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
