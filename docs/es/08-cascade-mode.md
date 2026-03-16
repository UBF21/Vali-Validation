# CascadeMode

`CascadeMode` controla qué ocurre cuando una regla falla: si se continúa evaluando las demás reglas o si se detiene la evaluación. Existen dos niveles de control:

1. **Por propiedad** — usando `.StopOnFirstFailure()` en el builder
2. **Global (todo el validador)** — sobreescribiendo `GlobalCascadeMode`

---

## Los dos modos

```csharp
public enum CascadeMode
{
    Continue,          // Default: evalúa todas las reglas aunque algunas fallen
    StopOnFirstFailure // Para al primer fallo por propiedad o por validador
}
```

---

## CascadeMode por propiedad

### StopOnFirstFailure por propiedad

Cuando usas `.StopOnFirstFailure()` en un builder, la evaluación de esa propiedad se detiene tras el primer fallo. Las demás propiedades del validador **sí se evalúan**.

```csharp
public class UserRegistrationValidator : AbstractValidator<UserRegistrationRequest>
{
    private readonly IUserRepository _users;

    public UserRegistrationValidator(IUserRepository users)
    {
        _users = users;

        RuleFor(x => x.Email)
            .NotEmpty()
                .WithMessage("El email es obligatorio.")         // Regla 1
            .Email()
                .WithMessage("El email no tiene formato válido.") // Regla 2
            .MustAsync(async (email, ct) =>
                !await _users.EmailExistsAsync(email, ct))
                .WithMessage("Ese email ya está registrado.")    // Regla 3
            .StopOnFirstFailure();

        // Email y Password se evalúan de forma independiente:
        // Si Email falla en la regla 1, las reglas 2 y 3 del Email no se evalúan.
        // Pero Password SÍ se evalúa normalmente.
        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8);
    }
}
```

### Por qué es importante

Sin `StopOnFirstFailure`, si el email es `null`:

```
Email: El email es obligatorio.
Email: El email no tiene formato válido.
Email: <MustAsync lanza NullReferenceException porque email es null>
```

Con `StopOnFirstFailure`, si el email es `null`:

```
Email: El email es obligatorio.
```

Solo el primer error. Las reglas siguientes no se ejecutan, lo que es más seguro y más útil para el usuario.

### Ejemplo real: validación de tarjeta de crédito

```csharp
public class PaymentValidator : AbstractValidator<PaymentRequest>
{
    public PaymentValidator()
    {
        // Sin StopOnFirstFailure:
        // Si CardNumber es null → "es obligatorio", "no es válido (Luhn)", etc.
        // Con StopOnFirstFailure:
        // Si CardNumber es null → solo "es obligatorio"
        RuleFor(x => x.CardNumber)
            .NotEmpty()
                .WithMessage("El número de tarjeta es obligatorio.")
            .NoWhitespace()
                .WithMessage("El número de tarjeta no debe contener espacios.")
            .IsNumeric()
                .WithMessage("El número de tarjeta solo puede contener dígitos.")
            .LengthBetween(13, 19)
                .WithMessage("El número de tarjeta debe tener entre 13 y 19 dígitos.")
            .CreditCard()
                .WithMessage("El número de tarjeta no es válido.")
            .StopOnFirstFailure();

        RuleFor(x => x.ExpiryMonth)
            .GreaterThan(0)
            .LessThanOrEqualTo(12)
            .StopOnFirstFailure();

        RuleFor(x => x.ExpiryYear)
            .GreaterThan(DateTime.Today.Year - 1)
            .LessThanOrEqualTo(DateTime.Today.Year + 20)
            .StopOnFirstFailure();

        RuleFor(x => x.Cvv)
            .NotEmpty()
            .IsNumeric()
            .LengthBetween(3, 4)
            .StopOnFirstFailure();
    }
}
```

---

## CascadeMode global

El modo global se aplica a **todo el validador**. Cuando se activa `StopOnFirstFailure` de forma global:

- Si la primera **propiedad** evaluada tiene un error, las propiedades siguientes **no se evalúan**.
- Esto es diferente al por propiedad, donde solo se detiene la propiedad específica.

