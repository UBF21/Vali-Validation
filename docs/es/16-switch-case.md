# Switch / Case — Validación condicional avanzada

Vali-Validation ofrece dos mecanismos complementarios para aplicar reglas distintas según el valor de una propiedad discriminadora:

- **`RuleSwitch`** — opera a nivel de validador: según el valor de una propiedad, activa un conjunto de reglas que puede afectar a **múltiples propiedades** del objeto.
- **`SwitchOn`** — opera a nivel de propiedad: según el valor de otra propiedad, aplica reglas distintas sobre **la misma propiedad** en la que se encadena.

Ambos garantizan **exclusividad**: solo se ejecuta un caso por validación. Las reglas definidas fuera del bloque switch siempre se ejecutan. Esta exclusividad es lo que los diferencia de `When`/`Unless`, que son condiciones independientes sin garantía de exclusividad.

---

## Tabla comparativa general

| Caracteristica              | `RuleSwitch`                             | `SwitchOn`                                      | `When` / `Unless`                     |
|-----------------------------|------------------------------------------|-------------------------------------------------|---------------------------------------|
| Nivel de aplicacion         | Validador completo                       | Propiedad individual                            | Regla individual                      |
| Propiedades afectadas       | Multiples                                | Solo la propiedad del RuleFor                  | Solo la propiedad del RuleFor         |
| Exclusividad de casos       | Si, solo un caso se ejecuta             | Si, solo un caso se ejecuta                    | No, cada condicion es independiente   |
| Discriminador               | Cualquier propiedad del objeto           | Cualquier propiedad del objeto                 | Predicado booleano arbitrario         |
| Caso por defecto            | `.Default(...)`                          | `.Default(...)`                                 | No aplica                             |
| Sintaxis                    | `RuleSwitch(x => x.Prop).Case(...)`     | `RuleFor(x => x.P).SwitchOn(x => x.Q).Case(...)` | `.NotEmpty().When(x => x.Active)`   |
| Uso tipico                  | Tipos de pago, rol de usuario            | Formato de campo segun tipo                    | Campos opcionales condicionados       |

---

## RuleSwitch en profundidad

### Que es RuleSwitch

`RuleSwitch` se define dentro del constructor de `AbstractValidator<T>`. Permite agrupar bloques de reglas sobre **cualquier numero de propiedades** y activar exactamente uno de esos bloques dependiendo del valor de una propiedad discriminadora.

Es equivalente a un `switch` de C# pero aplicado sobre la logica de validacion: cuando el discriminador tiene el valor `"A"`, se ejecutan las reglas del caso `"A"`; si tiene el valor `"B"`, las del caso `"B"`. Si ninguno coincide y hay un caso `Default`, se ejecutan esas reglas.

### Interfaz ICaseBuilder

```csharp
public interface ICaseBuilder<T, TKey> where T : class
{
    // Aplica las reglas configuradas en 'configure' cuando la propiedad
    // discriminadora es igual a 'value'.
    ICaseBuilder<T, TKey> Case(TKey value, Action<AbstractValidator<T>> configure);

    // Aplica estas reglas cuando ningun Case coincide.
    ICaseBuilder<T, TKey> Default(Action<AbstractValidator<T>> configure);
}
```

Dentro del `Action<AbstractValidator<T>> configure` tienes acceso a todos los metodos del validador: `RuleFor`, `RuleForEach`, otro `RuleSwitch` anidado, `Include`, etc.

### Sintaxis basica

```csharp
RuleSwitch(x => x.Propiedad)
    .Case(valor1, rules =>
    {
        rules.RuleFor(x => x.OtraPropiedad).NotEmpty();
    })
    .Case(valor2, rules =>
    {
        rules.RuleFor(x => x.OtraPropiedad2).GreaterThan(0);
    })
    .Default(rules =>
    {
        // Reglas cuando ninguno de los casos anteriores coincide
        rules.RuleFor(x => x.Referencia).NotEmpty();
    });
```

---

### Ejemplo 1 — Metodo de pago (string discriminador)

Este es el caso de uso mas frecuente: un campo `Method` que puede ser `"credit_card"`, `"bank_transfer"` o `"paypal"`, y cada uno requiere campos distintos.

```csharp
public class PaymentDto
{
    public string Method { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? CardNumber { get; set; }
    public string? Cvv { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public string? Iban { get; set; }
    public string? BankCode { get; set; }
    public string? PaypalEmail { get; set; }
    public string? Reference { get; set; }
}

public class PaymentValidator : AbstractValidator<PaymentDto>
{
    public PaymentValidator()
    {
        // --- Reglas globales: se ejecutan SIEMPRE, independientemente del metodo ---
        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("El importe debe ser mayor que cero.");

        RuleFor(x => x.Method)
            .NotEmpty()
            .WithMessage("El metodo de pago es obligatorio.")
            .In(new[] { "credit_card", "bank_transfer", "paypal" })
            .WithMessage("El metodo de pago no es valido.");

        // --- Switch: solo se ejecuta el bloque que coincide con Method ---
        RuleSwitch(x => x.Method)
            .Case("credit_card", rules =>
            {
                rules.RuleFor(x => x.CardNumber)
                    .NotEmpty().WithMessage("El numero de tarjeta es obligatorio.")
                    .CreditCard().WithMessage("El numero de tarjeta no es valido.");

                rules.RuleFor(x => x.Cvv)
                    .NotEmpty().WithMessage("El CVV es obligatorio.")
                    .MinimumLength(3).WithMessage("El CVV debe tener al menos 3 caracteres.")
                    .MaximumLength(4).WithMessage("El CVV no puede tener mas de 4 caracteres.")
                    .IsNumeric().WithMessage("El CVV solo puede contener digitos.");

                rules.RuleFor(x => x.ExpirationDate)
                    .NotNull().WithMessage("La fecha de vencimiento es obligatoria.")
                    .FutureDate().WithMessage("La tarjeta esta vencida.");
            })
            .Case("bank_transfer", rules =>
            {
                rules.RuleFor(x => x.Iban)
                    .NotEmpty().WithMessage("El IBAN es obligatorio.")
                    .Iban().WithMessage("El IBAN no tiene un formato valido.");

                rules.RuleFor(x => x.BankCode)
                    .NotEmpty().WithMessage("El codigo bancario es obligatorio.")
                    .MinimumLength(4).WithMessage("El codigo bancario debe tener al menos 4 caracteres.");
            })
            .Case("paypal", rules =>
            {
                rules.RuleFor(x => x.PaypalEmail)
                    .NotEmpty().WithMessage("El email de PayPal es obligatorio.")
                    .Email().WithMessage("El email de PayPal no tiene formato valido.");
            })
            .Default(rules =>
            {
                // Si el Method no es ninguno de los anteriores (aunque ya lo validamos con In),
                // pedimos una referencia generica.
                rules.RuleFor(x => x.Reference)
                    .NotEmpty().WithMessage("La referencia es obligatoria para metodos de pago no reconocidos.");
            });
    }
}
```

**Como usar este validador:**

```csharp
var payment = new PaymentDto
{
    Method = "credit_card",
    Amount = 150.00m,
    CardNumber = "4111111111111111",
    Cvv = "123",
    ExpirationDate = DateTime.UtcNow.AddYears(2)
};

var validator = new PaymentValidator();
var result = await validator.ValidateAsync(payment);

if (!result.IsValid)
{
    foreach (var (field, errors) in result.Errors)
        Console.WriteLine($"{field}: {string.Join(", ", errors)}");
}
```

---

### Ejemplo 2 — Enum como discriminador (tipo de usuario)

Cuando el discriminador es un enum, `RuleSwitch` funciona exactamente igual. El tipo inferido de `TKey` sera el enum.

```csharp
public enum UserRole
{
    Admin,
    Client,
    Guest
}

public class CreateUserDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string? Department { get; set; }
    public string? ManagerId { get; set; }
    public string? InvitationCode { get; set; }
    public int? AccessLevel { get; set; }
    public bool? CanExport { get; set; }
}

public class CreateUserValidator : AbstractValidator<CreateUserDto>
{
    public CreateUserValidator()
    {
        // Reglas comunes a todos los roles
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre no puede superar los 100 caracteres.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El email es obligatorio.")
            .Email().WithMessage("El email no tiene formato valido.");

        // El discriminador es un enum: TKey = UserRole
        RuleSwitch(x => x.Role)
            .Case(UserRole.Admin, rules =>
            {
                rules.RuleFor(x => x.Department)
                    .NotEmpty().WithMessage("Los administradores deben pertenecer a un departamento.");

                rules.RuleFor(x => x.AccessLevel)
                    .NotNull().WithMessage("El nivel de acceso es obligatorio para administradores.")
                    .GreaterThan(0).WithMessage("El nivel de acceso debe ser mayor que cero.")
                    .LessThanOrEqualTo(10).WithMessage("El nivel de acceso maximo es 10.");
            })
            .Case(UserRole.Client, rules =>
            {
                rules.RuleFor(x => x.ManagerId)
                    .NotEmpty().WithMessage("Los clientes deben tener un gestor asignado.");

                rules.RuleFor(x => x.CanExport)
                    .NotNull().WithMessage("Debe indicarse si el cliente puede exportar datos.");
            })
            .Case(UserRole.Guest, rules =>
            {
                rules.RuleFor(x => x.InvitationCode)
                    .NotEmpty().WithMessage("Los usuarios invitados deben proporcionar un codigo de invitacion.")
                    .MinimumLength(8).WithMessage("El codigo de invitacion debe tener al menos 8 caracteres.")
                    .MaximumLength(32).WithMessage("El codigo de invitacion no puede superar los 32 caracteres.");
            });
    }
}
```

