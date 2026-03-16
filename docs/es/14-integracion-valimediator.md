# Integración con Vali-Mediator

El paquete `Vali-Validation.ValiMediator` conecta Vali-Validation con Vali-Mediator. A diferencia de la integración con MediatR (que siempre lanza `ValidationException`), este behavior detecta el tipo de retorno del handler y actúa de forma diferente según sea `Result<T>` o no.

## Instalar

```bash
dotnet add package Vali-Validation.ValiMediator
```

Este paquete ya incluye `Vali-Validation` (core) como dependencia transitiva.

---

## La diferencia clave respecto a MediatR

Con **MediatR**, la validación siempre lanza una excepción:

```
Request → ValidationBehavior → falla → throw ValidationException
```

Con **Vali-Mediator**, el behavior conoce el tipo de retorno:

```
Request → ValidationBehavior → falla y TResponse es Result<T>
                                    → return Result<T>.Fail(errors, ErrorType.Validation)
                              → falla y TResponse NO es Result<T>
                                    → throw ValidationException
```

Esto significa que si tu handler devuelve `Result<T>`, **nunca necesitas try/catch para errores de validación**. El fallo llega como un `Result<T>` que puedes manejar en el caller de forma expresiva.

---

## Cómo funciona el pipeline

```
Request (IRequest<Result<ProductDto>>)
  │
  ▼
ValidationBehavior<TRequest, Result<ProductDto>>
  │
  ├─ ¿Hay IValidator<TRequest>?
  │    No → llama a next() sin validar
  │    Sí → ValidateAsync(request)
  │              │
  │              ├─ IsValid → llama a next() → handler
  │              └─ !IsValid → ¿TResponse es Result<T>?
  │                                Sí → Result<ProductDto>.Fail(errors, ErrorType.Validation)
  │                                No → throw ValidationException
  ▼
Handler (solo si es válido)
```

---

## Métodos de registro

### Opción 1: AddValiValidationBehavior (dentro de AddValiMediator)

```csharp
builder.Services.AddValiMediator(config =>
{
    config.RegisterServicesFromAssembly(typeof(Program).Assembly);
    config.AddValiValidationBehavior(); // Agrega el behavior al pipeline
});

// Registra los validadores por separado
builder.Services.AddValidationsFromAssembly(typeof(Program).Assembly);
```

### Opción 2: AddValiMediatorWithValidation (todo en uno)

```csharp
builder.Services.AddValiMediatorWithValidation(
    config => config.RegisterServicesFromAssembly(typeof(Program).Assembly),
    validatorsAssembly: typeof(Program).Assembly,
    lifetime: ServiceLifetime.Transient);
```

Si los validadores están en otro assembly:

```csharp
builder.Services.AddValiMediatorWithValidation(
    config => config.RegisterServicesFromAssembly(ApplicationAssembly.Reference),
    validatorsAssembly: ApplicationAssembly.Reference,
    lifetime: ServiceLifetime.Scoped);
```

---

## Ejemplo completo con Result\<T\>

