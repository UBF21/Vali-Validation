# Reglas básicas

Este documento cubre todas las reglas síncronas disponibles en `IRuleBuilder<T, TProperty>`. Las reglas asíncronas (`MustAsync`, `DependentRuleAsync`, `Custom`, `Transform`, `SetValidator`) se describen en [Reglas avanzadas](06-reglas-avanzadas.md).

---

## Nulidad y vacío

### `NotNull()`

Verifica que el valor no sea `null`.

```csharp
public class ProductValidator : AbstractValidator<Product>
{
    public ProductValidator()
    {
        RuleFor(x => x.Category).NotNull();
        RuleFor(x => x.Tags).NotNull();
    }
}
```

Diferencia con `NotEmpty()`: `NotNull()` solo rechaza `null`. Un string vacío `""` pasaría `NotNull()` pero fallaría `NotEmpty()`.

### `Null()`

Verifica que el valor sea `null`. Útil para validar que ciertos campos NO deben enviarse en peticiones específicas.

```csharp
public class UpdatePasswordRequestValidator : AbstractValidator<UpdatePasswordRequest>
{
    public UpdatePasswordRequestValidator()
    {
        // El campo Id no se debe enviar en el body (se toma del JWT)
        RuleFor(x => x.UserId).Null()
            .WithMessage("No envíes el ID de usuario en el body; se toma del token.");
    }
}
```

### `NotEmpty()`

Verifica que el valor no sea `null`, no sea string vacío (`""`), y no sea solo espacios en blanco. Para colecciones, también verifica que no estén vacías.

```csharp
public class ArticleValidator : AbstractValidator<Article>
{
    public ArticleValidator()
    {
        RuleFor(x => x.Title).NotEmpty();
        RuleFor(x => x.Content).NotEmpty();
        RuleFor(x => x.Author).NotEmpty();

        // Para strings con espacios: "   " falla NotEmpty()
        RuleFor(x => x.Slug)
            .NotEmpty()
            .NoWhitespace();
    }
}
```

### `Empty()`

Verifica que el valor sea `null` o string vacío. Poco frecuente, pero útil para validar campos que deben estar en blanco en ciertos contextos.

```csharp
RuleFor(x => x.InternalNotes)
    .Empty()
        .WithMessage("Los notas internas no deben enviarse desde la API pública.")
    .When(x => x.Source == RequestSource.PublicApi);
```

---

## Igualdad

### `EqualTo(TProperty value)`

Verifica que el valor sea igual al valor dado.

```csharp
public class AcceptTermsValidator : AbstractValidator<RegistrationRequest>
{
    public AcceptTermsValidator()
    {
        RuleFor(x => x.AcceptTerms)
            .EqualTo(true)
                .WithMessage("Debes aceptar los términos y condiciones.");
    }
}
```

### `NotEqual(TProperty value)`

Verifica que el valor sea distinto al valor dado.

```csharp
RuleFor(x => x.NewPassword)
    .NotEqual("password")
        .WithMessage("No uses 'password' como contraseña.")
    .NotEqual("12345678")
        .WithMessage("Elige una contraseña más segura.");
```

### `EqualToProperty(Expression<Func<T, TProperty>> otherProp)`

Verifica que el valor sea igual al de otra propiedad del mismo objeto. Ideal para confirmación de contraseña o email.

```csharp
public class ChangePasswordValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(8);

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty()
            .EqualToProperty(x => x.NewPassword)
                .WithMessage("Las contraseñas no coinciden.");

        // La nueva contraseña no debe ser igual a la actual
        RuleFor(x => x.NewPassword)
            .NotEqual("") // Ya cubierto por NotEmpty, pero ilustra el uso
            .Must((request, newPwd) => newPwd != request.CurrentPassword)
                .WithMessage("La nueva contraseña debe ser distinta a la actual.");
    }
}
```

---

## Longitud de strings

### `MinimumLength(int n)`

Verifica que la longitud del string sea al menos `n` caracteres.

```csharp
RuleFor(x => x.Username).MinimumLength(3);
RuleFor(x => x.Password).MinimumLength(8);
```

### `MaximumLength(int n)`

Verifica que la longitud del string no supere `n` caracteres.

```csharp
RuleFor(x => x.Name).MaximumLength(200);
RuleFor(x => x.Bio).MaximumLength(500);
RuleFor(x => x.Email).MaximumLength(320); // Límite RFC 5321
```

### `LengthBetween(int min, int max)`

Verifica que la longitud esté entre `min` y `max` (ambos inclusive).

```csharp
RuleFor(x => x.PhoneNumber)
    .LengthBetween(7, 15)
        .WithMessage("El teléfono debe tener entre 7 y 15 dígitos.");

RuleFor(x => x.PostalCode)
    .LengthBetween(5, 10)
        .WithMessage("El código postal debe tener entre 5 y 10 caracteres.");
```

---

## Rango numérico

Estas reglas trabajan con `IComparable`. Funcionan con `int`, `decimal`, `double`, `DateTime`, `long`, etc.

### `GreaterThan(IComparable threshold)`

