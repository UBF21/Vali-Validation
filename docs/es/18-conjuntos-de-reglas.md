# Conjuntos de reglas (Rule Sets)

Los conjuntos de reglas permiten que un mismo validador ejecute selectivamente un subconjunto de sus reglas para una llamada en particular, en lugar de ejecutar siempre todo. Caso de uso típico: el mismo DTO se valida de forma distinta en diferentes etapas de un flujo — por ejemplo, guardar un "borrador" solo necesita unos pocos campos requeridos, mientras que el "checkout" necesita el conjunto completo.

---

## Etiquetar reglas con InRuleSet

`InRuleSet` es un modificador disponible en `IRuleBuilder<T, TProperty>` — etiqueta la(s) regla(s) definida(s) por ese builder con uno o más nombres.

```csharp
public class CheckoutValidator : AbstractValidator<CheckoutRequest>
{
    public CheckoutValidator()
    {
        // Siempre se ejecuta — sin llamada a InRuleSet, etiqueta implícita "default"
        RuleFor(x => x.CartId).NotEmpty();

        // Solo se ejecuta cuando se incluye "checkout"
        RuleFor(x => x.PaymentMethod)
            .NotEmpty()
                .WithMessage("Select a payment method before checking out.")
            .InRuleSet("checkout");

        RuleFor(x => x.ShippingAddress)
            .NotNull()
            .SetValidator(new AddressValidator())
            .InRuleSet("checkout");

        // Etiquetada con dos conjuntos de reglas — se ejecuta si se incluye cualquiera de los dos
        RuleFor(x => x.CouponCode)
            .MustAsync(async (code, ct) => await _coupons.IsValidAsync(code, ct))
                .WithMessage("The coupon code is not valid.")
            .InRuleSet("checkout", "coupon-preview");
    }
}
```

Las reglas que **no** tienen llamada a `InRuleSet` llevan la etiqueta implícita `"default"`.

Llamar a `InRuleSet` más de una vez sobre el mismo builder es aditivo — agrega más etiquetas, no reemplaza las existentes:

```csharp
RuleFor(x => x.TaxId)
    .NotEmpty()
    .InRuleSet("checkout")
    .InRuleSet("business-account"); // Ahora etiquetada con AMBOS "checkout" y "business-account"
```

---

## Restringir una llamada de validación a conjuntos de reglas específicos

Usa `IncludeRuleSets` sobre el `ValidationOptions` que se pasa a `Validate`/`ValidateAsync`:

```csharp
// Solo ejecuta las reglas etiquetadas "checkout" (más... ver la nota sobre "default" abajo)
var result = validator.Validate(request, o => o.IncludeRuleSets("checkout"));

// El overload async recibe un CancellationToken igual que el resto de la API async
var result = await validator.ValidateAsync(request, o => o.IncludeRuleSets("checkout"), ct);

// Múltiples conjuntos de reglas — una regla se ejecuta si coincide con CUALQUIERA de los nombres incluidos
var result = validator.Validate(request, o => o.IncludeRuleSets("checkout", "coupon-preview"));
```

`IncludeRuleSets` lanza `ArgumentException` si se llama sin argumentos — no hay forma de pasar un filtro vacío a través de este overload y obtener "no ejecutar nada" o "ejecutar todo" por accidente.

### El conjunto de reglas implícito "default"

Las reglas sin llamada a `InRuleSet` **no** se incluyen automáticamente solo porque se ejecutó una validación filtrada. Si quieres que las reglas sin etiqueta se ejecuten junto con un conjunto de reglas nombrado, incluye `"default"` explícitamente:

```csharp
// Ejecuta ÚNICAMENTE las reglas etiquetadas "checkout" — CartId (sin etiqueta, "default") se OMITE
validator.Validate(request, o => o.IncludeRuleSets("checkout"));

// Ejecuta las reglas sin etiqueta Y las reglas etiquetadas "checkout"
validator.Validate(request, o => o.IncludeRuleSets("default", "checkout"));
```

### Llamar a Validate sin opciones ejecuta todo

