# Inyección de dependencias

## Registro automático con AddValidationsFromAssembly

El método de extensión `AddValidationsFromAssembly` escanea un assembly y registra automáticamente todas las clases que implementan `IValidator<T>` en el contenedor DI.

```csharp
// Program.cs
using Vali_Validation.Core.Extensions;

builder.Services.AddValidationsFromAssembly(
    assembly: typeof(Program).Assembly,
    lifetime: ServiceLifetime.Transient); // Transient es el valor por defecto
```

Solo necesitas pasar el assembly. El método descubre todos los validadores concretos (no abstractos, no interfaces) que heredan de `AbstractValidator<T>` y los registra como `IValidator<T>`.

### Registro desde múltiples assemblies

Si tus validadores están repartidos en varios proyectos, llama al método una vez por assembly:

```csharp
builder.Services.AddValidationsFromAssembly(typeof(Program).Assembly);
builder.Services.AddValidationsFromAssembly(typeof(ApplicationLayerMarker).Assembly);
builder.Services.AddValidationsFromAssembly(typeof(InfrastructureLayerMarker).Assembly);
```

### Marker classes para assemblies

Una práctica común es crear una clase "marker" vacía en cada proyecto para identificar su assembly:

```csharp
// MyApp.Application/ApplicationAssembly.cs
namespace MyApp.Application;

public static class ApplicationAssembly
{
    public static readonly Assembly Reference = typeof(ApplicationAssembly).Assembly;
}

// MyApp.Api/Program.cs
builder.Services.AddValidationsFromAssembly(ApplicationAssembly.Reference);
builder.Services.AddValidationsFromAssembly(typeof(Program).Assembly);
```

---

## ServiceLifetime: cuál elegir

| Lifetime | Descripción | Cuándo usar |
|---|---|---|
| `Transient` | Nueva instancia en cada resolución | Validadores sin dependencias o con dependencias Transient |
| `Scoped` | Una instancia por request HTTP | Si el validador depende de un DbContext o servicio Scoped |
| `Singleton` | Una instancia para toda la app | Solo si el validador no tiene ninguna dependencia mutable |

### Regla de oro

> La lifetime del validador debe ser igual o más corta que la lifetime de sus dependencias más largas.

```csharp
// Correcto: DbContext es Scoped, el validador también es Scoped
builder.Services.AddValidationsFromAssembly(assembly, ServiceLifetime.Scoped);

// Incorrecto: DbContext es Scoped, el validador es Singleton → captive dependency
builder.Services.AddValidationsFromAssembly(assembly, ServiceLifetime.Singleton); // EVITAR
```

### Ejemplo con DbContext

```csharp
// Validador que accede a la BD (depende de DbContext que es Scoped)
public class CreateUserValidator : AbstractValidator<CreateUserRequest>
{
    private readonly AppDbContext _context;

    public CreateUserValidator(AppDbContext context)
    {
        _context = context;

        RuleFor(x => x.Email)
            .MustAsync(async (email, ct) =>
            {
                return !await _context.Users.AnyAsync(u => u.Email == email, ct);
            })
            .WithMessage("El email ya está registrado.");
    }
}

// Program.cs — registrar como Scoped porque depende de DbContext (Scoped)
builder.Services.AddValidationsFromAssembly(
    typeof(Program).Assembly,
    ServiceLifetime.Scoped);
```

---

## Inyectar IValidator\<T\> en constructores

Una vez registrado, puedes inyectar `IValidator<T>` en cualquier clase gestionada por DI:

### En un endpoint Minimal API

```csharp
app.MapPost("/products", async (
    [FromBody] CreateProductRequest request,
    [FromServices] IValidator<CreateProductRequest> validator,
    [FromServices] IProductRepository repository,
    CancellationToken ct) =>
{
    var result = await validator.ValidateAsync(request, ct);
    if (!result.IsValid)
        return Results.ValidationProblem(result.Errors.ToDictionary(
            k => k.Key, v => v.Value.ToArray()));

    var product = await repository.CreateAsync(request, ct);
    return Results.Created($"/products/{product.Id}", product);
});
```

### En un controlador MVC

```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IValidator<CreateUserRequest> _validator;
    private readonly IUserService _userService;

    public UsersController(
        IValidator<CreateUserRequest> validator,
        IUserService userService)
    {
        _validator = validator;
        _userService = userService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateUserRequest request,
        CancellationToken ct)
    {
        var result = await _validator.ValidateAsync(request, ct);
        if (!result.IsValid)
            return BadRequest(result.Errors);

        var user = await _userService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }
}
```

### En un servicio de aplicación

```csharp
public class OrderService : IOrderService
{
    private readonly IValidator<CreateOrderRequest> _createValidator;
    private readonly IValidator<UpdateOrderRequest> _updateValidator;
    private readonly IOrderRepository _repository;

    public OrderService(
        IValidator<CreateOrderRequest> createValidator,
        IValidator<UpdateOrderRequest> updateValidator,
        IOrderRepository repository)
    {
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _repository = repository;
    }

    public async Task<Order> CreateAsync(CreateOrderRequest request, CancellationToken ct)
    {
        await _createValidator.ValidateAndThrowAsync(request, ct);
        return await _repository.CreateAsync(request, ct);
    }

    public async Task<Order> UpdateAsync(int id, UpdateOrderRequest request, CancellationToken ct)
    {
        await _updateValidator.ValidateAndThrowAsync(request, ct);
        return await _repository.UpdateAsync(id, request, ct);
    }
}
```