```csharp
RuleFor(x => x.Age).GreaterThan(0);
RuleFor(x => x.Price).GreaterThan(0m);
RuleFor(x => x.EventDate).GreaterThan(DateTime.Today);
```

### `GreaterThanOrEqualTo(IComparable threshold)`

```csharp
RuleFor(x => x.Stock).GreaterThanOrEqualTo(0);
RuleFor(x => x.Quantity).GreaterThanOrEqualTo(1);
```

### `LessThan(IComparable threshold)`

```csharp
RuleFor(x => x.Age).LessThan(150);
RuleFor(x => x.Discount).LessThan(100m);
```

### `LessThanOrEqualTo(IComparable threshold)`

```csharp
RuleFor(x => x.MaxRetries).LessThanOrEqualTo(10);
RuleFor(x => x.Percentage).LessThanOrEqualTo(100m);
```

### `Between<TComparable>(TComparable min, TComparable max)`

Incluye los extremos (min y max son valores válidos).

```csharp
RuleFor(x => x.Month).Between(1, 12)
    .WithMessage("El mes debe estar entre 1 y 12.");

RuleFor(x => x.Rating).Between(1, 5)
    .WithMessage("La calificación debe estar entre 1 y 5.");
```

### `ExclusiveBetween<TComparable>(TComparable min, TComparable max)`

Excluye los extremos (min y max no son válidos).

```csharp
RuleFor(x => x.Probability)
    .ExclusiveBetween(0m, 1m)
        .WithMessage("La probabilidad debe ser un valor entre 0 y 1 (sin incluir extremos).");
```

### `Positive()`

Verifica que el número sea estrictamente mayor que 0. El cero no es positivo.

```csharp
RuleFor(x => x.Price).Positive();
RuleFor(x => x.Quantity).Positive();
```

### `NonNegative()`

Verifica que el número sea mayor o igual a 0. A diferencia de `Positive()`, acepta el cero. Útil para campos como stock, edad o puntajes donde cero es un valor válido.

```csharp
RuleFor(x => x.Stock).NonNegative()
    .WithMessage("El stock no puede ser negativo.");

RuleFor(x => x.PenaltyPoints).NonNegative()
    .WithMessage("Los puntos de penalización no pueden ser negativos.");
```

### `Percentage()`

Verifica que el número esté entre 0 y 100 (ambos inclusive). Semánticamente más claro que `.Between(0, 100)` cuando el campo representa un porcentaje.

```csharp
RuleFor(x => x.TaxRate).Percentage()
    .WithMessage("La tasa impositiva debe ser un porcentaje entre 0 y 100.");

RuleFor(x => x.DiscountPercent).Percentage()
    .WithMessage("El descuento debe ser un porcentaje entre 0 y 100.");
```

### `Precision(int totalDigits, int decimalPlaces)`

Verifica que el número decimal respete un máximo de `totalDigits` dígitos en total y `decimalPlaces` decimales. Equivalente a la restricción `DECIMAL(total, decimals)` de SQL. Útil para campos financieros que se almacenarán en base de datos.

```csharp
RuleFor(x => x.Price)
    .Precision(10, 2)
        .WithMessage("El precio admite máximo 10 dígitos en total y 2 decimales (ej: 99999999.99).");

RuleFor(x => x.ExchangeRate)
    .Precision(12, 6)
        .WithMessage("El tipo de cambio admite máximo 12 dígitos con 6 decimales.");
```

### `MultipleOfProperty(Expression<Func<T, TProperty>> otherExpression)`

Verifica que el valor sea múltiplo del valor de otra propiedad del mismo objeto. Complementa a `MultipleOf(decimal factor)` cuando el factor proviene del mismo objeto.

```csharp
// El importe total del lote debe ser múltiplo del precio unitario
RuleFor(x => x.BatchAmount)
    .MultipleOfProperty(x => x.UnitPrice)
        .WithMessage("El importe del lote debe ser múltiplo del precio unitario.");
```

### `Negative()`

Verifica que el número sea estrictamente menor que 0.

```csharp
RuleFor(x => x.TemperatureOffset).Negative()
    .WithMessage("El offset de temperatura debe ser negativo para este modo.");
```

### `NotZero()`

Verifica que el número no sea 0. Útil cuando tanto positivos como negativos son válidos, pero el cero no tiene sentido.

```csharp
RuleFor(x => x.ScaleFactor).NotZero()
    .WithMessage("El factor de escala no puede ser 0.");
```

### `Odd()`

Verifica que el número sea impar.

```csharp
RuleFor(x => x.ThreadCount).Odd()
    .WithMessage("El número de hilos debe ser impar para este algoritmo.");
```

### `Even()`

Verifica que el número sea par.

```csharp
RuleFor(x => x.BatchSize)
    .Even()
        .WithMessage("El tamaño del lote debe ser par.")
    .GreaterThan(0);
```

### `MultipleOf(decimal factor)`

Verifica que el número sea múltiplo del factor dado.

