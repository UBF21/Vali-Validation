# Reglas avanzadas

Este documento cubre las reglas que van más allá de las comprobaciones estáticas: predicados personalizados, lógica asíncrona, dependencias entre propiedades, comparaciones cross-property, requerido condicional, transformaciones y validadores anidados.

---

## Must

`Must` permite definir cualquier predicado síncrono. Es la válvula de escape cuando ninguna regla predefinida cubre tu caso.

```csharp
RuleFor(x => x.BirthDate)
    .Must(date => date.Year >= 1900)
        .WithMessage("La fecha de nacimiento no puede ser anterior a 1900.")
    .Must(date => DateTime.Today.Year - date.Year >= 18)
        .WithMessage("Debes tener al menos 18 años.");
```

`Must` también puede acceder al objeto raíz completo a través de una sobrecarga con dos parámetros:

```csharp
RuleFor(x => x.EndDate)
    .Must((request, endDate) => endDate > request.StartDate)
        .WithMessage("La fecha de fin debe ser posterior a la fecha de inicio.");

RuleFor(x => x.MaximumDiscount)
    .Must((request, maxDiscount) => maxDiscount < request.OriginalPrice)
        .WithMessage("El descuento máximo no puede superar el precio original.");
```

### Ejemplo real: validación de rango de fechas

```csharp
public class CreateCampaignValidator : AbstractValidator<CreateCampaignRequest>
{
    public CreateCampaignValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);

        RuleFor(x => x.StartDate)
            .FutureDate()
                .WithMessage("La campaña debe comenzar en el futuro.");

        RuleFor(x => x.EndDate)
            .FutureDate()
                .WithMessage("La campaña debe terminar en el futuro.")
            .Must((req, endDate) => endDate > req.StartDate)
                .WithMessage("La fecha de fin debe ser posterior a la fecha de inicio.")
            .Must((req, endDate) => (endDate - req.StartDate).TotalDays <= 365)
                .WithMessage("La duración de la campaña no puede superar 365 días.");

        RuleFor(x => x.Budget)
            .GreaterThan(0)
                .WithMessage("El presupuesto debe ser positivo.")
            .Must((req, budget) => budget <= req.MaxBudget)
                .WithMessage("El presupuesto no puede superar el máximo autorizado.")
            .When(x => x.Budget.HasValue);
    }
}
```

---

## MustAsync

`MustAsync` es la versión asíncrona de `Must`. Úsalo cuando la validación requiera I/O: consultas a base de datos, llamadas a APIs externas, etc.

### Sin CancellationToken

```csharp
RuleFor(x => x.Email)
    .MustAsync(async email =>
    {
        var exists = await _userRepository.ExistsByEmailAsync(email);
        return !exists;
    })
    .WithMessage("Ya existe un usuario registrado con ese email.");
```

### Con CancellationToken

```csharp
RuleFor(x => x.Email)
    .MustAsync(async (email, ct) =>
    {
        var exists = await _userRepository.ExistsByEmailAsync(email, ct);
        return !exists;
    })
    .WithMessage("Ya existe un usuario registrado con ese email.");
```

### Con acceso al objeto raíz

```csharp
RuleFor(x => x.ProductId)
    .MustAsync(async (request, productId, ct) =>
    {
        // Verifica que el producto pertenezca a la categoría especificada
        var product = await _productRepository.GetByIdAsync(productId, ct);
        return product?.CategoryId == request.CategoryId;
    })
    .WithMessage("El producto no pertenece a la categoría especificada.");
```

### Ejemplo completo: registro de usuario

```csharp
public class RegisterUserValidator : AbstractValidator<RegisterUserRequest>
{
    private readonly IUserRepository _users;
    private readonly IEmailBlacklist _blacklist;

    public RegisterUserValidator(IUserRepository users, IEmailBlacklist blacklist)
    {
        _users = users;
        _blacklist = blacklist;

        RuleFor(x => x.Username)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(50)
            .IsAlphanumeric()
            .MustAsync(async (username, ct) =>
            {
                return !await _users.UsernameExistsAsync(username, ct);
            })
            .WithMessage("Ese nombre de usuario ya está en uso.");

        RuleFor(x => x.Email)
            .NotEmpty()
            .Email()
            .MustAsync(async (email, ct) =>
            {
                return !await _users.EmailExistsAsync(email, ct);
            })
            .WithMessage("Ese email ya está registrado.")
            .MustAsync(async (email, ct) =>
            {
                return !await _blacklist.IsBlacklistedAsync(email, ct);
            })
            .WithMessage("No se puede registrar con ese proveedor de email.");
    }
}
```

