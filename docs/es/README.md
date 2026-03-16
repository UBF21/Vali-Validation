# Documentación de Vali-Validation (Español)

Bienvenido a la documentación en español de **Vali-Validation**, la biblioteca de validación fluent para .NET 7/8/9 del ecosistema Vali.

---

## Índice

### Fundamentos

| # | Documento | Descripción |
|---|---|---|
| 01 | [Introducción](01-introduccion.md) | Qué es Vali-Validation, comparación con FluentValidation, ecosistema de paquetes y tabla de compatibilidad |
| 02 | [Instalación](02-instalacion.md) | Cómo instalar cada paquete NuGet, cuándo usar cada uno, estructura de proyectos recomendada |
| 03 | [Inicio rápido](03-inicio-rapido.md) | Ejemplo completo desde cero: modelo, validador, DI, endpoint y prueba en 10 minutos |

### Validadores y reglas

| # | Documento | Descripción |
|---|---|---|
| 04 | [Validadores](04-validadores.md) | `AbstractValidator<T>`, `RuleFor`, `RuleForEach`, `Include`, `RuleSwitch`, métodos de validación (`ValidateAsync`, `ValidateParallelAsync`, `ValidateAndThrow`) |
| 05 | [Reglas básicas](05-reglas-basicas.md) | Catálogo completo de reglas síncronas: nulidad, igualdad, longitud, rangos numéricos, strings, formato, fechas y colecciones |
| 06 | [Reglas avanzadas](06-reglas-avanzadas.md) | `Must`, `MustAsync`, `DependentRuleAsync`, `Custom` con contexto, `Transform`, `SetValidator`, `SwitchOn`, `RuleForEach` avanzado, reglas cross-property, requerido condicional |
| 07 | [Modificadores](07-modificadores.md) | `WithMessage` (con placeholders), `WithErrorCode`, `OverridePropertyName`, `StopOnFirstFailure`, `When`/`Unless`, `WhenAsync`/`UnlessAsync` |
| 08 | [CascadeMode](08-cascade-mode.md) | Control del flujo de validación por propiedad vs global, cuándo usar cada modo |

### Resultados y errores

| # | Documento | Descripción |
|---|---|---|
| 09 | [Resultado de validación](09-resultado-validacion.md) | `ValidationResult` completo: `Errors`, `ErrorCodes`, `IsValid`, `ErrorCount`, `ToFlatList`, `Merge`, `HasErrorFor`, uso en APIs y testing |
| 10 | [Excepciones](10-excepciones.md) | `ValidationException`, `ValidateAndThrow` vs resultado de valor, cuándo usar cada enfoque, captura en middleware |

### Integración y DI

| # | Documento | Descripción |
|---|---|---|
| 11 | [Inyección de dependencias](11-inyeccion-dependencias.md) | `AddValidationsFromAssembly`, lifetimes, `IValidator<T>` en constructores, ejemplo completo de `Program.cs`, testing con mocks |
| 12 | [ASP.NET Core](12-integracion-aspnetcore.md) | Middleware `UseValiValidationExceptionHandler`, `WithValiValidation<T>` para Minimal API, `[ValiValidate]` para MVC, cuándo usar cada uno |
| 13 | [MediatR](13-integracion-mediatr.md) | `Vali-Validation.MediatR`: setup, behavior pipeline, ejemplo completo con command/validator/handler |
| 14 | [Vali-Mediator](14-integracion-valimediator.md) | `Vali-Validation.ValiMediator`: behavior con `Result<T>` vs tipos simples, ejemplo completo |

### Patrones avanzados

| # | Documento | Descripción |
|---|---|---|
| 15 | [Patrones avanzados](15-patrones-avanzados.md) | Validadores anidados con `SetValidator`, `Include` para herencia, validación condicional compleja con `RuleSwitch`/`SwitchOn`, passwords, colecciones anidadas, extensiones de `IRuleBuilder`, combinación de nuevas reglas en escenarios reales |
| 16 | [Switch / Case](16-switch-case.md) | Referencia completa de `RuleSwitch` y `SwitchOn`: sintaxis, 12+ ejemplos reales (e-commerce, multi-tenant, préstamos, notificaciones, mediciones científicas), árbol de decisión, tests xUnit, antipatrones |

---

## Guía de lectura rápida

### Soy nuevo en Vali-Validation

1. Lee [Introducción](01-introduccion.md) para entender el propósito y el ecosistema
2. Sigue el [Inicio rápido](03-inicio-rapido.md) para tener algo funcionando
3. Consulta [Reglas básicas](05-reglas-basicas.md) cuando necesites una regla específica

### Quiero integrar con ASP.NET Core

1. [Instalación](02-instalacion.md) — paquetes `Vali-Validation` + `Vali-Validation.AspNetCore`
2. [Inyección de dependencias](11-inyeccion-dependencias.md) — registro en `Program.cs`
3. [ASP.NET Core](12-integracion-aspnetcore.md) — middleware, filtros y atributos

### Quiero integrar con MediatR