### Activar el modo global

```csharp
public class StrictPaymentValidator : AbstractValidator<PaymentRequest>
{
    protected override CascadeMode GlobalCascadeMode => CascadeMode.StopOnFirstFailure;

    public StrictPaymentValidator()
    {
        RuleFor(x => x.CardNumber)
            .NotEmpty()
            .CreditCard();

        RuleFor(x => x.Amount)
            .GreaterThan(0);

        // Si CardNumber falla, Amount no se evalúa
        // Si CardNumber pasa pero Amount falla, el validador para aquí
    }
}
```

### Diferencia visual entre modos

Dado este request inválido:

```csharp
var request = new PaymentRequest
{
    CardNumber = "",    // Falla
    Amount = -10,       // También falla
    Currency = ""       // También falla
};
```

**Con `CascadeMode.Continue` (default):**

```json
{
  "CardNumber": ["El número de tarjeta es obligatorio."],
  "Amount": ["El importe debe ser positivo."],
  "Currency": ["La moneda es obligatoria."]
}
```

**Con `GlobalCascadeMode = CascadeMode.StopOnFirstFailure`:**

```json
{
  "CardNumber": ["El número de tarjeta es obligatorio."]
}
```

Solo el error de la primera propiedad que falla. El validador se detiene ahí.

---

## Combinando ambos niveles

Puedes usar ambos niveles simultáneamente. Por ejemplo, modo global `Continue` pero `StopOnFirstFailure` en propiedades específicas:

```csharp
public class CreateOrderValidator : AbstractValidator<CreateOrderRequest>
{
    // Modo global: evalúa todas las propiedades aunque alguna falle
    protected override CascadeMode GlobalCascadeMode => CascadeMode.Continue;

    public CreateOrderValidator()
    {
        // Esta propiedad para tras el primer fallo (evita llamada a BD si está vacío)
        RuleFor(x => x.CustomerId)
            .NotEmpty()
            .MustAsync(async id => await _customers.ExistsAsync(id))
            .StopOnFirstFailure();

        // Esta propiedad también para (evita NRE si Card es null)
        RuleFor(x => x.CardNumber)
            .NotEmpty()
            .CreditCard()
            .StopOnFirstFailure();

        // Esta propiedad evalúa todas sus reglas (modo default)
        RuleFor(x => x.Notes)
            .MaximumLength(500)
            .NoWhitespace();
    }
}
```

---

## Modo global StopOnFirstFailure con propiedades específicas

Cuando el modo global es `StopOnFirstFailure` pero quieres que una propiedad específica evalúe todas sus reglas, puedes usar `StopOnFirstFailure()` solo en las propiedades que lo necesitan — el modo global ya lo aplica a todas, pero no hay manera de "desactivarlo" por propiedad.

En la práctica, si necesitas este control fino, es mejor usar el modo global `Continue` y agregar `StopOnFirstFailure()` solo donde lo necesites:

```csharp
// Patrón recomendado: global Continue + StopOnFirstFailure selectivo
public class FlexibleValidator : AbstractValidator<ComplexRequest>
{
    // Continue es el default, no necesitas sobreescribir si es el comportamiento que quieres
    // protected override CascadeMode GlobalCascadeMode => CascadeMode.Continue;

    public FlexibleValidator()
    {
        // Para en el primer fallo: crítico para evitar errores en cascada
        RuleFor(x => x.UserId)
            .NotEmpty()
            .MustAsync(async id => await _users.ExistsAsync(id))
            .StopOnFirstFailure();

        // Evalúa todas las reglas: útil para dar feedback completo al usuario
        RuleFor(x => x.Email)
            .NotEmpty()
                .WithMessage("El email es obligatorio.")
            .Email()
                .WithMessage("El formato del email no es válido.")
            .MaximumLength(320)
                .WithMessage("El email es demasiado largo.");
        // Sin StopOnFirstFailure: si NotEmpty falla, Email y MaximumLength también se evalúan

        // Para en el primer fallo: reglas costosas
        RuleFor(x => x.ExternalApiToken)
            .NotEmpty()
            .MustAsync(async token => await _api.ValidateTokenAsync(token))
            .StopOnFirstFailure();
    }
}
```