> **Rendimiento:** Si tienes múltiples `MustAsync` independientes, considera usar `ValidateParallelAsync` para ejecutarlos en paralelo. Ver [Validadores](04-validadores.md).

---

## DependentRuleAsync

`DependentRuleAsync` define una regla asíncrona que depende de **dos propiedades** del mismo objeto. Es útil cuando la validación de una propiedad depende del valor de otra y necesita lógica asíncrona.

```csharp
DependentRuleAsync(
    x => x.ProductId,    // primera propiedad
    x => x.WarehouseId,  // segunda propiedad
    async (productId, warehouseId) =>
    {
        return await _inventory.IsProductAvailableInWarehouseAsync(productId, warehouseId);
    }
)
.WithMessage("El producto no está disponible en el almacén especificado.");
```

### Ejemplo: descuento válido para un producto

```csharp
public class ApplyCouponValidator : AbstractValidator<ApplyCouponRequest>
{
    private readonly ICouponService _couponService;

    public ApplyCouponValidator(ICouponService couponService)
    {
        _couponService = couponService;

        RuleFor(x => x.CouponCode).NotEmpty();
        RuleFor(x => x.ProductId).NotEmpty();

        // La regla dependiente valida la combinación cupón + producto
        DependentRuleAsync(
            x => x.CouponCode,
            x => x.ProductId,
            async (couponCode, productId) =>
            {
                return await _couponService.IsValidForProductAsync(couponCode, productId);
            }
        )
        .WithMessage("El cupón no es válido para este producto.");
    }
}
```

---

## Custom

`Custom` da control total sobre la validación. Recibes el valor de la propiedad y un `CustomContext<T>` que te permite añadir errores de forma granular, incluyendo errores para otras propiedades o con códigos de error.

### Interfaz de CustomContext\<T\>

```csharp
// ctx.Instance — el objeto T completo
// ctx.AddFailure(message) — agrega error a la propiedad actual
// ctx.AddFailure(property, message) — agrega error a una propiedad específica
// ctx.AddFailure(property, message, errorCode) — con código de error
```

### Ejemplo: validación cruzada compleja

```csharp
public class TransferFundsValidator : AbstractValidator<TransferFundsRequest>
{
    private readonly IAccountRepository _accounts;

    public TransferFundsValidator(IAccountRepository accounts)
    {
        _accounts = accounts;

        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.SourceAccountId).NotEmpty();
        RuleFor(x => x.DestinationAccountId).NotEmpty();

        RuleFor(x => x.SourceAccountId)
            .Custom(async (sourceId, ctx) =>
            {
                // Accede al request completo via ctx.Instance
                var request = ctx.Instance;

                var sourceAccount = await _accounts.GetByIdAsync(sourceId);
                if (sourceAccount == null)
                {
                    ctx.AddFailure("SourceAccountId", "La cuenta de origen no existe.", "ACCOUNT_NOT_FOUND");
                    return;
                }

                if (sourceAccount.OwnerId != request.RequestingUserId)
                {
                    ctx.AddFailure("SourceAccountId", "No tienes permisos sobre la cuenta de origen.", "UNAUTHORIZED");
                    return;
                }

                if (sourceAccount.Balance < request.Amount)
                {
                    // Agrega el error a Amount, no a SourceAccountId
                    ctx.AddFailure("Amount", $"Saldo insuficiente. Disponible: {sourceAccount.Balance:C}.", "INSUFFICIENT_FUNDS");
                }

                if (!sourceAccount.AllowsTransfers)
                {
                    ctx.AddFailure("SourceAccountId", "Esta cuenta no permite transferencias.", "TRANSFERS_DISABLED");
                }
            });
    }
}
```

### Ejemplo: validación de horario laboral

