# Resultado de validación

`ValidationResult` es el objeto que devuelven `Validate()` y `ValidateAsync()`. Contiene todos los errores encontrados durante la validación, agrupados por nombre de propiedad.

---

## Estructura

```csharp
public class ValidationResult
{
    // Errores por propiedad: { "Email": ["No válido", "Ya existe"] }
    public Dictionary<string, List<string>> Errors { get; }

    // Códigos de error por propiedad: { "Email": ["INVALID_FORMAT", "ALREADY_EXISTS"] }
    public Dictionary<string, List<string>> ErrorCodes { get; }

    // true si no hay errores
    public bool IsValid { get; }

    // Número total de mensajes de error (suma de todos los errores de todas las propiedades)
    public int ErrorCount { get; }

    // Nombres de propiedades que tienen errores
    public IReadOnlyList<string> PropertyNames { get; }
}
```

---

## IsValid y ErrorCount

```csharp
var result = await validator.ValidateAsync(request);

if (!result.IsValid)
{
    Console.WriteLine($"Validación fallida con {result.ErrorCount} error(es).");
}

// IsValid equivale a: result.Errors.Count == 0
// ErrorCount es la suma total: si Email tiene 2 errores y Name tiene 1, ErrorCount es 3
```

---

## Acceder a los errores

### Por propiedad directamente

```csharp
if (result.Errors.TryGetValue("Email", out var emailErrors))
{
    foreach (var error in emailErrors)
        Console.WriteLine($"Email: {error}");
}
```

### Iterar todos los errores

```csharp
foreach (var (propertyName, errors) in result.Errors)
{
    foreach (var error in errors)
    {
        Console.WriteLine($"{propertyName}: {error}");
    }
}
```

### Comprobar si una propiedad tiene errores

```csharp
bool hasEmailError = result.HasErrorFor("Email");
bool hasNameError = result.HasErrorFor("Name");
```

### Obtener errores de una propiedad

```csharp
// Devuelve List<string> vacío si no hay errores para esa propiedad
List<string> emailErrors = result.ErrorsFor("Email");

if (emailErrors.Count > 0)
{
    // ...
}
```

### Obtener el primer error de una propiedad

```csharp
// Devuelve null si no hay errores para esa propiedad
string? firstError = result.FirstError("Email");

if (firstError != null)
{
    Console.WriteLine($"Primer error de Email: {firstError}");
}
```

### PropertyNames

```csharp
// Lista de propiedades que tienen al menos un error
IReadOnlyList<string> failedProperties = result.PropertyNames;

Console.WriteLine($"Propiedades con error: {string.Join(", ", failedProperties)}");
// Output: "Propiedades con error: Email, Password, BirthDate"
```

---

## AddError

`AddError` permite agregar errores manualmente a un `ValidationResult`. Útil para combinar validación con lógica de negocio:

```csharp
// Sin código de error
result.AddError("Email", "El email ya está en uso.");

// Con código de error
result.AddError("Email", "El email ya está en uso.", "EMAIL_ALREADY_EXISTS");
```

Ejemplo de uso en un servicio que combina validación y lógica:

```csharp
public async Task<ValidationResult> ValidateAndCheckBusinessRulesAsync(
    CreateOrderRequest request,
    CancellationToken ct)
{
    // Primero valida con el validador estándar
    var result = await _validator.ValidateAsync(request, ct);

    // Si ya hay errores, no continúes con las reglas de negocio
    if (!result.IsValid)
        return result;

    // Reglas de negocio que no encajan en el validador
    var customer = await _customers.GetByIdAsync(request.CustomerId, ct);
    if (customer.IsBlocked)
    {
        result.AddError("CustomerId",
            "El cliente está bloqueado y no puede realizar pedidos.",
            "CUSTOMER_BLOCKED");
    }

    if (customer.CreditLimit < request.TotalAmount)
    {
        result.AddError("TotalAmount",
            $"El importe supera el límite de crédito del cliente ({customer.CreditLimit:C}).",
            "CREDIT_LIMIT_EXCEEDED");
    }

    return result;
}
```

