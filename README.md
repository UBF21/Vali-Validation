# <img src="https://github.com/UBF21/Vali-Validation/blob/main/Vali-Validation/VV.png?raw=true" alt="Logo de Vali Validation" style="width: 46px; height: 46px; max-width: 300px;"> Vali-Validation – Lightweight Validation Library for .NET


## Introduction 🚀
Vali‑Validation is a lightweight and extensible .NET validation library designed as an alternative to FluentValidation. It provides a fluent, expressive API to define validation rules on your models and commands, with built‑in support for:

- Null / Empty checks
- String rules (length, prefix/suffix, regex)
- Numeric & date comparisons
- Collection rules (empty, count, membership)
- Custom predicates

Integrates seamlessly via dependency injection, making it ideal for Clean or Onion–style architectures.

## Installation 📦
To add Vali-Mediator to your .NET project, install it via NuGet with the following command:

```sh
dotnet add package Vali-Validation
```
- **Targets**: NET 7/8/9/10
- Dependencies: only **Microsoft.Extensions.DependencyInjection.Abstractions**

## 🚀 Quick Start

Inherit from AbstractValidator<T> and use RuleFor(...) to declare rules:

```csharp
using Vali_Validation.Core.Rules;
using Vali_Validation.Core.Validators;

public class UserDto
{
    public string Name  { get; set; }
    public string Email { get; set; }
    public int    Age   { get; set; }
}

public class UserDtoValidator : AbstractValidator<UserDto>
{
    public UserDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotNull()
            .NotEmpty()
            .MinimumLength(3);

        RuleFor(x => x.Email)
            .NotNull()
            .NotEmpty()
            .Email();

        RuleFor(x => x.Age)
            .GreaterThan(0)
            .NotZero();
    }
}
```
##  Register in DI Container

In**Program.cs** or **Startup.cs**:

```csharp
In Program.cs or Startup.cs:

csharp
using Vali_Validation.Core.Extensions; 

var builder = WebApplication.CreateBuilder(args);

builder.Services
       .AddValidationsFromAssembly(Assembly.GetExecutingAssembly());

var app = builder.Build();
```
## Validate Instances

Inject **IValidator<T>** and call **Validate(...)**:

```csharp
public class UserService
{
    private readonly IValidator<UserDto> _validator;

    public UserService(IValidator<UserDto> validator)
    {
        _validator = validator;
    }

    public void CreateUser(UserDto user)
    {
        var result = _validator.Validate(user);
        if (!result.IsValid)
        {
            foreach (var kv in result.Errors)
            {
                Console.WriteLine($"{kv.Key}: {string.Join(", ", kv.Value)}");
            }
            return;
        }
        // Proceed with creation…
    }
}
```
## Key Validation Methods

| Method                                     | Description                                                                           |
|--------------------------------------------|---------------------------------------------------------------------------------------|
| `NotNull()`                                | Ensures value is not null.                                                            |
| `NotEmpty()`                               | For strings/collections: not null, not empty (and not whitespace for strings).        |
| `Empty()`                                  | Ensures string is empty or collection has no items.                                   |
| `Email()`                                  | Must match a standard e‑mail regex pattern.                                           |
| `Url()`                                    | Must be a valid HTTP/HTTPS URL.                                                       |
| `MinimumLength(n)`                         | String length ≥ n.                                                                    |
| `MaximumLength(n)`                         | String length ≤ n.                                                                    |
| `Matches(pattern)`                         | Value must match a regular expression.                                                |
| `StartsWith(prefix)`                       | String must start with given prefix.                                                  |
| `EndsWith(suffix)`                         | String must end with given suffix.                                                    |
| `MustContain(sub, comp)`                   | String must contain `sub` (with optional `StringComparison`).                         |
| `EqualTo(other)`                           | Value must equal `other`.                                                             |
| `GreaterThan(x)`                           | Numeric/comparable > x.                                                               |
| `LessThan(x)`                              | Numeric/comparable < x.                                                               |
| `Between(min,max)`                         | Inclusive range for any `IComparable` (numbers, dates…).                             |
| `Positive()`, `Negative()`, `NotZero()`    | Numeric comparisons against zero.                                                     |
| `FutureDate()`, `PastDate()`, `Today()`    | `DateTime` comparisons relative to now/today.                                         |
| `In(IEnumerable<T>)`                       | Value must exist in provided collection.                                              |
| `HasCount(n)`, `NotEmptyCollection()`      | Collection must have exactly _n_ items or at least one.                               |
| `Must(predicate)`                          | Custom boolean predicate rule.                                                        |
| `WithMessage(msg)`                         | Override the default error message of the most recently added rule.                   |

