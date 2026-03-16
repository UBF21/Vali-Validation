# Vali-Mediator Integration

The `Vali-Validation.ValiMediator` package connects Vali-Validation with Vali-Mediator. Unlike the MediatR integration (which always throws `ValidationException`), this behavior detects the handler's return type and acts differently depending on whether it is `Result<T>` or not.

## Install

```bash
dotnet add package Vali-Validation.ValiMediator
```

This package already includes `Vali-Validation` (core) as a transitive dependency.

---

## The Key Difference from MediatR

With **MediatR**, validation always throws an exception:

```
Request → ValidationBehavior → fails → throw ValidationException
```

With **Vali-Mediator**, the behavior knows the return type:

```
Request → ValidationBehavior → fails and TResponse is Result<T>
                                    → return Result<T>.Fail(errors, ErrorType.Validation)
                              → fails and TResponse is NOT Result<T>
                                    → throw ValidationException
```

This means that if your handler returns `Result<T>`, **you never need try/catch for validation errors**. The failure arrives as a `Result<T>` that you can handle in the caller in an expressive way.

---

## How the Pipeline Works

```
Request (IRequest<Result<ProductDto>>)
  │
  ▼
ValidationBehavior<TRequest, Result<ProductDto>>
  │
  ├─ Is there an IValidator<TRequest>?
  │    No → calls next() without validating
  │    Yes → ValidateAsync(request)
  │              │
  │              ├─ IsValid → calls next() → handler
  │              └─ !IsValid → Is TResponse Result<T>?
  │                                Yes → Result<ProductDto>.Fail(errors, ErrorType.Validation)
  │                                No → throw ValidationException
  ▼
Handler (only if valid)
```

---

## Registration Methods

### Option 1: AddValiValidationBehavior (inside AddValiMediator)

```csharp
builder.Services.AddValiMediator(config =>
{
    config.RegisterServicesFromAssembly(typeof(Program).Assembly);
    config.AddValiValidationBehavior(); // Adds the behavior to the pipeline
});

// Register validators separately
builder.Services.AddValidationsFromAssembly(typeof(Program).Assembly);
```

### Option 2: AddValiMediatorWithValidation (all-in-one)

```csharp
builder.Services.AddValiMediatorWithValidation(
    config => config.RegisterServicesFromAssembly(typeof(Program).Assembly),
    validatorsAssembly: typeof(Program).Assembly,
    lifetime: ServiceLifetime.Transient);
```

If validators are in another assembly:

```csharp
builder.Services.AddValiMediatorWithValidation(
    config => config.RegisterServicesFromAssembly(ApplicationAssembly.Reference),
    validatorsAssembly: ApplicationAssembly.Reference,
    lifetime: ServiceLifetime.Scoped);
```

---

## Complete Example with Result\<T\>

### Command and Validator

```csharp
// Application/Commands/PlaceOrder/PlaceOrderCommand.cs
public record PlaceOrderCommand : IRequest<Result<OrderConfirmation>>
{
    public string CustomerId { get; init; } = string.Empty;
    public List<OrderItem> Items { get; init; } = new();
    public string ShippingAddressId { get; init; } = string.Empty;
    public string? CouponCode { get; init; }
}

public record OrderItem(string ProductId, int Quantity);
public record OrderConfirmation(string OrderId, decimal Total, DateTime EstimatedDelivery);
```