---

### Ejemplo 3 — Entero como discriminador (nivel de suscripcion)

```csharp
public class SubscriptionPlanDto
{
    public int Level { get; set; }          // 1=Free, 2=Pro, 3=Enterprise
    public string CompanyName { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public int? MaxUsers { get; set; }
    public string? SlaLevel { get; set; }
    public string? ContractId { get; set; }
    public bool? CustomDomain { get; set; }
}

public class SubscriptionPlanValidator : AbstractValidator<SubscriptionPlanDto>
{
    public SubscriptionPlanValidator()
    {
        RuleFor(x => x.Level)
            .Between(1, 3).WithMessage("El nivel de suscripcion debe estar entre 1 y 3.");

        RuleFor(x => x.CompanyName)
            .NotEmpty().WithMessage("El nombre de empresa es obligatorio.");

        RuleFor(x => x.ContactEmail)
            .NotEmpty().WithMessage("El email de contacto es obligatorio.")
            .Email().WithMessage("El email de contacto no tiene formato valido.");

        RuleSwitch(x => x.Level)
            .Case(1, rules =>
            {
                // Plan Free: sin campos adicionales, solo validamos que MaxUsers no exceda el limite
                rules.RuleFor(x => x.MaxUsers)
                    .Must(m => m == null || m <= 5)
                    .WithMessage("El plan Free esta limitado a 5 usuarios.");
            })
            .Case(2, rules =>
            {
                // Plan Pro: requiere maxUsers y custom domain
                rules.RuleFor(x => x.MaxUsers)
                    .NotNull().WithMessage("El plan Pro debe especificar el numero maximo de usuarios.")
                    .GreaterThan(0).WithMessage("El numero maximo de usuarios debe ser positivo.")
                    .LessThanOrEqualTo(50).WithMessage("El plan Pro permite un maximo de 50 usuarios.");

                rules.RuleFor(x => x.CustomDomain)
                    .NotNull().WithMessage("Debe indicarse si se necesita dominio personalizado.");
            })
            .Case(3, rules =>
            {
                // Plan Enterprise: requiere SLA y contrato
                rules.RuleFor(x => x.SlaLevel)
                    .NotEmpty().WithMessage("El plan Enterprise requiere un nivel de SLA.")
                    .In(new[] { "bronze", "silver", "gold", "platinum" })
                    .WithMessage("El nivel de SLA debe ser bronze, silver, gold o platinum.");

                rules.RuleFor(x => x.ContractId)
                    .NotEmpty().WithMessage("El plan Enterprise requiere un identificador de contrato.")
                    .Guid().WithMessage("El identificador de contrato debe ser un GUID valido.");

                rules.RuleFor(x => x.MaxUsers)
                    .NotNull().WithMessage("El plan Enterprise debe especificar el numero maximo de usuarios.");
            });
    }
}
```

---

### Ejemplo 4 — Tipo de envio con objeto complejo

```csharp
public enum ShippingType
{
    HomeDelivery,
    StorePickup,
    Locker
}

public class OrderShippingDto
{
    public ShippingType ShippingType { get; set; }
    public string? Street { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public string? StoreId { get; set; }
    public string? LockerId { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTime? PreferredDeliveryDate { get; set; }
}

public class OrderShippingValidator : AbstractValidator<OrderShippingDto>
{
    public OrderShippingValidator()
    {
        RuleSwitch(x => x.ShippingType)
            .Case(ShippingType.HomeDelivery, rules =>
            {
                rules.RuleFor(x => x.Street)
                    .NotEmpty().WithMessage("La direccion es obligatoria para envio a domicilio.");

                rules.RuleFor(x => x.City)
                    .NotEmpty().WithMessage("La ciudad es obligatoria para envio a domicilio.");

                rules.RuleFor(x => x.PostalCode)
                    .NotEmpty().WithMessage("El codigo postal es obligatorio.")
                    .Matches(@"^\d{5}$").WithMessage("El codigo postal debe tener exactamente 5 digitos.");

                rules.RuleFor(x => x.PhoneNumber)
                    .NotEmpty().WithMessage("El telefono de contacto es obligatorio para envio a domicilio.")
                    .PhoneNumber().WithMessage("El telefono no tiene formato valido.");

                rules.RuleFor(x => x.PreferredDeliveryDate)
                    .FutureDate().When(x => x.PreferredDeliveryDate.HasValue)
                    .WithMessage("La fecha preferida de entrega debe ser en el futuro.");
            })
            .Case(ShippingType.StorePickup, rules =>
            {
                rules.RuleFor(x => x.StoreId)
                    .NotEmpty().WithMessage("Debes seleccionar una tienda para la recogida.")
                    .NotEmptyGuid().WithMessage("El identificador de tienda no es valido.");
            })
            .Case(ShippingType.Locker, rules =>
            {
                rules.RuleFor(x => x.LockerId)
                    .NotEmpty().WithMessage("Debes seleccionar un locker para la recogida.")
                    .MinimumLength(4).WithMessage("El identificador de locker debe tener al menos 4 caracteres.");

                rules.RuleFor(x => x.PhoneNumber)
                    .NotEmpty().WithMessage("El telefono es obligatorio para envio a locker (se usara para notificar la entrega).")
                    .PhoneNumber().WithMessage("El telefono no tiene formato valido.");
            });
    }
}
```

---

### Ejemplo 5 — Uso de WithMessage y WithErrorCode dentro de los casos

Los modificadores `.WithMessage()` y `.WithErrorCode()` se encadenan sobre las reglas dentro de los casos exactamente igual que fuera de ellos.

```csharp
public class DocumentRequestDto
{
    public string DocumentType { get; set; } = string.Empty;
    public string DocumentNumber { get; set; } = string.Empty;
    public string IssuingCountry { get; set; } = string.Empty;
    public DateTime? ExpirationDate { get; set; }
}

public class DocumentRequestValidator : AbstractValidator<DocumentRequestDto>
{
    public DocumentRequestValidator()
    {
        RuleFor(x => x.DocumentType)
            .NotEmpty()
            .WithMessage("El tipo de documento es obligatorio.")
            .WithErrorCode("DOC_TYPE_REQUIRED");

        RuleSwitch(x => x.DocumentType)
            .Case("passport", rules =>
            {
                rules.RuleFor(x => x.DocumentNumber)
                    .NotEmpty()
                        .WithMessage("El numero de pasaporte es obligatorio.")
                        .WithErrorCode("PASSPORT_NUMBER_REQUIRED")
                    .Matches(@"^[A-Z]{2}\d{6,7}$")
                        .WithMessage("El numero de pasaporte debe comenzar con 2 letras mayusculas seguidas de 6 o 7 digitos.")
                        .WithErrorCode("PASSPORT_NUMBER_FORMAT");

                rules.RuleFor(x => x.ExpirationDate)
                    .NotNull()
                        .WithMessage("La fecha de vencimiento del pasaporte es obligatoria.")
                        .WithErrorCode("PASSPORT_EXPIRY_REQUIRED")
                    .FutureDate()
                        .WithMessage("El pasaporte esta vencido.")
                        .WithErrorCode("PASSPORT_EXPIRED");

                rules.RuleFor(x => x.IssuingCountry)
                    .NotEmpty()
                        .WithMessage("El pais emisor es obligatorio para pasaportes.")
                        .WithErrorCode("PASSPORT_COUNTRY_REQUIRED")
                    .CountryCode()
                        .WithMessage("El pais emisor debe ser un codigo ISO 3166-1 alpha-2 valido.")
                        .WithErrorCode("PASSPORT_COUNTRY_FORMAT");
            })
            .Case("dni", rules =>
            {
                rules.RuleFor(x => x.DocumentNumber)
                    .NotEmpty()
                        .WithMessage("El DNI es obligatorio.")
                        .WithErrorCode("DNI_REQUIRED")
                    .IsNumeric()
                        .WithMessage("El DNI solo puede contener digitos.")
                        .WithErrorCode("DNI_FORMAT")
                    .LengthBetween(8, 8)
                        .WithMessage("El DNI debe tener exactamente 8 digitos.")
                        .WithErrorCode("DNI_LENGTH");
            })
            .Default(rules =>
            {
                rules.RuleFor(x => x.DocumentNumber)
                    .NotEmpty()
                        .WithMessage("El numero de documento es obligatorio.")
                        .WithErrorCode("DOC_NUMBER_REQUIRED");
            });
    }
}
```

---

### Ejemplo 6 — When y Unless dentro de los casos

Las condiciones `When` y `Unless` se pueden usar dentro de los casos de `RuleSwitch` sin ninguna restriccion. Son utiles cuando dentro de un caso hay reglas que dependen de condiciones adicionales.