1. [Instalación](02-instalacion.md) — paquete `Vali-Validation.MediatR`
2. [MediatR](13-integracion-mediatr.md) — setup y ejemplo completo

### Quiero integrar con Vali-Mediator

1. [Instalación](02-instalacion.md) — paquete `Vali-Validation.ValiMediator`
2. [Vali-Mediator](14-integracion-valimediator.md) — setup y comportamiento con `Result<T>`

### Tengo un caso de uso complejo

1. [Reglas avanzadas](06-reglas-avanzadas.md) — `MustAsync`, `Custom`, `Transform`, `SetValidator`, `SwitchOn`
2. [Modificadores](07-modificadores.md) — `When`/`Unless`, `WhenAsync`/`UnlessAsync`
3. [Validadores](04-validadores.md) — `RuleSwitch` para validación condicional por casos sobre múltiples propiedades
4. [Patrones avanzados](15-patrones-avanzados.md) — composición, herencia, colecciones anidadas, validación polimórfica

---

## Referencia rápida de reglas

### Por categoría

| Categoría | Reglas principales |
|---|---|
| Nulidad/vacío | `NotNull`, `Null`, `NotEmpty`, `Empty` |
| Igualdad | `EqualTo`, `NotEqual`, `EqualToProperty` |
| Longitud | `MinimumLength`, `MaximumLength`, `LengthBetween` |
| Rango numérico | `GreaterThan`, `LessThan`, `Between`, `Positive`, `NonNegative`, `Negative`, `NotZero`, `Odd`, `Even`, `MultipleOf`, `MultipleOfProperty`, `MaxDecimalPlaces`, `Percentage`, `Precision` |
| Cross-property | `GreaterThanProperty`, `GreaterThanOrEqualToProperty`, `LessThanProperty`, `LessThanOrEqualToProperty`, `NotEqualToProperty`, `MultipleOfProperty` |
| Requerido condicional | `RequiredIf`, `RequiredUnless` |
| Strings | `Matches`, `MustContain`, `StartsWith`, `EndsWith`, `IsAlpha`, `IsAlphanumeric`, `IsNumeric`, `Lowercase`, `Uppercase`, `NoWhitespace`, `MinWords`, `MaxWords`, `Slug`, `NoHtmlTags`, `NoSqlInjectionPatterns` |
| Formato | `Email`, `Url`, `PhoneNumber`, `IPv4`, `IPv6`, `MacAddress`, `CreditCard`, `Guid`, `NotEmptyGuid`, `IsEnum<T>`, `Iban`, `CountryCode`, `CurrencyCode`, `IsValidJson`, `IsValidBase64`, `Latitude`, `Longitude` |
| Password | `HasUppercase`, `HasLowercase`, `HasDigit`, `HasSpecialChar`, `PasswordPolicy` |
| Fechas | `FutureDate`, `PastDate`, `Today`, `MinAge`, `MaxAge`, `DateBetween`, `NotExpired`, `WithinNext`, `WithinLast`, `IsWeekday`, `IsWeekend` |
| Colecciones | `NotEmptyCollection`, `HasCount`, `MinCount`, `MaxCount`, `Unique`, `AllSatisfy`, `AnySatisfy`, `In`, `NotIn` |
| Personalizadas | `Must`, `MustAsync`, `DependentRuleAsync`, `Custom`, `Transform`, `SetValidator` |
| Validación por casos | `RuleSwitch` (múltiples propiedades según discriminador), `SwitchOn` (una propiedad con reglas distintas por valor) |

### Modificadores disponibles

| Modificador | Efecto |
|---|---|
| `.WithMessage(msg)` | Reemplaza el mensaje de la última regla |
| `.WithErrorCode(code)` | Agrega código a `ErrorCodes` para la última regla |
| `.OverridePropertyName(name)` | Cambia la clave en `Errors` para todo el builder |
| `.StopOnFirstFailure()` | Detiene la evaluación de la propiedad al primer fallo |
| `.When(condition)` | Aplica las reglas solo si la condición es verdadera |
| `.Unless(condition)` | Aplica las reglas solo si la condición es falsa |
| `.WhenAsync(condition)` | `When` con condición asíncrona |
| `.UnlessAsync(condition)` | `Unless` con condición asíncrona |

---

## Paquetes NuGet

| Paquete | Instalación |
|---|---|
| Core | `dotnet add package Vali-Validation` |
| MediatR | `dotnet add package Vali-Validation.MediatR` |
| Vali-Mediator | `dotnet add package Vali-Validation.ValiMediator` |
| ASP.NET Core | `dotnet add package Vali-Validation.AspNetCore` |

---

## Recursos adicionales

- **Repositorio GitHub:** [Vali-Validation](https://github.com/feliperafaelmontenegro/Vali-Validation)
- **Repositorio Vali-Mediator:** [Vali-Mediator](https://github.com/feliperafaelmontenegro/Vali-Mediator)
- **NuGet:** [nuget.org/profiles/feliperafaelmontenegro](https://nuget.org)