```csharp
RuleFor(x => x.ScheduledTime)
    .Custom((scheduledTime, ctx) =>
    {
        var request = ctx.Instance;

        if (scheduledTime.DayOfWeek == DayOfWeek.Saturday ||
            scheduledTime.DayOfWeek == DayOfWeek.Sunday)
        {
            ctx.AddFailure("ScheduledTime",
                "No se pueden programar tareas en fin de semana.",
                "WEEKEND_NOT_ALLOWED");
            return;
        }

        if (scheduledTime.Hour < 8 || scheduledTime.Hour >= 18)
        {
            ctx.AddFailure("ScheduledTime",
                "Las tareas solo se pueden programar entre las 8:00 y las 18:00.",
                "OUTSIDE_BUSINESS_HOURS");
        }

        if (request.Priority == TaskPriority.Low && scheduledTime.Hour < 10)
        {
            ctx.AddFailure("ScheduledTime",
                "Las tareas de baja prioridad no pueden programarse antes de las 10:00.",
                "LOW_PRIORITY_TOO_EARLY");
        }
    });
```

---

## SwitchOn — Reglas condicionales por valor en una propiedad

`SwitchOn` se encadena en `RuleFor` y permite aplicar reglas distintas sobre **la misma propiedad** según el valor de otra propiedad del objeto. Solo se ejecuta el primer caso que coincide.

### Cuándo usarlo

Úsalo cuando el **formato o las restricciones de un campo** dependen del valor de otro campo. Por ejemplo, un número de documento cuya longitud y patrón cambian según el tipo de documento.

A diferencia de `When`/`Unless`, que son independientes entre sí, `SwitchOn` es **exclusivo**: los casos son mutuamente excluyentes y solo se evalúa uno.

### Sintaxis

```csharp
RuleFor(x => x.Propiedad)
    .SwitchOn(x => x.PropiedadDiscriminadora)
    .Case(valor1, b => b.NotEmpty().Matches(@"^pattern1$"))
    .Case(valor2, b => b.NotEmpty().IsNumeric().MinimumLength(8))
    .Default(     b => b.NotEmpty());
```

`ISwitchOnBuilder<T, TProperty, TKey>` expone:

```csharp
ISwitchOnBuilder<T, TProperty, TKey> Case(TKey value, Action<IRuleBuilder<T, TProperty>> configure)
ISwitchOnBuilder<T, TProperty, TKey> Default(Action<IRuleBuilder<T, TProperty>> configure)
```

El bloque `Default` es opcional. Si no se define y ningún caso coincide, no se aplica ninguna regla sobre esa propiedad para ese objeto.

### Ejemplo: número de documento según tipo

```csharp
public class DocumentDto
{
    public string DocumentType { get; set; } = string.Empty; // "passport" | "dni" | "ruc"
    public string DocumentNumber { get; set; } = string.Empty;
}

public class DocumentValidator : AbstractValidator<DocumentDto>
{
    public DocumentValidator()
    {
        RuleFor(x => x.DocumentNumber)
            .SwitchOn(x => x.DocumentType)
            .Case("passport", b => b.NotEmpty().Matches(@"^[A-Z]{2}\d{6}$")
                .WithMessage("El pasaporte debe tener 2 letras mayúsculas seguidas de 6 dígitos."))
            .Case("dni",      b => b.NotEmpty().IsNumeric().MinimumLength(8).MaximumLength(8)
                .WithMessage("El DNI debe tener exactamente 8 dígitos."))
            .Case("ruc",      b => b.NotEmpty().IsNumeric().MinimumLength(11).MaximumLength(11)
                .WithMessage("El RUC debe tener exactamente 11 dígitos."))
            .Default(         b => b.NotEmpty());
    }
}
```

### Ejemplo adicional: campo de medición con unidades distintas

```csharp
public class MeasurementDto
{
    public string Unit { get; set; } = string.Empty; // "kg" | "percentage" | "text"
    public string Value { get; set; } = string.Empty;
}

public class MeasurementValidator : AbstractValidator<MeasurementDto>
{
    public MeasurementValidator()
    {
        RuleFor(x => x.Unit)
            .NotEmpty()
            .In(new[] { "kg", "percentage", "text" });

        RuleFor(x => x.Value)
            .SwitchOn(x => x.Unit)
            .Case("kg", b => b
                .NotEmpty()
                .Transform(v => decimal.TryParse(v, out var d) ? d : -1m)
                .NonNegative()
                    .WithMessage("El peso no puede ser negativo.")
                .MaxDecimalPlaces(3)
                    .WithMessage("El peso admite hasta 3 decimales."))
            .Case("percentage", b => b
                .NotEmpty()
                .Transform(v => decimal.TryParse(v, out var d) ? d : -1m)
                .Percentage()
                    .WithMessage("El porcentaje debe estar entre 0 y 100."))
            .Case("text", b => b
                .NotEmpty()
                .MaximumLength(500));
    }
}
```

