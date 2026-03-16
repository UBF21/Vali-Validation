# Excepciones de validación

## ValidationException

`ValidationException` es la excepción tipada que se lanza cuando una validación falla. Contiene el `ValidationResult` completo con todos los errores.

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

### Acceder a los errores desde la excepción

```csharp
try
{
    await validator.ValidateAndThrowAsync(request);
}
catch (ValidationException ex)
{
    // Todos los errores disponibles en ex.ValidationResult
    foreach (var (property, errors) in ex.ValidationResult.Errors)
    {
        Console.WriteLine($"{property}: {string.Join(", ", errors)}");
    }

    // O como lista plana
    var flatErrors = ex.ValidationResult.ToFlatList();
    Console.WriteLine(string.Join("\n", flatErrors));

    // O comprobar propiedades específicas
    if (ex.ValidationResult.HasErrorFor("Email"))
    {
        var emailErrors = ex.ValidationResult.ErrorsFor("Email");
    }
}
```

---

## ValidateAndThrow vs ValidateAsync + comprobación manual

Hay dos enfoques para manejar la validación en tu código:

### Enfoque 1: resultado de valor

```csharp
var result = await validator.ValidateAsync(request, ct);
if (!result.IsValid)
{
    // Manejar el error sin excepción
    return new ServiceResponse
    {
        Success = false,
        Errors = result.Errors
    };
}
// Continuar con la lógica de negocio
```

### Enfoque 2: excepción

```csharp
// Lanza ValidationException si !result.IsValid
await validator.ValidateAndThrowAsync(request, ct);
// Si llegamos aquí, la validación pasó
// Continuar con la lógica de negocio
```

---

## Cuándo usar cada enfoque

### Usa el resultado de valor cuando:

1. **Usas Vali-Mediator con `Result<T>`** — El behavior de pipeline ya maneja esto; en el handler nunca necesitas try/catch.
2. **Estás en una capa de presentación/endpoint** que quiere devolver un 400 con el cuerpo de errores directamente.
3. **La validación es opcional o parcial** — por ejemplo, validar solo algunos campos de un formulario multi-paso.
4. **Necesitas combinar múltiples resultados** — usando `result.Merge()`.

```csharp
// Ejemplo: endpoint Minimal API que devuelve 400 con errores
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

### Usa ValidateAndThrow cuando:

1. **Estás en una capa de servicio** que no puede devolver un resultado HTTP directamente.
2. **Usas MediatR** con el behavior de excepción (el middleware de ASP.NET Core captura la excepción y devuelve 400).
3. **El contrato de la función garantiza un input válido** — lanzar simplifica el código del caller.
4. **Quieres el patrón "fail fast"** en una operación que no debería continuar con datos inválidos.

```csharp
// Ejemplo: capa de servicio
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
        // Falla rápido si el request es inválido
        await _validator.ValidateAndThrowAsync(request, ct);

        // A partir de aquí, sabemos que el request es válido
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

## ValidateAndThrow (síncrono)

```csharp
// Solo ejecuta reglas síncronas
validator.ValidateAndThrow(request);
```

> Úsalo solo si tienes certeza de que no hay reglas asíncronas en el validador. En aplicaciones ASP.NET Core, siempre prefiere `ValidateAndThrowAsync`.

---

## ValidateAndThrowAsync

```csharp
// Sin CancellationToken
await validator.ValidateAndThrowAsync(request);

// Con CancellationToken (recomendado)
await validator.ValidateAndThrowAsync(request, cancellationToken);
```

### Ejemplo en controlador MVC

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
        // El middleware UseValiValidationExceptionHandler captura ValidationException
        // y devuelve automáticamente HTTP 400 con application/problem+json
        var order = await _orderService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }
}

// El servicio lanza si es inválido
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

## Capturar ValidationException manualmente

Si no usas el middleware de ASP.NET Core, puedes capturar la excepción manualmente:

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

## ValidationException en el middleware de ASP.NET Core

El paquete `Vali-Validation.AspNetCore` incluye un middleware que captura automáticamente `ValidationException` y devuelve HTTP 400 con formato RFC 7807 (`application/problem+json`):

```csharp
// Program.cs
app.UseValiValidationExceptionHandler();
```

Cuando se lanza `ValidationException`, el middleware produce:

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Validation Failed",
  "status": 400,
  "errors": {
    "Email": ["El email no es válido."],
    "Password": ["La contraseña debe tener al menos 8 caracteres."]
  }
}
```

Ver [Integración con ASP.NET Core](12-integracion-aspnetcore.md) para más detalles.

---

## ValidationException vs otros tipos de excepción

`ValidationException` es para errores de validación (input del usuario incorrecto). No la uses para errores de lógica de negocio o errores de sistema:

```csharp
// Correcto: ValidationException para input inválido
if (!result.IsValid)
    throw new ValidationException(result);

// Incorrecto: no uses ValidationException para errores de negocio o sistema
// throw new ValidationException(...); // Para "el producto está descontinuado"
// throw new ValidationException(...); // Para "base de datos no disponible"

// Correcto para lógica de negocio: excepción de dominio
if (product.IsDiscontinued)
    throw new DomainException("El producto está descontinuado.");

// Correcto para errores de sistema: excepción estándar
if (connection.State != ConnectionState.Open)
    throw new InvalidOperationException("La conexión a la base de datos no está disponible.");
```

---

## Patrón recomendado por tipo de proyecto

### API REST con MediatR

```
Request → ValidationBehavior (lanza ValidationException) → Middleware (captura, devuelve 400)
```

```csharp
// Program.cs
app.UseValiValidationExceptionHandler(); // Captura ValidationException globalmente
services.AddMediatRWithValidation(cfg => cfg.RegisterServicesFromAssembly(assembly), assembly);
// No necesitas try/catch en los handlers
```

### API REST con Vali-Mediator (Result<T>)

```
Request → ValidationBehavior → Si falla: Result<T>.Fail sin excepción
```

```csharp
// Program.cs
services.AddValiMediatorWithValidation(config => config.RegisterServicesFromAssembly(assembly), assembly);
// No necesitas middleware de excepción para validación
// El handler recibe Result<T>.Fail si la validación falla
```

### Servicio de aplicación sin mediator

```csharp
public class UserService
{
    private readonly IValidator<CreateUserRequest> _validator;

    public UserService(IValidator<CreateUserRequest> validator)
    {
        _validator = validator;
    }

    // Opción A: devuelve ValidationResult (no lanza)
    public async Task<(User? user, ValidationResult validation)> TryCreateAsync(
        CreateUserRequest request, CancellationToken ct)
    {
        var result = await _validator.ValidateAsync(request, ct);
        if (!result.IsValid)
            return (null, result);

        var user = await CreateUserInternalAsync(request, ct);
        return (user, result);
    }

    // Opción B: lanza ValidationException
    public async Task<User> CreateAsync(CreateUserRequest request, CancellationToken ct)
    {
        await _validator.ValidateAndThrowAsync(request, ct);
        return await CreateUserInternalAsync(request, ct);
    }

    private async Task<User> CreateUserInternalAsync(CreateUserRequest request, CancellationToken ct)
    {
        // Implementación...
        return new User();
    }
}
```

---

## Siguientes pasos

- **[Inyección de dependencias](11-inyeccion-dependencias.md)** — Cómo registrar y resolver validadores
- **[ASP.NET Core](12-integracion-aspnetcore.md)** — Middleware que captura ValidationException
- **[MediatR](13-integracion-mediatr.md)** — Pipeline behavior que lanza ValidationException
