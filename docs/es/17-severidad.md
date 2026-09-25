# Severidad (Warnings vs Errors)

Por defecto, cualquier fallo de una regla bloquea la validación: hace que `ValidationResult.IsValid` sea `false` y aparece en `Errors`/`ErrorCodes`. `Severity` permite marcar una regla como **no bloqueante** — el fallo se sigue registrando, pero nunca hace fallar la validación ni aparece en la superficie de errores clásica.

Esto es útil para reglas de negocio que merecen la atención del llamador sin rechazar la solicitud: "este descuento es inusualmente alto", "este campo está deprecado, por favor migra", "esta orden está cerca del límite de crédito del cliente".

---

## El enum Severity

```csharp
namespace Vali_Validation.Core.Results;

public enum Severity
{
    Error = 0,   // Por defecto. Bloquea la validación.
    Warning = 1, // No bloqueante. Se registra, no afecta IsValid.
    Info = 2     // No bloqueante. Igual que Warning, con distinto peso semántico.
}
```

Toda regla es `Severity.Error` a menos que se indique explícitamente lo contrario.

---

## WithSeverity

`WithSeverity` es un modificador, exactamente igual que `WithMessage` o `WithErrorCode`: se aplica a la **última regla** de la cadena.

```csharp
public class OrderValidator : AbstractValidator<Order>
{
    public OrderValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty(); // Severity.Error (por defecto) — bloquea IsValid

        RuleFor(x => x.Discount)
            .LessThanOrEqualTo(50)
                .WithSeverity(Severity.Warning)
                .WithMessage("Discount above 50% requires manager approval.");
    }
}
```

Si `Discount` es `60`, el `ValidationResult` resultante:

```csharp
result.IsValid;                          // true — un Warning nunca bloquea
result.Failures.Count;                   // 1
result.Failures[0].Severity;             // Severity.Warning
result.Errors.ContainsKey("Discount");   // false — los fallos Warning no aparecen en Errors
```

Si `Email` también está vacío, `IsValid` pasa a `false` por el fallo de severidad `Error` — el `Warning` de `Discount` es irrelevante para esa decisión de cualquier forma.

### Combinándolo con otros modificadores

`WithSeverity` se combina con `WithMessage`, `WithErrorCode`, `When`/`Unless` en cualquier orden, igual que el resto de la cadena de modificadores:

```csharp
RuleFor(x => x.Inventory)
    .LessThan(10)
        .WithSeverity(Severity.Warning)
        .WithErrorCode("LOW_STOCK")
        .WithMessage("Inventory is running low ({PropertyValue} units left).")
    .When(x => x.TrackInventory);
```

---

## ValidationResult.Failures: la fuente de verdad

`Failures` es la lista completa y consciente de la severidad de todo lo que produjo una ejecución de validación — `Errors`/`ErrorCodes` son solo vistas filtradas sobre ella (únicamente `Severity.Error`), mantenidas por compatibilidad hacia atrás.

```csharp
public sealed class ValidationFailure
{
    public string PropertyName { get; }
    public string Message { get; }
    public Severity Severity { get; }
    public string? ErrorCode { get; }
}
```

```csharp
var result = validator.Validate(order);

foreach (var failure in result.Failures)
{
    Console.WriteLine($"[{failure.Severity}] {failure.PropertyName}: {failure.Message}");
}
// [Error] Email: The Email field cannot be empty.
// [Warning] Discount: Discount above 50% requires manager approval.
```

Filtra por severidad cuando necesites separar ambas:

```csharp
var blocking = result.Failures.Where(f => f.Severity == Severity.Error).ToList();
var warnings = result.Failures.Where(f => f.Severity == Severity.Warning).ToList();

if (warnings.Count > 0)
{
    _logger.LogWarning("Order passed validation with {Count} warning(s): {Messages}",
        warnings.Count, string.Join(" | ", warnings.Select(w => w.Message)));
}
```

### AddFailure vs AddError

`ValidationResult` expone tanto un método de propósito general como un atajo para el caso más común:

```csharp
// Severidad explícita y código de error (opcional)
result.AddFailure("Discount", "Discount exceeds the recommended threshold.", Severity.Warning);
result.AddFailure("Email", "The email is already in use.", Severity.Error, "EMAIL_ALREADY_EXISTS");

// AddError es exactamente AddFailure(property, message, Severity.Error, errorCode)
result.AddError("Email", "The email is already in use.", "EMAIL_ALREADY_EXISTS");
```

