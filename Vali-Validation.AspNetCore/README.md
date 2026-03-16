# Vali-Validation.AspNetCore

ASP.NET Core integration for [Vali-Validation](https://github.com/UBF21/Vali-Validation). Provides middleware, endpoint filters, and action filter attributes for automatic HTTP request validation.

## Installation

```bash
dotnet add package Vali-Validation.AspNetCore
```

## Features

- **ValiValidationMiddleware** — catches `ValidationException` and returns an RFC 7807 `problem+json` HTTP 400 response.
- **ValiValidationFilter\<T\>** — Minimal API `IEndpointFilter` that validates a request DTO before the endpoint executes.
- **ValiValidateAttribute** — MVC `IAsyncActionFilter` attribute for automatic controller action validation.

## ValiValidationMiddleware

Catches any `ValidationException` thrown anywhere in the pipeline and returns a structured 400 response.

```csharp
// Program.cs
app.UseValiValidationExceptionHandler(); // place before routing/controllers
```

Optional DI helper for ProblemDetails:

```csharp
builder.Services.AddValiValidationProblemDetails();
```

## ValiValidationFilter\<T\> — Minimal API

Validates a bound argument of type `T` before the endpoint executes. If no `IValidator<T>` is registered, the filter is a no-op.

```csharp
// Using the fluent extension (recommended)
app.MapPost("/users", (CreateUserDto dto) => Results.Ok())
   .WithValiValidation<CreateUserDto>();

// Or explicitly
app.MapPost("/users", (CreateUserDto dto) => Results.Ok())
   .AddEndpointFilter<ValiValidationFilter<CreateUserDto>>();
```

Validators must be registered in DI:

```csharp
builder.Services.AddScoped<IValidator<CreateUserDto>, CreateUserDtoValidator>();
// or using the bulk-registration helper from Vali-Validation:
builder.Services.AddValidationsFromAssembly(typeof(CreateUserDtoValidator).Assembly);
```

## ValiValidateAttribute — MVC Controllers

Apply to a controller or a single action to validate all incoming arguments automatically.

```csharp
[ApiController]
[ValiValidate]
public class UsersController : ControllerBase
{
    [HttpPost]
    public IActionResult Create(CreateUserDto dto) => Ok();
}
```

Or per action:

```csharp
[HttpPost]
[ValiValidate]
public IActionResult Create(CreateUserDto dto) => Ok();
```

## Validation Error Response Format (RFC 7807)

When validation fails, the response is HTTP 400 with `Content-Type: application/problem+json`:

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Name": ["'Name' must not be empty."],
    "Email": ["'Email' is not a valid email address."],
    "Age": ["'Age' must be greater than 0."]
  }
}
```

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
