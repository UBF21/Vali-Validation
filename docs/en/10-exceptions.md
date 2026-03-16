# Validation Exceptions

## ValidationException

`ValidationException` is the typed exception thrown when validation fails. It contains the complete `ValidationResult` with all errors.

```csharp
public sealed class ValidationException : Exception
{
    public ValidationResult ValidationResult { get; }

    public ValidationException(ValidationResult result)
        : base("Validation failed. See ValidationResult for details.")
    {
        ValidationResult = result;
    }
}
```

### Accessing Errors from the Exception

```csharp
try
{
    await validator.ValidateAndThrowAsync(request);
}
catch (ValidationException ex)
{
    // All errors available in ex.ValidationResult
    foreach (var (property, errors) in ex.ValidationResult.Errors)
    {
        Console.WriteLine($"{property}: {string.Join(", ", errors)}");
    }

    // Or as a flat list
    var flatErrors = ex.ValidationResult.ToFlatList();
    Console.WriteLine(string.Join("\n", flatErrors));

    // Or checking specific properties
    if (ex.ValidationResult.HasErrorFor("Email"))
    {
        var emailErrors = ex.ValidationResult.ErrorsFor("Email");
    }
}
```

---

## ValidateAndThrow vs ValidateAsync + Manual Check

There are two approaches for handling validation in your code:

### Approach 1: Value Result

```csharp
var result = await validator.ValidateAsync(request, ct);
if (!result.IsValid)
{
    // Handle the error without an exception
    return new ServiceResponse
    {
        Success = false,
        Errors = result.Errors
    };
}
// Continue with business logic
```

### Approach 2: Exception

```csharp
// Throws ValidationException if !result.IsValid
await validator.ValidateAndThrowAsync(request, ct);
// If we get here, validation passed
// Continue with business logic
```

---

## When to Use Each Approach

### Use the value result when:

1. **You use Vali-Mediator with `Result<T>`** — The pipeline behavior already handles this; in the handler you never need try/catch.
2. **You are in a presentation/endpoint layer** that wants to return a 400 with the error body directly.
3. **Validation is optional or partial** — for example, validating only some fields of a multi-step form.
4. **You need to combine multiple results** — using `result.Merge()`.

```csharp
// Example: Minimal API endpoint that returns 400 with errors
app.MapPost("/products", async (
    CreateProductRequest request,
    IValidator<CreateProductRequest> validator) =>
{
    var result = await validator.ValidateAsync(request);
    if (!result.IsValid)
        return Results.ValidationProblem(result.Errors.ToDictionary(
            k => k.Key, v => v.Value.ToArray()));

    // ...
    return Results.Created("/products/1", request);
});
```

### Use ValidateAndThrow when:

1. **You are in a service layer** that cannot return an HTTP result directly.
2. **You use MediatR** with the exception behavior (the ASP.NET Core middleware catches the exception and returns 400).
3. **The function contract guarantees valid input** — throwing simplifies the caller code.
4. **You want the "fail fast" pattern** in an operation that should not continue with invalid data.

```csharp
// Example: service layer
public class ProductService
{
    private readonly IValidator<CreateProductRequest> _validator;

    public ProductService(IValidator<CreateProductRequest> validator)
    {
        _validator = validator;
    }

    public async Task<Product> CreateProductAsync(
        CreateProductRequest request,
        CancellationToken ct = default)
    {
        // Fail fast if the request is invalid
        await _validator.ValidateAndThrowAsync(request, ct);

        // From here, we know the request is valid
        var product = new Product
        {
            Name = request.Name,
            Price = request.Price
        };

        return await _repository.SaveAsync(product, ct);
    }
}
```

---

## ValidateAndThrow (synchronous)

```csharp
// Only executes synchronous rules
validator.ValidateAndThrow(request);
```

> Use it only if you are certain there are no async rules in the validator. In ASP.NET Core applications, always prefer `ValidateAndThrowAsync`.

---

## ValidateAndThrowAsync

```csharp
// Without CancellationToken
await validator.ValidateAndThrowAsync(request);

// With CancellationToken (recommended)
await validator.ValidateAndThrowAsync(request, cancellationToken);
```

### Example in MVC Controller