---

## ToFlatList

`ToFlatList()` devuelve todos los errores como una lista plana de strings con el formato `"PropertyName: message"`.

```csharp
var result = await validator.ValidateAsync(request);

// Todos los errores en formato legible
List<string> flatErrors = result.ToFlatList();
foreach (var error in flatErrors)
    Console.WriteLine(error);

// Output:
// Name: El nombre es obligatorio.
// Email: El email no tiene formato válido.
// Email: El email ya está registrado.
// Password: Debe tener al menos 8 caracteres.
```

Útil para logging:

```csharp
if (!result.IsValid)
{
    _logger.LogWarning("Validación fallida: {Errors}",
        string.Join(" | ", result.ToFlatList()));
}
```

---

## Merge

`Merge` combina los errores de otro `ValidationResult` en el actual. Los errores se acumulan por propiedad:

```csharp
var mainResult = await _mainValidator.ValidateAsync(request);
var addressResult = await _addressValidator.ValidateAsync(request.Address);
var paymentResult = await _paymentValidator.ValidateAsync(request.Payment);

// Combina todos en uno
mainResult.Merge(addressResult);
mainResult.Merge(paymentResult);

if (!mainResult.IsValid)
{
    // mainResult.Errors contiene errores de los tres validadores
    return BadRequest(mainResult.Errors);
}
```

Ejemplo de merge con prefijos personalizados:

```csharp
public async Task<ValidationResult> ValidateComplexOrderAsync(
    ComplexOrderRequest request,
    CancellationToken ct)
{
    var result = new ValidationResult();

    // Valida el header del pedido
    var headerResult = await _headerValidator.ValidateAsync(request, ct);
    result.Merge(headerResult);

    // Valida cada línea manualmente y agrega errores con prefijo
    for (int i = 0; i < request.Lines.Count; i++)
    {
        var lineResult = await _lineValidator.ValidateAsync(request.Lines[i], ct);
        foreach (var (property, errors) in lineResult.Errors)
        {
            foreach (var error in errors)
                result.AddError($"Lines[{i}].{property}", error);
        }
    }

    return result;
}
```

---

## ErrorCodes

`ErrorCodes` es un diccionario paralelo a `Errors` que contiene los códigos de error asignados con `WithErrorCode`. Un mensaje puede tener código o no, de forma independiente.

```csharp
var result = await validator.ValidateAsync(request);

// Errores con sus códigos
foreach (var (property, codes) in result.ErrorCodes)
{
    Console.WriteLine($"{property}: {string.Join(", ", codes)}");
}

// Verificar si hay un código específico
bool hasCreditLimitError = result.ErrorCodes
    .Any(kvp => kvp.Value.Contains("CREDIT_LIMIT_EXCEEDED"));
```

### Uso en respuesta de API estructurada

```csharp
[HttpPost("orders")]
public async Task<IActionResult> CreateOrder(
    [FromBody] CreateOrderRequest request,
    [FromServices] IValidator<CreateOrderRequest> validator)
{
    var result = await validator.ValidateAsync(request, HttpContext.RequestAborted);

    if (!result.IsValid)
    {
        return BadRequest(new ApiErrorResponse
        {
            Message = "La validación ha fallado.",
            Errors = result.Errors.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToArray()),
            ErrorCodes = result.ErrorCodes.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToArray())
        });
    }

    var order = await _orderService.CreateAsync(request);
    return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
}

public class ApiErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, string[]> Errors { get; set; } = new();
    public Dictionary<string, string[]> ErrorCodes { get; set; } = new();
}
```

Respuesta:

```json
{
  "message": "La validación ha fallado.",
  "errors": {
    "Email": ["El email ya está registrado."],
    "Amount": ["El importe supera el límite de crédito."]
  },
  "errorCodes": {
    "Email": ["EMAIL_ALREADY_EXISTS"],
    "Amount": ["CREDIT_LIMIT_EXCEEDED"]
  }
}
```