```csharp
RuleFor(x => x.Amount)
    .MultipleOf(0.01m)
        .WithMessage("El importe debe tener máximo 2 decimales.");

RuleFor(x => x.PageSize)
    .MultipleOf(10)
        .WithMessage("El tamaño de página debe ser múltiplo de 10 (10, 20, 30...).");
```

### `MaxDecimalPlaces(int places)`

Verifica que el número decimal no tenga más de `places` decimales.

```csharp
RuleFor(x => x.Price)
    .MaxDecimalPlaces(2)
        .WithMessage("El precio no puede tener más de 2 decimales.");

RuleFor(x => x.ExchangeRate)
    .MaxDecimalPlaces(6)
        .WithMessage("El tipo de cambio no puede tener más de 6 decimales.");
```

---

## Strings

### `Matches(string pattern)`

Verifica que el string coincida con la expresión regular dada.

```csharp
RuleFor(x => x.PostalCode)
    .Matches(@"^\d{5}(-\d{4})?$")
        .WithMessage("El código postal debe ser ZIP (XXXXX o XXXXX-XXXX).");

RuleFor(x => x.Slug)
    .Matches(@"^[a-z0-9-]+$")
        .WithMessage("El slug solo puede contener letras minúsculas, números y guiones.");

RuleFor(x => x.InvoiceNumber)
    .Matches(@"^INV-\d{4}-\d{6}$")
        .WithMessage("El número de factura debe tener el formato INV-YYYY-XXXXXX.");
```

### `MustContain(string substring, StringComparison comparison)`

Verifica que el string contenga el substring dado. La comparación es `OrdinalIgnoreCase` por defecto.

```csharp
RuleFor(x => x.Password)
    .MustContain("@")
        .WithMessage("La contraseña debe contener el símbolo @.");

// Con comparación sensible a mayúsculas
RuleFor(x => x.ApiKey)
    .MustContain("sk-", StringComparison.Ordinal);
```

### `NotContains(string substring, StringComparison comparison)`

Verifica que el string NO contenga el substring dado.

```csharp
RuleFor(x => x.Username)
    .NotContains("admin", StringComparison.OrdinalIgnoreCase)
        .WithMessage("El nombre de usuario no puede contener 'admin'.")
    .NotContains("root")
        .WithMessage("El nombre de usuario no puede contener 'root'.");
```

### `StartsWith(string prefix)`

```csharp
RuleFor(x => x.Iban)
    .StartsWith("ES")
        .WithMessage("El IBAN debe comenzar con 'ES' para cuentas españolas.");

RuleFor(x => x.OrderId)
    .StartsWith("ORD-")
        .WithMessage("El ID de pedido debe comenzar con 'ORD-'.");
```

### `EndsWith(string suffix)`

```csharp
RuleFor(x => x.Email)
    .EndsWith("@empresa.com", StringComparison.OrdinalIgnoreCase)
        .WithMessage("Solo se aceptan emails corporativos (@empresa.com).");
```

### `IsAlpha()`

Verifica que el string contenga solo letras. Incluye caracteres acentuados (á, é, ñ, ü, etc.).

```csharp
RuleFor(x => x.FirstName)
    .IsAlpha()
        .WithMessage("El nombre solo puede contener letras.");

RuleFor(x => x.LastName)
    .IsAlpha()
        .WithMessage("El apellido solo puede contener letras.");
```

### `IsAlphanumeric()`

Verifica que el string contenga solo letras, números y guión bajo (`_`).

```csharp
RuleFor(x => x.Username)
    .IsAlphanumeric()
        .WithMessage("El usuario solo puede contener letras, números y guión bajo.");

RuleFor(x => x.Identifier)
    .IsAlphanumeric();
```

### `IsNumeric()`

Verifica que el string contenga solo dígitos (0-9). No acepta signos ni decimales.

```csharp
RuleFor(x => x.PostalCode)
    .IsNumeric()
        .WithMessage("El código postal solo puede contener dígitos.");

RuleFor(x => x.PinCode)
    .IsNumeric()
    .LengthBetween(4, 6);
```

### `Lowercase()`

Verifica que todo el string esté en minúsculas.

```csharp
RuleFor(x => x.Slug)
    .Lowercase()
        .WithMessage("El slug debe estar en minúsculas.");
```

### `Uppercase()`

Verifica que todo el string esté en mayúsculas.

```csharp
RuleFor(x => x.CountryCode)
    .Uppercase()
    .LengthBetween(2, 3)
        .WithMessage("El código de país debe estar en mayúsculas (ej: ES, USA).");
```

### `NoWhitespace()`

Verifica que el string no contenga espacios en blanco (ni espacios, ni tabs, ni saltos de línea).

```csharp
RuleFor(x => x.ApiKey)
    .NoWhitespace()
        .WithMessage("La API key no puede contener espacios.");

RuleFor(x => x.Username)
    .NoWhitespace()
        .WithMessage("El nombre de usuario no puede contener espacios.");
```

### `MinWords(int n)`

Verifica que el string tenga al menos `n` palabras (separadas por espacios).

