# Dependency Injection

## Automatic Registration with AddValidationsFromAssembly

The `AddValidationsFromAssembly` extension method scans an assembly and automatically registers all classes that implement `IValidator<T>` in the DI container.

```csharp
// Program.cs
using Vali_Validation.Core.Extensions;

builder.Services.AddValidationsFromAssembly(
    assembly: typeof(Program).Assembly,
    lifetime: ServiceLifetime.Transient); // Transient is the default value
```

You only need to pass the assembly. The method discovers all concrete (non-abstract, non-interface) validators that inherit from `AbstractValidator<T>` and registers them as `IValidator<T>`.

### Registration from Multiple Assemblies

If your validators are spread across several projects, call the method once per assembly:

```csharp
builder.Services.AddValidationsFromAssembly(typeof(Program).Assembly);
builder.Services.AddValidationsFromAssembly(typeof(ApplicationLayerMarker).Assembly);
builder.Services.AddValidationsFromAssembly(typeof(InfrastructureLayerMarker).Assembly);
```

### Marker Classes for Assemblies

A common practice is to create an empty "marker" class in each project to identify its assembly:

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

## ServiceLifetime: Which One to Choose

| Lifetime | Description | When to use |
|---|---|---|
| `Transient` | New instance on each resolution | Validators with no dependencies or with Transient dependencies |
| `Scoped` | One instance per HTTP request | If the validator depends on a DbContext or Scoped service |
| `Singleton` | One instance for the entire app | Only if the validator has no mutable dependencies |

### Golden Rule

> The validator lifetime must be equal to or shorter than the lifetime of its longest-lived dependencies.

```csharp
// Correct: DbContext is Scoped, the validator is also Scoped
builder.Services.AddValidationsFromAssembly(assembly, ServiceLifetime.Scoped);

// Incorrect: DbContext is Scoped, the validator is Singleton → captive dependency
builder.Services.AddValidationsFromAssembly(assembly, ServiceLifetime.Singleton); // AVOID
```

### Example with DbContext

```csharp
// Validator that accesses the DB (depends on DbContext which is Scoped)
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
            .WithMessage("The email is already registered.");
    }
}

// Program.cs — register as Scoped because it depends on DbContext (Scoped)
builder.Services.AddValidationsFromAssembly(
    typeof(Program).Assembly,
    ServiceLifetime.Scoped);
```

---

## Injecting IValidator\<T\> into Constructors

Once registered, you can inject `IValidator<T>` into any DI-managed class:

### In a Minimal API Endpoint

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

### In an MVC Controller

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

### In an Application Service

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

## Complete Program.cs Example

```csharp
using Vali_Validation.Core.Extensions;
using Vali_Validation.AspNetCore; // For UseValiValidationExceptionHandler

var builder = WebApplication.CreateBuilder(args);

// ── Infrastructure ────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// ── Validation ────────────────────────────────────────────────────────────────
// Registers all IValidator<T> from the Application assembly
// Scoped because some validators depend on AppDbContext
builder.Services.AddValidationsFromAssembly(
    ApplicationAssembly.Reference,
    ServiceLifetime.Scoped);

// If you also have validators in the API project (rare, but possible)
builder.Services.AddValidationsFromAssembly(
    typeof(Program).Assembly,
    ServiceLifetime.Transient);

// ── ASP.NET Core ──────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Support for ValidationProblem in Minimal APIs
builder.Services.AddValiValidationProblemDetails();

// ── Application Services ─────────────────────────────────────────────────────
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IProductService, ProductService>();

var app = builder.Build();

// ── HTTP Pipeline ─────────────────────────────────────────────────────────────
app.UseSwagger();
app.UseSwaggerUI();

// Catches ValidationException and returns HTTP 400 in RFC 7807 format
app.UseValiValidationExceptionHandler();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Minimal API endpoints
app.MapPost("/products", async (
    CreateProductRequest request,
    IProductService productService,
    CancellationToken ct) =>
{
    // ProductService.CreateAsync calls ValidateAndThrowAsync internally
    // If it fails, UseValiValidationExceptionHandler returns 400
    var product = await productService.CreateAsync(request, ct);
    return Results.Created($"/products/{product.Id}", product);
});

app.Run();
```

---

## Manual Registration (without AddValidationsFromAssembly)

If you prefer to register validators manually (useful in small projects or for fine control):

```csharp
// Individual registration
builder.Services.AddTransient<IValidator<CreateProductRequest>, CreateProductRequestValidator>();
builder.Services.AddScoped<IValidator<CreateOrderRequest>, CreateOrderRequestValidator>();
builder.Services.AddSingleton<IValidator<AppSettings>, AppSettingsValidator>();

// With factory (for validators with complex dependencies)
builder.Services.AddScoped<IValidator<CreateUserRequest>>(sp =>
{
    var context = sp.GetRequiredService<AppDbContext>();
    var emailService = sp.GetRequiredService<IEmailService>();
    return new CreateUserRequestWithEmailVerificationValidator(context, emailService);
});
```

---

## Dynamic Validator Resolution

In advanced cases, you can resolve the correct validator at runtime using the container:

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
            return new ValidationResult(); // No validator = valid

        return await validator.ValidateAsync(instance, ct);
    }
}
```

---

## Testing: Mocking IValidator\<T\>

Since code consumes `IValidator<T>`, you can mock it in tests without spinning up the full DI container:

```csharp
// With Moq
public class OrderServiceTests
{
    [Fact]
    public async Task CreateAsync_ValidRequest_CallsRepository()
    {
        // Arrange
        var validatorMock = new Mock<IValidator<CreateOrderRequest>>();
        validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<CreateOrderRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult()); // No errors

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
        failResult.AddError("CustomerId", "The customer does not exist.");

        var validatorMock = new Mock<IValidator<CreateOrderRequest>>();
        validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<CreateOrderRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(failResult);

        // ValidateAndThrowAsync calls ValidateAsync internally
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

## Next Steps

- **[ASP.NET Core](12-aspnetcore-integration.md)** — Middleware, endpoint filters and action filters
- **[MediatR](13-mediatr-integration.md)** — Registration alongside MediatR
- **[Vali-Mediator](14-valimediator-integration.md)** — Registration alongside Vali-Mediator