---

## Uso en ASP.NET Core con ValidationProblem

Para Minimal API, `Results.ValidationProblem` espera `Dictionary<string, string[]>`:

```csharp
app.MapPost("/users", async (CreateUserRequest request, IValidator<CreateUserRequest> validator) =>
{
    var result = await validator.ValidateAsync(request);
    if (!result.IsValid)
    {
        var errors = result.Errors.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.ToArray());

        return Results.ValidationProblem(errors);
    }

    // Procesar...
    return Results.Ok();
});
```

Respuesta automática en formato RFC 7807:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Email": ["El email ya está registrado."]
  }
}
```

---

## Serialización

`ValidationResult` se puede serializar directamente con `System.Text.Json`:

```csharp
var result = await validator.ValidateAsync(request);
var json = JsonSerializer.Serialize(new
{
    isValid = result.IsValid,
    errors = result.Errors,
    errorCodes = result.ErrorCodes
});
```

---

## Construir ValidationResult manualmente

Puedes crear un `ValidationResult` desde cero, útil en tests o en orquestadores de validación:

```csharp
var result = new ValidationResult();
result.AddError("Name", "El nombre es obligatorio.", "NAME_REQUIRED");
result.AddError("Email", "El email no es válido.", "EMAIL_INVALID");
result.AddError("Email", "El email ya existe.", "EMAIL_EXISTS");

Console.WriteLine(result.IsValid);      // false
Console.WriteLine(result.ErrorCount);   // 3
Console.WriteLine(result.PropertyNames.Count); // 2 (Name, Email)

foreach (var line in result.ToFlatList())
    Console.WriteLine(line);
// Name: El nombre es obligatorio.
// Email: El email no es válido.
// Email: El email ya existe.
```

---

## Testing con ValidationResult

```csharp
public class CreateProductValidatorTests
{
    private readonly CreateProductValidator _validator;

    public CreateProductValidatorTests()
    {
        _validator = new CreateProductValidator();
    }

    [Fact]
    public async Task Name_TooShort_ShouldFailWithCorrectMessage()
    {
        var request = new CreateProductRequest { Name = "AB", Price = 10m };

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor("Name"));
        Assert.Contains("al menos 3 caracteres", result.FirstError("Name"));
    }

    [Fact]
    public async Task ValidRequest_ShouldPass()
    {
        var request = new CreateProductRequest
        {
            Name = "Laptop Pro",
            Price = 999.99m,
            Stock = 10,
            Category = "Electronics"
        };

        var result = await _validator.ValidateAsync(request);

        Assert.True(result.IsValid);
        Assert.Equal(0, result.ErrorCount);
    }

    [Fact]
    public async Task MultipleErrors_AllReported()
    {
        var request = new CreateProductRequest { Name = "", Price = -5m };

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor("Name"));
        Assert.True(result.HasErrorFor("Price"));

        // Asegúrate de que todos los errores están presentes
        var flatErrors = result.ToFlatList();
        Assert.Contains(flatErrors, e => e.Contains("Name"));
        Assert.Contains(flatErrors, e => e.Contains("Price"));
    }

    [Fact]
    public async Task WithErrorCode_ReturnsCorrectCode()
    {
        var request = new CreateProductRequest { Name = "", Price = 10m };

        var result = await _validator.ValidateAsync(request);

        Assert.True(result.ErrorCodes.ContainsKey("Name"));
        Assert.Contains("NAME_REQUIRED", result.ErrorCodes["Name"]);
    }
}
```

---

## Siguientes pasos

- **[Excepciones](10-excepciones.md)** — ValidationException y ValidateAndThrow
- **[ASP.NET Core](12-integracion-aspnetcore.md)** — Integración con middleware y filtros que usan ValidationResult
