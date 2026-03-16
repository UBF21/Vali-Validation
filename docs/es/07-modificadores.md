# Modificadores de reglas

Los modificadores se encadenan después de una regla para cambiar su comportamiento o el mensaje de error. Se aplican a la **última regla** definida en la cadena.

---

## WithMessage

`WithMessage` reemplaza el mensaje de error predeterminado de la última regla.

```csharp
RuleFor(x => x.Email)
    .NotEmpty()
        .WithMessage("El email es obligatorio.")
    .Email()
        .WithMessage("El formato del email no es válido.");
```

### Placeholders

`WithMessage` soporta dos placeholders que se sustituyen en tiempo de ejecución:

| Placeholder | Valor |
|---|---|
| `{PropertyName}` | Nombre de la propiedad (o el valor de `OverridePropertyName`) |
| `{PropertyValue}` | Valor actual de la propiedad |

```csharp
RuleFor(x => x.Username)
    .MaximumLength(50)
        .WithMessage("El campo {PropertyName} no puede superar los 50 caracteres. Valor actual: '{PropertyValue}'.");
```

Si el username es `"este_nombre_de_usuario_es_demasiado_largo"`, el error será:

```
El campo Username no puede superar los 50 caracteres. Valor actual: 'este_nombre_de_usuario_es_demasiado_largo'.
```

### Ejemplo con múltiples mensajes

```csharp
public class CreateProductValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
                .WithMessage("El nombre del producto es obligatorio.")
            .MinimumLength(3)
                .WithMessage("El nombre '{PropertyValue}' es demasiado corto (mínimo 3 caracteres).")
            .MaximumLength(200)
                .WithMessage("El nombre no puede superar los 200 caracteres.");

        RuleFor(x => x.Price)
            .GreaterThan(0m)
                .WithMessage("El precio debe ser mayor que 0. Valor recibido: {PropertyValue}.")
            .MaxDecimalPlaces(2)
                .WithMessage("El precio no puede tener más de 2 decimales.");

        RuleFor(x => x.Stock)
            .GreaterThanOrEqualTo(0)
                .WithMessage("El campo {PropertyName} no puede ser negativo.");
    }
}
```

### Anti-patrón: mensaje genérico

```csharp
// Incorrecto: un solo WithMessage para todas las reglas
RuleFor(x => x.Email)
    .NotEmpty()
    .Email()
    .MaximumLength(320)
    .WithMessage("Email inválido."); // Solo aplica a MaximumLength, no a todas las anteriores
```

`WithMessage` siempre se aplica a la **última regla** antes de él. Para mensajes por regla, coloca `WithMessage` inmediatamente después de cada regla.

---

## WithErrorCode

`WithErrorCode` asigna un código de error a la última regla. Los códigos aparecen en `ValidationResult.ErrorCodes`, que es útil para respuestas de API estructuradas o para localización del lado del cliente.

```csharp
RuleFor(x => x.Email)
    .NotEmpty()
        .WithErrorCode("EMAIL_REQUIRED")
    .Email()
        .WithErrorCode("EMAIL_INVALID_FORMAT")
    .MustAsync(async (email, ct) => !await _users.ExistsByEmailAsync(email, ct))
        .WithErrorCode("EMAIL_ALREADY_EXISTS")
        .WithMessage("Ya existe una cuenta con ese email.");
```

Puedes combinar `WithMessage` y `WithErrorCode` en cualquier orden:

```csharp
RuleFor(x => x.Age)
    .GreaterThanOrEqualTo(18)
        .WithMessage("Debes tener al menos 18 años.")
        .WithErrorCode("AGE_BELOW_MINIMUM");
```

### Uso en la respuesta de API

```csharp
var result = await validator.ValidateAsync(request);
if (!result.IsValid)
{
    return BadRequest(new
    {
        errors = result.Errors,
        errorCodes = result.ErrorCodes
    });
}
```

Respuesta:

```json
{
  "errors": {
    "Email": ["Ya existe una cuenta con ese email."]
  },
  "errorCodes": {
    "Email": ["EMAIL_ALREADY_EXISTS"]
  }
}
```

### Códigos estándar recomendados

```csharp
public static class ValidationCodes
{
    public const string Required = "REQUIRED";
    public const string InvalidFormat = "INVALID_FORMAT";
    public const string TooShort = "TOO_SHORT";
    public const string TooLong = "TOO_LONG";
    public const string AlreadyExists = "ALREADY_EXISTS";
    public const string NotFound = "NOT_FOUND";
    public const string Unauthorized = "UNAUTHORIZED";
    public const string OutOfRange = "OUT_OF_RANGE";
}

public class OrderValidator : AbstractValidator<CreateOrderRequest>
{
    public OrderValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty()
                .WithErrorCode(ValidationCodes.Required)
            .MustAsync(async id => await _products.ExistsAsync(id))
                .WithErrorCode(ValidationCodes.NotFound)
                .WithMessage("El producto no existe.");
    }
}
```

