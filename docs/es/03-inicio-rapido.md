# Inicio rápido

Este documento muestra un ejemplo completo y funcional desde cero: definir un modelo, crear un validador, registrarlo en DI y usarlo en un endpoint de ASP.NET Core. El objetivo es que tengas validación funcionando en menos de 10 minutos.

## 1. Instalar los paquetes

```bash
dotnet add package Vali-Validation
dotnet add package Vali-Validation.AspNetCore
```

## 2. Definir el modelo

```csharp
// Models/CreateProductRequest.cs
public class CreateProductRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
}
```

## 3. Crear el validador

Los validadores heredan de `AbstractValidator<T>`. Todas las reglas se definen en el constructor.

```csharp
// Validators/CreateProductRequestValidator.cs
using Vali_Validation.Core.Validators;

public class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    private static readonly string[] ValidCategories =
        new[] { "Electronics", "Clothing", "Food", "Books", "Other" };

    public CreateProductRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
                .WithMessage("El nombre del producto es obligatorio.")
            .MinimumLength(3)
                .WithMessage("El nombre debe tener al menos 3 caracteres.")
            .MaximumLength(200)
                .WithMessage("El nombre no puede superar los 200 caracteres.");

        RuleFor(x => x.Description)
            .MaximumLength(1000)
                .WithMessage("La descripción no puede superar los 1000 caracteres.");

        RuleFor(x => x.Price)
            .GreaterThan(0)
                .WithMessage("El precio debe ser mayor que 0.")
            .LessThan(100000)
                .WithMessage("El precio no puede superar 100,000.");

        RuleFor(x => x.Stock)
            .GreaterThanOrEqualTo(0)
                .WithMessage("El stock no puede ser negativo.");

        RuleFor(x => x.Category)
            .NotEmpty()
                .WithMessage("La categoría es obligatoria.")
            .In(ValidCategories)
                .WithMessage("La categoría debe ser una de: Electronics, Clothing, Food, Books, Other.");

        RuleFor(x => x.ImageUrl)
            .Url()
                .WithMessage("La URL de imagen no es válida.")
            .When(x => x.ImageUrl != null);
    }
}
```

## 4. Registrar en DI

```csharp
// Program.cs
using Vali_Validation.Core.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Registra todos los IValidator<T> del assembly actual
builder.Services.AddValidationsFromAssembly(typeof(Program).Assembly);

// Si usas AspNetCore:
builder.Services.AddValiValidationProblemDetails();

var app = builder.Build();

// Middleware que convierte ValidationException en HTTP 400
app.UseValiValidationExceptionHandler();
```

## 5. Usar en un endpoint

### Opción A: Minimal API con endpoint filter

```csharp
// Program.cs (continuación)
app.MapPost("/products", async (
    CreateProductRequest request,
    IValidator<CreateProductRequest> validator,
    IProductRepository repository) =>
{
    // Validar manualmente
    var result = await validator.ValidateAsync(request);
    if (!result.IsValid)
    {
        return Results.ValidationProblem(result.Errors.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.ToArray()));
    }

    var product = await repository.CreateAsync(request);
    return Results.Created($"/products/{product.Id}", product);
});
```

O usando el endpoint filter (más limpio):

```csharp
app.MapPost("/products", async (
    CreateProductRequest request,
    IProductRepository repository) =>
{
    var product = await repository.CreateAsync(request);
    return Results.Created($"/products/{product.Id}", product);
})
.WithValiValidation<CreateProductRequest>(); // Validación automática antes del handler
```

### Opción B: Controlador MVC

```csharp
[ApiController]
[Route("api/[controller]")]
[ValiValidate] // Valida automáticamente todos los argumentos con IValidator<T> registrado
public class ProductsController : ControllerBase
{
    private readonly IProductRepository _repository;

    public ProductsController(IProductRepository repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request)
    {
        // Si llegamos aquí, la validación ya pasó
        var product = await _repository.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var product = await _repository.GetByIdAsync(id);
        return product is null ? NotFound() : Ok(product);
    }
}
```

## 6. Ejecutar y probar

Ejecuta la aplicación:

```bash
dotnet run
```

Prueba con datos inválidos:

```bash
curl -X POST http://localhost:5000/products \
  -H "Content-Type: application/json" \
  -d '{"name": "A", "price": -5, "stock": -1, "category": "Unknown"}'
```

