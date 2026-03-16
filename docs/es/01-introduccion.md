# Introducción a Vali-Validation

## ¿Qué es Vali-Validation?

Vali-Validation es una biblioteca de validación fluent para .NET, diseñada para ser ligera, expresiva y sin dependencias externas innecesarias. Permite definir reglas de validación de forma declarativa mediante una API fluent, separando la lógica de validación del modelo de dominio y de los controladores.

La biblioteca sigue el mismo patrón conceptual que FluentValidation, pero está construida desde cero con un enfoque en:

- **Cero dependencias externas** en el paquete core (solo `Microsoft.Extensions.DependencyInjection.Abstractions`)
- **Soporte nativo de async/await** sin los problemas de deadlock presentes en otras bibliotecas
- **Integración de primera clase** con el ecosistema Vali (Vali-Mediator)
- **API moderna** aprovechando las características de C# 11 y .NET 7+

## ¿Por qué existe?

FluentValidation es excelente, pero en proyectos que usan Vali-Mediator o que buscan minimizar dependencias transitivas, Vali-Validation ofrece:

1. **Integración nativa con `Result<T>`** — En lugar de lanzar excepciones al usar Vali-Mediator, el behavior devuelve `Result<T>.Fail(...)` directamente, sin try/catch en los handlers.
2. **Sin dependencias de terceros** en el core — No se arrastra NuGet packages que podrían tener conflictos de versión.
3. **ValidateParallelAsync** — Ejecuta reglas asíncronas en paralelo de forma nativa, sin configuración adicional.
4. **API de colecciones rica** — `RuleForEach`, `Unique()`, `AllSatisfy()`, `AnySatisfy()`, `In()`, `NotIn()` incluidos de serie.

## Comparación con FluentValidation

| Característica | Vali-Validation | FluentValidation |
|---|---|---|
| Dependencias externas (core) | Solo DI Abstractions | Ninguna |
| Soporte async nativo | Sí, con CT | Sí, con CT |
| Validación en paralelo | `ValidateParallelAsync` | No nativo |
| Integración Mediator | ValiMediator (Result<T>) | MediatR (excepción) |
| `Transform<TNew>()` | Sí | Sí (`Transform`) |
| `Custom()` con contexto | Sí (`CustomContext<T>`) | Sí (`ValidationContext<T>`) |
| `SetValidator` anidado | Sí | Sí |
| `RuleForEach` | Sí | Sí |
| `Include` (herencia) | Sí | Sí |
| `When` / `Unless` async | `WhenAsync` / `UnlessAsync` | No nativo |
| Reglas de password | `HasUppercase`, `HasDigit`, etc. | Extensiones separadas |
| Reglas de colección | `Unique`, `AllSatisfy`, `In`, etc. | Parcial |
| Tarjetas de crédito (Luhn) | `CreditCard()` | Sí |
| Licencia | MIT | Apache 2.0 |
| Targets | net7/8/9 | netstandard2.0+ |

> **Nota:** Si ya usas FluentValidation en un proyecto maduro con muchas reglas personalizadas, la migración puede no valer la pena solo por el cambio. Vali-Validation brilla especialmente en proyectos nuevos que usan Vali-Mediator o que quieren un stack completamente controlado.

## Ecosistema de paquetes

Vali-Validation está dividido en paquetes separados para que solo instales lo que necesitas:

### `Vali-Validation` (core)

El paquete principal. Contiene:

- `AbstractValidator<T>` — clase base para todos los validadores
- `IValidator<T>` — interfaz para inyección de dependencias
- `IRuleBuilder<T, TProperty>` — interfaz fluent con todas las reglas
- `ValidationResult` — resultado con errores y códigos
- `ValidationException` — excepción tipada
- DI registration (`AddValidationsFromAssembly`)

**Dependencia:** `Microsoft.Extensions.DependencyInjection.Abstractions` (solo la abstracción, no el contenedor completo)

```xml
<PackageReference Include="Vali-Validation" Version="*" />
```

### `Vali-Validation.MediatR`

Integración con **MediatR**. Registra un `IPipelineBehavior<TRequest, TResponse>` que valida automáticamente el request antes de llegar al handler. Si la validación falla, lanza `ValidationException`.

```xml
<PackageReference Include="Vali-Validation.MediatR" Version="*" />
```

### `Vali-Validation.ValiMediator`