## Cascade Mode (Stop-on-First-Failure)

Vali-Validation supports FluentValidation-equivalent cascade control at two levels:

**Rule level** — stop evaluating further rules on ONE property after its first failure:

```csharp
RuleFor(x => x.Email)
    .NotEmpty()
    .Email()
    .StopOnFirstFailure();
```

**Class level** — stop evaluating rules on OTHER properties once any rule anywhere fails, by overriding `GlobalCascadeMode` in your validator:

```csharp
public class UserValidator : AbstractValidator<User>
{
    protected override CascadeMode GlobalCascadeMode => CascadeMode.StopOnFirstFailure;

    public UserValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().Email();
    }
}
```

`GlobalCascadeMode` defaults to `CascadeMode.Continue` (validate everything, collect all errors) — the common case for surfacing every problem to the user at once. Note `ValidateParallelAsync()` always runs every rule regardless of `GlobalCascadeMode`, since early-stop and parallel execution are mutually exclusive by design.

## Severity (Warnings vs Errors)

Mark a rule as non-blocking with `.WithSeverity(Severity.Warning)` — it still shows up in the new
`result.Failures`, but never flips `IsValid` to `false` and never appears in `Errors`/`ErrorCodes`:

```csharp
RuleFor(x => x.Discount)
    .LessThanOrEqualTo(50)
    .WithSeverity(Severity.Warning)
    .WithMessage("Discount above 50% requires manager approval.");
```

## RuleSets

Tag rules with `.InRuleSet("name")` and run only those rules for a given call via `IncludeRuleSets`:

```csharp
RuleFor(x => x.PaymentMethod).NotEmpty().InRuleSet("checkout");

var result = validator.Validate(order, o => o.IncludeRuleSets("checkout"));
```

Untagged rules carry the implicit tag `"default"`; a plain `Validate(order)` call (no options)
still runs everything regardless of tags.

## Localization

Built-in messages ship in English and Spanish, resolved automatically from
`CultureInfo.CurrentUICulture`. Override per call or register your own language:

```csharp
var result = validator.Validate(dto, opts => opts.WithLanguage("es"));

LanguageManager.RegisterLanguage("pt", new Dictionary<MessageKey, string> { /* ... */ });
```

## Global Configuration

`ValiValidationOptions.Global` sets app-wide defaults once at startup — cascade mode, default
language, and resolvers for the property/display names used in messages and error keys:

```csharp
ValiValidationOptions.Global.DefaultCascadeMode = CascadeMode.StopOnFirstFailure;
ValiValidationOptions.Global.DefaultLanguage = "es";
```

## Reusable Custom Rules (PropertyValidator)

Implement `IPropertyValidator<TProperty>` for a custom rule you want to unit-test or share across
validators, instead of an inline `.Must(...)` predicate:

```csharp
public class EvenNumberValidator : IPropertyValidator<int>
{
    public bool IsValid(int value) => value % 2 == 0;
    public IReadOnlyDictionary<string, string> Messages { get; } = new Dictionary<string, string>
    {
        ["en"] = "The {PropertyName} field must be an even number."
    };
}

RuleFor(x => x.Age).SetPropertyValidator(new EvenNumberValidator());
```