```csharp
RuleFor(x => x.FullName)
    .MinWords(2)
        .WithMessage("Por favor introduce tu nombre completo (nombre y apellido).");

RuleFor(x => x.ProductDescription)
    .MinWords(10)
        .WithMessage("La descripción debe tener al menos 10 palabras.");
```

### `MaxWords(int n)`

Verifica que el string no tenga más de `n` palabras.

```csharp
RuleFor(x => x.TagLine)
    .MaxWords(10)
        .WithMessage("El slogan no puede superar las 10 palabras.");
```

---

## Formato y validación de datos

### `Email()`

Verifica que el string tenga un formato de email válido.

```csharp
RuleFor(x => x.Email)
    .NotEmpty()
    .Email()
        .WithMessage("El email no tiene un formato válido.");

// Múltiples destinatarios no son válidos con esta regla:
// "user@example.com, other@example.com" FALLA
```

### `Url()`

Verifica que el string sea una URL HTTP o HTTPS válida.

```csharp
RuleFor(x => x.Website)
    .Url()
        .WithMessage("La URL debe comenzar con http:// o https://.");

RuleFor(x => x.CallbackUrl)
    .NotEmpty()
    .Url()
    .StartsWith("https://")
        .WithMessage("El callback URL debe usar HTTPS.");
```

### `PhoneNumber()`

Verifica que el string sea un número de teléfono en formato E.164 (ej: `+12025551234`). Siempre debe comenzar con `+` seguido del código de país.

```csharp
RuleFor(x => x.PhoneNumber)
    .PhoneNumber()
        .WithMessage("El teléfono debe estar en formato E.164 (ej: +34612345678).");
```

### `IPv4()`

Verifica que el string sea una dirección IPv4 válida.

```csharp
RuleFor(x => x.ServerIp)
    .IPv4()
        .WithMessage("La IP del servidor no es una dirección IPv4 válida.");

RuleFor(x => x.AllowedIp)
    .IPv4()
    .When(x => x.AllowedIp != null);
```

### `IPv6()`

Verifica que el string sea una dirección IPv6 válida. Complementa a `IPv4()` para entornos de infraestructura modernos.

```csharp
RuleFor(x => x.ServerAddress)
    .IPv6()
        .WithMessage("La dirección del servidor no es una IPv6 válida.");

// Acepta automáticamente según el tipo de IP
RuleFor(x => x.IpAddress)
    .IPv4()
    .When(x => !x.IpAddress.Contains(':'))
    .IPv6()
    .When(x => x.IpAddress.Contains(':'));
```

### `MacAddress()`

Verifica que el string sea una dirección MAC válida, aceptando tanto el formato con dos puntos (`AA:BB:CC:DD:EE:FF`) como con guiones (`AA-BB-CC-DD-EE-FF`). Útil en sistemas de control de acceso de red o configuraciones de dispositivos IoT.

```csharp
RuleFor(x => x.DeviceMac)
    .NotEmpty()
    .MacAddress()
        .WithMessage("La dirección MAC no tiene un formato válido (ej: AA:BB:CC:DD:EE:FF).");
```

### `Slug()`

Verifica que el string sea un slug de URL válido: solo letras minúsculas, números y guiones, sin espacios ni caracteres especiales. Imprescindible para URLs amigables de artículos, productos o páginas.

```csharp
RuleFor(x => x.UrlSlug)
    .NotEmpty()
    .Slug()
        .WithMessage("El slug solo puede contener letras minúsculas, números y guiones (ej: mi-articulo-2024).");

// Ejemplo válido: "introduccion-a-dotnet-9"
// Ejemplo inválido: "Introducción a .NET 9", "mi articulo", "mi_articulo"
```

### `Latitude()`

Verifica que el número esté en el rango de latitud válida (-90 a 90). Útil para coordenadas geográficas en sistemas de mapas o geolocalización.

```csharp
RuleFor(x => x.Latitude)
    .Latitude()
        .WithMessage("La latitud debe estar entre -90 y 90 grados.");
```

### `Longitude()`

Verifica que el número esté en el rango de longitud válida (-180 a 180).

```csharp
RuleFor(x => x.Longitude)
    .Longitude()
        .WithMessage("La longitud debe estar entre -180 y 180 grados.");

// Validar coordenadas completas
public class LocationValidator : AbstractValidator<Location>
{
    public LocationValidator()
    {
        RuleFor(x => x.Latitude).Latitude();
        RuleFor(x => x.Longitude).Longitude();
    }
}
```

### `CountryCode()`

Verifica que el string sea un código de país ISO 3166-1 alpha-2 válido: exactamente 2 letras mayúsculas (ej: `PE`, `US`, `ES`, `MX`). Más estricto que `Uppercase().LengthBetween(2,2)` porque solo acepta códigos reconocidos por el estándar ISO.

```csharp
RuleFor(x => x.CountryCode)
    .CountryCode()
        .WithMessage("El código de país debe ser un código ISO 3166-1 alpha-2 válido (ej: PE, US, ES).");

RuleFor(x => x.ShippingCountry)
    .NotEmpty()
    .CountryCode();
```

### `CurrencyCode()`