---

## Cuándo usar cada modo

### Usa `StopOnFirstFailure` por propiedad cuando:

1. **Las reglas siguientes pueden lanzar excepciones** si las anteriores no pasaron (ej: `null` en una regla que espera string)
2. **Hay reglas asíncronas costosas** que no deben ejecutarse si una validación básica ya falló
3. **Los mensajes acumulados serían confusos** (ej: "es null" y "no tiene formato válido" para el mismo campo)

```csharp
// Ejemplo 1: evitar NPE
RuleFor(x => x.Address)
    .NotNull()
        .WithMessage("La dirección es obligatoria.")
    .Must(addr => addr.PostalCode.Length == 5) // Lanzaría NPE si addr es null
    .StopOnFirstFailure();

// Ejemplo 2: evitar llamadas costosas
RuleFor(x => x.Email)
    .NotEmpty()
    .Email()
    .MustAsync(async email => !await _users.ExistsByEmailAsync(email)) // Solo si formato es válido
    .StopOnFirstFailure();
```

### Usa `GlobalCascadeMode = StopOnFirstFailure` cuando:

1. **La validación del primer campo es requisito previo** para los demás (ej: tipo de operación define qué campos son obligatorios)
2. **El rendimiento es crítico** y quieres minimizar el trabajo de validación
3. **Prefieres feedback incremental** (una propiedad a la vez) en lugar de todos los errores de golpe

```csharp
// Ejemplo: wizard de múltiples pasos donde cada paso valida una sección
public class WizardStep1Validator : AbstractValidator<WizardRequest>
{
    protected override CascadeMode GlobalCascadeMode => CascadeMode.StopOnFirstFailure;

    public WizardStep1Validator()
    {
        // Si el tipo de cuenta no es válido, no tiene sentido validar el resto
        RuleFor(x => x.AccountType)
            .NotEmpty()
            .IsEnum<AccountType>();

        RuleFor(x => x.CompanyName)
            .NotEmpty()
            .MaximumLength(200);

        // ... más campos del paso 1
    }
}
```

### Usa `Continue` (default) cuando:

1. **Quieres mostrar todos los errores de una vez** (mejor UX en formularios)
2. **Las reglas son independientes** y no hay riesgo de excepciones en cascada
3. **Estás en una API** donde el cliente quiere saber todos los problemas del request

---

## Ejemplo comparativo: formulario de registro

```csharp
// UX de formulario: muestra todos los errores al usuario a la vez
public class RegistrationFormValidator : AbstractValidator<RegistrationForm>
{
    // Continue por defecto
    public RegistrationFormValidator()
    {
        // StopOnFirstFailure solo donde es necesario
        RuleFor(x => x.Email)
            .NotEmpty().Email().StopOnFirstFailure();

        RuleFor(x => x.Password)
            .NotEmpty().MinimumLength(8).HasUppercase().HasLowercase().HasDigit().HasSpecialChar();
        // No StopOnFirstFailure: el usuario verá todos los requisitos que no cumple

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty()
            .EqualToProperty(x => x.Password)
            .StopOnFirstFailure();

        RuleFor(x => x.BirthDate)
            .PastDate()
            .Must(d => DateTime.Today.Year - d.Year >= 18)
            .StopOnFirstFailure();
    }
}
```

Con datos: `Password = "abc"` (sin mayúsculas, sin dígito, sin especial):

```json
{
  "Password": [
    "La contraseña debe tener al menos 8 caracteres.",
    "Debe contener al menos una mayúscula.",
    "Debe contener al menos un número.",
    "Debe contener al menos un carácter especial."
  ]
}
```

El usuario ve todos los requisitos que incumple en una sola respuesta.

---

## Siguientes pasos

- **[Resultado de validación](09-resultado-validacion.md)** — Cómo leer y usar ValidationResult
- **[Modificadores](07-modificadores.md)** — StopOnFirstFailure y otros modificadores en detalle