```csharp
// Application/Commands/PlaceOrder/PlaceOrderCommandValidator.cs
public class PlaceOrderCommandValidator : AbstractValidator<PlaceOrderCommand>
{
    private readonly IProductRepository _products;
    private readonly ICustomerRepository _customers;

    public PlaceOrderCommandValidator(
        IProductRepository products,
        ICustomerRepository customers)
    {
        _products = products;
        _customers = customers;

        RuleFor(x => x.CustomerId)
            .NotEmpty()
                .WithMessage("The customer ID is required.")
                .WithErrorCode("CUSTOMER_ID_REQUIRED")
            .MustAsync(async (id, ct) => await _customers.ExistsAsync(id, ct))
                .WithMessage("The customer does not exist.")
                .WithErrorCode("CUSTOMER_NOT_FOUND")
            .StopOnFirstFailure();

        RuleFor(x => x.Items)
            .NotEmptyCollection()
                .WithMessage("The order must have at least one item.")
                .WithErrorCode("ITEMS_REQUIRED");

        RuleForEach(x => x.Items)
            .Must(item => item.Quantity > 0)
                .WithMessage("The quantity must be greater than 0.")
            .MustAsync(async (item, ct) =>
                await _products.IsAvailableAsync(item.ProductId, item.Quantity, ct))
                .WithMessage("The product is not available in the requested quantity.")
                .WithErrorCode("PRODUCT_UNAVAILABLE");

        RuleFor(x => x.ShippingAddressId)
            .NotEmpty()
                .WithMessage("The shipping address is required.");

        RuleFor(x => x.CouponCode)
            .MinimumLength(6)
                .WithMessage("The coupon code must have at least 6 characters.")
            .MaximumLength(20)
            .When(x => x.CouponCode != null);
    }
}
```

### Handler Returning Result\<T\>

```csharp
// Application/Commands/PlaceOrder/PlaceOrderHandler.cs
public class PlaceOrderHandler : IRequestHandler<PlaceOrderCommand, Result<OrderConfirmation>>
{
    private readonly IOrderRepository _orders;
    private readonly IPricingService _pricing;
    private readonly ILogger<PlaceOrderHandler> _logger;

    public PlaceOrderHandler(
        IOrderRepository orders,
        IPricingService pricing,
        ILogger<PlaceOrderHandler> logger)
    {
        _orders = orders;
        _pricing = pricing;
        _logger = logger;
    }

    public async Task<Result<OrderConfirmation>> Handle(
        PlaceOrderCommand command,
        CancellationToken ct)
    {
        // If we get here, validation has already passed.
        // No try/catch needed for validation errors.

        _logger.LogInformation(
            "Processing order for customer {CustomerId} with {ItemCount} item(s)",
            command.CustomerId,
            command.Items.Count);

        // Calculate total price (may apply coupon)
        var total = await _pricing.CalculateAsync(
            command.Items,
            command.CouponCode,
            ct);

        // Check business rules (not input validation)
        if (total > 10000 && command.Items.Count > 100)
        {
            return Result<OrderConfirmation>.Fail(
                "Orders with more than 100 items cannot exceed €10,000.",
                ErrorType.Validation);
        }

        // Create the order
        var order = await _orders.CreateAsync(new CreateOrderSpec
        {
            CustomerId = command.CustomerId,
            Items = command.Items,
            ShippingAddressId = command.ShippingAddressId,
            Total = total
        }, ct);

        var confirmation = new OrderConfirmation(
            order.Id,
            order.Total,
            DateTime.UtcNow.AddDays(3));

        return Result<OrderConfirmation>.Ok(confirmation);
    }
}
```

### Endpoint Consuming the Result\<T\>

```csharp
// Api/Endpoints/OrderEndpoints.cs
app.MapPost("/api/orders", async (
    [FromBody] PlaceOrderCommand command,
    [FromServices] IValiMediator mediator,
    CancellationToken ct) =>
{
    var result = await mediator.Send(command, ct);

    return result.Match(
        onSuccess: confirmation => Results.Created(
            $"/api/orders/{confirmation.OrderId}",
            confirmation),
        onFailure: (error, errorType) => errorType switch
        {
            ErrorType.Validation => Results.BadRequest(new { error }),
            ErrorType.NotFound   => Results.NotFound(new { error }),
            _                    => Results.Problem(error)
        });
})
.WithTags("Orders")
.WithName("PlaceOrder");
```

### Alternative MVC Controller