---

## OverridePropertyName

`OverridePropertyName` cambia la clave que aparece en `ValidationResult.Errors`. Útil cuando el nombre de la propiedad en C# no es amigable para el cliente.

```csharp
public class UpdateUserDtoValidator : AbstractValidator<UpdateUserDto>
{
    public UpdateUserDtoValidator()
    {
        // En el DTO la propiedad se llama Email, pero el cliente espera "emailAddress"
        RuleFor(x => x.Email)
            .NotEmpty()
            .Email()
            .OverridePropertyName("emailAddress");

        // Propiedad anidada — cambiar el nombre completo
        RuleFor(x => x.Address.PostalCode)
            .Matches(@"^\d{5}$")
            .OverridePropertyName("postalCode"); // En lugar de "Address.PostalCode"
    }
}
```

Resultado con `OverridePropertyName`:

```json
{
  "emailAddress": ["The email field is not valid."],
  "postalCode": ["The postal code must have 5 digits."]
}
```

### Expresiones complejas

Si usas `RuleFor` con expresiones que no son simples member access (ej: `x => x.Name.ToLower()`), el nombre de propiedad extraído puede ser incorrecto. Usa `OverridePropertyName` para solucionarlo:

```csharp
RuleFor(x => x.Name.Trim())
    .NotEmpty()
    .OverridePropertyName("Name"); // Sin esto, el nombre podría ser incorrecto
```

---

## StopOnFirstFailure

`StopOnFirstFailure` detiene la evaluación de las reglas de esa propiedad tan pronto como una falla. Las reglas siguientes no se evalúan.

```csharp
RuleFor(x => x.Email)
    .NotNull()
        .WithMessage("El email es obligatorio.")
    .NotEmpty()
        .WithMessage("El email no puede estar vacío.")
    .Email()
        .WithMessage("El email no tiene formato válido.")
    .MustAsync(async email => !await _users.ExistsByEmailAsync(email))
        .WithMessage("Ese email ya está registrado.")
    .StopOnFirstFailure(); // Si NotNull falla, no evalúa las siguientes
```

Sin `StopOnFirstFailure`, si `Email` es `null`:
- Falla `NotNull` → "El email es obligatorio."
- Falla `NotEmpty` → "El email no puede estar vacío."
- Falla `Email` → "El email no tiene formato válido."
- Se llama `MustAsync` con `null` → posible excepción

Con `StopOnFirstFailure`, si `Email` es `null`:
- Falla `NotNull` → "El email es obligatorio."
- Las demás reglas no se evalúan

> Este es el modificador por propiedad. Para detener todas las propiedades tras el primer fallo, usa `CascadeMode` global. Ver [CascadeMode](08-cascade-mode.md).

### Cuándo es esencial

`StopOnFirstFailure` es especialmente importante cuando:

1. Las reglas posteriores asumen que las anteriores pasaron (ej: `Email()` asume que el string no es null)
2. Las reglas asíncronas son costosas y no quieres hacer llamadas innecesarias a la BD
3. Los mensajes de error acumulados serían confusos para el usuario (ej: "es null" Y "no tiene formato válido")

```csharp
RuleFor(x => x.ProductId)
    .NotEmpty()
        .WithMessage("El ID del producto es obligatorio.")
    .MustAsync(async id => await _products.ExistsAsync(id))
        .WithMessage("El producto no existe.")
    .MustAsync(async id => await _products.IsActiveAsync(id))
        .WithMessage("El producto no está activo.")
    .StopOnFirstFailure(); // Evita llamar a la BD si el ID está vacío
```

---

## When

`When` aplica las reglas anteriores en la cadena **solo si la condición es verdadera**. Si la condición es falsa, las reglas se saltan sin generar errores.

```csharp
RuleFor(x => x.VatNumber)
    .NotEmpty()
        .WithMessage("El número de IVA es obligatorio para empresas.")
    .Matches(@"^ES[A-Z0-9]{9}$")
        .WithMessage("El número de IVA español no tiene el formato correcto.")
    .When(x => x.CustomerType == CustomerType.Company);
```

### When aplica a TODAS las reglas anteriores del builder

`When` aplica a todas las reglas definidas en ese `RuleFor`. En el ejemplo anterior, tanto `NotEmpty()` como `Matches()` solo se evalúan si `CustomerType == Company`.

Si quieres que `When` aplique solo a una regla específica, colócalo después de esa regla y antes de la siguiente:

```csharp
// When solo aplica a Matches, no a NotEmpty
RuleFor(x => x.VatNumber)
    .NotEmpty()
        .WithMessage("El número de IVA es obligatorio.");  // Siempre se evalúa

// Builder separado para la regla condicional
RuleFor(x => x.VatNumber)
    .Matches(@"^ES[A-Z0-9]{9}$")
        .WithMessage("El NIF/CIF español no tiene el formato correcto.")
    .When(x => x.CustomerType == CustomerType.Company);
```

### Ejemplos prácticos

```csharp
public class CreateListingValidator : AbstractValidator<CreateListingRequest>
{
    public CreateListingValidator()
    {
        // Campos básicos (siempre)
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Price).GreaterThan(0);

        // Campos de subasta (solo si es subasta)
        RuleFor(x => x.AuctionEndDate)
            .NotNull()
                .WithMessage("La subasta debe tener una fecha de cierre.")
            .FutureDate()
                .WithMessage("La fecha de cierre debe ser en el futuro.")
            .When(x => x.ListingType == ListingType.Auction);

        // Precio de reserva (solo en subastas con precio de reserva)
        RuleFor(x => x.ReservePrice)
            .GreaterThan(0)
            .Must((req, reserve) => reserve < req.Price)
                .WithMessage("El precio de reserva debe ser menor que el precio inicial.")
            .When(x => x.ListingType == ListingType.Auction && x.HasReservePrice);

        // Descuento (solo si hay descuento activo)
        RuleFor(x => x.DiscountPercentage)
            .Between(1m, 90m)
                .WithMessage("El descuento debe estar entre 1% y 90%.")
            .When(x => x.HasDiscount);

        // Peso y dimensiones (solo para productos físicos)
        RuleFor(x => x.WeightKg)
            .GreaterThan(0)
            .LessThan(1000)
            .When(x => x.RequiresShipping);

        RuleFor(x => x.ShippingAddress)
            .NotNull()
            .SetValidator(new AddressValidator())
            .When(x => x.RequiresShipping);
    }
}
```

---

## Unless

`Unless` es la negación de `When`. Aplica las reglas si la condición es **falsa**.

```csharp
RuleFor(x => x.AlternativeEmail)
    .NotEmpty()
        .WithMessage("Si no tienes teléfono, el email alternativo es obligatorio.")
    .Email()
    .Unless(x => !string.IsNullOrEmpty(x.PhoneNumber));

// Equivalente con When:
RuleFor(x => x.AlternativeEmail)
    .NotEmpty()
    .Email()
    .When(x => string.IsNullOrEmpty(x.PhoneNumber));
```

### Ejemplo: campos opcionales con restricciones

```csharp
public class UpdateProfileValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileValidator()
    {
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(100);

        // Bio es opcional, pero si se envía debe tener contenido real
        RuleFor(x => x.Bio)
            .NotEmpty()
                .WithMessage("Si incluyes una bio, no puede estar vacía.")
            .MaximumLength(500)
            .Unless(x => x.Bio == null); // Solo valida si no es null

        // Website es opcional, pero si se envía debe ser URL válida
        RuleFor(x => x.Website)
            .Url()
                .WithMessage("La URL del sitio web no es válida.")
            .Unless(x => x.Website == null);

        // El teléfono solo es obligatorio si no hay otro método de contacto
        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
                .WithMessage("Necesitas al menos un método de contacto (email o teléfono).")
            .Unless(x => !string.IsNullOrEmpty(x.AlternativeEmail));
    }
}
```

---

## WhenAsync

`WhenAsync` es la versión asíncrona de `When`. Permite condiciones que requieren I/O.

```csharp
RuleFor(x => x.NewEmail)
    .NotEmpty()
    .Email()
    .MustAsync(async (email, ct) => !await _users.ExistsByEmailAsync(email, ct))
        .WithMessage("Ese email ya está en uso.")
    .WhenAsync(async (request, ct) =>
    {
        // Solo valida el nuevo email si el usuario quiere cambiarlo
        var currentUser = await _users.GetByIdAsync(request.UserId, ct);
        return currentUser?.Email != request.NewEmail;
    });
```

> **Nota:** `WhenAsync` solo funciona cuando se llama a `ValidateAsync`. Si se llama a `Validate` (síncrono), las reglas con `WhenAsync` se saltan.

### Ejemplo: permisos condicionales