Integración con **Vali-Mediator**. Similar al anterior, pero cuando `TResponse` es `Result<T>`, devuelve `Result<T>.Fail(errors, ErrorType.Validation)` en lugar de lanzar una excepción. Esto permite manejar errores de validación en el mismo flujo que otros errores de dominio.

```xml
<PackageReference Include="Vali-Validation.ValiMediator" Version="*" />
```

### `Vali-Validation.AspNetCore`

Integración con **ASP.NET Core**. Incluye:

- Middleware que captura `ValidationException` y devuelve HTTP 400 con formato `application/problem+json`
- `ValiValidationFilter<T>` para Minimal API (endpoint filter)
- `ValiValidateAttribute` para controladores MVC (action filter)

```xml
<PackageReference Include="Vali-Validation.AspNetCore" Version="*" />
```

## Tabla de compatibilidad

| Paquete | net7.0 | net8.0 | net9.0 |
|---|---|---|---|
| `Vali-Validation` | ✓ | ✓ | ✓ |
| `Vali-Validation.MediatR` | ✓ | ✓ | ✓ |
| `Vali-Validation.ValiMediator` | ✓ | ✓ | ✓ |
| `Vali-Validation.AspNetCore` | ✓ | ✓ | ✓ |

> El código fuente del core usa C# 11. No se usan características de C# 12+ (collection expressions `[]`, primary constructors en clases, etc.), por lo que es compatible con el toolchain de .NET 7 SDK en adelante.

## Relación con Vali-Mediator

Vali-Validation forma parte del ecosistema **Vali**, junto a **Vali-Mediator**. Vali-Mediator es un mediador ligero (similar a MediatR) que usa `Result<T>` como tipo de retorno estándar para manejar errores sin excepciones.

La integración `Vali-Validation.ValiMediator` conecta ambos mundos: el validador detecta si el tipo de respuesta es `Result<T>` y, en ese caso, retorna el fallo en lugar de lanzar una excepción. Esto significa que el código del handler nunca necesita un try/catch para errores de validación:

```csharp
// Sin Vali-Validation.ValiMediator:
public async Task<Result<OrderDto>> Handle(CreateOrderCommand command)
{
    try
    {
        // lógica...
    }
    catch (ValidationException ex)
    {
        return Result<OrderDto>.Fail(ex.Message, ErrorType.Validation);
    }
}

// Con Vali-Validation.ValiMediator:
public async Task<Result<OrderDto>> Handle(CreateOrderCommand command)
{
    // Si la validación falla, el behavior devuelve Result<OrderDto>.Fail(...) automáticamente.
    // Este código nunca se ejecuta si el command no es válido.
    var order = await _orderService.CreateAsync(command);
    return Result<OrderDto>.Ok(new OrderDto(order));
}
```

## Conceptos fundamentales

Antes de entrar en el detalle, hay tres conceptos clave:

### 1. Separación validador/modelo

Los validadores viven en clases separadas del modelo. El modelo no sabe que existe un validador.

```csharp
// Modelo limpio, sin anotaciones
public class CreateUserRequest
{
    public string Name { get; set; }
    public string Email { get; set; }
    public int Age { get; set; }
}

// Validador separado
public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().Email();
        RuleFor(x => x.Age).GreaterThan(0).LessThan(150);
    }
}
```

### 2. Validación como pipeline

En aplicaciones reales, la validación ocurre antes de que el handler de un comando/query procese la solicitud. Los paquetes de integración implementan esto como un pipeline behavior:

```
Request → ValidationBehavior → Handler → Response
              ↓ (fallo)
           ValidationException / Result<T>.Fail
```

### 3. ValidationResult como valor

`ValidationResult` es un objeto de valor que contiene todos los errores encontrados. La validación siempre se completa (no lanza por defecto), permitiendo recopilar todos los errores en una sola pasada:

```csharp
var result = await validator.ValidateAsync(request);
if (!result.IsValid)
{
    // result.Errors tiene TODOS los errores, no solo el primero
    foreach (var (property, errors) in result.Errors)
    {
        Console.WriteLine($"{property}: {string.Join(", ", errors)}");
    }
}
```

## Siguientes pasos

- **[Instalación](02-instalacion.md)** — Cómo instalar cada paquete y cuándo usar cada uno
- **[Inicio rápido](03-inicio-rapido.md)** — Un ejemplo completo en minutos
- **[Validadores](04-validadores.md)** — AbstractValidator en profundidad