```csharp
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IValiMediator _mediator;

    public OrdersController(IValiMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [ProducesResponseType(typeof(OrderConfirmation), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Place(
        [FromBody] PlaceOrderCommand command,
        CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);

        if (!result.IsSuccess)
        {
            return result.ErrorType switch
            {
                ErrorType.Validation => BadRequest(new { error = result.Error }),
                ErrorType.NotFound   => NotFound(new { error = result.Error }),
                _                    => Problem(result.Error)
            };
        }

        var confirmation = result.Value;
        return CreatedAtAction(
            nameof(GetById),
            new { id = confirmation.OrderId },
            confirmation);
    }
}
```

---

## Handlers That Do NOT Return Result\<T\>

If the handler returns a simple type (not `Result<T>`), the behavior throws `ValidationException` just like in MediatR:

```csharp
// This handler returns string, not Result<string>
public record GenerateReportCommand(string ReportType) : IRequest<string>;

public class GenerateReportHandler : IRequestHandler<GenerateReportCommand, string>
{
    public async Task<string> Handle(GenerateReportCommand command, CancellationToken ct)
    {
        return await GenerateAsync(command.ReportType, ct);
    }
}

// In this case, if validation fails → throw ValidationException
// You need UseValiValidationExceptionHandler middleware to catch it
```

---

## Complete Program.cs with Vali-Mediator

```csharp
using Vali_Validation.ValiMediator;

var builder = WebApplication.CreateBuilder(args);

// Infrastructure
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// Vali-Mediator + Validation (all-in-one)
builder.Services.AddValiMediatorWithValidation(
    config => config.RegisterServicesFromAssembly(typeof(Program).Assembly),
    validatorsAssembly: typeof(Program).Assembly,
    lifetime: ServiceLifetime.Scoped); // Scoped because validators use DbContext

// ASP.NET Core
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Middleware for ValidationException (for handlers that don't return Result<T>)
builder.Services.AddValiValidationProblemDetails();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// Catches ValidationException from handlers that don't use Result<T>
app.UseValiValidationExceptionHandler();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

---

## Result\<T\> Detection

The behavior detects `Result<T>` by checking if `TResponse` is a generic type whose generic base type is `Result<>`:

```csharp
bool isResultType = typeof(TResponse).IsGenericType &&
    typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>);
```

This means it works with any `Result<T>`, regardless of what `T` is:

- `Result<OrderConfirmation>` → returns `Result<OrderConfirmation>.Fail(...)`
- `Result<string>` → returns `Result<string>.Fail(...)`
- `Result<List<ProductDto>>` → returns `Result<List<ProductDto>>.Fail(...)`

---

## MediatR vs Vali-Mediator Comparison

| Aspect | MediatR | Vali-Mediator |
|---|---|---|
| Validation failure | Throws `ValidationException` | `Result<T>.Fail` (if returns `Result<T>`) |
| Handler needs try/catch | Yes (or middleware) | No (for handlers with `Result<T>`) |
| Return type | Any type | `Result<T>` recommended |
| Middleware required | Yes for automatic 400 | Only for handlers without `Result<T>` |
| Error expressiveness | Exception | Typed return value |

### Code Pattern Without Vali-Mediator (MediatR)

```csharp
// Controller needs try/catch or there is a global middleware
[HttpPost]
public async Task<IActionResult> Create([FromBody] CreateProductCommand command)
{
    try
    {
        var product = await _mediator.Send(command);
        return CreatedAtAction(...);
    }
    catch (ValidationException ex)
    {
        return BadRequest(ex.ValidationResult.Errors);
    }
}
```

### Code Pattern with Vali-Mediator

```csharp
// Clean controller, handles Result<T> as a value
[HttpPost]
public async Task<IActionResult> Create([FromBody] PlaceOrderCommand command)
{
    var result = await _mediator.Send(command);
    return result.IsSuccess
        ? CreatedAtAction(nameof(GetById), new { id = result.Value.OrderId }, result.Value)
        : BadRequest(new { error = result.Error });
}
```

---

## Next Steps

- **[Advanced Patterns](15-advanced-patterns.md)** — Composition, inheritance and complex cases
- **[ASP.NET Core](12-aspnetcore-integration.md)** — Middleware for handlers without Result\<T\>
- **[Validation Result](09-validation-result.md)** — Details of ValidationResult and ErrorCodes