Verifica que el string sea un código de moneda ISO 4217 válido: exactamente 3 letras mayúsculas (ej: `USD`, `EUR`, `PEN`, `GBP`). Recomendado para campos de moneda en transacciones financieras.

```csharp
RuleFor(x => x.Currency)
    .CurrencyCode()
        .WithMessage("El código de moneda debe ser un código ISO 4217 válido (ej: USD, EUR, PEN).");

public class PaymentValidator : AbstractValidator<PaymentRequest>
{
    public PaymentValidator()
    {
        RuleFor(x => x.Amount).Positive().Precision(12, 2);
        RuleFor(x => x.Currency).CurrencyCode();
        RuleFor(x => x.CountryCode).CountryCode();
    }
}
```

### `IsValidJson()`

Verifica que el string sea un JSON válido (puede ser objeto, array, string, número o booleano JSON). Útil para campos de metadatos, configuraciones dinámicas o payloads de webhook.

```csharp
RuleFor(x => x.Metadata)
    .IsValidJson()
        .WithMessage("El campo Metadata debe ser un JSON válido.");

RuleFor(x => x.WebhookPayload)
    .NotEmpty()
    .IsValidJson()
        .WithMessage("El payload del webhook debe ser un JSON válido.");
```

### `IsValidBase64()`

Verifica que el string sea una cadena Base64 válida. Útil para imágenes, archivos o datos binarios codificados en Base64.

```csharp
RuleFor(x => x.ImageData)
    .IsValidBase64()
        .WithMessage("El campo ImageData debe ser una cadena Base64 válida.");

RuleFor(x => x.SignatureBytes)
    .NotEmpty()
    .IsValidBase64()
        .WithMessage("La firma debe estar codificada en Base64.");
```

### `NoHtmlTags()`

Verifica que el string no contenga etiquetas HTML. Protege contra inyección de HTML en campos de texto libre que se muestran en interfaces web.

```csharp
RuleFor(x => x.Comment)
    .NoHtmlTags()
        .WithMessage("El comentario no puede contener etiquetas HTML.");

RuleFor(x => x.Bio)
    .MaximumLength(500)
    .NoHtmlTags()
        .WithMessage("La biografía no puede contener HTML.");
```

### `NoSqlInjectionPatterns()`

Verifica que el string no contenga patrones comunes de SQL injection (como `'; DROP`, `OR 1=1`, `UNION SELECT`, etc.). Es una capa de defensa en profundidad adicional a las consultas parametrizadas.

```csharp
RuleFor(x => x.SearchTerm)
    .NoSqlInjectionPatterns()
        .WithMessage("El término de búsqueda contiene caracteres no permitidos.");

RuleFor(x => x.Username)
    .NoSqlInjectionPatterns()
    .NoHtmlTags();
```

> **Nota de seguridad:** Esta regla complementa (no reemplaza) el uso de consultas parametrizadas y ORMs. Nunca confíes únicamente en validación de entrada para prevenir inyección SQL.

### `Iban()`

Verifica que el string sea un número IBAN válido según el algoritmo mod-97 (ISO 13616). Acepta IBANs de cualquier país, no solo de España. Más robusto que validar solo con regex.

```csharp
RuleFor(x => x.BankAccount)
    .NotEmpty()
    .Iban()
        .WithMessage("El número de cuenta IBAN no es válido.");

public class TransferValidator : AbstractValidator<TransferRequest>
{
    public TransferValidator()
    {
        RuleFor(x => x.OriginIban).Iban();
        RuleFor(x => x.DestinationIban)
            .Iban()
            .When(x => x.DeliveryMethod == "transfer");
    }
}
```

### `CreditCard()`

Verifica que el string sea un número de tarjeta de crédito válido según el algoritmo de Luhn. Acepta strings con o sin espacios/guiones.

```csharp
RuleFor(x => x.CardNumber)
    .NotEmpty()
    .CreditCard()
        .WithMessage("El número de tarjeta no es válido.");
```

> **Nota de seguridad:** Esta regla solo valida el formato matemático (Luhn), no verifica que la tarjeta exista o tenga fondos. Los números de tarjeta nunca deben almacenarse sin tokenización.

### `Guid()`

Verifica que el string sea un GUID válido en cualquier formato estándar.

```csharp
RuleFor(x => x.ExternalId)
    .Guid()
        .WithMessage("El ID externo debe ser un GUID válido.");

// Acepta: "6ba7b810-9dad-11d1-80b4-00c04fd430c8"
// Acepta: "{6ba7b810-9dad-11d1-80b4-00c04fd430c8}"
// Acepta: "6ba7b8109dad11d180b400c04fd430c8"
```

### `NotEmptyGuid()`

Verifica que el string sea un GUID válido y distinto de `Guid.Empty` (`00000000-0000-0000-0000-000000000000`).

```csharp
RuleFor(x => x.UserId)
    .NotEmptyGuid()
        .WithMessage("El ID de usuario no puede ser vacío.");
```

### `IsEnum<TEnum>()`

Verifica que el string sea un nombre de miembro válido del enum `TEnum`.