### Diferencia con When/Unless

| | `When`/`Unless` | `SwitchOn` |
|---|---|---|
| Exclusividad | Independientes — varios pueden activarse | Exclusivo — solo un caso se ejecuta |
| Ámbito | Una regla dentro de un `RuleFor` | Todas las reglas de un grupo de casos |
| Legibilidad | Bueno para 1-2 condiciones | Bueno para 3 o más alternativas |

```csharp
// When/Unless: ambas condiciones pueden evaluarse independientemente
RuleFor(x => x.Value)
    .MinimumLength(8).When(x => x.Type == "password")
    .Email().When(x => x.Type == "email"); // ambas se evalúan

// SwitchOn: solo se ejecuta el caso que coincide
RuleFor(x => x.Value)
    .SwitchOn(x => x.Type)
    .Case("password", b => b.MinimumLength(8))
    .Case("email",    b => b.Email()); // solo uno se ejecuta
```

---

## Transform

`Transform` convierte el valor de la propiedad antes de aplicar las reglas. Devuelve un nuevo `IRuleBuilder<T, TNew>` para las reglas siguientes.

### Caso de uso: normalizar antes de validar

```csharp
public class SearchProductsValidator : AbstractValidator<SearchProductsRequest>
{
    public SearchProductsValidator()
    {
        // Transforma el string a minúsculas antes de validar longitud y contenido
        RuleFor(x => x.SearchTerm)
            .Transform(term => term?.Trim().ToLowerInvariant())
            .MinimumLength(2)
                .WithMessage("El término de búsqueda debe tener al menos 2 caracteres.")
            .MaximumLength(100)
            .When(x => x.SearchTerm != null);

        // Transforma la fecha a UTC antes de validar que sea futura
        RuleFor(x => x.FromDate)
            .Transform(date => date.ToUniversalTime())
            .FutureDate()
            .When(x => x.FromDate != default);
    }
}
```

### Caso de uso: extraer parte del valor

```csharp
public class DocumentValidator : AbstractValidator<DocumentRequest>
{
    public DocumentValidator()
    {
        // Valida solo la extensión del nombre de archivo
        RuleFor(x => x.FileName)
            .Transform(name => Path.GetExtension(name).ToLowerInvariant())
            .In(new[] { ".pdf", ".docx", ".xlsx", ".png", ".jpg" })
                .WithMessage("El formato de archivo no está permitido.");

        // Valida solo el dominio del email
        RuleFor(x => x.Email)
            .Transform(email => email.Split('@').LastOrDefault() ?? "")
            .NotIn(new[] { "mailinator.com", "guerrillamail.com", "tempmail.com" })
                .WithMessage("No se aceptan emails de dominios temporales.");
    }
}
```

### Caso de uso: convertir tipo

```csharp
public class ParsedNumberValidator : AbstractValidator<ParseNumberRequest>
{
    public ParsedNumberValidator()
    {
        // Convierte el string a decimal y valida el rango
        RuleFor(x => x.AmountString)
            .Transform(s => decimal.TryParse(s, out var result) ? result : -1m)
            .GreaterThan(0m)
                .WithMessage("El importe debe ser un número positivo.")
            .MaxDecimalPlaces(2)
                .WithMessage("El importe no puede tener más de 2 decimales.");
    }
}
```

---

## SetValidator

`SetValidator` delega la validación de una propiedad compleja a otro validador. Los errores del validador anidado se agregan con el nombre de la propiedad como prefijo.

### Uso básico

```csharp
public class AddressValidator : AbstractValidator<Address>
{
    public AddressValidator()
    {
        RuleFor(x => x.Street).NotEmpty().MaximumLength(200);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PostalCode).NotEmpty().Matches(@"^\d{5}$");
        RuleFor(x => x.CountryCode).NotEmpty().LengthBetween(2, 3).Uppercase();
    }
}

public class CreateShipmentValidator : AbstractValidator<CreateShipmentRequest>
{
    public CreateShipmentValidator()
    {
        RuleFor(x => x.TrackingNumber).NotEmpty();

        // Los errores de dirección aparecen como "ShippingAddress.Street", etc.
        RuleFor(x => x.ShippingAddress)
            .NotNull()
                .WithMessage("La dirección de envío es obligatoria.")
            .SetValidator(new AddressValidator());

        RuleFor(x => x.BillingAddress)
            .SetValidator(new AddressValidator())
            .When(x => x.BillingAddress != null);
    }
}
```