```csharp
public class InsuranceClaimDto
{
    public string ClaimType { get; set; } = string.Empty;   // "medical", "vehicle", "home"
    public decimal Amount { get; set; }
    public string? HospitalName { get; set; }
    public string? TreatmentCode { get; set; }
    public bool IsEmergency { get; set; }
    public string? VehiclePlate { get; set; }
    public string? AccidentReport { get; set; }
    public string? PropertyAddress { get; set; }
    public string? PoliceReport { get; set; }
    public bool IsTheft { get; set; }
}

public class InsuranceClaimValidator : AbstractValidator<InsuranceClaimDto>
{
    public InsuranceClaimValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("El importe reclamado debe ser mayor que cero.");

        RuleSwitch(x => x.ClaimType)
            .Case("medical", rules =>
            {
                rules.RuleFor(x => x.HospitalName)
                    .NotEmpty()
                    .WithMessage("El nombre del hospital es obligatorio para reclamaciones medicas.");

                // TreatmentCode solo es obligatorio si NO es una emergencia.
                // En emergencias se puede rellenar a posteriori.
                rules.RuleFor(x => x.TreatmentCode)
                    .NotEmpty()
                    .WithMessage("El codigo de tratamiento es obligatorio cuando no es una emergencia.")
                    .Unless(x => x.IsEmergency);
            })
            .Case("vehicle", rules =>
            {
                rules.RuleFor(x => x.VehiclePlate)
                    .NotEmpty().WithMessage("La matricula del vehiculo es obligatoria.")
                    .Matches(@"^[A-Z0-9\-]{5,10}$").WithMessage("La matricula no tiene formato valido.");

                rules.RuleFor(x => x.AccidentReport)
                    .NotEmpty().WithMessage("El numero de parte de accidente es obligatorio.");
            })
            .Case("home", rules =>
            {
                rules.RuleFor(x => x.PropertyAddress)
                    .NotEmpty().WithMessage("La direccion del inmueble es obligatoria.");

                // El informe policial solo es obligatorio en caso de robo.
                rules.RuleFor(x => x.PoliceReport)
                    .NotEmpty()
                    .WithMessage("El informe policial es obligatorio en caso de robo.")
                    .When(x => x.IsTheft);
            });
    }
}
```

---

### Tabla de comportamiento de RuleSwitch