Respuesta HTTP 400:

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Validation Failed",
  "status": 400,
  "errors": {
    "Name": ["El nombre debe tener al menos 3 caracteres."],
    "Price": ["El precio debe ser mayor que 0."],
    "Stock": ["El stock no puede ser negativo."],
    "Category": ["La categoría debe ser una de: Electronics, Clothing, Food, Books, Other."]
  }
}
```

Prueba con datos válidos:

```bash
curl -X POST http://localhost:5000/products \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Laptop Pro 15",
    "description": "Laptop de alto rendimiento",
    "price": 1299.99,
    "stock": 50,
    "category": "Electronics",
    "imageUrl": "https://example.com/laptop.jpg"
  }'
```

Respuesta HTTP 201:

```json
{
  "id": 1,
  "name": "Laptop Pro 15",
  "price": 1299.99,
  "category": "Electronics"
}
```

## Ejemplo completo: registro, login, perfil

Para ilustrar más casos de uso, aquí hay un ejemplo más completo con tres validadores relacionados:

```csharp
// Modelos
public class RegisterRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public DateTime BirthDate { get; set; }
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class UpdateProfileRequest
{
    public string DisplayName { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? Website { get; set; }
    public string? PhoneNumber { get; set; }
}
```

```csharp
// Validadores

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(50)
            .IsAlphanumeric()
                .WithMessage("El nombre de usuario solo puede contener letras, números y guión bajo.");

        RuleFor(x => x.Email)
            .NotEmpty()
            .Email()
                .WithMessage("El formato de email no es válido.");

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .HasUppercase()
                .WithMessage("La contraseña debe contener al menos una mayúscula.")
            .HasLowercase()
                .WithMessage("La contraseña debe contener al menos una minúscula.")
            .HasDigit()
                .WithMessage("La contraseña debe contener al menos un número.")
            .HasSpecialChar()
                .WithMessage("La contraseña debe contener al menos un carácter especial.");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty()
            .EqualToProperty(x => x.Password)
                .WithMessage("Las contraseñas no coinciden.");

        RuleFor(x => x.BirthDate)
            .PastDate()
                .WithMessage("La fecha de nacimiento debe estar en el pasado.")
            .Must(date => DateTime.Today.Year - date.Year >= 18)
                .WithMessage("Debes tener al menos 18 años para registrarte.");
    }
}

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
                .WithMessage("El email es obligatorio.")
            .Email()
                .WithMessage("El formato de email no es válido.");

        RuleFor(x => x.Password)
            .NotEmpty()
                .WithMessage("La contraseña es obligatoria.");
    }
}

public class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Bio)
            .MaximumLength(500)
            .When(x => x.Bio != null);

        RuleFor(x => x.Website)
            .Url()
                .WithMessage("La URL del sitio web no es válida.")
            .When(x => x.Website != null);

        RuleFor(x => x.PhoneNumber)
            .PhoneNumber()
                .WithMessage("El número de teléfono debe estar en formato E.164 (ej: +34612345678).")
            .When(x => x.PhoneNumber != null);
    }
}
```

```csharp
// Program.cs completo
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Validación
builder.Services.AddValidationsFromAssembly(typeof(Program).Assembly);
builder.Services.AddValiValidationProblemDetails();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseValiValidationExceptionHandler();
app.UseAuthorization();
app.MapControllers();

// Endpoints Minimal API
app.MapPost("/auth/register", async (RegisterRequest request, IAuthService authService) =>
{
    var user = await authService.RegisterAsync(request);
    return Results.Created($"/users/{user.Id}", user);
})
.WithValiValidation<RegisterRequest>()
.WithTags("Auth");

app.MapPost("/auth/login", async (LoginRequest request, IAuthService authService) =>
{
    var token = await authService.LoginAsync(request);
    return Results.Ok(new { token });
})
.WithValiValidation<LoginRequest>()
.WithTags("Auth");

app.Run();
```

## Resumen de lo aprendido

En este ejemplo has visto:

1. **Definir un validador** heredando de `AbstractValidator<T>` y usando `RuleFor` en el constructor
2. **Reglas encadenadas** con mensajes personalizados via `WithMessage`
3. **Condicionales** con `When` para reglas opcionales
4. **Registro en DI** con `AddValidationsFromAssembly`
5. **Uso en endpoints** de forma manual o automática con `WithValiValidation<T>`
6. **Formato de errores** estándar `application/problem+json`

## Siguientes pasos

- **[Validadores en profundidad](04-validadores.md)** — AbstractValidator, Include, CascadeMode global, ValidateParallelAsync
- **[Reglas básicas](05-reglas-basicas.md)** — Catálogo completo de todas las reglas disponibles
- **[Modificadores](07-modificadores.md)** — WithMessage, When/Unless, StopOnFirstFailure y más