Si `ShippingAddress.Street` está vacío, el resultado tendrá:

```json
{
  "ShippingAddress.Street": ["The Street field cannot be empty."]
}
```

### SetValidator con dependencias

Si el validador anidado tiene dependencias, puedes resolverlo desde el contenedor DI:

```csharp
public class CreateOrderValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderValidator(IServiceProvider services)
    {
        var addressValidator = services.GetRequiredService<IValidator<Address>>();

        RuleFor(x => x.ShippingAddress)
            .SetValidator((AbstractValidator<Address>)addressValidator);
    }
}
```

### SetValidator con colecciones (RuleForEach)

Combina `RuleForEach` con `SetValidator` para validar listas de objetos complejos:

```csharp
public class OrderLineValidator : AbstractValidator<OrderLine>
{
    public OrderLineValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.UnitPrice).GreaterThan(0m);
        RuleFor(x => x.Discount)
            .Between(0m, 100m)
                .WithMessage("El descuento debe estar entre 0% y 100%.")
            .When(x => x.Discount.HasValue);
    }
}

public class CreateOrderValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();

        RuleFor(x => x.Lines)
            .NotEmptyCollection()
                .WithMessage("El pedido debe tener al menos una línea.");

        // Valida cada línea con OrderLineValidator
        // Errores: "Lines[0].ProductId", "Lines[1].UnitPrice", etc.
        RuleForEach(x => x.Lines)
            .SetValidator(new OrderLineValidator());
    }
}
```

---

## RuleForEach avanzado

`RuleForEach` soporta la mayoría de modificadores disponibles en `RuleFor`. Aquí algunos patrones avanzados:

### Con When condicional

```csharp
// Solo valida los items digitales
RuleForEach(x => x.Items)
    .Must(item => !string.IsNullOrEmpty(item.DownloadUrl))
        .WithMessage("Los productos digitales deben tener URL de descarga.")
    .When(x => x.DeliveryType == DeliveryType.Digital);
```

### Con MustAsync y acceso al objeto raíz

```csharp
RuleForEach(x => x.ProductIds)
    .MustAsync(async (request, productId, ct) =>
    {
        return await _products.BelongsToCatalogAsync(productId, request.CatalogId, ct);
    })
    .WithMessage("El producto no pertenece al catálogo seleccionado.");
```

### Con StopOnFirstFailure por elemento

```csharp
RuleForEach(x => x.Recipients)
    .NotEmpty()
        .WithMessage("El destinatario no puede estar vacío.")
    .Email()
        .WithMessage("El destinatario debe ser un email válido.")
    .StopOnFirstFailure(); // Para de validar el elemento tras el primer error
```

---

## Reglas Cross-Property

Las reglas cross-property comparan el valor de una propiedad con el valor de **otra propiedad del mismo objeto** en tiempo de ejecución. Son más expresivas que `Must((request, value) => ...)` porque nombran explícitamente la propiedad con la que se compara, lo que mejora los mensajes de error automáticos.

### `GreaterThanProperty(Expression<Func<T, TProperty>> otherExpression)`

Verifica que el valor sea estrictamente mayor que el de otra propiedad.

```csharp
public class CreateCampaignValidator : AbstractValidator<CreateCampaignRequest>
{
    public CreateCampaignValidator()
    {
        RuleFor(x => x.StartDate).FutureDate();

        // EndDate debe ser posterior a StartDate
        RuleFor(x => x.EndDate)
            .GreaterThanProperty(x => x.StartDate)
                .WithMessage("La fecha de fin debe ser posterior a la fecha de inicio.");
    }
}
```

### `GreaterThanOrEqualToProperty(Expression<Func<T, TProperty>> otherExpression)`

Verifica que el valor sea mayor o igual al de otra propiedad.

```csharp
// El precio de venta no puede ser inferior al precio de coste
RuleFor(x => x.SellingPrice)
    .GreaterThanOrEqualToProperty(x => x.CostPrice)
        .WithMessage("El precio de venta no puede ser inferior al precio de coste.");
```

