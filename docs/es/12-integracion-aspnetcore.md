# Integración con ASP.NET Core

El paquete `Vali-Validation.AspNetCore` proporciona tres mecanismos de integración con ASP.NET Core, cada uno pensado para un caso de uso diferente.

## Instalar el paquete

```bash
dotnet add package Vali-Validation.AspNetCore
```

---

## Middleware: UseValiValidationExceptionHandler

El middleware captura todas las `ValidationException` que se propaguen a través del pipeline HTTP y las convierte en una respuesta HTTP 400 con formato `application/problem+json` (RFC 7807).

### Registrar el middleware

```csharp
// Program.cs
app.UseValiValidationExceptionHandler();
```

> **Orden importante:** registra el middleware **antes** de `UseRouting`, `UseAuthentication`, `UseAuthorization` y `MapControllers`/`MapEndpoints`. El middleware debe estar al principio del pipeline para capturar excepciones de toda la cadena.

```csharp
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// Primero el middleware de validación
app.UseValiValidationExceptionHandler();

// Luego el resto del pipeline
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok());

app.Run();
```

### Formato de la respuesta

Cuando una `ValidationException` es capturada, la respuesta tiene este formato:

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Validation Failed",
  "status": 400,
  "errors": {
    "Name": ["El nombre es obligatorio.", "El nombre debe tener al menos 3 caracteres."],
    "Email": ["El email no tiene formato válido."],
    "Price": ["El precio debe ser mayor que 0."]
  }
}
```

El `Content-Type` de la respuesta es `application/problem+json`.

### Cuándo lanza ValidationException

El middleware es útil cuando combinas con `ValidateAndThrowAsync` en la capa de servicio:

```csharp
// Servicio que lanza
public class ProductService
{
    private readonly IValidator<CreateProductRequest> _validator;

    public ProductService(IValidator<CreateProductRequest> validator)
    {
        _validator = validator;
    }

    public async Task<Product> CreateAsync(CreateProductRequest request, CancellationToken ct)
    {
        await _validator.ValidateAndThrowAsync(request, ct);
        // Si llegamos aquí, la validación pasó
        return await _repository.CreateAsync(request, ct);
    }
}

// Controlador que no necesita manejo de errores
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _service;

    public ProductsController(IProductService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductRequest request,
        CancellationToken ct)
    {
        // ProductService.CreateAsync lanza ValidationException si falla la validación
        // El middleware lo captura y devuelve 400 automáticamente
        var product = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }
}
```

### Con MediatR

El patrón es el mismo con MediatR — el behavior lanza `ValidationException` y el middleware la captura:

```csharp
// Handler limpio, sin manejo de errores de validación
public class CreateProductHandler : IRequestHandler<CreateProductCommand, ProductDto>
{
    private readonly IProductRepository _repository;

    public CreateProductHandler(IProductRepository repository)
    {
        _repository = repository;
    }

    public async Task<ProductDto> Handle(
        CreateProductCommand command,
        CancellationToken ct)
    {
        // Si llega aquí, la validación del command ya pasó
        var product = await _repository.CreateAsync(command, ct);
        return new ProductDto(product);
    }
}
```

---

## AddValiValidationProblemDetails

`AddValiValidationProblemDetails` registra el soporte de `ProblemDetails` estándar de ASP.NET Core. Es necesario para que `Results.ValidationProblem()` en Minimal APIs use el formato RFC 7807 correctamente.

```csharp
builder.Services.AddValiValidationProblemDetails();
```

Esta llamada es especialmente útil si también usas `Results.ValidationProblem()` directamente en tus endpoints Minimal API.

---

## ValiValidationFilter\<T\>: endpoint filter para Minimal API

`ValiValidationFilter<T>` es un endpoint filter que valida automáticamente el argumento de tipo `T` antes de ejecutar el handler del endpoint. Si la validación falla, devuelve `Results.ValidationProblem(errors)` sin ejecutar el handler.

### Registrar con WithValiValidation\<T\>

```csharp
// Agrega el filtro al endpoint — el handler no se ejecuta si la validación falla
app.MapPost("/products", async (
    [FromBody] CreateProductRequest request,
    [FromServices] IProductRepository repository,
    CancellationToken ct) =>
{
    // Si llegamos aquí, CreateProductRequest ya fue validado
    var product = await repository.CreateAsync(request, ct);
    return Results.Created($"/products/{product.Id}", product);
})
.WithValiValidation<CreateProductRequest>();
```

### Ventajas sobre la validación manual

```csharp
// Sin WithValiValidation: validación manual en cada endpoint
app.MapPost("/products", async (request, validator, repository, ct) =>
{
    var result = await validator.ValidateAsync(request, ct);
    if (!result.IsValid)
        return Results.ValidationProblem(result.Errors.ToDictionary(
            k => k.Key, v => v.Value.ToArray()));

    var product = await repository.CreateAsync(request, ct);
    return Results.Created($"/products/{product.Id}", product);
});

// Con WithValiValidation: el handler queda limpio
app.MapPost("/products", async (request, repository, ct) =>
{
    var product = await repository.CreateAsync(request, ct);
    return Results.Created($"/products/{product.Id}", product);
})
.WithValiValidation<CreateProductRequest>();
```

### Ejemplo con múltiples endpoints

```csharp
var productGroup = app.MapGroup("/products")
    .WithTags("Products");