```csharp
public enum OrderStatus { Pending, Processing, Shipped, Delivered, Cancelled }

public class UpdateOrderStatusValidator : AbstractValidator<UpdateOrderStatusRequest>
{
    public UpdateOrderStatusValidator()
    {
        RuleFor(x => x.Status)
            .IsEnum<OrderStatus>()
                .WithMessage("El estado debe ser uno de: Pending, Processing, Shipped, Delivered, Cancelled.");
    }
}
```

---

## Password y seguridad

Estas reglas se aplican a strings y verifican la presencia de ciertos tipos de caracteres.

### `HasUppercase()`

```csharp
RuleFor(x => x.Password).HasUppercase()
    .WithMessage("La contraseña debe contener al menos una letra mayúscula.");
```

### `HasLowercase()`

```csharp
RuleFor(x => x.Password).HasLowercase()
    .WithMessage("La contraseña debe contener al menos una letra minúscula.");
```

### `HasDigit()`

```csharp
RuleFor(x => x.Password).HasDigit()
    .WithMessage("La contraseña debe contener al menos un número.");
```

### `HasSpecialChar()`

Verifica que contenga al menos un carácter no alfanumérico (ej: `!@#$%^&*`).

```csharp
RuleFor(x => x.Password).HasSpecialChar()
    .WithMessage("La contraseña debe contener al menos un carácter especial (!@#$%...).");
```

### `PasswordPolicy(int minLength, bool requireUppercase, bool requireLowercase, bool requireDigit, bool requireSpecialChar)`

Aplica una política de contraseña completa en una sola regla. Equivale a encadenar `MinimumLength`, `HasUppercase`, `HasLowercase`, `HasDigit` y `HasSpecialChar`, pero con un único mensaje de error que describe todos los requisitos incumplidos. Los parámetros tienen valores por defecto: `minLength=8`, `requireUppercase=true`, `requireLowercase=true`, `requireDigit=true`, `requireSpecialChar=true`.

```csharp
// Política estricta para usuarios administradores
RuleFor(x => x.Password)
    .PasswordPolicy(minLength: 12, requireSpecialChar: true);

// Política básica para aplicaciones internas
RuleFor(x => x.Password)
    .PasswordPolicy(minLength: 8, requireSpecialChar: false);

// Política solo de longitud y dígito
RuleFor(x => x.Pin)
    .PasswordPolicy(minLength: 6, requireUppercase: false, requireLowercase: false,
        requireDigit: true, requireSpecialChar: false);
```

> **Cuándo usar `PasswordPolicy` vs reglas individuales:** Usa `PasswordPolicy` cuando quieras un mensaje de error único y conciso. Usa las reglas individuales (`HasUppercase`, etc.) cuando necesites mensajes de error separados por criterio para una UX más detallada.

### Validador de contraseña completo

```csharp
public class PasswordValidator : AbstractValidator<SetPasswordRequest>
{
    public PasswordValidator()
    {
        RuleFor(x => x.Password)
            .NotEmpty()
                .WithMessage("La contraseña es obligatoria.")
            .MinimumLength(12)
                .WithMessage("La contraseña debe tener al menos 12 caracteres.")
            .MaximumLength(128)
                .WithMessage("La contraseña no puede superar los 128 caracteres.")
            .HasUppercase()
                .WithMessage("Debe contener al menos una mayúscula.")
            .HasLowercase()
                .WithMessage("Debe contener al menos una minúscula.")
            .HasDigit()
                .WithMessage("Debe contener al menos un número.")
            .HasSpecialChar()
                .WithMessage("Debe contener al menos un carácter especial.")
            .NotContains("password", StringComparison.OrdinalIgnoreCase)
                .WithMessage("La contraseña no puede contener la palabra 'password'.")
            .NotContains("123456")
                .WithMessage("La contraseña no puede contener la secuencia '123456'.");

        RuleFor(x => x.ConfirmPassword)
            .EqualToProperty(x => x.Password)
                .WithMessage("Las contraseñas no coinciden.");
    }
}
```

---

## Fechas

### `FutureDate()`

Verifica que la fecha sea posterior a `DateTime.Now` (o `DateTime.UtcNow`).

```csharp
public class CreateEventValidator : AbstractValidator<CreateEventRequest>
{
    public CreateEventValidator()
    {
        RuleFor(x => x.EventDate)
            .FutureDate()
                .WithMessage("La fecha del evento debe ser en el futuro.");

        RuleFor(x => x.RegistrationDeadline)
            .FutureDate()
                .WithMessage("La fecha límite de registro debe ser en el futuro.")
            .LessThan(x => x.EventDate)
                .WithMessage("La fecha límite de registro debe ser antes del evento.");
    }
}
```

### `PastDate()`

Verifica que la fecha sea anterior a `DateTime.Now`.

```csharp
RuleFor(x => x.BirthDate)
    .PastDate()
        .WithMessage("La fecha de nacimiento debe estar en el pasado.");

RuleFor(x => x.DocumentIssueDate)
    .PastDate()
        .WithMessage("La fecha de emisión del documento debe ser en el pasado.");
```