```csharp
public class AssignRoleValidator : AbstractValidator<AssignRoleRequest>
{
    private readonly IPermissionService _permissions;
    private readonly IRoleRepository _roles;

    public AssignRoleValidator(IPermissionService permissions, IRoleRepository roles)
    {
        _permissions = permissions;
        _roles = roles;

        RuleFor(x => x.RoleId)
            .NotEmpty()
            .MustAsync(async (id, ct) => await _roles.ExistsAsync(id, ct))
                .WithMessage("El rol no existe.");

        // Solo el superadmin puede asignar roles de administrador
        RuleFor(x => x.RoleId)
            .MustAsync(async (req, roleId, ct) =>
            {
                var role = await _roles.GetByIdAsync(roleId, ct);
                return !role.IsAdminRole;
            })
            .WithMessage("Solo los superadmins pueden asignar roles de administrador.")
            .WhenAsync(async (req, ct) =>
            {
                return !await _permissions.IsSuperAdminAsync(req.AssignedBy, ct);
            });
    }
}
```

---

## UnlessAsync

`UnlessAsync` es la negación asíncrona de `WhenAsync`.

```csharp
RuleFor(x => x.Price)
    .GreaterThan(0)
    .UnlessAsync(async (request, ct) =>
    {
        // El precio puede ser 0 si el producto está en un catálogo gratuito
        return await _catalogs.IsFreeAsync(request.CatalogId, ct);
    });
```

---

## Combinaciones de modificadores

Los modificadores se pueden combinar. El orden habitual es:

```
.ReglaA()
    .WithMessage("mensaje")
    .WithErrorCode("CODIGO")
.ReglaB()
    .WithMessage("otro mensaje")
.OverridePropertyName("nombreAlternativo")
.When(condicion)
.StopOnFirstFailure()
```

> `OverridePropertyName`, `When`/`Unless`, y `StopOnFirstFailure` aplican al builder completo, no a la última regla. `WithMessage` y `WithErrorCode` aplican a la última regla.

### Ejemplo completo con todos los modificadores

```csharp
public class PaymentRequestValidator : AbstractValidator<PaymentRequest>
{
    private readonly ICurrencyService _currency;

    public PaymentRequestValidator(ICurrencyService currency)
    {
        _currency = currency;

        RuleFor(x => x.Amount)
            .GreaterThan(0m)
                .WithMessage("El importe debe ser mayor que 0.")
                .WithErrorCode("AMOUNT_MUST_BE_POSITIVE")
            .LessThanOrEqualTo(999999.99m)
                .WithMessage("El importe máximo por transacción es 999,999.99.")
                .WithErrorCode("AMOUNT_EXCEEDS_LIMIT")
            .MaxDecimalPlaces(2)
                .WithMessage("El importe no puede tener más de 2 decimales.")
            .StopOnFirstFailure();

        RuleFor(x => x.Currency)
            .NotEmpty()
                .WithErrorCode("CURRENCY_REQUIRED")
            .Uppercase()
                .WithMessage("El código de moneda debe estar en mayúsculas (ej: EUR, USD).")
            .MustAsync(async (currency, ct) =>
                await _currency.IsSupportedAsync(currency, ct))
                .WithMessage("La moneda '{PropertyValue}' no está soportada.")
                .WithErrorCode("CURRENCY_NOT_SUPPORTED")
            .OverridePropertyName("currencyCode");

        RuleFor(x => x.CardNumber)
            .NotEmpty()
                .WithMessage("El número de tarjeta es obligatorio.")
            .CreditCard()
                .WithMessage("El número de tarjeta no es válido (falla el checksum de Luhn).")
                .WithErrorCode("INVALID_CARD_NUMBER")
            .StopOnFirstFailure()
            .When(x => x.PaymentMethod == PaymentMethod.Card);

        RuleFor(x => x.BankAccount)
            .NotEmpty()
                .WithMessage("El IBAN es obligatorio para transferencias.")
            .Matches(@"^[A-Z]{2}\d{2}[A-Z0-9]+$")
                .WithMessage("El IBAN tiene un formato inválido.")
                .WithErrorCode("INVALID_IBAN")
            .When(x => x.PaymentMethod == PaymentMethod.BankTransfer);
    }
}
```

---

## Resumen rápido

| Modificador | Aplica a | Efecto |
|---|---|---|
| `WithMessage(msg)` | Última regla | Reemplaza el mensaje de error |
| `WithErrorCode(code)` | Última regla | Agrega código al resultado |
| `OverridePropertyName(name)` | Todo el builder | Cambia la clave en Errors |
| `StopOnFirstFailure()` | Todo el builder | Para al primer fallo por propiedad |
| `When(condition)` | Todo el builder | Aplica solo si condición es true |
| `Unless(condition)` | Todo el builder | Aplica solo si condición es false |
| `WhenAsync(condition)` | Todo el builder | Igual que When pero async |
| `UnlessAsync(condition)` | Todo el builder | Igual que Unless pero async |

## Siguientes pasos

- **[CascadeMode](08-cascade-mode.md)** — Detener evaluación a nivel de validador completo
- **[Resultado de validación](09-resultado-validacion.md)** — Cómo usar ErrorCodes y el resto de ValidationResult