### `LessThanProperty(Expression<Func<T, TProperty>> otherExpression)`

Verifica que el valor sea estrictamente menor que el de otra propiedad.

```csharp
public class CreateEventValidator : AbstractValidator<CreateEventRequest>
{
    public CreateEventValidator()
    {
        // La fecha de registro debe cerrar antes de que comience el evento
        RuleFor(x => x.RegistrationDeadline)
            .LessThanProperty(x => x.EventDate)
                .WithMessage("La fecha límite de registro debe ser anterior a la fecha del evento.");
    }
}
```

### `LessThanOrEqualToProperty(Expression<Func<T, TProperty>> otherExpression)`

Verifica que el valor sea menor o igual al de otra propiedad.

```csharp
// El descuento no puede superar el precio original
RuleFor(x => x.DiscountAmount)
    .LessThanOrEqualToProperty(x => x.OriginalPrice)
        .WithMessage("El descuento no puede ser mayor que el precio original.");

// La fecha de inicio puede ser igual o anterior a la fecha de fin
RuleFor(x => x.StartDate)
    .LessThanOrEqualToProperty(x => x.EndDate)
        .WithMessage("La fecha de inicio debe ser anterior o igual a la fecha de fin.");
```

### `NotEqualToProperty(Expression<Func<T, TProperty>> otherExpression)`

Verifica que el valor sea distinto al de otra propiedad. El caso de uso más común: nueva contraseña distinta de la actual.

```csharp
public class ChangePasswordValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(8)
            // La nueva contraseña no puede ser igual a la actual
            .NotEqualToProperty(x => x.CurrentPassword)
                .WithMessage("La nueva contraseña debe ser distinta a la contraseña actual.");

        RuleFor(x => x.ConfirmPassword)
            .EqualToProperty(x => x.NewPassword)
                .WithMessage("Las contraseñas no coinciden.");
    }
}
```

### `MultipleOfProperty(Expression<Func<T, TProperty>> otherExpression)`

Verifica que el valor sea múltiplo del valor de otra propiedad. Útil para lotes, paginación dinámica o reglas de negocio donde el múltiplo varía por objeto.

```csharp
public class BatchOrderValidator : AbstractValidator<BatchOrderRequest>
{
    public BatchOrderValidator()
    {
        RuleFor(x => x.UnitPrice).Positive();

        // El importe total del lote debe ser múltiplo del precio unitario
        RuleFor(x => x.BatchAmount)
            .MultipleOfProperty(x => x.UnitPrice)
                .WithMessage("El importe del lote debe ser múltiplo del precio unitario.");
    }
}
```

### Ejemplo completo: formulario de reserva con fechas cruzadas

```csharp
public class CreateBookingRequest
{
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public int Guests { get; set; }
    public decimal? SpecialDiscount { get; set; }
    public decimal RoomRate { get; set; }
}

public class CreateBookingValidator : AbstractValidator<CreateBookingRequest>
{
    public CreateBookingValidator()
    {
        RuleFor(x => x.CheckIn)
            .FutureDate()
                .WithMessage("La fecha de entrada debe ser en el futuro.");

        RuleFor(x => x.CheckOut)
            .GreaterThanProperty(x => x.CheckIn)
                .WithMessage("La fecha de salida debe ser posterior a la fecha de entrada.");

        RuleFor(x => x.Guests)
            .GreaterThan(0)
                .WithMessage("Debe haber al menos 1 huésped.");

        RuleFor(x => x.SpecialDiscount)
            .LessThanOrEqualToProperty(x => x.RoomRate)
                .WithMessage("El descuento especial no puede superar la tarifa de la habitación.")
            .When(x => x.SpecialDiscount.HasValue);
    }
}
```

---

## Requerido Condicional

Las reglas de requerido condicional verifican que un campo no sea nulo ni vacío **solo cuando se cumple una condición**. Son más semánticas que `NotEmpty().When(...)` porque expresan la intención directamente: "este campo es obligatorio si...".

### `RequiredIf(Func<T, bool> condition)`

Hace que la propiedad sea obligatoria (no null ni vacía) cuando la condición evaluada sobre el objeto completo es verdadera.