```csharp
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateOrderRequest request,
        CancellationToken ct)
    {
        // The UseValiValidationExceptionHandler middleware catches ValidationException
        // and automatically returns HTTP 400 with application/problem+json
        var order = await _orderService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }
}

// The service throws if invalid
public class OrderService
{
    private readonly IValidator<CreateOrderRequest> _validator;

    public OrderService(IValidator<CreateOrderRequest> validator)
    {
        _validator = validator;
    }

    public async Task<Order> CreateAsync(CreateOrderRequest request, CancellationToken ct)
    {
        await _validator.ValidateAndThrowAsync(request, ct);
        // ...
    }
}
```

---

## Catching ValidationException Manually

If you do not use the ASP.NET Core middleware, you can catch the exception manually:

```csharp
[HttpPost]
public async Task<IActionResult> CreateProduct(
    [FromBody] CreateProductRequest request,
    [FromServices] IProductService productService)
{
    try
    {
        var product = await productService.CreateProductAsync(request, HttpContext.RequestAborted);
        return CreatedAtAction(nameof(Get), new { id = product.Id }, product);
    }
    catch (ValidationException ex)
    {
        return BadRequest(new
        {
            title = "Validation Failed",
            errors = ex.ValidationResult.Errors
        });
    }
}
```

---

## ValidationException in the ASP.NET Core Middleware

The `Vali-Validation.AspNetCore` package includes a middleware that automatically catches `ValidationException` and returns HTTP 400 with RFC 7807 format (`application/problem+json`):

```csharp
// Program.cs
app.UseValiValidationExceptionHandler();
```

When a `ValidationException` is thrown, the middleware produces:

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Validation Failed",
  "status": 400,
  "errors": {
    "Email": ["The email is not valid."],
    "Password": ["The password must have at least 8 characters."]
  }
}
```

See [ASP.NET Core Integration](12-aspnetcore-integration.md) for more details.

---

## ValidationException vs Other Exception Types

`ValidationException` is for validation errors (incorrect user input). Do not use it for business logic errors or system errors:

```csharp
// Correct: ValidationException for invalid input
if (!result.IsValid)
    throw new ValidationException(result);

// Incorrect: do not use ValidationException for business or system errors
// throw new ValidationException(...); // For "the product is discontinued"
// throw new ValidationException(...); // For "database unavailable"

// Correct for business logic: domain exception
if (product.IsDiscontinued)
    throw new DomainException("The product is discontinued.");

// Correct for system errors: standard exception
if (connection.State != ConnectionState.Open)
    throw new InvalidOperationException("The database connection is not available.");
```

---

## Recommended Pattern by Project Type

### REST API with MediatR

```
Request → ValidationBehavior (throws ValidationException) → Middleware (catches, returns 400)
```

```csharp
// Program.cs
app.UseValiValidationExceptionHandler(); // Catches ValidationException globally
services.AddMediatRWithValidation(cfg => cfg.RegisterServicesFromAssembly(assembly), assembly);
// No try/catch needed in handlers
```

### REST API with Vali-Mediator (Result<T>)

```
Request → ValidationBehavior → If fails: Result<T>.Fail without exception
```

```csharp
// Program.cs
services.AddValiMediatorWithValidation(config => config.RegisterServicesFromAssembly(assembly), assembly);
// No exception middleware needed for validation
// The handler receives Result<T>.Fail if validation fails
```

### Application Service Without Mediator

```csharp
public class UserService
{
    private readonly IValidator<CreateUserRequest> _validator;

    public UserService(IValidator<CreateUserRequest> validator)
    {
        _validator = validator;
    }

    // Option A: returns ValidationResult (does not throw)
    public async Task<(User? user, ValidationResult validation)> TryCreateAsync(
        CreateUserRequest request, CancellationToken ct)
    {
        var result = await _validator.ValidateAsync(request, ct);
        if (!result.IsValid)
            return (null, result);

        var user = await CreateUserInternalAsync(request, ct);
        return (user, result);
    }

    // Option B: throws ValidationException
    public async Task<User> CreateAsync(CreateUserRequest request, CancellationToken ct)
    {
        await _validator.ValidateAndThrowAsync(request, ct);
        return await CreateUserInternalAsync(request, ct);
    }

    private async Task<User> CreateUserInternalAsync(CreateUserRequest request, CancellationToken ct)
    {
        // Implementation...
        return new User();
    }
}
```

---

## Next Steps

- **[Dependency Injection](11-dependency-injection.md)** — How to register and resolve validators
- **[ASP.NET Core](12-aspnetcore-integration.md)** — Middleware that catches ValidationException
- **[MediatR](13-mediatr-integration.md)** — Pipeline behavior that throws ValidationException