| Situacion                                    | Comportamiento                                                          |
|----------------------------------------------|-------------------------------------------------------------------------|
| El discriminador coincide con un Case        | Solo se ejecutan las reglas de ese Case                                 |
| El discriminador no coincide con ningun Case | Se ejecutan las reglas del Default (si existe). Si no hay Default, no se ejecuta nada del switch |
| El discriminador es null                     | Se compara con null usando `Equals`; coincide si hay `.Case(null, ...)`  |
| Reglas globales (fuera del switch)           | Se ejecutan SIEMPRE, antes del switch                                   |
| Multiples Cases con el mismo valor           | Solo el primero declarado se ejecuta (como un switch de C#)            |
| Switch sin ningun Case ni Default            | No tiene efecto                                                         |

---

## SwitchOn en profundidad

### Que es SwitchOn

`SwitchOn` es una extension del fluent builder de propiedades que permite aplicar **conjuntos distintos de reglas sobre la misma propiedad** dependiendo del valor de otra propiedad.

Se diferencia de `RuleSwitch` en que:
- Se define en la cadena de `RuleFor(x => x.Propiedad)`
- Solo afecta a esa propiedad
- El `Action` recibe un `IRuleBuilder<T, TProperty>` (builder de propiedad) en lugar de un `AbstractValidator<T>`

### Interfaz ISwitchOnBuilder

```csharp
public interface ISwitchOnBuilder<T, TProperty, TKey> where T : class
{
    // Aplica las reglas sobre la propiedad cuando el discriminador es igual a 'value'.
    ISwitchOnBuilder<T, TProperty, TKey> Case(TKey value, Action<IRuleBuilder<T, TProperty>> configure);

    // Aplica estas reglas cuando ningun Case coincide.
    ISwitchOnBuilder<T, TProperty, TKey> Default(Action<IRuleBuilder<T, TProperty>> configure);
}
```

### Sintaxis basica

```csharp
RuleFor(x => x.Propiedad)
    .SwitchOn(x => x.OtraPropiedad)
    .Case(valor1, b =>
    {
        b.NotEmpty().MinimumLength(5);
    })
    .Case(valor2, b =>
    {
        b.NotEmpty().IsNumeric().MaximumLength(11);
    })
    .Default(b =>
    {
        b.NotEmpty();
    });
```

---

### Ejemplo 1 — Numero de documento segun tipo (string discriminador)

```csharp
public class IdentityDto
{
    public string DocumentType { get; set; } = string.Empty; // "passport", "dni", "ruc", "ce"
    public string DocumentNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}

public class IdentityValidator : AbstractValidator<IdentityDto>
{
    public IdentityValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().WithMessage("El nombre es obligatorio.");
        RuleFor(x => x.LastName).NotEmpty().WithMessage("El apellido es obligatorio.");
        RuleFor(x => x.DocumentType)
            .NotEmpty().WithMessage("El tipo de documento es obligatorio.")
            .In(new[] { "passport", "dni", "ruc", "ce" })
            .WithMessage("El tipo de documento no es valido.");

        // DocumentNumber tiene reglas distintas segun DocumentType:
        RuleFor(x => x.DocumentNumber)
            .SwitchOn(x => x.DocumentType)
            .Case("passport", b =>
            {
                b.NotEmpty().WithMessage("El numero de pasaporte es obligatorio.")
                 .MinimumLength(6).WithMessage("El pasaporte debe tener al menos 6 caracteres.")
                 .MaximumLength(9).WithMessage("El pasaporte no puede superar los 9 caracteres.")
                 .Matches(@"^[A-Z]{2}\d{6,7}$")
                    .WithMessage("El formato del pasaporte debe ser 2 letras mayusculas seguidas de 6 o 7 digitos.");
            })
            .Case("dni", b =>
            {
                b.NotEmpty().WithMessage("El DNI es obligatorio.")
                 .IsNumeric().WithMessage("El DNI solo puede contener digitos.")
                 .LengthBetween(8, 8).WithMessage("El DNI debe tener exactamente 8 digitos.");
            })
            .Case("ruc", b =>
            {
                b.NotEmpty().WithMessage("El RUC es obligatorio.")
                 .IsNumeric().WithMessage("El RUC solo puede contener digitos.")
                 .LengthBetween(11, 11).WithMessage("El RUC debe tener exactamente 11 digitos.");
            })
            .Case("ce", b =>
            {
                b.NotEmpty().WithMessage("El carnet de extranjeria es obligatorio.")
                 .MaximumLength(12).WithMessage("El CE no puede superar los 12 caracteres.")
                 .IsAlphanumeric().WithMessage("El CE solo puede contener letras y numeros.");
            })
            .Default(b =>
            {
                b.NotEmpty().WithMessage("El numero de documento es obligatorio.");
            });
    }
}
```

---

### Ejemplo 2 — Unidades de medida (kg / lb / oz)

```csharp
public class WeightMeasurementDto
{
    public string Unit { get; set; } = string.Empty;  // "kg", "lb", "oz"
    public decimal Value { get; set; }
    public string ProductName { get; set; } = string.Empty;
}

public class WeightMeasurementValidator : AbstractValidator<WeightMeasurementDto>
{
    public WeightMeasurementValidator()
    {
        RuleFor(x => x.ProductName)
            .NotEmpty().WithMessage("El nombre del producto es obligatorio.");

        RuleFor(x => x.Unit)
            .NotEmpty().WithMessage("La unidad de medida es obligatoria.")
            .In(new[] { "kg", "lb", "oz" }).WithMessage("La unidad debe ser kg, lb u oz.");

        // El campo Value tiene rangos validos distintos segun la unidad elegida.
        // Un peso en kg no puede ser el mismo que en oz, por ejemplo.
        RuleFor(x => x.Value)
            .SwitchOn(x => x.Unit)
            .Case("kg", b =>
            {
                // Kilogramos: entre 0.001 y 50,000 kg
                b.GreaterThan(0).WithMessage("El peso en kg debe ser mayor que cero.")
                 .LessThanOrEqualTo(50000).WithMessage("El peso en kg no puede superar los 50,000 kg.")
                 .MaxDecimalPlaces(3).WithMessage("El peso en kg admite como maximo 3 decimales.");
            })
            .Case("lb", b =>
            {
                // Libras: entre 0.001 y 110,000 lb
                b.GreaterThan(0).WithMessage("El peso en lb debe ser mayor que cero.")
                 .LessThanOrEqualTo(110000).WithMessage("El peso en lb no puede superar las 110,000 lb.")
                 .MaxDecimalPlaces(2).WithMessage("El peso en lb admite como maximo 2 decimales.");
            })
            .Case("oz", b =>
            {
                // Onzas: entre 0.01 y 1,760,000 oz (equivalente a 50,000 kg aprox)
                b.GreaterThan(0).WithMessage("El peso en oz debe ser mayor que cero.")
                 .LessThanOrEqualTo(1760000).WithMessage("El peso en oz excede el maximo permitido.")
                 .MaxDecimalPlaces(1).WithMessage("El peso en oz admite como maximo 1 decimal.");
            })
            .Default(b =>
            {
                b.GreaterThan(0).WithMessage("El valor debe ser mayor que cero.");
            });
    }
}
```

---

### Ejemplo 3 — Canales de notificacion (email / sms / push)

```csharp
public class NotificationDto
{
    public string Channel { get; set; } = string.Empty;  // "email", "sms", "push"
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? Recipient { get; set; }  // email, telefono, o token de dispositivo
}

public class NotificationValidator : AbstractValidator<NotificationDto>
{
    public NotificationValidator()
    {
        RuleFor(x => x.Channel)
            .NotEmpty().WithMessage("El canal de notificacion es obligatorio.")
            .In(new[] { "email", "sms", "push" }).WithMessage("El canal debe ser email, sms o push.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("El titulo es obligatorio.");

        // El Body tiene restricciones de longitud distintas segun el canal:
        // - email: hasta 10,000 caracteres (contenido HTML/texto)
        // - sms: max 160 caracteres (limite estandar SMS)
        // - push: max 256 caracteres (limite de APNs/FCM)
        RuleFor(x => x.Body)
            .SwitchOn(x => x.Channel)
            .Case("email", b =>
            {
                b.NotEmpty().WithMessage("El cuerpo del email es obligatorio.")
                 .MaximumLength(10000).WithMessage("El cuerpo del email no puede superar los 10,000 caracteres.");
            })
            .Case("sms", b =>
            {
                b.NotEmpty().WithMessage("El mensaje SMS no puede estar vacio.")
                 .MaximumLength(160).WithMessage("El SMS no puede superar los 160 caracteres.")
                 .NoHtmlTags().WithMessage("Los SMS no pueden contener etiquetas HTML.");
            })
            .Case("push", b =>
            {
                b.NotEmpty().WithMessage("El cuerpo de la notificacion push es obligatorio.")
                 .MaximumLength(256).WithMessage("Las notificaciones push no pueden superar los 256 caracteres.")
                 .NoHtmlTags().WithMessage("Las notificaciones push no pueden contener etiquetas HTML.");
            })
            .Default(b =>
            {
                b.NotEmpty().WithMessage("El cuerpo del mensaje es obligatorio.");
            });

        // Recipient tambien tiene formato distinto segun el canal:
        RuleFor(x => x.Recipient)
            .SwitchOn(x => x.Channel)
            .Case("email", b =>
            {
                b.NotEmpty().WithMessage("El destinatario del email es obligatorio.")
                 .Email().WithMessage("El destinatario debe ser una direccion de email valida.");
            })
            .Case("sms", b =>
            {
                b.NotEmpty().WithMessage("El numero de telefono del destinatario es obligatorio.")
                 .PhoneNumber().WithMessage("El destinatario debe ser un numero de telefono valido.");
            })
            .Case("push", b =>
            {
                b.NotEmpty().WithMessage("El token de dispositivo es obligatorio.")
                 .MinimumLength(32).WithMessage("El token de dispositivo parece demasiado corto.")
                 .MaximumLength(512).WithMessage("El token de dispositivo parece demasiado largo.");
            });
    }
}
```

---

### Ejemplo 4 — Enum como discriminador en SwitchOn

```csharp
public enum MeasurementSystem
{
    Metric,
    Imperial
}

public class TemperatureReadingDto
{
    public MeasurementSystem System { get; set; }
    public double Temperature { get; set; }
    public string Location { get; set; } = string.Empty;
}

public class TemperatureReadingValidator : AbstractValidator<TemperatureReadingDto>
{
    public TemperatureReadingValidator()
    {
        RuleFor(x => x.Location)
            .NotEmpty().WithMessage("La ubicacion es obligatoria.");

        // La temperatura tiene un rango valido distinto segun el sistema de medicion:
        // Celsius: -273.15 a 100,000 (temperatura de fusion del sol es aprox 5,500 C)
        // Fahrenheit: -459.67 a 180,000
        RuleFor(x => x.Temperature)
            .SwitchOn(x => x.System)
            .Case(MeasurementSystem.Metric, b =>
            {
                b.GreaterThan(-273.15).WithMessage("La temperatura en Celsius no puede estar por debajo del cero absoluto (-273.15 C).")
                 .LessThan(100000).WithMessage("La temperatura en Celsius supera el maximo permitido.");
            })
            .Case(MeasurementSystem.Imperial, b =>
            {
                b.GreaterThan(-459.67).WithMessage("La temperatura en Fahrenheit no puede estar por debajo del cero absoluto (-459.67 F).")
                 .LessThan(180000).WithMessage("La temperatura en Fahrenheit supera el maximo permitido.");
            });
    }
}
```

---

### Ejemplo 5 — Combinando SwitchOn con When en el mismo builder

`When` y `Unless` se pueden usar dentro de cada caso de `SwitchOn`, de la misma forma que se usan fuera.

```csharp
public class ProductDto
{
    public string Category { get; set; } = string.Empty;  // "physical", "digital", "service"
    public decimal Price { get; set; }
    public decimal? DiscountPercent { get; set; }
    public bool HasDiscount { get; set; }
    public string? LicenseKey { get; set; }
    public int? StockQuantity { get; set; }
}

public class ProductValidator : AbstractValidator<ProductDto>
{
    public ProductValidator()
    {
        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("El precio debe ser mayor que cero.");

        // DiscountPercent tiene reglas distintas segun la categoria del producto.
        // Ademas, dentro de cada caso, solo se valida si HasDiscount es true.
        RuleFor(x => x.DiscountPercent)
            .SwitchOn(x => x.Category)
            .Case("physical", b =>
            {
                // Los productos fisicos pueden tener descuentos de hasta 50%
                b.NotNull().WithMessage("El porcentaje de descuento es obligatorio cuando hay descuento.")
                 .Between(0.01m, 50.0m).WithMessage("El descuento para productos fisicos debe estar entre 0.01% y 50%.")
                 .When(x => x.HasDiscount);
            })
            .Case("digital", b =>
            {
                // Los productos digitales pueden tener descuentos de hasta 80%
                b.NotNull().WithMessage("El porcentaje de descuento es obligatorio cuando hay descuento.")
                 .Between(0.01m, 80.0m).WithMessage("El descuento para productos digitales debe estar entre 0.01% y 80%.")
                 .When(x => x.HasDiscount);
            })
            .Case("service", b =>
            {
                // Los servicios no permiten descuentos automaticos
                b.Null().WithMessage("Los servicios no permiten descuentos automaticos. Elimina el porcentaje.")
                 .When(x => x.HasDiscount);
            });

        // StockQuantity tambien es condicional segun la categoria
        RuleFor(x => x.StockQuantity)
            .SwitchOn(x => x.Category)
            .Case("physical", b =>
            {
                b.NotNull().WithMessage("El stock es obligatorio para productos fisicos.")
                 .GreaterThanOrEqualTo(0).WithMessage("El stock no puede ser negativo.");
            })
            .Case("digital", b =>
            {
                b.Null().WithMessage("Los productos digitales no tienen stock fisico.");
            });
    }
}
```

---

### Ejemplo 6 — Multiples SwitchOn en un mismo validador

Se pueden declarar tantos `SwitchOn` como propiedades necesiten validacion condicional. Cada uno es independiente.

```csharp
public class LoanApplicationDto
{
    public string LoanType { get; set; } = string.Empty;    // "personal", "mortgage", "vehicle"
    public decimal RequestedAmount { get; set; }
    public int TermMonths { get; set; }
    public decimal? InterestRate { get; set; }
    public string? PropertyAddress { get; set; }
    public decimal? PropertyValue { get; set; }
    public string? VehicleBrand { get; set; }
    public string? VehicleModel { get; set; }
    public int? VehicleYear { get; set; }
}

public class LoanApplicationValidator : AbstractValidator<LoanApplicationDto>
{
    public LoanApplicationValidator()
    {
        RuleFor(x => x.LoanType)
            .NotEmpty().WithMessage("El tipo de prestamo es obligatorio.")
            .In(new[] { "personal", "mortgage", "vehicle" })
            .WithMessage("El tipo de prestamo no es valido.");

        // RequestedAmount tiene diferentes limites segun el tipo de prestamo
        RuleFor(x => x.RequestedAmount)
            .SwitchOn(x => x.LoanType)
            .Case("personal", b =>
            {
                b.GreaterThan(0).WithMessage("El monto debe ser mayor que cero.")
                 .LessThanOrEqualTo(50000).WithMessage("El prestamo personal no puede superar los $50,000.");
            })
            .Case("mortgage", b =>
            {
                b.GreaterThan(0).WithMessage("El monto debe ser mayor que cero.")
                 .GreaterThanOrEqualTo(50000).WithMessage("El prestamo hipotecario debe ser de al menos $50,000.")
                 .LessThanOrEqualTo(5000000).WithMessage("El prestamo hipotecario no puede superar los $5,000,000.");
            })
            .Case("vehicle", b =>
            {
                b.GreaterThan(0).WithMessage("El monto debe ser mayor que cero.")
                 .LessThanOrEqualTo(150000).WithMessage("El prestamo vehicular no puede superar los $150,000.");
            });

        // TermMonths tambien tiene rangos validos distintos
        RuleFor(x => x.TermMonths)
            .SwitchOn(x => x.LoanType)
            .Case("personal", b =>
            {
                b.Between(6, 84).WithMessage("El plazo del prestamo personal debe estar entre 6 y 84 meses.");
            })
            .Case("mortgage", b =>
            {
                b.Between(60, 360).WithMessage("El plazo del prestamo hipotecario debe estar entre 60 y 360 meses (5 a 30 anos).");
            })
            .Case("vehicle", b =>
            {
                b.Between(12, 72).WithMessage("El plazo del prestamo vehicular debe estar entre 12 y 72 meses.");
            });
    }
}
```

---

## Diferencias detalladas: RuleSwitch vs SwitchOn vs When/Unless

### Tabla comparativa extensa

| Caracteristica                          | `RuleSwitch`                     | `SwitchOn`                              | `When` / `Unless`                   |
|-----------------------------------------|----------------------------------|-----------------------------------------|-------------------------------------|
| Alcance                                 | Todo el validador                | Una propiedad                           | Una propiedad                       |
| Propiedades afectadas por el bloque     | Multiples (cualquiera del objeto) | Solo la propiedad del RuleFor           | Solo la propiedad del RuleFor       |
| Tipo del action en los casos            | `Action<AbstractValidator<T>>`   | `Action<IRuleBuilder<T, TProperty>>`    | Predicado `Func<T, bool>`           |
| Exclusividad de ejecucion              | Si (como switch de C#)           | Si (como switch de C#)                  | No (cada condicion es independiente) |
| Puede afectar a N propiedades a la vez  | Si                               | No                                      | No                                  |
| Puede usar RuleFor dentro              | Si                               | No (ya esta dentro de un RuleFor)       | No aplica                           |
| Puede usar RuleForEach dentro          | Si                               | No                                      | No aplica                           |
| Puede incluir otro RuleSwitch          | Si (anidado)                     | No                                      | No aplica                           |
| Posicion en el codigo del validador    | Nivel constructor, mismo nivel que RuleFor | Encadenado dentro del RuleFor | Encadenado dentro del RuleFor       |
| Sintaxis del discriminador             | Expresion lambda                 | Expresion lambda                        | Predicado booleano                  |
| Caso por defecto                       | `.Default(...)`                  | `.Default(...)`                         | No tiene caso por defecto           |

---

### Arbol de decision: como elegir

```
Necesito aplicar reglas distintas segun el valor de una propiedad:
|
+-- ¿Las reglas afectan a MULTIPLES propiedades del objeto?
|   |
|   +-- SI  -->  Usar RuleSwitch
|   |
|   +-- NO (solo afecta a una propiedad)
|       |
|       +-- ¿El discriminador es el VALOR EXACTO de otra propiedad
|           y quiero garantizar exclusividad entre los casos?
|           |
|           +-- SI  -->  Usar SwitchOn en el RuleFor de esa propiedad
|           |
|           +-- NO (es un predicado booleano complejo, o no necesito
|                   exclusividad)
|               |
|               +-->  Usar When / Unless en el RuleFor de esa propiedad
```

**Ejemplo de cuándo usar cada uno:**

```csharp
// RuleSwitch: multiples propiedades afectadas segun PaymentMethod
RuleSwitch(x => x.PaymentMethod)
    .Case("card", rules =>
    {
        rules.RuleFor(x => x.CardNumber).CreditCard();  // propiedad 1
        rules.RuleFor(x => x.Cvv).IsNumeric();          // propiedad 2
        rules.RuleFor(x => x.Expiry).FutureDate();      // propiedad 3
    });

// SwitchOn: solo afecta a DocumentNumber, pero su formato varia segun DocumentType
RuleFor(x => x.DocumentNumber)
    .SwitchOn(x => x.DocumentType)
    .Case("dni", b => b.LengthBetween(8, 8).IsNumeric())
    .Case("passport", b => b.Matches(@"^[A-Z]{2}\d{6,7}$"));

// When: no necesito exclusividad, es una condicion adicional simple
RuleFor(x => x.MiddleName)
    .MaximumLength(50)
    .When(x => !string.IsNullOrEmpty(x.MiddleName));
```

---

## Casos de uso avanzados y patrones reales

### Patron 1 — E-commerce: orden segun tipo de envio

```csharp
public class CreateOrderDto
{
    public List<OrderLineDto> Lines { get; set; } = new();
    public string ShippingMethod { get; set; } = string.Empty;  // "standard", "express", "same_day", "pickup"
    public string? DeliveryStreet { get; set; }
    public string? DeliveryCity { get; set; }
    public string? DeliveryPostalCode { get; set; }
    public string? PickupStoreId { get; set; }
    public string? ContactPhone { get; set; }
    public DateTime? DeliveryWindow { get; set; }
    public string? SpecialInstructions { get; set; }
}

public class OrderLineDto
{
    public string Sku { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class CreateOrderValidator : AbstractValidator<CreateOrderDto>
{
    public CreateOrderValidator()
    {
        // Reglas sobre las lineas: siempre se validan
        RuleFor(x => x.Lines)
            .NotEmptyCollection().WithMessage("El pedido debe tener al menos un articulo.");

        RuleForEach(x => x.Lines)
            .NotEmpty();

        RuleFor(x => x.ShippingMethod)
            .NotEmpty().WithMessage("El metodo de envio es obligatorio.")
            .In(new[] { "standard", "express", "same_day", "pickup" })
            .WithMessage("El metodo de envio no es valido.");

        // Segun el metodo de envio, se requieren campos distintos
        RuleSwitch(x => x.ShippingMethod)
            .Case("standard", rules =>
            {
                rules.RuleFor(x => x.DeliveryStreet).NotEmpty().WithMessage("La calle de entrega es obligatoria.");
                rules.RuleFor(x => x.DeliveryCity).NotEmpty().WithMessage("La ciudad de entrega es obligatoria.");
                rules.RuleFor(x => x.DeliveryPostalCode)
                    .NotEmpty().WithMessage("El codigo postal es obligatorio.")
                    .Matches(@"^\d{5}$").WithMessage("El codigo postal debe tener 5 digitos.");
            })
            .Case("express", rules =>
            {
                rules.RuleFor(x => x.DeliveryStreet).NotEmpty().WithMessage("La calle de entrega es obligatoria.");
                rules.RuleFor(x => x.DeliveryCity).NotEmpty().WithMessage("La ciudad de entrega es obligatoria.");
                rules.RuleFor(x => x.DeliveryPostalCode)
                    .NotEmpty().WithMessage("El codigo postal es obligatorio.")
                    .Matches(@"^\d{5}$").WithMessage("El codigo postal debe tener 5 digitos.");
                rules.RuleFor(x => x.ContactPhone)
                    .NotEmpty().WithMessage("El telefono de contacto es obligatorio para envio express.")
                    .PhoneNumber().WithMessage("El telefono no tiene formato valido.");
            })
            .Case("same_day", rules =>
            {
                rules.RuleFor(x => x.DeliveryStreet).NotEmpty().WithMessage("La calle de entrega es obligatoria.");
                rules.RuleFor(x => x.DeliveryCity).NotEmpty().WithMessage("La ciudad de entrega es obligatoria.");
                rules.RuleFor(x => x.ContactPhone)
                    .NotEmpty().WithMessage("El telefono es obligatorio para envio en el dia.")
                    .PhoneNumber().WithMessage("El telefono no tiene formato valido.");
                rules.RuleFor(x => x.DeliveryWindow)
                    .NotNull().WithMessage("La ventana horaria de entrega es obligatoria para envio en el dia.")
                    .FutureDate().WithMessage("La ventana horaria debe ser en el futuro.");
            })
            .Case("pickup", rules =>
            {
                rules.RuleFor(x => x.PickupStoreId)
                    .NotEmpty().WithMessage("Debes seleccionar una tienda para la recogida.")
                    .NotEmptyGuid().WithMessage("El identificador de tienda no es valido.");
            });

        // SpecialInstructions es opcional, pero tiene limite de longitud
        RuleFor(x => x.SpecialInstructions)
            .MaximumLength(500)
            .WithMessage("Las instrucciones especiales no pueden superar los 500 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.SpecialInstructions));
    }
}
```

---

### Patron 2 — Formulario multi-paso (step 1 / 2 / 3)

```csharp
public class MultiStepFormDto
{
    public int Step { get; set; }           // 1, 2 o 3

    // Step 1: datos personales
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime? DateOfBirth { get; set; }

    // Step 2: datos de contacto
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }

    // Step 3: datos bancarios
    public string? Iban { get; set; }
    public string? CardHolder { get; set; }
}

public class MultiStepFormValidator : AbstractValidator<MultiStepFormDto>
{
    public MultiStepFormValidator()
    {
        RuleFor(x => x.Step)
            .Between(1, 3).WithMessage("El paso debe ser 1, 2 o 3.");

        // Segun el paso actual, se validan grupos distintos de campos.
        // Los campos de pasos anteriores no se revalidan (ya fueron validados antes).
        RuleSwitch(x => x.Step)
            .Case(1, rules =>
            {
                rules.RuleFor(x => x.FirstName)
                    .NotEmpty().WithMessage("El nombre es obligatorio.")
                    .MaximumLength(50).WithMessage("El nombre no puede superar los 50 caracteres.");

                rules.RuleFor(x => x.LastName)
                    .NotEmpty().WithMessage("El apellido es obligatorio.")
                    .MaximumLength(100).WithMessage("El apellido no puede superar los 100 caracteres.");

                rules.RuleFor(x => x.DateOfBirth)
                    .NotNull().WithMessage("La fecha de nacimiento es obligatoria.")
                    .PastDate().WithMessage("La fecha de nacimiento debe ser en el pasado.")
                    .MinAge(18).WithMessage("Debes ser mayor de 18 anos para registrarte.");
            })
            .Case(2, rules =>
            {
                rules.RuleFor(x => x.Email)
                    .NotEmpty().WithMessage("El email es obligatorio.")
                    .Email().WithMessage("El email no tiene formato valido.");

                rules.RuleFor(x => x.Phone)
                    .NotEmpty().WithMessage("El telefono es obligatorio.")
                    .PhoneNumber().WithMessage("El telefono no tiene formato valido.");

                rules.RuleFor(x => x.Address)
                    .NotEmpty().WithMessage("La direccion es obligatoria.")
                    .MaximumLength(200).WithMessage("La direccion no puede superar los 200 caracteres.");
            })
            .Case(3, rules =>
            {
                rules.RuleFor(x => x.Iban)
                    .NotEmpty().WithMessage("El IBAN es obligatorio.")
                    .Iban().WithMessage("El IBAN no tiene formato valido.");

                rules.RuleFor(x => x.CardHolder)
                    .NotEmpty().WithMessage("El nombre del titular es obligatorio.")
                    .IsAlpha().WithMessage("El nombre del titular solo puede contener letras.")
                    .MaximumLength(100).WithMessage("El nombre del titular no puede superar los 100 caracteres.");
            });
    }
}
```

---

### Patron 3 — API multi-tenant (B2B / B2C / internal)

```csharp
public enum ClientType
{
    B2B,
    B2C,
    Internal
}

public class CreateInvoiceDto
{
    public ClientType ClientType { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string? TaxId { get; set; }           // RUC/CIF — obligatorio para B2B
    public string? Email { get; set; }            // Obligatorio para B2C
    public decimal TotalAmount { get; set; }
    public string? InternalCostCenter { get; set; }  // Solo Internal
    public string? ApprovalCode { get; set; }         // Solo Internal
    public string? PaymentTerms { get; set; }         // Solo B2B
}

public class CreateInvoiceValidator : AbstractValidator<CreateInvoiceDto>
{
    public CreateInvoiceValidator()
    {
        RuleFor(x => x.ClientName)
            .NotEmpty().WithMessage("El nombre del cliente es obligatorio.")
            .MaximumLength(200).WithMessage("El nombre no puede superar los 200 caracteres.");

        RuleFor(x => x.TotalAmount)
            .GreaterThan(0).WithMessage("El total de la factura debe ser mayor que cero.");

        RuleSwitch(x => x.ClientType)
            .Case(ClientType.B2B, rules =>
            {
                rules.RuleFor(x => x.TaxId)
                    .NotEmpty().WithMessage("El NIF/CIF/RUC es obligatorio para clientes empresariales.")
                    .MinimumLength(9).WithMessage("El identificador fiscal parece demasiado corto.");

                rules.RuleFor(x => x.PaymentTerms)
                    .NotEmpty().WithMessage("Los terminos de pago son obligatorios para clientes B2B.")
                    .In(new[] { "net15", "net30", "net60", "immediate" })
                    .WithMessage("Los terminos de pago deben ser net15, net30, net60 o immediate.");
            })
            .Case(ClientType.B2C, rules =>
            {
                rules.RuleFor(x => x.Email)
                    .NotEmpty().WithMessage("El email es obligatorio para clientes particulares.")
                    .Email().WithMessage("El email no tiene formato valido.");
            })
            .Case(ClientType.Internal, rules =>
            {
                rules.RuleFor(x => x.InternalCostCenter)
                    .NotEmpty().WithMessage("El centro de coste interno es obligatorio.")
                    .Matches(@"^CC-\d{4}$").WithMessage("El centro de coste debe tener el formato CC-XXXX.");

                rules.RuleFor(x => x.ApprovalCode)
                    .NotEmpty().WithMessage("El codigo de aprobacion es obligatorio para facturas internas.")
                    .MinimumLength(8).WithMessage("El codigo de aprobacion debe tener al menos 8 caracteres.");
            });
    }
}
```

---

### Patron 4 — RuleSwitch y SwitchOn combinados en el mismo validador

Este es el patron mas poderoso: usar `RuleSwitch` para activar bloques de reglas sobre multiples propiedades, y `SwitchOn` para ajustar el formato de un campo especifico.

```csharp
public class ScientificSampleDto
{
    public string SampleType { get; set; } = string.Empty;  // "liquid", "solid", "gas"
    public string Unit { get; set; } = string.Empty;
    public double MeasuredValue { get; set; }
    public string ContainerType { get; set; } = string.Empty;
    public double? Temperature { get; set; }
    public string? TemperatureUnit { get; set; }  // "celsius", "fahrenheit", "kelvin"
    public string LabCode { get; set; } = string.Empty;
    public DateTime CollectionDate { get; set; }
}

public class ScientificSampleValidator : AbstractValidator<ScientificSampleDto>
{
    public ScientificSampleValidator()
    {
        // Reglas globales
        RuleFor(x => x.LabCode)
            .NotEmpty().WithMessage("El codigo de laboratorio es obligatorio.")
            .Matches(@"^LAB-\d{4}-[A-Z]{2}$").WithMessage("El codigo de laboratorio debe tener el formato LAB-XXXX-XX.");

        RuleFor(x => x.CollectionDate)
            .PastDate().WithMessage("La fecha de recoleccion no puede ser futura.");

        RuleFor(x => x.SampleType)
            .NotEmpty().WithMessage("El tipo de muestra es obligatorio.")
            .In(new[] { "liquid", "solid", "gas" }).WithMessage("El tipo de muestra debe ser liquid, solid o gas.");

        // RuleSwitch: activa bloques distintos de reglas segun el tipo de muestra
        RuleSwitch(x => x.SampleType)
            .Case("liquid", rules =>
            {
                rules.RuleFor(x => x.Unit)
                    .NotEmpty().WithMessage("La unidad de medida es obligatoria.")
                    .In(new[] { "ml", "L", "cc" }).WithMessage("Las muestras liquidas usan ml, L o cc.");

                rules.RuleFor(x => x.ContainerType)
                    .NotEmpty().WithMessage("El tipo de contenedor es obligatorio.")
                    .In(new[] { "vial", "tube", "flask" }).WithMessage("Los contenedores para liquidos son vial, tube o flask.");

                rules.RuleFor(x => x.Temperature)
                    .NotNull().WithMessage("La temperatura de almacenamiento es obligatoria para muestras liquidas.");
            })
            .Case("solid", rules =>
            {
                rules.RuleFor(x => x.Unit)
                    .NotEmpty().WithMessage("La unidad de medida es obligatoria.")
                    .In(new[] { "g", "kg", "mg" }).WithMessage("Las muestras solidas usan g, kg o mg.");

                rules.RuleFor(x => x.ContainerType)
                    .NotEmpty().WithMessage("El tipo de contenedor es obligatorio.")
                    .In(new[] { "bag", "box", "jar" }).WithMessage("Los contenedores para solidos son bag, box o jar.");
            })
            .Case("gas", rules =>
            {
                rules.RuleFor(x => x.Unit)
                    .NotEmpty().WithMessage("La unidad de medida es obligatoria.")
                    .In(new[] { "ppm", "ppb", "%vol" }).WithMessage("Las muestras gaseosas usan ppm, ppb o %vol.");

                rules.RuleFor(x => x.ContainerType)
                    .NotEmpty().WithMessage("El tipo de contenedor es obligatorio.")
                    .In(new[] { "canister", "cylinder", "bag" }).WithMessage("Los contenedores para gases son canister, cylinder o bag.");

                rules.RuleFor(x => x.Temperature)
                    .NotNull().WithMessage("La temperatura es critica para muestras gaseosas.");
            });

        // SwitchOn: el valor de temperatura tiene validacion distinta segun la unidad de temperatura
        RuleFor(x => x.Temperature)
            .SwitchOn(x => x.TemperatureUnit)
            .Case("celsius", b =>
            {
                b.GreaterThan(-273.15).WithMessage("La temperatura en Celsius no puede ser inferior al cero absoluto.")
                 .LessThan(10000).WithMessage("La temperatura en Celsius supera el maximo para muestras de laboratorio.");
            })
            .Case("fahrenheit", b =>
            {
                b.GreaterThan(-459.67).WithMessage("La temperatura en Fahrenheit no puede ser inferior al cero absoluto.")
                 .LessThan(18000).WithMessage("La temperatura en Fahrenheit supera el maximo para muestras de laboratorio.");
            })
            .Case("kelvin", b =>
            {
                b.GreaterThan(0).WithMessage("La temperatura en Kelvin debe ser mayor que cero absoluto.")
                 .LessThan(10273.15).WithMessage("La temperatura en Kelvin supera el maximo para muestras de laboratorio.");
            })
            .Default(b =>
            {
                // Si hay temperatura pero no hay unidad de temperatura, es un error
                b.Null().WithMessage("Si se especifica temperatura, debe indicarse tambien la unidad (celsius, fahrenheit o kelvin).")
                 .When(x => x.Temperature.HasValue);
            });
    }
}
```

---

### Patron 5 — Prestamos financieros segun tipo

```csharp
public class LoanRequestDto
{
    public string LoanType { get; set; } = string.Empty;
    public string ApplicantName { get; set; } = string.Empty;
    public string ApplicantId { get; set; } = string.Empty;
    public decimal RequestedAmount { get; set; }
    public int TermMonths { get; set; }
    public decimal MonthlyIncome { get; set; }
    // Hipotecario
    public string? PropertyAddress { get; set; }
    public decimal? PropertyAppraisalValue { get; set; }
    public decimal? DownPaymentAmount { get; set; }
    // Vehicular
    public string? VehicleBrand { get; set; }
    public string? VehicleModel { get; set; }
    public int? VehicleYear { get; set; }
    public decimal? VehicleValue { get; set; }
    // Personal
    public string? PurposeDescription { get; set; }
}

public class LoanRequestValidator : AbstractValidator<LoanRequestDto>
{
    public LoanRequestValidator()
    {
        RuleFor(x => x.ApplicantName)
            .NotEmpty().WithMessage("El nombre del solicitante es obligatorio.");

        RuleFor(x => x.ApplicantId)
            .NotEmpty().WithMessage("El documento de identidad es obligatorio.");

        RuleFor(x => x.MonthlyIncome)
            .GreaterThan(0).WithMessage("El ingreso mensual debe ser mayor que cero.");

        RuleFor(x => x.LoanType)
            .NotEmpty().WithMessage("El tipo de prestamo es obligatorio.")
            .In(new[] { "personal", "mortgage", "vehicle" })
            .WithMessage("El tipo de prestamo debe ser personal, mortgage o vehicle.");

        // Restricciones de monto segun tipo
        RuleFor(x => x.RequestedAmount)
            .SwitchOn(x => x.LoanType)
            .Case("personal", b =>
            {
                b.GreaterThan(500).WithMessage("El monto minimo del prestamo personal es $500.")
                 .LessThanOrEqualTo(50000).WithMessage("El monto maximo del prestamo personal es $50,000.");
            })
            .Case("mortgage", b =>
            {
                b.GreaterThanOrEqualTo(50000).WithMessage("El monto minimo del prestamo hipotecario es $50,000.")
                 .LessThanOrEqualTo(5000000).WithMessage("El monto maximo del prestamo hipotecario es $5,000,000.");
            })
            .Case("vehicle", b =>
            {
                b.GreaterThan(1000).WithMessage("El monto minimo del prestamo vehicular es $1,000.")
                 .LessThanOrEqualTo(150000).WithMessage("El monto maximo del prestamo vehicular es $150,000.");
            });

        // Plazo segun tipo
        RuleFor(x => x.TermMonths)
            .SwitchOn(x => x.LoanType)
            .Case("personal", b => b.Between(6, 84).WithMessage("El plazo del prestamo personal debe estar entre 6 y 84 meses."))
            .Case("mortgage", b => b.Between(60, 360).WithMessage("El plazo hipotecario debe estar entre 60 y 360 meses."))
            .Case("vehicle", b => b.Between(12, 72).WithMessage("El plazo vehicular debe estar entre 12 y 72 meses."));

        // Campos especificos por tipo de prestamo
        RuleSwitch(x => x.LoanType)
            .Case("mortgage", rules =>
            {
                rules.RuleFor(x => x.PropertyAddress)
                    .NotEmpty().WithMessage("La direccion del inmueble es obligatoria para prestamos hipotecarios.");

                rules.RuleFor(x => x.PropertyAppraisalValue)
                    .NotNull().WithMessage("El valor de tasacion es obligatorio.")
                    .GreaterThan(0).WithMessage("El valor de tasacion debe ser mayor que cero.");

                rules.RuleFor(x => x.DownPaymentAmount)
                    .NotNull().WithMessage("El importe de la entrada es obligatorio.")
                    .GreaterThan(0).WithMessage("La entrada debe ser mayor que cero.");
            })
            .Case("vehicle", rules =>
            {
                rules.RuleFor(x => x.VehicleBrand)
                    .NotEmpty().WithMessage("La marca del vehiculo es obligatoria.");

                rules.RuleFor(x => x.VehicleModel)
                    .NotEmpty().WithMessage("El modelo del vehiculo es obligatorio.");

                rules.RuleFor(x => x.VehicleYear)
                    .NotNull().WithMessage("El ano del vehiculo es obligatorio.")
                    .GreaterThanOrEqualTo(2000).WithMessage("Solo se financian vehiculos del ano 2000 en adelante.")
                    .LessThanOrEqualTo(DateTime.UtcNow.Year + 1)
                    .WithMessage("El ano del vehiculo no puede ser mayor al proximo ano.");

                rules.RuleFor(x => x.VehicleValue)
                    .NotNull().WithMessage("El valor del vehiculo es obligatorio.")
                    .GreaterThan(0).WithMessage("El valor del vehiculo debe ser mayor que cero.");
            })
            .Case("personal", rules =>
            {
                rules.RuleFor(x => x.PurposeDescription)
                    .NotEmpty().WithMessage("La descripcion del proposito del prestamo es obligatoria.")
                    .MinimumLength(20).WithMessage("La descripcion debe tener al menos 20 caracteres.")
                    .MaximumLength(500).WithMessage("La descripcion no puede superar los 500 caracteres.");
            });
    }
}
```

---

## Integracion con ASP.NET Core, MediatR y Vali-Mediator

Los validadores que usan `RuleSwitch` o `SwitchOn` se integran con ASP.NET Core, MediatR y Vali-Mediator de forma completamente transparente. No requieren ningun cambio en la capa de infraestructura.

### Registro en ASP.NET Core

```csharp
// Program.cs
builder.Services.AddValidationsFromAssembly(typeof(Program).Assembly);
```

### Uso en un endpoint con filtro de validacion

```csharp
app.MapPost("/payments", async (PaymentDto dto, IValidator<PaymentDto> validator) =>
{
    var result = await validator.ValidateAsync(dto);
    if (!result.IsValid)
        return Results.ValidationProblem(result.ToDictionary());

    // ... procesar pago
    return Results.Ok();
});
```

### Uso con ValidationBehavior en MediatR

```csharp
// El validador se ejecuta automaticamente antes del handler
public class ProcessPaymentCommand : IRequest<Unit>
{
    public PaymentDto Payment { get; set; } = new();
}

public class ProcessPaymentCommandValidator : AbstractValidator<ProcessPaymentCommand>
{
    public ProcessPaymentCommandValidator()
    {
        RuleFor(x => x.Payment.Amount).GreaterThan(0);

        // RuleSwitch funciona exactamente igual dentro de un command validator
        RuleSwitch(x => x.Payment.Method)
            .Case("credit_card", rules =>
            {
                rules.RuleFor(x => x.Payment.CardNumber).NotEmpty().CreditCard();
            });
    }
}
```

### Uso con Vali-Mediator

```csharp
// Program.cs
builder.Services.AddValiMediator(config =>
{
    config.RegisterServicesFromAssembly(typeof(Program).Assembly);
    config.AddValiValidation(typeof(Program).Assembly);
});
```

El `ValidationBehavior` detecta automaticamente los errores de validacion y los convierte a `Result<T>.Fail(...)` si el response es un `Result<T>`, o lanza `ValidationException` en caso contrario. Esto funciona igual con `RuleSwitch` y `SwitchOn`.

---

## Testing de validadores con RuleSwitch y SwitchOn

### Configuracion del proyecto de tests

```xml
<!-- Vali-Validation.Tests.csproj -->
<ItemGroup>
  <PackageReference Include="xunit" Version="2.9.0" />
  <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
</ItemGroup>
```

### Tests para RuleSwitch

```csharp
using Xunit;
using Vali_Validation.Core.Validators;

public class PaymentValidatorTests
{
    private readonly PaymentValidator _validator = new();

    // ---- Tarjeta de credito ----

    [Fact]
    public async Task CreditCard_ValidData_ShouldPass()
    {
        var dto = new PaymentDto
        {
            Method = "credit_card",
            Amount = 100.00m,
            CardNumber = "4111111111111111",
            Cvv = "123",
            ExpirationDate = DateTime.UtcNow.AddYears(2)
        };

        var result = await _validator.ValidateAsync(dto);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task CreditCard_MissingCvv_ShouldFail()
    {
        var dto = new PaymentDto
        {
            Method = "credit_card",
            Amount = 100.00m,
            CardNumber = "4111111111111111",
            Cvv = null,
            ExpirationDate = DateTime.UtcNow.AddYears(2)
        };

        var result = await _validator.ValidateAsync(dto);

        Assert.False(result.IsValid);
        Assert.True(result.Errors.ContainsKey("Cvv"));
    }

    // ---- Transferencia bancaria ----

    [Fact]
    public async Task BankTransfer_ValidData_ShouldPass()
    {
        var dto = new PaymentDto
        {
            Method = "bank_transfer",
            Amount = 250.00m,
            Iban = "ES9121000418450200051332",
            BankCode = "BSABESBB"
        };

        var result = await _validator.ValidateAsync(dto);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task BankTransfer_MissingIban_ShouldFailWithIbanError()
    {
        var dto = new PaymentDto
        {
            Method = "bank_transfer",
            Amount = 250.00m,
            Iban = null,
            BankCode = "BSABESBB"
        };

        var result = await _validator.ValidateAsync(dto);

        Assert.False(result.IsValid);
        Assert.True(result.Errors.ContainsKey("Iban"));
    }

    // ---- Exclusividad: el caso bank_transfer no debe validar campos de credit_card ----

    [Fact]
    public async Task BankTransfer_WithMissingCardFields_ShouldNotReportCardErrors()
    {
        // CardNumber no es requerido cuando el metodo es bank_transfer
        var dto = new PaymentDto
        {
            Method = "bank_transfer",
            Amount = 250.00m,
            Iban = "ES9121000418450200051332",
            BankCode = "BSABESBB"
            // CardNumber, Cvv y ExpirationDate estan ausentes — no deberia importar
        };

        var result = await _validator.ValidateAsync(dto);

        Assert.True(result.IsValid);
        Assert.False(result.Errors.ContainsKey("CardNumber"));
        Assert.False(result.Errors.ContainsKey("Cvv"));
    }

    // ---- Default: metodo desconocido ----

    [Fact]
    public async Task UnknownMethod_WithoutReference_ShouldFailWithReferenceError()
    {
        var dto = new PaymentDto
        {
            Method = "crypto",  // No coincide con ningun Case
            Amount = 100.00m,
            Reference = null    // El Default requiere Reference
        };

        var result = await _validator.ValidateAsync(dto);

        // La regla global de In() fallara, pero tambien el Default del switch
        Assert.True(result.Errors.ContainsKey("Reference"));
    }
}
```

### Tests para SwitchOn

```csharp
public class IdentityValidatorTests
{
    private readonly IdentityValidator _validator = new();

    [Theory]
    [InlineData("passport", "AB123456")]      // Formato valido
    [InlineData("passport", "XY9999999")]     // Formato valido (7 digitos)
    public async Task Passport_ValidNumber_ShouldPass(string type, string number)
    {
        var dto = new IdentityDto
        {
            DocumentType = type,
            DocumentNumber = number,
            FirstName = "Juan",
            LastName = "Perez"
        };

        var result = await _validator.ValidateAsync(dto);

        Assert.True(result.IsValid, string.Join(", ", result.Errors.SelectMany(e => e.Value)));
    }

    [Theory]
    [InlineData("passport", "12345678")]    // Sin prefijo de letras
    [InlineData("passport", "ab123456")]    // Letras en minuscula
    [InlineData("passport", "ABCDE")]       // Demasiado corto
    public async Task Passport_InvalidNumber_ShouldFail(string type, string number)
    {
        var dto = new IdentityDto
        {
            DocumentType = type,
            DocumentNumber = number,
            FirstName = "Juan",
            LastName = "Perez"
        };

        var result = await _validator.ValidateAsync(dto);

        Assert.False(result.IsValid);
        Assert.True(result.Errors.ContainsKey("DocumentNumber"));
    }

    [Fact]
    public async Task Dni_ValidNumber_ShouldPass()
    {
        var dto = new IdentityDto
        {
            DocumentType = "dni",
            DocumentNumber = "12345678",
            FirstName = "Maria",
            LastName = "Garcia"
        };

        var result = await _validator.ValidateAsync(dto);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("1234567")]    // Solo 7 digitos (DNI necesita 8)
    [InlineData("123456789")]  // 9 digitos (DNI necesita exactamente 8)
    [InlineData("1234567A")]   // Contiene una letra
    public async Task Dni_InvalidNumber_ShouldFail(string number)
    {
        var dto = new IdentityDto
        {
            DocumentType = "dni",
            DocumentNumber = number,
            FirstName = "Maria",
            LastName = "Garcia"
        };

        var result = await _validator.ValidateAsync(dto);

        Assert.False(result.IsValid);
        Assert.True(result.Errors.ContainsKey("DocumentNumber"));
    }

    [Fact]
    public async Task Ruc_ValidNumber_ShouldPass()
    {
        var dto = new IdentityDto
        {
            DocumentType = "ruc",
            DocumentNumber = "20123456789",
            FirstName = "Empresa",
            LastName = "SA"
        };

        var result = await _validator.ValidateAsync(dto);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task SwitchOn_ExclusivityVerification_DniRulesDoNotApplyForPassport()
    {
        // Pasaporte con formato correcto para pasaporte.
        // Las reglas de DNI (longitud 8, solo numeros) NO deben aplicar.
        var dto = new IdentityDto
        {
            DocumentType = "passport",
            DocumentNumber = "AB123456",
            FirstName = "John",
            LastName = "Doe"
        };

        var result = await _validator.ValidateAsync(dto);

        // AB123456 no es solo numeros ni tiene exactamente 8 digitos,
        // pero eso es correcto para pasaporte — no deben saltar errores de DNI.
        Assert.True(result.IsValid);
    }
}
```

---

## Errores comunes y como evitarlos

### Antipatron 1 — Usar When/Unless cuando se necesita exclusividad

**Incorrecto:** Usar multiples `When` independientes puede llevar a que se ejecuten reglas de mas de un "caso" si las condiciones se solapan.

```csharp
// MAL: si PaymentMethod cambia entre las evaluaciones, o si las condiciones
// no son mutuamente excluyentes, podrian ejecutarse reglas de ambos bloques.
RuleFor(x => x.CardNumber)
    .NotEmpty().When(x => x.PaymentMethod == "credit_card");

RuleFor(x => x.Iban)
    .NotEmpty().When(x => x.PaymentMethod == "bank_transfer");
```

**Correcto:** Usar `RuleSwitch` garantiza que solo un bloque se ejecuta.

```csharp
// BIEN: exclusividad garantizada por diseño
RuleSwitch(x => x.PaymentMethod)
    .Case("credit_card", rules =>
    {
        rules.RuleFor(x => x.CardNumber).NotEmpty();
    })
    .Case("bank_transfer", rules =>
    {
        rules.RuleFor(x => x.Iban).NotEmpty();
    });
```

---

### Antipatron 2 — Poner reglas globales dentro de los casos

Si una regla aplica a todos los casos, no debe repetirse en cada Case.

```csharp
// MAL: Amount se valida dentro de cada caso, repetido innecesariamente
RuleSwitch(x => x.Method)
    .Case("credit_card", rules =>
    {
        rules.RuleFor(x => x.Amount).GreaterThan(0);  // repetido
        rules.RuleFor(x => x.CardNumber).NotEmpty();
    })
    .Case("paypal", rules =>
    {
        rules.RuleFor(x => x.Amount).GreaterThan(0);  // repetido
        rules.RuleFor(x => x.PaypalEmail).NotEmpty();
    });
```

```csharp
// BIEN: Amount es una regla global, fuera del switch
RuleFor(x => x.Amount).GreaterThan(0);

RuleSwitch(x => x.Method)
    .Case("credit_card", rules =>
    {
        rules.RuleFor(x => x.CardNumber).NotEmpty();
    })
    .Case("paypal", rules =>
    {
        rules.RuleFor(x => x.PaypalEmail).NotEmpty();
    });
```

---

### Antipatron 3 — Usar SwitchOn cuando el discriminador es la misma propiedad que se valida

```csharp
// MAL: SwitchOn sobre la misma propiedad que se valida es semanticamente confuso
// y no aporta valor; usa Must o reglas directas.
RuleFor(x => x.Status)
    .SwitchOn(x => x.Status)   // el discriminador es la misma propiedad
    .Case("active", b => b.NotEmpty())
    .Case("inactive", b => b.NotEmpty());
```

```csharp
// BIEN: si el discriminador es la misma propiedad, simplemente valida directamente
RuleFor(x => x.Status)
    .NotEmpty()
    .In(new[] { "active", "inactive" });
```

---

### Antipatron 4 — Olvidar el Default cuando el discriminador puede tener valores inesperados

Si el discriminador puede tener valores que no estan cubiertos por ningun `Case` y no hay `Default`, el switch no ejecutara nada para esos valores. Esto puede ser intencional, pero en muchos casos es un error.

```csharp
// Riesgo: si DocumentType es "ce" (carnet de extranjeria), no se ejecuta ningun Case
// y DocumentNumber no se valida en absoluto.
RuleFor(x => x.DocumentNumber)
    .SwitchOn(x => x.DocumentType)
    .Case("passport", b => b.Matches(@"^[A-Z]{2}\d{6,7}$"))
    .Case("dni", b => b.LengthBetween(8, 8).IsNumeric());
    // Sin Default
```

```csharp
// BIEN: agregar un Default que aplique una validacion minima para casos no cubiertos
RuleFor(x => x.DocumentNumber)
    .SwitchOn(x => x.DocumentType)
    .Case("passport", b => b.Matches(@"^[A-Z]{2}\d{6,7}$"))
    .Case("dni", b => b.LengthBetween(8, 8).IsNumeric())
    .Default(b => b.NotEmpty().WithMessage("El numero de documento es obligatorio."));
```

---

### Antipatron 5 — Anidar RuleSwitch dentro de SwitchOn

`SwitchOn` opera a nivel de propiedad y su `Action` recibe un `IRuleBuilder<T, TProperty>`, no un `AbstractValidator<T>`. Por lo tanto, no es posible llamar a `RuleSwitch` dentro de un caso de `SwitchOn`. Si necesitas esa logica, usa `RuleSwitch` en el nivel del validador.

```csharp
// INCORRECTO (no compila): no se puede usar RuleSwitch dentro de SwitchOn
RuleFor(x => x.Amount)
    .SwitchOn(x => x.Type)
    .Case("special", b =>
    {
        // b es IRuleBuilder<T, decimal>, no AbstractValidator<T>
        // b.RuleSwitch(...) no existe
    });
```

```csharp
// CORRECTO: usa RuleSwitch al nivel del validador para casos complejos
RuleSwitch(x => x.Type)
    .Case("special", rules =>
    {
        rules.RuleFor(x => x.Amount).Between(100m, 10000m);
        // Puedes agregar mas propiedades aqui
    });
```

---

### Antipatron 6 — No aprovechar WithErrorCode para manejo programatico de errores

En APIs REST es comun que el frontend necesite distinguir el tipo de error para mostrar mensajes localizados. No usar `WithErrorCode` obliga a parsear mensajes de texto, lo cual es fragil.

```csharp
// MAL: el cliente tiene que parsear el string para saber que regla fallo
RuleSwitch(x => x.DocumentType)
    .Case("passport", rules =>
    {
        rules.RuleFor(x => x.DocumentNumber)
            .Matches(@"^[A-Z]{2}\d{6,7}$")
            .WithMessage("El formato del pasaporte es incorrecto");  // sin codigo
    });
```

```csharp
// BIEN: con ErrorCode, el cliente puede actuar programaticamente
RuleSwitch(x => x.DocumentType)
    .Case("passport", rules =>
    {
        rules.RuleFor(x => x.DocumentNumber)
            .NotEmpty()
                .WithMessage("El numero de pasaporte es obligatorio.")
                .WithErrorCode("PASSPORT_NUMBER_REQUIRED")
            .Matches(@"^[A-Z]{2}\d{6,7}$")
                .WithMessage("El formato del pasaporte es incorrecto.")
                .WithErrorCode("PASSPORT_NUMBER_FORMAT");
    });
```

---

## Siguientes pasos

- [07 — Modificadores](./07-modificadores.md) — `WithMessage`, `WithErrorCode`, `OverridePropertyName`, `StopOnFirstFailure`
- [08 — CascadeMode](./08-cascade-mode.md) — como controlar la ejecucion de reglas tras el primer fallo
- [15 — Patrones avanzados](./15-patrones-avanzados.md) — composicion de validadores, herencia, `Include`, colecciones anidadas
- [12 — Integracion con ASP.NET Core](./12-integracion-aspnetcore.md) — middleware, filtros y registro automatico
- [13 — Integracion con MediatR](./13-integracion-mediatr.md) — `ValidationBehavior` para MediatR
- [14 — Integracion con Vali-Mediator](./14-integracion-valimediator.md) — `AddValiValidation` y manejo de `Result<T>`