### Comando y validador

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
                .WithMessage("El ID del cliente es obligatorio.")
                .WithErrorCode("CUSTOMER_ID_REQUIRED")
            .MustAsync(async (id, ct) => await _customers.ExistsAsync(id, ct))
                .WithMessage("El cliente no existe.")
                .WithErrorCode("CUSTOMER_NOT_FOUND")
            .StopOnFirstFailure();

        RuleFor(x => x.Items)
            .NotEmptyCollection()
                .WithMessage("El pedido debe tener al menos un artículo.")
                .WithErrorCode("ITEMS_REQUIRED");

        RuleForEach(x => x.Items)
            .Must(item => item.Quantity > 0)
                .WithMessage("La cantidad debe ser mayor que 0.")
            .MustAsync(async (item, ct) =>
                await _products.IsAvailableAsync(item.ProductId, item.Quantity, ct))
                .WithMessage("El producto no está disponible en la cantidad solicitada.")
                .WithErrorCode("PRODUCT_UNAVAILABLE");

        RuleFor(x => x.ShippingAddressId)
            .NotEmpty()
                .WithMessage("La dirección de envío es obligatoria.");

        RuleFor(x => x.CouponCode)
            .MinimumLength(6)
                .WithMessage("El código de cupón debe tener al menos 6 caracteres.")
            .MaximumLength(20)
            .When(x => x.CouponCode != null);
    }
}
```

### Handler que devuelve Result\<T\>

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
        // Si llegamos aquí, la validación ya pasó.
        // No necesitamos try/catch para errores de validación.

        _logger.LogInformation(
            "Procesando pedido para el cliente {CustomerId} con {ItemCount} artículo(s)",
            command.CustomerId,
            command.Items.Count);

        // Calcular precio total (puede aplicar cupón)
        var total = await _pricing.CalculateAsync(
            command.Items,
            command.CouponCode,
            ct);

        // Verificar reglas de negocio (no son validación de input)
        if (total > 10000 && command.Items.Count > 100)
        {
            return Result<OrderConfirmation>.Fail(
                "Los pedidos de más de 100 artículos no pueden superar los 10,000 €.",
                ErrorType.Validation);
        }

        // Crear el pedido
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

### Endpoint que consume el Result\<T\>

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

### Controlador MVC alternativo

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

## Handlers que NO devuelven Result\<T\>

Si el handler devuelve un tipo simple (no `Result<T>`), el behavior lanza `ValidationException` igual que en MediatR:

```csharp
// Este handler devuelve string, no Result<string>
public record GenerateReportCommand(string ReportType) : IRequest<string>;

public class GenerateReportHandler : IRequestHandler<GenerateReportCommand, string>
{
    public async Task<string> Handle(GenerateReportCommand command, CancellationToken ct)
    {
        return await GenerateAsync(command.ReportType, ct);
    }
}

// En este caso, si la validación falla → throw ValidationException
// Necesitas el middleware UseValiValidationExceptionHandler para capturarla
```

---

## Program.cs completo con Vali-Mediator

```csharp
using Vali_Validation.ValiMediator;

var builder = WebApplication.CreateBuilder(args);

// Infraestructura
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// Vali-Mediator + Validación (todo en uno)
builder.Services.AddValiMediatorWithValidation(
    config => config.RegisterServicesFromAssembly(typeof(Program).Assembly),
    validatorsAssembly: typeof(Program).Assembly,
    lifetime: ServiceLifetime.Scoped); // Scoped porque los validadores usan DbContext

// ASP.NET Core
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Middleware para ValidationException (para handlers que no devuelven Result<T>)
builder.Services.AddValiValidationProblemDetails();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// Captura ValidationException de handlers que no usan Result<T>
app.UseValiValidationExceptionHandler();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

---

## Detección de Result\<T\>

El behavior detecta `Result<T>` comprobando si `TResponse` es un tipo genérico cuyo tipo genérico base es `Result<>`:

```csharp
bool isResultType = typeof(TResponse).IsGenericType &&
    typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>);
```

Esto significa que funciona con cualquier `Result<T>`, independientemente de cuál sea `T`:

- `Result<OrderConfirmation>` → devuelve `Result<OrderConfirmation>.Fail(...)`
- `Result<string>` → devuelve `Result<string>.Fail(...)`
- `Result<List<ProductDto>>` → devuelve `Result<List<ProductDto>>.Fail(...)`

---

## Comparación MediatR vs Vali-Mediator

| Aspecto | MediatR | Vali-Mediator |
|---|---|---|
| Fallo de validación | Lanza `ValidationException` | `Result<T>.Fail` (si devuelve `Result<T>`) |
| Handler necesita try/catch | Sí (o middleware) | No (para handlers con `Result<T>`) |
| Tipo de retorno | Cualquier tipo | `Result<T>` recomendado |
| Middleware necesario | Sí para 400 automático | Solo para handlers sin `Result<T>` |
| Expresividad del error | Excepción | Valor de retorno tipado |

### Patrón de código sin Vali-Mediator (MediatR)

```csharp
// Controller necesita try/catch o hay middleware global
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

### Patrón de código con Vali-Mediator

```csharp
// Controller limpio, maneja Result<T> como valor
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

## Siguientes pasos

- **[Patrones avanzados](15-patrones-avanzados.md)** — Composición, herencia y casos complejos
- **[ASP.NET Core](12-integracion-aspnetcore.md)** — Middleware para handlers sin Result\<T\>
- **[Resultado de validación](09-resultado-validacion.md)** — Detalles de ValidationResult y ErrorCodes
