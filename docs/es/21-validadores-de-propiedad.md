# Validadores de propiedad (Property Validators)

`IPropertyValidator<TProperty>` es una alternativa de primera clase y reutilizable a escribir un predicado `.Must(...)` en línea o un método de extensión personalizado de `IRuleBuilder` (ver [Patrones avanzados — Extensiones de IRuleBuilder](15-patrones-avanzados.md)). Úsalo cuando una regla personalizada merezca probarse de forma aislada, o cuando quieras compartirla entre validadores sin exponer la maquinaria interna de registro de reglas del paquete.

---

## Cuándo usar SetPropertyValidator vs Must vs un método de extensión

| Enfoque | Ideal para |
|---|---|
| `.Must(predicate)` | Una verificación puntual, usada en un solo validador, sin necesidad de probarla de forma aislada |
| Método de extensión de `IRuleBuilder<T, TProperty>` | Una regla reutilizable que quieres invocar de forma fluida, p. ej. `.PasswordPolicy()` |
| `IPropertyValidator<TProperty>` + `.SetPropertyValidator(...)` | Una regla reutilizable que quieres probar como una clase independiente, sin depender de ningún `AbstractValidator<T>` |

Los tres enfoques se integran de forma idéntica con el resto de la cadena de modificadores (`WithMessage`, `WithErrorCode`, `WithSeverity`, `When`, `Unless`) — `SetPropertyValidator` es simplemente otro método de regla sobre `IRuleBuilder<T, TProperty>`.

---

## El contrato IPropertyValidator\<TProperty\>

```csharp
namespace Vali_Validation.Core.Rules;

public interface IPropertyValidator<TProperty>
{
    bool IsValid(TProperty value);

    IReadOnlyDictionary<string, string> Messages { get; }
}
```

- `IsValid` — el predicado. Devuelve `true` si el valor es válido.
- `Messages` — un diccionario código de idioma → texto de mensaje por defecto, usado cuando la regla falla y no hay un override `.WithMessage()` en la regla. Debe contener al menos una entrada. Consulta [Localización](19-localizacion.md#reglas-personalizadas-messages-de-ipropertyvalidator) para ver exactamente cómo se resuelve contra el idioma activo.

### Escribiendo uno directamente

```csharp
public class EvenNumberValidator : IPropertyValidator<int>
{
    public bool IsValid(int value) => value % 2 == 0;

    public IReadOnlyDictionary<string, string> Messages { get; } = new Dictionary<string, string>
    {
        ["en"] = "The {PropertyName} field must be an even number.",
        ["es"] = "El campo {PropertyName} debe ser un número par."
    };
}
```

```csharp
RuleFor(x => x.Age).SetPropertyValidator(new EvenNumberValidator());
```

---

## PropertyValidator\<TProperty\>: la clase base de conveniencia

Si no necesitas múltiples idiomas o quieres un mensaje por defecto rápido, extiende `PropertyValidator<TProperty>` en vez de implementar la interfaz directamente — te provee un `Messages` por defecto solo en inglés:

```csharp
namespace Vali_Validation.Core.Rules;

public abstract class PropertyValidator<TProperty> : IPropertyValidator<TProperty>
{
    public abstract bool IsValid(TProperty value);

    // Por defecto: { ["en"] = "The {PropertyName} field is invalid." }
    public virtual IReadOnlyDictionary<string, string> Messages { get; }
}
```

```csharp
public class EvenNumberValidator : PropertyValidator<int>
{
    public override bool IsValid(int value) => value % 2 == 0;

    // Sobrescribe solo si el mensaje en inglés por defecto no es suficiente
    public override IReadOnlyDictionary<string, string> Messages { get; } = new Dictionary<string, string>
    {
        ["en"] = "The {PropertyName} field must be an even number."
    };
}
```

Si no sobrescribes `Messages` en absoluto, un fallo se renderiza como: `"The Age field is invalid."`

---

## Adjuntarlo con SetPropertyValidator

`.SetPropertyValidator(IPropertyValidator<TProperty>)` adjunta el validador como la regla actual:

```csharp
public class ProductValidator : AbstractValidator<Product>
{
    public ProductValidator()
    {
        RuleFor(x => x.Sku)
            .NotEmpty()
            .SetPropertyValidator(new SkuFormatValidator())
                .WithErrorCode("INVALID_SKU_FORMAT");

        RuleFor(x => x.Quantity)
            .SetPropertyValidator(new EvenNumberValidator())
                .WithSeverity(Severity.Warning)
                .WithMessage("Quantity is usually shipped in pairs — double-check this order.");
    }
}
```

`SetPropertyValidator` lanza `ArgumentNullException` si `validator` es `null`.

---

## Ejemplo real: una regla de dominio compartida entre validadores

```csharp
// Compartida, probable de forma independiente
public class BusinessHoursValidator : PropertyValidator<TimeOnly>
{
    private static readonly TimeOnly Open = new(8, 0);
    private static readonly TimeOnly Close = new(18, 0);

    public override bool IsValid(TimeOnly value) => value >= Open && value < Close;

    public override IReadOnlyDictionary<string, string> Messages { get; } = new Dictionary<string, string>
    {
        ["en"] = "The {PropertyName} must be between 08:00 and 18:00.",
        ["es"] = "El campo {PropertyName} debe estar entre las 08:00 y las 18:00."
    };
}

public class ScheduleDeliveryValidator : AbstractValidator<ScheduleDeliveryRequest>
{
    public ScheduleDeliveryValidator()
    {
        RuleFor(x => x.RequestedTime)
            .SetPropertyValidator(new BusinessHoursValidator());
    }
}

public class ScheduleTechnicianVisitValidator : AbstractValidator<ScheduleTechnicianVisitRequest>
{
    public ScheduleTechnicianVisitValidator()
    {
        // La misma regla, reutilizada, sin duplicar la verificación de 08:00–18:00
        RuleFor(x => x.VisitTime)
            .SetPropertyValidator(new BusinessHoursValidator());
    }
}
```

### Probar el validador de forma aislada

Como `BusinessHoursValidator` es una clase simple sin ninguna dependencia de `AbstractValidator<T>` ni de una cadena `RuleFor`, es trivial probarla por su cuenta:

```csharp
public class BusinessHoursValidatorTests
{
    private readonly BusinessHoursValidator _validator = new();

    [Theory]
    [InlineData(8, 0, true)]
    [InlineData(17, 59, true)]
    [InlineData(7, 59, false)]
    [InlineData(18, 0, false)]
    public void IsValid_ReturnsExpectedResult(int hour, int minute, bool expected)
    {
        Assert.Equal(expected, _validator.IsValid(new TimeOnly(hour, minute)));
    }
}
```

---

## Placeholders en los mensajes

Solo `{PropertyName}` y `{PropertyValue}` se sustituyen en el texto de `Messages` de un `IPropertyValidator` — no existe un equivalente a los placeholders `{token}` personalizados estilo `Args` que usan internamente algunas reglas incorporadas.

```csharp
["en"] = "The {PropertyName} field ('{PropertyValue}') must be an even number."
```

---

## Próximos pasos

- **[Localización](19-localizacion.md#reglas-personalizadas-messages-de-ipropertyvalidator)** — cómo se resuelve exactamente `Messages` contra el idioma activo
- **[Patrones avanzados](15-patrones-avanzados.md)** — la alternativa de método de extensión de `IRuleBuilder` para reglas reutilizables
- **[Severidad](17-severidad.md)** — `.WithSeverity(...)` funciona sobre reglas `SetPropertyValidator` como sobre cualquier otra
