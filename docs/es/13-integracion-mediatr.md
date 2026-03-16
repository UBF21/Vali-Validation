# Integración con MediatR

El paquete `Vali-Validation.MediatR` registra un `IPipelineBehavior<TRequest, TResponse>` que valida automáticamente el request antes de que llegue al handler. Si la validación falla, lanza `ValidationException`.

## Instalar

```bash
dotnet add package Vali-Validation.MediatR
dotnet add package MediatR
```

El paquete `Vali-Validation.MediatR` ya depende de `Vali-Validation` (core), así que no necesitas instalarlo por separado en el proyecto de API.

---

## Cómo funciona el pipeline

```
Request
  │
  ▼
ValidationBehavior<TRequest, TResponse>
  │
  ├─ ¿Hay IValidator<TRequest> registrado?
  │    No  → llama a next() sin validar
  │    Sí  → llama a ValidateAsync(request)
  │              │
  │              ├─ IsValid → llama a next()
  │              └─ !IsValid → lanza ValidationException
  ▼
Handler
```

El behavior hace lo siguiente:

1. Intenta resolver `IValidator<TRequest>` del contenedor DI.
2. Si no hay validador registrado, el request pasa directamente al handler (no-op).
3. Si hay validador, ejecuta `ValidateAsync(request)`.
4. Si `!result.IsValid`, lanza `ValidationException` con todos los errores.
5. Si es válido, llama a `next()` y el handler se ejecuta.

---

## Métodos de registro

### Opción 1: AddValiValidationBehavior (solo el behavior)

```csharp
// Solo registra el behavior — tú registras MediatR y los validadores por separado
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
builder.Services.AddValidationsFromAssembly(assembly);
builder.Services.AddValiValidationBehavior(); // Agrega el behavior al pipeline
```

### Opción 2: AddMediatRWithValidation (todo en uno)

```csharp
// Registra MediatR, el behavior y los validadores del assembly en una sola llamada
builder.Services.AddMediatRWithValidation(
    cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly),
    validatorsAssembly: typeof(Program).Assembly,
    lifetime: ServiceLifetime.Transient);
```

Si los validadores están en un assembly diferente al de los handlers:

```csharp
builder.Services.AddMediatRWithValidation(
    cfg => cfg.RegisterServicesFromAssembly(typeof(ApplicationAssembly).Assembly),
    validatorsAssembly: typeof(ApplicationAssembly).Assembly,
    lifetime: ServiceLifetime.Scoped); // Scoped si los validadores usan DbContext
```

---

## Ejemplo completo

### Modelo y command

```csharp
// Application/Commands/CreateProduct/CreateProductCommand.cs
public record CreateProductCommand : IRequest<ProductDto>
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public int Stock { get; init; }
    public string Category { get; init; } = string.Empty;
}

public record ProductDto(int Id, string Name, decimal Price, string Category);
```

### Validador

```csharp
// Application/Commands/CreateProduct/CreateProductCommandValidator.cs
using Vali_Validation.Core.Validators;

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    private static readonly string[] ValidCategories =
        new[] { "Electronics", "Clothing", "Books", "Food", "Other" };

    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
                .WithMessage("El nombre del producto es obligatorio.")
                .WithErrorCode("NAME_REQUIRED")
            .MinimumLength(3)
                .WithMessage("El nombre debe tener al menos 3 caracteres.")
            .MaximumLength(200)
                .WithMessage("El nombre no puede superar los 200 caracteres.");

        RuleFor(x => x.Description)
            .MaximumLength(1000)
                .WithMessage("La descripción no puede superar los 1000 caracteres.");

        RuleFor(x => x.Price)
            .GreaterThan(0m)
                .WithMessage("El precio debe ser mayor que 0.")
                .WithErrorCode("PRICE_INVALID")
            .MaxDecimalPlaces(2)
                .WithMessage("El precio no puede tener más de 2 decimales.");

        RuleFor(x => x.Stock)
            .GreaterThanOrEqualTo(0)
                .WithMessage("El stock no puede ser negativo.");

        RuleFor(x => x.Category)
            .NotEmpty()
                .WithMessage("La categoría es obligatoria.")
            .In(ValidCategories)
                .WithMessage($"La categoría debe ser una de: {string.Join(", ", ValidCategories)}.");
    }
}
```

### Handler