```csharp
public class CreateCustomerRequest
{
    public bool IsCompany { get; set; }
    public string? CompanyName { get; set; }
    public string? TaxId { get; set; }
    public string? ContactPerson { get; set; }
}

public class CreateCustomerValidator : AbstractValidator<CreateCustomerRequest>
{
    public CreateCustomerValidator()
    {
        // La razón social es obligatoria si el cliente es empresa
        RuleFor(x => x.CompanyName)
            .RequiredIf(x => x.IsCompany)
                .WithMessage("La razón social es obligatoria para clientes empresa.");

        // El CIF es obligatorio si el cliente es empresa
        RuleFor(x => x.TaxId)
            .RequiredIf(x => x.IsCompany)
                .WithMessage("El CIF es obligatorio para clientes empresa.");
    }
}
```

### `RequiredIf<TOther>(Expression<Func<T, TOther>> otherProperty, TOther expectedValue)`

Hace que la propiedad sea obligatoria cuando **otra propiedad específica tiene un valor concreto**. Más expresivo que la sobrecarga con lambda cuando la condición es una comparación de igualdad simple.

```csharp
public class ShippingRequest
{
    public string DeliveryMethod { get; set; } = string.Empty; // "pickup" | "delivery"
    public string? ShippingAddress { get; set; }
    public string? PickupLocationId { get; set; }
}

public class ShippingValidator : AbstractValidator<ShippingRequest>
{
    public ShippingValidator()
    {
        // La dirección de envío es obligatoria cuando el método es "delivery"
        RuleFor(x => x.ShippingAddress)
            .RequiredIf(x => x.DeliveryMethod, "delivery")
                .WithMessage("La dirección de envío es obligatoria para entregas a domicilio.");

        // El punto de recogida es obligatorio cuando el método es "pickup"
        RuleFor(x => x.PickupLocationId)
            .RequiredIf(x => x.DeliveryMethod, "pickup")
                .WithMessage("Debes seleccionar un punto de recogida.");
    }
}
```

### `RequiredUnless(Func<T, bool> condition)`

Hace que la propiedad sea obligatoria **a menos que** la condición sea verdadera. Es el inverso de `RequiredIf`: el campo es requerido por defecto, excepto cuando se cumple la excepción.

```csharp
public class InvoiceRequest
{
    public bool IsExemptFromVat { get; set; }
    public string? VatNumber { get; set; }
    public bool IsDraft { get; set; }
    public string? CustomerEmail { get; set; }
}

public class InvoiceValidator : AbstractValidator<InvoiceRequest>
{
    public InvoiceValidator()
    {
        // El número de IVA es obligatorio a menos que el cliente esté exento
        RuleFor(x => x.VatNumber)
            .RequiredUnless(x => x.IsExemptFromVat)
                .WithMessage("El número de IVA es obligatorio para facturas no exentas.");

        // El email del cliente es obligatorio a menos que sea un borrador
        RuleFor(x => x.CustomerEmail)
            .RequiredUnless(x => x.IsDraft)
                .WithMessage("El email del cliente es obligatorio en facturas definitivas.");
    }
}
```

### Ejemplo completo: solicitud de préstamo

```csharp
public class LoanApplicationRequest
{
    public string ApplicantType { get; set; } = string.Empty; // "individual" | "company"
    public bool HasCoApplicant { get; set; }

    // Datos del co-solicitante: obligatorios solo si HasCoApplicant = true
    public string? CoApplicantName { get; set; }
    public string? CoApplicantId { get; set; }
    public decimal? CoApplicantIncome { get; set; }

    // Datos de empresa: obligatorios solo si ApplicantType = "company"
    public string? CompanyRegistrationNumber { get; set; }
    public string? CompanyName { get; set; }

    // Garantía: obligatoria a menos que el importe sea menor de 5000 €
    public decimal LoanAmount { get; set; }
    public string? CollateralDescription { get; set; }
}

public class LoanApplicationValidator : AbstractValidator<LoanApplicationRequest>
{
    public LoanApplicationValidator()
    {
        RuleFor(x => x.ApplicantType)
            .In(new[] { "individual", "company" });

        RuleFor(x => x.LoanAmount).Positive();

        // Datos del co-solicitante: solo si HasCoApplicant = true
        RuleFor(x => x.CoApplicantName)
            .RequiredIf(x => x.HasCoApplicant)
                .WithMessage("El nombre del co-solicitante es obligatorio.");

        RuleFor(x => x.CoApplicantId)
            .RequiredIf(x => x.HasCoApplicant)
                .WithMessage("El documento de identidad del co-solicitante es obligatorio.");

        RuleFor(x => x.CoApplicantIncome)
            .RequiredIf(x => x.HasCoApplicant)
                .WithMessage("Los ingresos del co-solicitante son obligatorios.");

        // Datos de empresa: solo si ApplicantType = "company"
        RuleFor(x => x.CompanyName)
            .RequiredIf(x => x.ApplicantType, "company")
                .WithMessage("La razón social es obligatoria para solicitantes empresa.");

        RuleFor(x => x.CompanyRegistrationNumber)
            .RequiredIf(x => x.ApplicantType, "company")
                .WithMessage("El número de registro mercantil es obligatorio para empresas.");

        // Garantía: obligatoria a menos que el préstamo sea de bajo importe
        RuleFor(x => x.CollateralDescription)
            .RequiredUnless(x => x.LoanAmount < 5000m)
                .WithMessage("Se requiere descripción de garantía para préstamos de 5000 € o más.");
    }
}
```