### `Today()`

Verifica que la fecha sea la fecha de hoy (sin hora).

```csharp
RuleFor(x => x.ReportDate)
    .Today()
        .WithMessage("La fecha del informe debe ser hoy.");
```

### `MinAge(int years)`

Verifica que una fecha de nacimiento implique una edad mínima en años completos a partir de hoy. Imprescindible para formularios con restricción de mayoría de edad.

```csharp
RuleFor(x => x.BirthDate)
    .PastDate()
    .MinAge(18)
        .WithMessage("Debes tener al menos 18 años para registrarte.");

// Restricción de edad para contenido adulto
RuleFor(x => x.BirthDate)
    .MinAge(21)
        .WithMessage("Debes tener al menos 21 años para acceder a este contenido.");
```

### `MaxAge(int years)`

Verifica que una fecha de nacimiento implique una edad máxima en años. Útil para productos o servicios dirigidos a menores o para validar edades de beneficiarios.

```csharp
// Seguro infantil: solo para menores de 18 años
RuleFor(x => x.BeneficiaryBirthDate)
    .MaxAge(17)
        .WithMessage("Este seguro es exclusivo para menores de 18 años.");
```

### `DateBetween(DateTime from, DateTime to)`

Verifica que la fecha esté dentro de un rango (ambos extremos inclusive). Ideal para periodos de inscripción, reservas o búsquedas acotadas.

```csharp
RuleFor(x => x.AppointmentDate)
    .DateBetween(DateTime.Today, DateTime.Today.AddMonths(3))
        .WithMessage("Solo se pueden agendar citas dentro de los próximos 3 meses.");

RuleFor(x => x.ReportDate)
    .DateBetween(new DateTime(2020, 1, 1), DateTime.Today)
        .WithMessage("La fecha del informe debe estar entre el 01/01/2020 y hoy.");
```

### `NotExpired()`

Verifica que la fecha no haya expirado, es decir, que sea mayor o igual a `DateTime.Now`. Útil para tokens, códigos de verificación o membresías.

```csharp
RuleFor(x => x.TokenExpiresAt)
    .NotExpired()
        .WithMessage("El token ha expirado. Solicita uno nuevo.");

RuleFor(x => x.MembershipExpiry)
    .NotExpired()
        .WithMessage("Tu membresía ha vencido.");
```

### `WithinNext(TimeSpan span)`

Verifica que la fecha esté dentro del próximo periodo a partir de ahora. Útil para validar recordatorios, agendas o eventos próximos.

```csharp
// La cita debe estar dentro de las próximas 24 horas
RuleFor(x => x.ReminderDate)
    .WithinNext(TimeSpan.FromHours(24))
        .WithMessage("El recordatorio debe programarse para las próximas 24 horas.");

// El evento debe ocurrir dentro de los próximos 6 meses
RuleFor(x => x.EventDate)
    .WithinNext(TimeSpan.FromDays(180))
        .WithMessage("El evento debe realizarse dentro de los próximos 6 meses.");
```

### `WithinLast(TimeSpan span)`

Verifica que la fecha esté dentro del periodo pasado a partir de ahora. Útil para validar fechas recientes de documentos, transacciones o registros de actividad.

```csharp
// El comprobante debe ser de los últimos 30 días
RuleFor(x => x.ReceiptDate)
    .WithinLast(TimeSpan.FromDays(30))
        .WithMessage("El comprobante debe tener como máximo 30 días de antigüedad.");

// La hora de registro de actividad no puede ser de hace más de 1 hora
RuleFor(x => x.ActivityTimestamp)
    .WithinLast(TimeSpan.FromHours(1))
        .WithMessage("El registro de actividad no puede tener más de 1 hora de antigüedad.");
```

### `IsWeekday()`

Verifica que la fecha caiga en un día laborable (lunes a viernes). Útil para programar citas, entregas o tareas que no se realizan en fin de semana.

```csharp
RuleFor(x => x.AppointmentDate)
    .IsWeekday()
        .WithMessage("Las citas solo pueden agendarse de lunes a viernes.");

RuleFor(x => x.DeliveryDate)
    .IsWeekday()
        .WithMessage("Las entregas solo se realizan en días laborables.");
```

### `IsWeekend()`

Verifica que la fecha caiga en sábado o domingo. Útil para eventos o servicios exclusivos de fin de semana.

```csharp
RuleFor(x => x.EventDate)
    .IsWeekend()
        .WithMessage("Las actividades de ocio solo se programan en fin de semana.");
```

---

## Colecciones

### `NotEmptyCollection()`

Verifica que la colección no sea `null` ni esté vacía.

```csharp
RuleFor(x => x.Tags)
    .NotEmptyCollection()
        .WithMessage("El artículo debe tener al menos una etiqueta.");

RuleFor(x => x.OrderLines)
    .NotEmptyCollection()
        .WithMessage("El pedido debe tener al menos un producto.");
```

### `HasCount(int n)`

Verifica que la colección tenga exactamente `n` elementos.