---

## Serialización

`ValidationResult` se serializa como `isValid` + un único arreglo `failures` mediante `System.Text.Json` con las opciones por defecto — no se requiere ningún converter personalizado ni naming policy. `Severity` se serializa como su nombre en texto.

```csharp
var json = JsonSerializer.Serialize(result);
```

```json
{
  "isValid": false,
  "failures": [
    { "property": "Email", "message": "The Email field must be a valid email address.", "severity": "Error" },
    { "property": "Discount", "message": "Discount above 50% requires manager approval.", "severity": "Warning" }
  ]
}
```

`errorCode` se incluye solo cuando está definido (se omite por completo del JSON cuando es `null`, no se serializa como `null`).

> Los diccionarios clásicos `Errors`/`ErrorCodes` están marcados como `[JsonIgnore]` — no aparecen en la salida serializada. Si código cliente existente parsea el formato antiguo `errors`/`errorCodes`, revisa [Migrando desde v2.x](#migrando-desde-v2x) más abajo antes de actualizar un contrato de API compartido.

---

## Dónde Severity NO aplica

`WithSeverity` sigue la misma regla de "última regla en la cadena" que `WithMessage`/`WithErrorCode`, con dos excepciones importantes:

1. **Reglas cross-property** (`RequiredIf`, `RequiredUnless`, `EqualToProperty` y el resto de reglas `*Property`) no se ven afectadas por una llamada a `.WithSeverity()` previa o posterior — siempre producen `Severity.Error`.
2. **`MustAsync` y `DependentRuleAsync`** siempre producen `Severity.Error` sin importar cualquier `.WithSeverity()` encadenado después. Llamar `.WithSeverity(...)` justo después de una de estas reglas silenciosamente no tiene efecto, o silenciosamente reasigna la severidad de otra regla síncrona anterior en la misma cadena, si existe alguna.

```csharp
// Esto NO convierte el fallo de MustAsync en un Warning — permanece como Severity.Error.
RuleFor(x => x.Email)
    .MustAsync(async (email, ct) => !await _users.ExistsByEmailAsync(email, ct))
        .WithMessage("That email is already in use.")
        .WithSeverity(Severity.Warning); // Sin efecto sobre la regla MustAsync
```

Si necesitas una verificación asíncrona no bloqueante, agrega el fallo manualmente con `result.AddFailure(...)` (ver [Reglas avanzadas — Custom](06-reglas-avanzadas.md#custom)) en lugar de depender de `WithSeverity` después de `MustAsync`.

---

## Migrando desde v2.x

- Si nunca necesitas las severidades `Warning`/`Info`, **no se requiere ningún cambio de código**. `Errors`, `ErrorCodes`, `ErrorsFor(...)`, `HasErrorFor(...)`, `FirstError(...)` e `IsValid` se comportan exactamente igual que antes — siempre reflejan únicamente los fallos `Severity.Error`.
- Si el código mutaba directamente `result.Errors`/`result.ErrorCodes`, o los asignaba a una variable tipada como `Dictionary<string, List<string>>`, eso ahora falla al compilar — ambos son `IReadOnlyDictionary<string, List<string>>`. El uso de solo lectura (lecturas por indexador, iteración, `ContainsKey`) no se ve afectado.
- Para leer los nuevos datos conscientes de severidad en vez de los diccionarios antiguos:

  ```csharp
  // Antiguo (v2.x)
  var emailErrors = result.Errors["Email"];

  // Nuevo (v3.x) — equivalente, pero pasando por Failures
  var emailErrors = result.Failures
      .Where(f => f.PropertyName == "Email")
      .Select(f => f.Message);
  ```

---

## Próximos pasos

- **[Resultado de validación](09-resultado-validacion.md)** — el resto de `ValidationResult`: `Errors`, `ErrorCodes`, `Merge`, testing
- **[Modificadores](07-modificadores.md)** — `WithMessage`, `WithErrorCode`, `When`/`Unless` y cómo se combinan con `WithSeverity`
- **[Conjuntos de reglas](18-conjuntos-de-reglas.md)** — restringir qué reglas se ejecutan por llamada de validación