productGroup.MapPost("/", async (
    [FromBody] CreateProductRequest request,
    [FromServices] IProductService service,
    CancellationToken ct) =>
{
    var product = await service.CreateAsync(request, ct);
    return Results.Created($"/products/{product.Id}", product);
})
.WithValiValidation<CreateProductRequest>();

productGroup.MapPut("/{id}", async (
    int id,
    [FromBody] UpdateProductRequest request,
    [FromServices] IProductService service,
    CancellationToken ct) =>
{
    var product = await service.UpdateAsync(id, request, ct);
    return Results.Ok(product);
})
.WithValiValidation<UpdateProductRequest>();

productGroup.MapDelete("/{id}", async (
    int id,
    [FromServices] IProductService service,
    CancellationToken ct) =>
{
    await service.DeleteAsync(id, ct);
    return Results.NoContent();
}); // Sin validación: no hay body que validar
```

### Respuesta de error

Cuando la validación falla, el filtro devuelve:

```http
HTTP/1.1 400 Bad Request
Content-Type: application/problem+json

{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Name": ["El nombre es obligatorio."],
    "Price": ["El precio debe ser mayor que 0."]
  }
}
```

### Configurar WithValiValidation sin el paquete AspNetCore

Si no quieres depender del paquete `AspNetCore`, puedes implementar el filtro manualmente:

```csharp
// Sin el paquete: implementación manual equivalente
app.MapPost("/products", async (request, repository, ct) =>
{
    var product = await repository.CreateAsync(request, ct);
    return Results.Created($"/products/{product.Id}", product);
})
.AddEndpointFilter<ValiValidationFilter<CreateProductRequest>>();
```

---

## ValiValidateAttribute: action filter para controladores MVC

`ValiValidateAttribute` es un action filter para controladores MVC. Cuando se aplica a un action o a un controlador, valida automáticamente todos los argumentos de la acción que tengan un `IValidator<T>` registrado en DI. Si alguno falla, devuelve `BadRequestObjectResult` con `ValidationProblemDetails`.

### Aplicar al controlador completo

```csharp
[ApiController]
[Route("api/[controller]")]
[ValiValidate] // Aplica a todas las acciones del controlador
public class ProductsController : ControllerBase
{
    private readonly IProductService _service;

    public ProductsController(IProductService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductRequest request, // Validado automáticamente
        CancellationToken ct)
    {
        // Si llegamos aquí, request es válido
        var product = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateProductRequest request, // Validado automáticamente
        CancellationToken ct)
    {
        var product = await _service.UpdateAsync(id, request, ct);
        return Ok(product);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id) // Sin body, no se valida nada
    {
        var product = await _service.GetByIdAsync(id);
        return product is null ? NotFound() : Ok(product);
    }
}
```

### Aplicar a una acción específica

```csharp
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _service;

    public OrdersController(IOrderService service)
    {
        _service = service;
    }

    [HttpPost]
    [ValiValidate] // Solo esta acción usa el filtro
    public async Task<IActionResult> Create(
        [FromBody] CreateOrderRequest request,
        CancellationToken ct)
    {
        var order = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct)
    {
        // Sin [ValiValidate]: no hay body que validar
        await _service.CancelAsync(id, ct);
        return NoContent();
    }
}
```

### Respuesta de error de ValiValidateAttribute

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "CustomerId": ["El cliente no existe."],
    "Items": ["El pedido debe tener al menos un artículo."]
  }
}
```

---

## Cuándo usar cada mecanismo

| Mecanismo | Cuándo usarlo |
|---|---|
| `UseValiValidationExceptionHandler` | Cuando la validación ocurre en la capa de servicio (con `ValidateAndThrowAsync`) o cuando usas el behavior de MediatR |
| `WithValiValidation<T>` | Endpoints Minimal API donde el handler recibe directamente el body |
| `[ValiValidate]` | Controladores MVC donde el action recibe el body como argumento |

### Mecanismos combinados

Puedes usarlos todos a la vez sin conflictos:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddValidationsFromAssembly(typeof(Program).Assembly);
builder.Services.AddValiValidationProblemDetails(); // Para Minimal API

var app = builder.Build();

// Middleware: captura ValidationException de la capa de servicio
app.UseValiValidationExceptionHandler();

// MVC con [ValiValidate] en los controladores
app.MapControllers();

// Minimal API con WithValiValidation
app.MapPost("/api/quick-order", async (QuickOrderRequest request, IOrderService service) =>
{
    var order = await service.CreateAsync(request);
    return Results.Ok(order);
})
.WithValiValidation<QuickOrderRequest>();
```

---

## Registro completo de Program.cs con ASP.NET Core

```csharp
using Vali_Validation.Core.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Infraestructura
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// Autenticación y autorización
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt => builder.Configuration.Bind("Jwt", opt));
builder.Services.AddAuthorization();

// Validación
builder.Services.AddValidationsFromAssembly(
    typeof(Program).Assembly,
    ServiceLifetime.Scoped);
builder.Services.AddValiValidationProblemDetails();

// ASP.NET Core
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Servicios de aplicación
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IOrderService, OrderService>();

var app = builder.Build();

// Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseValiValidationExceptionHandler(); // Antes de authentication/authorization
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Endpoints adicionales
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
   .AllowAnonymous();

app.Run();
```

---

## Siguientes pasos

- **[MediatR](13-integracion-mediatr.md)** — Behavior de pipeline para MediatR
- **[Vali-Mediator](14-integracion-valimediator.md)** — Behavior de pipeline con Result\<T\>
- **[Inyección de dependencias](11-inyeccion-dependencias.md)** — Registro y resolución de validadores