---

## Combinar reglas avanzadas

### Validador complejo de e-commerce

```csharp
public class PlaceOrderValidator : AbstractValidator<PlaceOrderRequest>
{
    private readonly IProductRepository _products;
    private readonly ICouponRepository _coupons;
    private readonly ICustomerRepository _customers;

    public PlaceOrderValidator(
        IProductRepository products,
        ICouponRepository coupons,
        ICustomerRepository customers)
    {
        _products = products;
        _coupons = coupons;
        _customers = customers;

        // Cliente
        RuleFor(x => x.CustomerId)
            .NotEmpty()
            .MustAsync(async (id, ct) => await _customers.ExistsAsync(id, ct))
                .WithMessage("El cliente no existe.");

        // Líneas de pedido
        RuleFor(x => x.Items)
            .NotEmptyCollection();

        RuleForEach(x => x.Items)
            .SetValidator(new OrderItemValidator(_products));

        // Dirección
        RuleFor(x => x.ShippingAddress)
            .NotNull()
            .SetValidator(new AddressValidator());

        // Cupón (opcional)
        RuleFor(x => x.CouponCode)
            .MustAsync(async (req, code, ct) =>
            {
                var coupon = await _coupons.GetByCodeAsync(code, ct);
                if (coupon == null) return false;
                if (coupon.ExpiresAt < DateTime.UtcNow) return false;
                return coupon.MinimumOrderAmount <= req.Items.Sum(i => i.TotalPrice);
            })
            .WithMessage("El cupón no es válido o no aplica al importe del pedido.")
            .When(x => !string.IsNullOrEmpty(x.CouponCode));

        // Regla custom para validación cruzada
        RuleFor(x => x.PaymentMethod)
            .Custom((payment, ctx) =>
            {
                var request = ctx.Instance;
                if (payment.Type == PaymentType.BankTransfer && request.Items.Count > 50)
                {
                    ctx.AddFailure("PaymentMethod",
                        "No se pueden realizar pedidos de más de 50 artículos con transferencia bancaria.",
                        "BANK_TRANSFER_LIMIT");
                }
            });
    }
}

public class OrderItemValidator : AbstractValidator<OrderItem>
{
    private readonly IProductRepository _products;

    public OrderItemValidator(IProductRepository products)
    {
        _products = products;

        RuleFor(x => x.ProductId)
            .NotEmpty();

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .LessThanOrEqualTo(1000)
                .WithMessage("La cantidad máxima por artículo es 1000.");

        RuleFor(x => x.ProductId)
            .MustAsync(async (item, productId, ct) =>
            {
                return await _products.IsAvailableAsync(productId, item.Quantity, ct);
            })
            .WithMessage("El producto no tiene suficiente stock.");
    }
}
```

---

## Siguientes pasos

- **[Modificadores](07-modificadores.md)** — WithMessage, WithErrorCode, When/Unless, OverridePropertyName
- **[Resultado de validación](09-resultado-validacion.md)** — Cómo trabajar con ValidationResult y ErrorCodes
- **[Patrones avanzados](15-patrones-avanzados.md)** — Composición de validadores, herencia, RuleSwitch/SwitchOn combinados, casos complejos