`Validate(instance)` (sin lambda de `ValidationOptions`) no se ve afectado en absoluto por los conjuntos de reglas — ejecuta todas las reglas sin importar ninguna etiqueta `InRuleSet`, exactamente como antes de que existieran los conjuntos de reglas:

```csharp
// Ejecuta TODAS las reglas — CartId, PaymentMethod, ShippingAddress, CouponCode
var result = validator.Validate(request);
```

No existe ningún overload que permita pasar un filtro de conjuntos de reglas *vacío* a `Validate(instance, configureOptions)` para obtener el comportamiento de "ejecutar todo" de esa forma — usa el overload sin parámetros para eso.

---

## Combinar IncludeRuleSets con WithLanguage

`ValidationOptions` también incluye `WithLanguage` (ver [Localización](19-localizacion.md)) — ambos se configuran sobre el mismo objeto de opciones y se encadenan de forma fluida:

```csharp
var result = validator.Validate(request, o => o
    .IncludeRuleSets("checkout")
    .WithLanguage("es"));
```

---

## Ejemplo real: wizard de varios pasos

```csharp
public class CreateListingRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public decimal? Price { get; set; }
    public string? Category { get; set; }
    public List<string> Photos { get; set; } = new();
    public bool ShippingIncluded { get; set; }
    public string? ShippingCountry { get; set; }
}

public class CreateListingValidator : AbstractValidator<CreateListingRequest>
{
    public CreateListingValidator()
    {
        // Paso 1: información básica — requerida en todos los pasos
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Category).NotEmpty();

        // Paso 2: precio — solo se valida cuando el usuario llega a ese paso
        RuleFor(x => x.Price)
            .NotNull()
            .GreaterThan(0m)
            .InRuleSet("pricing");

        // Paso 3: fotos — solo se valida justo antes de publicar
        RuleFor(x => x.Photos)
            .MinCount(1)
                .WithMessage("Add at least one photo before publishing.")
            .InRuleSet("publish");

        RuleFor(x => x.ShippingCountry)
            .NotEmpty()
                .WithMessage("Select a shipping country.")
            .InRuleSet("publish")
            .When(x => x.ShippingIncluded);
    }
}
```

Uso por paso del wizard:

```csharp
// Paso 1: solo las reglas default (Title, Category)
var step1 = validator.Validate(request);
step1 = validator.Validate(request, o => o.IncludeRuleSets("default"));

// Paso 2: default + pricing
var step2 = validator.Validate(request, o => o.IncludeRuleSets("default", "pricing"));

// Publicación final: todo
var publishResult = validator.Validate(request, o => o.IncludeRuleSets("default", "pricing", "publish"));

// Equivalente a publishResult — sin filtro se ejecuta todo incondicionalmente
var publishResultAlt = validator.Validate(request);
```

---

## Rule Sets vs When/Unless

Los conjuntos de reglas y `When`/`Unless` resuelven problemas distintos y a menudo se combinan:

| | Conjuntos de reglas | `When` / `Unless` |
|---|---|---|
| Lo decide | El **llamador** de `Validate`/`ValidateAsync`, por cada llamada | Los **propios datos**, evaluados durante la validación |
| Uso típico | "Solo ejecutar las reglas de checkout durante el checkout" | "Solo requerir el NIF para clientes empresa" |
| Granularidad | Reglas completas incluidas/excluidas por llamada | Las reglas siempre se consideran, la condición se evalúa contra la instancia |

Se combinan libremente — una regla puede estar etiquetada con `InRuleSet` y a la vez protegida con `When`:

```csharp
RuleFor(x => x.ShippingCountry)
    .NotEmpty()
    .InRuleSet("publish")
    .When(x => x.ShippingIncluded);
```

---

## Próximos pasos

- **[Modificadores](07-modificadores.md)** — la cadena completa de modificadores (`WithMessage`, `When`/`Unless`, etc.) a la que se une `InRuleSet`
- **[Localización](19-localizacion.md)** — `ValidationOptions.WithLanguage`, configurado junto a `IncludeRuleSets`
- **[Validadores](04-validadores.md)** — los overloads de `Validate`/`ValidateAsync` en detalle
