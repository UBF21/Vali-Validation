# Quick Start

This document shows a complete, working example from scratch: defining a model, creating a validator, registering it in DI and using it in an ASP.NET Core endpoint. The goal is to have validation working in less than 10 minutes.

## 1. Install the Packages

```bash
dotnet add package Vali-Validation
dotnet add package Vali-Validation.AspNetCore
```

## 2. Define the Model

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

## 3. Create the Validator

Validators inherit from `AbstractValidator<T>`. All rules are defined in the constructor.

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
                .WithMessage("The product name is required.")
            .MinimumLength(3)
                .WithMessage("The name must have at least 3 characters.")
            .MaximumLength(200)
                .WithMessage("The name cannot exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(1000)
                .WithMessage("The description cannot exceed 1000 characters.");

        RuleFor(x => x.Price)
            .GreaterThan(0)
                .WithMessage("The price must be greater than 0.")
            .LessThan(100000)
                .WithMessage("The price cannot exceed 100,000.");

        RuleFor(x => x.Stock)
            .GreaterThanOrEqualTo(0)
                .WithMessage("The stock cannot be negative.");

        RuleFor(x => x.Category)
            .NotEmpty()
                .WithMessage("The category is required.")
            .In(ValidCategories)
                .WithMessage("The category must be one of: Electronics, Clothing, Food, Books, Other.");

        RuleFor(x => x.ImageUrl)
            .Url()
                .WithMessage("The image URL is not valid.")
            .When(x => x.ImageUrl != null);
    }
}
```

## 4. Register in DI

```csharp
// Program.cs
using Vali_Validation.Core.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Registers all IValidator<T> from the current assembly
builder.Services.AddValidationsFromAssembly(typeof(Program).Assembly);

// If using AspNetCore:
builder.Services.AddValiValidationProblemDetails();

var app = builder.Build();

// Middleware that converts ValidationException to HTTP 400
app.UseValiValidationExceptionHandler();
```

## 5. Use in an Endpoint

### Option A: Minimal API with endpoint filter

```csharp
// Program.cs (continued)
app.MapPost("/products", async (
    CreateProductRequest request,
    IValidator<CreateProductRequest> validator,
    IProductRepository repository) =>
{
    // Validate manually
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

Or using the endpoint filter (cleaner):

```csharp
app.MapPost("/products", async (
    CreateProductRequest request,
    IProductRepository repository) =>
{
    var product = await repository.CreateAsync(request);
    return Results.Created($"/products/{product.Id}", product);
})
.WithValiValidation<CreateProductRequest>(); // Automatic validation before the handler
```

### Option B: MVC Controller

```csharp
[ApiController]
[Route("api/[controller]")]
[ValiValidate] // Automatically validates all arguments with a registered IValidator<T>
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
        // If we get here, validation has already passed
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

## 6. Run and Test

Run the application:

```bash
dotnet run
```

Test with invalid data:

```bash
curl -X POST http://localhost:5000/products \
  -H "Content-Type: application/json" \
  -d '{"name": "A", "price": -5, "stock": -1, "category": "Unknown"}'
```

HTTP 400 response:

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Validation Failed",
  "status": 400,
  "errors": {
    "Name": ["The name must have at least 3 characters."],
    "Price": ["The price must be greater than 0."],
    "Stock": ["The stock cannot be negative."],
    "Category": ["The category must be one of: Electronics, Clothing, Food, Books, Other."]
  }
}
```

Test with valid data:

```bash
curl -X POST http://localhost:5000/products \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Laptop Pro 15",
    "description": "High-performance laptop",
    "price": 1299.99,
    "stock": 50,
    "category": "Electronics",
    "imageUrl": "https://example.com/laptop.jpg"
  }'
```

HTTP 201 response:

```json
{
  "id": 1,
  "name": "Laptop Pro 15",
  "price": 1299.99,
  "category": "Electronics"
}
```

## Complete Example: Registration, Login, Profile

To illustrate more use cases, here is a more complete example with three related validators:

```csharp
// Models
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
// Validators

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(50)
            .IsAlphanumeric()
                .WithMessage("The username can only contain letters, numbers and underscores.");

        RuleFor(x => x.Email)
            .NotEmpty()
            .Email()
                .WithMessage("The email format is not valid.");

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .HasUppercase()
                .WithMessage("The password must contain at least one uppercase letter.")
            .HasLowercase()
                .WithMessage("The password must contain at least one lowercase letter.")
            .HasDigit()
                .WithMessage("The password must contain at least one number.")
            .HasSpecialChar()
                .WithMessage("The password must contain at least one special character.");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty()
            .EqualToProperty(x => x.Password)
                .WithMessage("The passwords do not match.");

        RuleFor(x => x.BirthDate)
            .PastDate()
                .WithMessage("The birth date must be in the past.")
            .Must(date => DateTime.Today.Year - date.Year >= 18)
                .WithMessage("You must be at least 18 years old to register.");
    }
}

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
                .WithMessage("The email is required.")
            .Email()
                .WithMessage("The email format is not valid.");

        RuleFor(x => x.Password)
            .NotEmpty()
                .WithMessage("The password is required.");
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
                .WithMessage("The website URL is not valid.")
            .When(x => x.Website != null);

        RuleFor(x => x.PhoneNumber)
            .PhoneNumber()
                .WithMessage("The phone number must be in E.164 format (e.g. +12025551234).")
            .When(x => x.PhoneNumber != null);
    }
}
```

```csharp
// Complete Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Validation
builder.Services.AddValidationsFromAssembly(typeof(Program).Assembly);
builder.Services.AddValiValidationProblemDetails();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseValiValidationExceptionHandler();
app.UseAuthorization();
app.MapControllers();

// Minimal API endpoints
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

## Summary of What You Learned

In this example you saw:

1. **Defining a validator** by inheriting from `AbstractValidator<T>` and using `RuleFor` in the constructor
2. **Chained rules** with custom messages via `WithMessage`
3. **Conditionals** with `When` for optional rules
4. **DI registration** with `AddValidationsFromAssembly`
5. **Usage in endpoints** manually or automatically with `WithValiValidation<T>`
6. **Standard error format** `application/problem+json`

## Next Steps

- **[Validators in depth](04-validators.md)** — AbstractValidator, Include, global CascadeMode, ValidateParallelAsync
- **[Basic Rules](05-basic-rules.md)** — Complete catalog of all available rules
- **[Modifiers](07-modifiers.md)** — WithMessage, When/Unless, StopOnFirstFailure and more