```csharp
// Application/Commands/CreateProduct/CreateProductHandler.cs
public class CreateProductHandler : IRequestHandler<CreateProductCommand, ProductDto>
{
    private readonly IProductRepository _repository;
    private readonly ILogger<CreateProductHandler> _logger;

    public CreateProductHandler(IProductRepository repository, ILogger<CreateProductHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<ProductDto> Handle(
        CreateProductCommand command,
        CancellationToken cancellationToken)
    {
        // Si llegamos aquí, la validación ya pasó en el behavior
        // No necesitamos validar manualmente
        _logger.LogInformation("Creando producto: {Name}", command.Name);

        var product = new Product
        {
            Name = command.Name,
            Description = command.Description,
            Price = command.Price,
            Stock = command.Stock,
            Category = command.Category
        };

        var created = await _repository.CreateAsync(product, cancellationToken);

        return new ProductDto(created.Id, created.Name, created.Price, created.Category);
    }
}
```

### Controlador

```csharp
// Api/Controllers/ProductsController.cs
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductCommand command,
        CancellationToken ct)
    {
        // El behavior valida antes de llegar al handler.
        // Si la validación falla → ValidationException → middleware devuelve 400.
        // Si la validación pasa → handler devuelve ProductDto.
        var product = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var product = await _mediator.Send(new GetProductByIdQuery(id), ct);
        return product is null ? NotFound() : Ok(product);
    }
}
```

### Program.cs

```csharp
using Vali_Validation.Core.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Infraestructura
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// MediatR + Validación (todo en uno)
builder.Services.AddMediatRWithValidation(
    cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly),
    validatorsAssembly: typeof(Program).Assembly,
    lifetime: ServiceLifetime.Transient);

// ASP.NET Core
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseValiValidationExceptionHandler(); // Captura ValidationException del behavior
app.MapControllers();

app.Run();
```

---

## Comportamiento cuando no hay validador

Si envías un command o query sin validador registrado, el behavior lo deja pasar sin error:

```csharp
// Esta query no tiene validador
public record GetAllProductsQuery : IRequest<List<ProductDto>>;

// El behavior no hace nada, el handler se ejecuta directamente
var products = await _mediator.Send(new GetAllProductsQuery());
```

Esto es intencional: no es obligatorio tener un validador para cada request. Solo los requests que lo necesiten.

---

## Requests con múltiples validadores

Si por alguna razón registras varios validadores para el mismo tipo, el behavior usa el primero que resuelve el contenedor. Para evitar ambigüedad, usa `AddValidationsFromAssembly` que garantiza una sola implementación por tipo.

---

## CancellationToken en el behavior

El behavior pasa el `CancellationToken` del pipeline a `ValidateAsync`, por lo que si el cliente cancela la petición, la validación asíncrona también se cancela:

```csharp
// En el validador, el CT del behavior llega aquí:
RuleFor(x => x.Email)
    .MustAsync(async (email, ct) =>
    {
        // Si el cliente cancela, ct está cancelado y la query lanza OperationCanceledException
        return !await _users.EmailExistsAsync(email, ct);
    });
```

---

## Flujo de error completo

```
POST /api/products
Body: { "name": "", "price": -5, "category": "Unknown" }

→ MediatR pipeline
→ ValidationBehavior<CreateProductCommand, ProductDto>
    → ValidateAsync(command)
    → result.IsValid = false
    → throw new ValidationException(result)
→ UseValiValidationExceptionHandler middleware
    → Response 400:
       {
         "type": "https://tools.ietf.org/html/rfc7807",
         "title": "Validation Failed",
         "status": 400,
         "errors": {
           "Name": ["El nombre del producto es obligatorio."],
           "Price": ["El precio debe ser mayor que 0."],
           "Category": ["La categoría debe ser una de: Electronics, Clothing, Books, Food, Other."]
         }
       }
```

---

## Testing del validador con MediatR

```csharp
public class CreateProductCommandValidatorTests
{
    private readonly CreateProductCommandValidator _validator = new();

    [Fact]
    public async Task EmptyName_ShouldFail()
    {
        var command = new CreateProductCommand { Name = "", Price = 10, Category = "Books" };
        var result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor("Name"));
    }

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var command = new CreateProductCommand
        {
            Name = "Clean Code",
            Price = 29.99m,
            Stock = 100,
            Category = "Books"
        };

        var result = await _validator.ValidateAsync(command);
        Assert.True(result.IsValid);
    }
}
```

---

## Siguientes pasos

- **[Vali-Mediator](14-integracion-valimediator.md)** — Alternativa con Result\<T\> en lugar de excepciones
- **[ASP.NET Core](12-integracion-aspnetcore.md)** — Middleware que captura la excepción del behavior