---

## Ejemplo completo de Program.cs

```csharp
using Vali_Validation.Core.Extensions;
using Vali_Validation.AspNetCore; // Para UseValiValidationExceptionHandler

var builder = WebApplication.CreateBuilder(args);

// ── Infraestructura ──────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// ── Validación ───────────────────────────────────────────────────────────────
// Registra todos los IValidator<T> del assembly de Application
// Scoped porque algunos validadores dependen de AppDbContext
builder.Services.AddValidationsFromAssembly(
    ApplicationAssembly.Reference,
    ServiceLifetime.Scoped);

// Si también tienes validadores en el API project (raros, pero posible)
builder.Services.AddValidationsFromAssembly(
    typeof(Program).Assembly,
    ServiceLifetime.Transient);

// ── ASP.NET Core ─────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Soporte para ValidationProblem en Minimal APIs
builder.Services.AddValiValidationProblemDetails();

// ── Servicios de aplicación ──────────────────────────────────────────────────
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IProductService, ProductService>();

var app = builder.Build();

// ── Pipeline HTTP ────────────────────────────────────────────────────────────
app.UseSwagger();
app.UseSwaggerUI();

// Captura ValidationException y devuelve HTTP 400 en formato RFC 7807
app.UseValiValidationExceptionHandler();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Endpoints Minimal API
app.MapPost("/products", async (
    CreateProductRequest request,
    IProductService productService,
    CancellationToken ct) =>
{
    // ProductService.CreateAsync llama a ValidateAndThrowAsync internamente
    // Si falla, UseValiValidationExceptionHandler devuelve 400
    var product = await productService.CreateAsync(request, ct);
    return Results.Created($"/products/{product.Id}", product);
});

app.Run();
```

---

## Registro manual (sin AddValidationsFromAssembly)

Si prefieres registrar validadores manualmente (útil en proyectos pequeños o para control fino):

```csharp
// Registro individual
builder.Services.AddTransient<IValidator<CreateProductRequest>, CreateProductRequestValidator>();
builder.Services.AddScoped<IValidator<CreateOrderRequest>, CreateOrderRequestValidator>();
builder.Services.AddSingleton<IValidator<AppSettings>, AppSettingsValidator>();

// Con factory (para validadores con dependencias complejas)
builder.Services.AddScoped<IValidator<CreateUserRequest>>(sp =>
{
    var context = sp.GetRequiredService<AppDbContext>();
    var emailService = sp.GetRequiredService<IEmailService>();
    return new CreateUserRequestWithEmailVerificationValidator(context, emailService);
});
```

---

## Resolución dinámica de validadores

En casos avanzados, puedes resolver el validador correcto en tiempo de ejecución usando el contenedor:

```csharp
public class ValidationService
{
    private readonly IServiceProvider _serviceProvider;

    public ValidationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<ValidationResult> ValidateAsync<T>(T instance, CancellationToken ct)
        where T : class
    {
        var validator = _serviceProvider.GetService<IValidator<T>>();
        if (validator == null)
            return new ValidationResult(); // Sin validador = válido

        return await validator.ValidateAsync(instance, ct);
    }
}
```

---

## Testing: mock de IValidator\<T\>

Gracias a que el código consume `IValidator<T>`, puedes mockearlo en tests sin levantar el contenedor DI completo:

```csharp
// Con Moq
public class OrderServiceTests
{
    [Fact]
    public async Task CreateAsync_ValidRequest_CallsRepository()
    {
        // Arrange
        var validatorMock = new Mock<IValidator<CreateOrderRequest>>();
        validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<CreateOrderRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult()); // Sin errores

        var repositoryMock = new Mock<IOrderRepository>();
        var service = new OrderService(validatorMock.Object, repositoryMock.Object);
        var request = new CreateOrderRequest { CustomerId = "123" };

        // Act
        await service.CreateAsync(request, CancellationToken.None);

        // Assert
        repositoryMock.Verify(r => r.CreateAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_InvalidRequest_ThrowsValidationException()
    {
        // Arrange
        var failResult = new ValidationResult();
        failResult.AddError("CustomerId", "El cliente no existe.");

        var validatorMock = new Mock<IValidator<CreateOrderRequest>>();
        validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<CreateOrderRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(failResult);

        // ValidateAndThrowAsync llama a ValidateAsync internamente
        validatorMock
            .Setup(v => v.ValidateAndThrowAsync(It.IsAny<CreateOrderRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException(failResult));

        var service = new OrderService(validatorMock.Object, Mock.Of<IOrderRepository>());

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => service.CreateAsync(new CreateOrderRequest(), CancellationToken.None));
    }
}
```

---

## Siguientes pasos

- **[ASP.NET Core](12-integracion-aspnetcore.md)** — Middleware, endpoint filters y action filters
- **[MediatR](13-integracion-mediatr.md)** — Registro junto con MediatR
- **[Vali-Mediator](14-integracion-valimediator.md)** — Registro junto con Vali-Mediator