## Nested Validators from DI (InjectValidator)

Resolve a nested validator through `IServiceProvider` instead of constructing it yourself:

```csharp
RuleFor(x => x.Address).InjectValidator<Order, Address>(serviceProvider);
```

## Error Handling & Result Format

### Iterating Errors

```csharp
var result = validator.Validate(user);
if (!result.IsValid)
{
  foreach (var kv in result.Errors)
  {
    Console.WriteLine($"— {kv.Key}:");
    foreach (var msg in kv.Value)
      Console.WriteLine($"   • {msg}");
  }
}
```
### Console Output Example:

```yaml
— Name:
• The Name field cannot be empty.
• The Name field must be at least 3 characters long.
— Email:
• The Email field must be a valid email address.
```
### JSON Payload for Web APIs

```json
{
  "Name": [
    "The Name field cannot be empty.",
    "The Name field must be at least 3 characters long."
  ],
  "Email": [
    "The Email field must be a valid email address."
  ]
}

```
Clients receive a dictionary where each key is a property name and each value is an array of messages.

## Example: CQRS + EF Core – Validating a Post Creation Command

This example shows how to wire up and use Vali‑Validation in a typical CQRS + Entity Framework Core scenario.

###  Define the Command

```csharp
using MediatR;

public class CreatePostCommand : IRequest<int>
{
    public string    Title       { get; set; }
    public string    Content     { get; set; }
    public DateTime  PublishDate { get; set; }
    public int       AuthorId    { get; set; }
}
```
### Create the Validator

```csharp
using Vali_Validation.Core.Rules;
using Vali_Validation.Core.Validators;

public class CreatePostCommandValidator : AbstractValidator<CreatePostCommand>
{
    public CreatePostCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotNull().WithMessage("Title is required.")
            .NotEmpty().WithMessage("Title cannot be empty.")
            .MinimumLength(5).WithMessage("Title must be at least 5 characters long.");

        RuleFor(x => x.Content)
            .NotNull().WithMessage("Content is required.")
            .NotEmpty().WithMessage("Content cannot be empty.");

        RuleFor(x => x.PublishDate)
            .GreaterThan(DateTime.UtcNow.AddDays(-1))
            .WithMessage("Publish date cannot be in the past.");

        RuleFor(x => x.AuthorId)
            .GreaterThan(0).WithMessage("AuthorId must be a positive integer.");
    }
}
```
### Implement the EF Core Handler

```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;

public class CreatePostHandler : IRequestHandler<CreatePostCommand, int>
{
    private readonly BloggingContext _context;

    public CreatePostHandler(BloggingContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(CreatePostCommand request, CancellationToken cancellationToken)
    {
        var post = new Post
        {
            Title       = request.Title,
            Content     = request.Content,
            PublishDate = request.PublishDate,
            AuthorId    = request.AuthorId
        };

        _context.Posts.Add(post);
        await _context.SaveChangesAsync(cancellationToken);
        return post.Id;
    }
}
```

### Register Validation and Pipeline Behavior

```csharp
using Vali_Validation.Core.Extensions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    // EF Core DbContext
    .AddDbContext<BloggingContext>(opts => 
        opts.UseSqlServer(builder.Configuration.GetConnectionString("Default")))

 services.AddMediatR(cfg => 
        {
            // MediatR handlers
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
           // Add the validation pipeline behavior
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        });

    // Vali‑Validation: automatically register all IValidator<T> in this assembly
    builder.Services.AddValidationsFromAssembly(Assembly.GetExecutingAssembly());

var app = builder.Build();

```
### Usage in Controller or Service

```csharp
[ApiController]
[Route("api/posts")]
public class PostsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PostsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreatePostCommand cmd)
    {
        var result = await _mediator.Send(cmd);
        return CreatedAtAction(nameof(GetById), new { id = result }, null);
    }

    // ...
}
```
If any rule fails, the **ValidationBehavior** in the pipeline will detect **IValidator<CreatePostCommand>**, call **Validate(cmd)**, and typically throw a **ValidationException*+ or return a 400 with the error dictionary:

```json
{
  "Title": [
    "Title must be at least 5 characters long."
  ],
  "PublishDate": [
    "Publish date cannot be in the past."
  ]
}
```
With this setup, every *CreatePostCommand** is automatically validated—no manual checks in your handlers or controllers.

> [!WARNING]
> To have your API automatically return the JSON error payload as shown below, you must register a MediatR **ValidationBehavior<TRequest,TResponse>** (or equivalent pipeline) that invokes all **IValidator<TRequest>** instances and throws a **ValidationException**. You must also add middleware (or exception filter) to catch that exception and write ex.Errors as the HTTP 400 response body.

## Features and Enhancements 🌟

### Recent Updates

- Fluent, chainable syntax
- Rich set of built‑in validators
- Automatic DI registration (**AddValidationsFromAssembly**)
- Lightweight: minimal dependencies
- Clean / Onion architecture–friendly
- Async rules (**MustAsync**, **DependentRuleAsync**), with `CancellationToken` propagated end to end
- Severity-aware results (**Severity.Error/Warning/Info** via **WithSeverity**) — warnings surface without failing validation
- **RuleSets** — tag rules with **InRuleSet** and run a subset via **IncludeRuleSets**
- Block-level **When**/**Unless** to wrap multiple rules under one shared condition
- **InjectValidator** — resolve nested validators from DI
- Reusable custom rules via **IPropertyValidator&lt;T&gt;** / **PropertyValidator&lt;T&gt;**
- Error message localization (English/Spanish built in, extensible via **LanguageManager.RegisterLanguage**)
- App-wide defaults via **ValiValidationOptions.Global**
- .NET 10 support

Follow the project on GitHub for updates on new features and improvements!

## Donations 💖
If you find **Vali-Validation** useful and would like to support its development, consider making a donation:

- **For Latin America**: [Donate via MercadoPago](https://link.mercadopago.com.pe/felipermm)
- **For International Donations**: [Donate via PayPal](https://paypal.me/felipeRMM?country.x=PE&locale.x=es_XC)


Your contributions help keep this project alive and improve its development! 🚀

## License 📜
This project is licensed under the [Apache License 2.0](https://www.apache.org/licenses/LICENSE-2.0).

## Contributions 🤝
Feel free to open issues and submit pull requests to improve this library!

## Estándares de Desarrollo

> Última actualización: 2026-09-21.

### Tamaño
- Líneas por archivo: 300
- Líneas por función: 30
- Parámetros por función: 3
- Longitud de línea: 120 caracteres

### Complejidad
- Ciclomática máxima: 10
- Cognitiva máxima: 15
- Anidamiento máximo: 3 niveles

### C# / .NET
- Nullable reference types: habilitado
- Multi-target: net7.0 / net8.0 / net9.0 — todo cambio debe compilar en los tres

### Tests
- Cobertura mínima: 80%
- Framework: xUnit
- Tipos requeridos: Unit + Integration (AspNetCore.Tests)

### Naming
- Clases/tipos/interfaces: PascalCase
- Métodos/variables/parámetros: camelCase
- Archivos: PascalCase (nombre del tipo)
- Constantes: PascalCase (convención .NET)

### Arquitectura
- Dependencias: paquetes de integración (AspNetCore/MediatR/ValiMediator) → core, nunca al revés
- Core sin dependencias externas más allá de `Microsoft.Extensions.DependencyInjection.Abstractions`
- Orden de imports: sistema → externas → internas

### PRs / Commits
- LOC máximas por PR: 400
- Formato: Conventional Commits
- Granularidad: un commit por unidad lógica de cambio

### Seguridad
- Secrets en código: Prohibido siempre
- Validación de inputs: Obligatoria en boundary
- Datos sensibles en logs: Prohibido siempre