```csharp
RuleFor(x => x.SecurityQuestions)
    .HasCount(3)
        .WithMessage("Debes proporcionar exactamente 3 preguntas de seguridad.");
```

### `MinCount(int n)`

Verifica que la colección tenga al menos `n` elementos.

```csharp
RuleFor(x => x.Photos)
    .MinCount(1)
        .WithMessage("Debes subir al menos una foto.")
    .MaxCount(10)
        .WithMessage("No puedes subir más de 10 fotos.");
```

### `MaxCount(int n)`

Verifica que la colección no tenga más de `n` elementos.

```csharp
RuleFor(x => x.Tags)
    .MaxCount(5)
        .WithMessage("No puedes añadir más de 5 etiquetas.");
```

### `Unique()`

Verifica que todos los elementos de la colección sean distintos (sin duplicados).

```csharp
RuleFor(x => x.SelectedRoleIds)
    .Unique()
        .WithMessage("Los roles seleccionados no pueden repetirse.");

RuleFor(x => x.EmailAddresses)
    .Unique()
        .WithMessage("La lista de emails no puede tener duplicados.");
```

### `AllSatisfy(Func<object, bool> predicate)`

Verifica que todos los elementos de la colección cumplan el predicado.

```csharp
RuleFor(x => x.FileNames)
    .AllSatisfy(name => ((string)name).EndsWith(".pdf"))
        .WithMessage("Todos los archivos deben ser PDF.");

RuleFor(x => x.Prices)
    .AllSatisfy(p => (decimal)p > 0)
        .WithMessage("Todos los precios deben ser positivos.");
```

### `AnySatisfy(Func<object, bool> predicate)`

Verifica que al menos un elemento cumpla el predicado.

```csharp
RuleFor(x => x.ContactMethods)
    .AnySatisfy(method => (string)method == "email")
        .WithMessage("Debe existir al menos un método de contacto por email.");
```

### `In(IEnumerable<TProperty> values)`

Verifica que el valor esté dentro de la lista de valores permitidos.

```csharp
private static readonly string[] AllowedCurrencies = new[] { "EUR", "USD", "GBP", "JPY" };

RuleFor(x => x.Currency)
    .In(AllowedCurrencies)
        .WithMessage("La moneda debe ser EUR, USD, GBP o JPY.");

RuleFor(x => x.Priority)
    .In(new[] { 1, 2, 3, 4, 5 })
        .WithMessage("La prioridad debe ser un valor del 1 al 5.");
```

### `NotIn(IEnumerable<TProperty> values)`

Verifica que el valor NO esté en la lista de valores prohibidos.

```csharp
private static readonly string[] ReservedUsernames = new[] { "admin", "root", "system", "api" };

RuleFor(x => x.Username)
    .NotIn(ReservedUsernames)
        .WithMessage("Ese nombre de usuario está reservado.");
```

---

## Resumen de reglas disponibles

| Categoría | Reglas |
|---|---|
| Nulidad/vacío | `NotNull`, `Null`, `NotEmpty`, `Empty` |
| Igualdad | `EqualTo`, `NotEqual`, `EqualToProperty` |
| Longitud | `MinimumLength`, `MaximumLength`, `LengthBetween` |
| Rango numérico | `GreaterThan`, `GreaterThanOrEqualTo`, `LessThan`, `LessThanOrEqualTo`, `Between`, `ExclusiveBetween`, `Positive`, `NonNegative`, `Negative`, `NotZero`, `Odd`, `Even`, `MultipleOf`, `MultipleOfProperty`, `MaxDecimalPlaces`, `Percentage`, `Precision` |
| String | `Matches`, `MustContain`, `NotContains`, `StartsWith`, `EndsWith`, `IsAlpha`, `IsAlphanumeric`, `IsNumeric`, `Lowercase`, `Uppercase`, `NoWhitespace`, `MinWords`, `MaxWords`, `Slug`, `NoHtmlTags`, `NoSqlInjectionPatterns` |
| Formato | `Email`, `Url`, `PhoneNumber`, `IPv4`, `IPv6`, `MacAddress`, `CreditCard`, `Guid`, `NotEmptyGuid`, `IsEnum<T>`, `Iban`, `CountryCode`, `CurrencyCode`, `IsValidJson`, `IsValidBase64`, `Latitude`, `Longitude` |
| Password | `HasUppercase`, `HasLowercase`, `HasDigit`, `HasSpecialChar`, `PasswordPolicy` |
| Fechas | `FutureDate`, `PastDate`, `Today`, `MinAge`, `MaxAge`, `DateBetween`, `NotExpired`, `WithinNext`, `WithinLast`, `IsWeekday`, `IsWeekend` |
| Colecciones | `NotEmptyCollection`, `HasCount`, `MinCount`, `MaxCount`, `Unique`, `AllSatisfy`, `AnySatisfy`, `In`, `NotIn` |

## Siguientes pasos

- **[Reglas avanzadas](06-reglas-avanzadas.md)** — Must, MustAsync, Custom, Transform, SetValidator
- **[Modificadores](07-modificadores.md)** — WithMessage, WithErrorCode, When/Unless y más
