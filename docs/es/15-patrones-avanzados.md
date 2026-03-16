# Patrones avanzados

Este documento cubre casos de uso complejos: composición de validadores, herencia, validación condicional avanzada, passwords, y colecciones anidadas.

---

## Validadores anidados con SetValidator

### Modelo con múltiples niveles de anidamiento

```csharp
public class CreateInvoiceRequest
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public CustomerInfo Customer { get; set; } = new();
    public Address BillingAddress { get; set; } = new();
    public Address ShippingAddress { get; set; } = new();
    public List<InvoiceLine> Lines { get; set; } = new();
    public PaymentInfo Payment { get; set; } = new();
}

public class CustomerInfo
{
    public string Name { get; set; } = string.Empty;
    public string TaxId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class Address
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
}

public class InvoiceLine
{
    public string ProductCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal? DiscountPercent { get; set; }
}

public class PaymentInfo
{
    public string Method { get; set; } = string.Empty;
    public string? Iban { get; set; }
    public string? CardLastFour { get; set; }
}
```

### Validadores por sección

```csharp
public class CustomerInfoValidator : AbstractValidator<CustomerInfo>
{
    public CustomerInfoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.TaxId)
            .NotEmpty()
            .Matches(@"^[A-Z0-9]{9}$")
                .WithMessage("El NIF/CIF debe tener 9 caracteres alfanuméricos en mayúsculas.");

        RuleFor(x => x.Email)
            .NotEmpty()
            .Email();
    }
}

public class AddressValidator : AbstractValidator<Address>
{
    public AddressValidator()
    {
        RuleFor(x => x.Street)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.City)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.PostalCode)
            .NotEmpty()
            .Matches(@"^\d{5}$")
                .WithMessage("El código postal debe tener 5 dígitos.");

        RuleFor(x => x.CountryCode)
            .NotEmpty()
            .LengthBetween(2, 3)
            .Uppercase()
                .WithMessage("El código de país debe estar en mayúsculas (ej: ES, FR, DE).");
    }
}

public class InvoiceLineValidator : AbstractValidator<InvoiceLine>
{
    public InvoiceLineValidator()
    {
        RuleFor(x => x.ProductCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.UnitPrice).GreaterThan(0m).MaxDecimalPlaces(2);

        RuleFor(x => x.DiscountPercent)
            .ExclusiveBetween(0m, 100m)
                .WithMessage("El descuento debe ser un porcentaje entre 0 y 100 (exclusivo).")
            .When(x => x.DiscountPercent.HasValue);
    }
}

public class PaymentInfoValidator : AbstractValidator<PaymentInfo>
{
    private static readonly string[] ValidMethods = new[] { "card", "transfer", "cash" };

    public PaymentInfoValidator()
    {
        RuleFor(x => x.Method)
            .NotEmpty()
            .In(ValidMethods)
                .WithMessage("El método de pago debe ser: card, transfer o cash.");

        RuleFor(x => x.Iban)
            .NotEmpty()
                .WithMessage("El IBAN es obligatorio para transferencias.")
            .Matches(@"^[A-Z]{2}\d{2}[A-Z0-9]{4}\d{14}$")
                .WithMessage("El IBAN no tiene formato válido.")
            .When(x => x.Method == "transfer");

        RuleFor(x => x.CardLastFour)
            .NotEmpty()
                .WithMessage("Los últimos 4 dígitos son obligatorios para pagos con tarjeta.")
            .Matches(@"^\d{4}$")
                .WithMessage("Deben ser exactamente 4 dígitos.")
            .When(x => x.Method == "card");
    }
}
```

### Validador raíz que compone todo

```csharp
public class CreateInvoiceValidator : AbstractValidator<CreateInvoiceRequest>
{
    public CreateInvoiceValidator()
    {
        RuleFor(x => x.InvoiceNumber)
            .NotEmpty()
            .Matches(@"^INV-\d{4}-\d{6}$")
                .WithMessage("El número de factura debe tener el formato INV-YYYY-XXXXXX.");

        RuleFor(x => x.Customer)
            .NotNull()
            .SetValidator(new CustomerInfoValidator());
        // Errores: Customer.Name, Customer.TaxId, Customer.Email

        RuleFor(x => x.BillingAddress)
            .NotNull()
            .SetValidator(new AddressValidator());
        // Errores: BillingAddress.Street, BillingAddress.City, etc.

        RuleFor(x => x.ShippingAddress)
            .SetValidator(new AddressValidator())
            .When(x => x.ShippingAddress != null);

        RuleFor(x => x.Lines)
            .NotEmptyCollection()
                .WithMessage("La factura debe tener al menos una línea.");

        RuleForEach(x => x.Lines)
            .SetValidator(new InvoiceLineValidator());
        // Errores: Lines[0].ProductCode, Lines[1].UnitPrice, etc.

        RuleFor(x => x.Payment)
            .NotNull()
            .SetValidator(new PaymentInfoValidator());
    }
}
```

---

## Include para herencia de validadores

`Include` permite construir validadores por capas, donde cada capa agrega reglas sin conocer las reglas de las demás.

### Caso: entidades de dominio con campos comunes

```csharp
// Interfaz que todas las entidades auditables implementan
public interface IAuditableEntity
{
    string CreatedBy { get; }
    DateTime CreatedAt { get; }
}

// Request de creación (sin auditoría, la pone el sistema)
public class CreateProductRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

// Request de actualización de admin (con auditoría manual)
public class AdminUpdateProductRequest : CreateProductRequest
{
    public int Id { get; set; }
    public string ModifiedBy { get; set; } = string.Empty;
    public string ModificationReason { get; set; } = string.Empty;
}
```

```csharp
// Validador base para los campos comunes a crear y actualizar
public class ProductBaseValidator : AbstractValidator<CreateProductRequest>
{
    public ProductBaseValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(200);

        RuleFor(x => x.Price)
            .GreaterThan(0m)
            .MaxDecimalPlaces(2);
    }
}

// Validador específico de creación
public class CreateProductValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductValidator()
    {
        Include(new ProductBaseValidator());
        // Solo las reglas de ProductBaseValidator, nada más
    }
}

// Validador de actualización admin — extiende las reglas base
public class AdminUpdateProductValidator : AbstractValidator<AdminUpdateProductRequest>
{
    public AdminUpdateProductValidator()
    {
        // Incluye las reglas base del CreateProductRequest
        Include(new ProductBaseValidator());

        // Agrega reglas específicas de actualización
        RuleFor(x => x.Id)
            .GreaterThan(0)
                .WithMessage("El ID del producto debe ser válido.");

        RuleFor(x => x.ModifiedBy)
            .NotEmpty()
                .WithMessage("Se requiere el nombre del administrador que modifica.");

        RuleFor(x => x.ModificationReason)
            .NotEmpty()
                .WithMessage("Se requiere una razón para la modificación.")
            .MinimumLength(20)
                .WithMessage("La razón debe tener al menos 20 caracteres.")
            .MaximumLength(500);
    }
}
```

### Caso: validadores multi-tenant con reglas comunes

```csharp
// Reglas comunes para todos los tenants
public class BaseUserValidator : AbstractValidator<UserRequest>
{
    protected BaseUserValidator()
    {
        RuleFor(x => x.Email).NotEmpty().Email();
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
    }
}

// Reglas adicionales para el tenant A (con verificación 2FA obligatoria)
public class TenantAUserValidator : AbstractValidator<UserRequest>
{
    public TenantAUserValidator()
    {
        Include(new BaseUserValidator());

        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
                .WithMessage("El tenant A requiere número de teléfono para 2FA.")
            .PhoneNumber();

        RuleFor(x => x.Department)
            .NotEmpty()
                .WithMessage("El tenant A requiere que se especifique el departamento.");
    }
}

// Reglas adicionales para el tenant B (sin restricciones extra)
public class TenantBUserValidator : AbstractValidator<UserRequest>
{
    public TenantBUserValidator()
    {
        Include(new BaseUserValidator());
        // Solo las reglas base
    }
}
```

---

## Validación condicional compleja

### Patrón: tipo de entidad determina campos obligatorios

```csharp
public class CreateCustomerRequest
{
    public string CustomerType { get; set; } = string.Empty; // "individual" | "company"

    // Campos para individuos
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? NationalId { get; set; }

    // Campos para empresas
    public string? CompanyName { get; set; }
    public string? TaxId { get; set; }
    public string? LegalRepresentative { get; set; }

    // Campos comunes
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
}

public class CreateCustomerValidator : AbstractValidator<CreateCustomerRequest>
{
    public CreateCustomerValidator()
    {
        // Tipo siempre obligatorio
        RuleFor(x => x.CustomerType)
            .NotEmpty()
            .In(new[] { "individual", "company" })
                .WithMessage("El tipo de cliente debe ser 'individual' o 'company'.")
            .StopOnFirstFailure();

        // Campos comunes
        RuleFor(x => x.Email).NotEmpty().Email();
        RuleFor(x => x.Phone).NotEmpty().PhoneNumber();

        // Campos de individual — solo si es individual
        RuleFor(x => x.FirstName)
            .NotEmpty()
                .WithMessage("El nombre es obligatorio para clientes individuales.")
            .MaximumLength(100)
            .When(x => x.CustomerType == "individual");

        RuleFor(x => x.LastName)
            .NotEmpty()
                .WithMessage("El apellido es obligatorio para clientes individuales.")
            .MaximumLength(100)
            .When(x => x.CustomerType == "individual");

        RuleFor(x => x.BirthDate)
            .NotNull()
                .WithMessage("La fecha de nacimiento es obligatoria para clientes individuales.")
            .PastDate()
            .Must(d => d.HasValue && DateTime.Today.Year - d.Value.Year >= 18)
                .WithMessage("El cliente debe ser mayor de edad.")
            .When(x => x.CustomerType == "individual");

        RuleFor(x => x.NationalId)
            .NotEmpty()
                .WithMessage("El DNI/NIE es obligatorio para clientes individuales.")
            .Matches(@"^[0-9]{8}[A-Z]$|^[XYZ][0-9]{7}[A-Z]$")
                .WithMessage("El DNI/NIE no tiene formato válido.")
            .When(x => x.CustomerType == "individual");

        // Campos de empresa — solo si es empresa
        RuleFor(x => x.CompanyName)
            .NotEmpty()
                .WithMessage("La razón social es obligatoria para empresas.")
            .MaximumLength(300)
            .When(x => x.CustomerType == "company");

        RuleFor(x => x.TaxId)
            .NotEmpty()
                .WithMessage("El CIF es obligatorio para empresas.")
            .Matches(@"^[A-Z][0-9]{7}[A-Z0-9]$")
                .WithMessage("El CIF no tiene formato válido.")
            .When(x => x.CustomerType == "company");

        RuleFor(x => x.LegalRepresentative)
            .NotEmpty()
                .WithMessage("El representante legal es obligatorio para empresas.")
            .When(x => x.CustomerType == "company");
    }
}
```

### Patrón: validación dependiente del contexto del pedido

```csharp
public class ShippingOptionsRequest
{
    public string DeliveryType { get; set; } = string.Empty; // "standard", "express", "pickup"

    public string? PickupLocationId { get; set; }     // solo para pickup
    public DateTime? PreferredDeliveryDate { get; set; } // solo para express
    public decimal? InsuredValue { get; set; }         // opcional para todos

    public Address? DeliveryAddress { get; set; }      // obligatorio para standard/express
}

public class ShippingOptionsValidator : AbstractValidator<ShippingOptionsRequest>
{
    private static readonly string[] ValidTypes = new[] { "standard", "express", "pickup" };

    public ShippingOptionsValidator()
    {
        RuleFor(x => x.DeliveryType)
            .NotEmpty()
            .In(ValidTypes)
            .StopOnFirstFailure();

        // Para recogida en tienda: necesita punto de recogida
        RuleFor(x => x.PickupLocationId)
            .NotEmpty()
                .WithMessage("Debes seleccionar un punto de recogida.")
            .When(x => x.DeliveryType == "pickup");

        // Para envío estándar/express: necesita dirección de entrega
        RuleFor(x => x.DeliveryAddress)
            .NotNull()
                .WithMessage("La dirección de entrega es obligatoria.")
            .SetValidator(new AddressValidator())
            .When(x => x.DeliveryType is "standard" or "express");

        // Para express: puede especificar fecha preferida
        RuleFor(x => x.PreferredDeliveryDate)
            .FutureDate()
                .WithMessage("La fecha de entrega preferida debe ser en el futuro.")
            .Must(d => d.HasValue && (d.Value - DateTime.Today).TotalDays <= 30)
                .WithMessage("La fecha preferida no puede ser más de 30 días en el futuro.")
            .When(x => x.DeliveryType == "express" && x.PreferredDeliveryDate.HasValue);

        // Valor asegurado: siempre opcional, pero si se especifica debe ser positivo
        RuleFor(x => x.InsuredValue)
            .GreaterThan(0m)
                .WithMessage("El valor asegurado debe ser mayor que 0.")
            .LessThanOrEqualTo(50000m)
                .WithMessage("El valor asegurado máximo es 50,000 €.")
            .When(x => x.InsuredValue.HasValue);
    }
}
```

---

## Validación de passwords avanzada

```csharp
public class PasswordPolicyValidator : AbstractValidator<SetPasswordRequest>
{
    private static readonly string[] CommonPasswords = new[]
    {
        "password", "12345678", "qwerty123", "admin1234", "letmein!"
    };

    public PasswordPolicyValidator()
    {
        RuleFor(x => x.NewPassword)
            // Estructura básica
            .NotEmpty()
                .WithMessage("La contraseña es obligatoria.")
            .MinimumLength(12)
                .WithMessage("La contraseña debe tener al menos 12 caracteres.")
            .MaximumLength(128)
                .WithMessage("La contraseña no puede superar los 128 caracteres.")
            // Complejidad
            .HasUppercase()
                .WithMessage("Debe contener al menos una letra mayúscula.")
            .HasLowercase()
                .WithMessage("Debe contener al menos una letra minúscula.")
            .HasDigit()
                .WithMessage("Debe contener al menos un número.")
            .HasSpecialChar()
                .WithMessage("Debe contener al menos un carácter especial (! @ # $ % ...).")
            // Contraseñas prohibidas
            .Must(pwd => !CommonPasswords.Any(common =>
                pwd.Contains(common, StringComparison.OrdinalIgnoreCase)))
                .WithMessage("La contraseña contiene una secuencia demasiado común.")
                .WithErrorCode("PASSWORD_TOO_COMMON")
            // Sin espacios
            .NoWhitespace()
                .WithMessage("La contraseña no puede contener espacios.")
            .StopOnFirstFailure();

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty()
            .EqualToProperty(x => x.NewPassword)
                .WithMessage("Las contraseñas no coinciden.")
            .StopOnFirstFailure();
    }
}
```

---

## Colecciones anidadas complejas

### Árbol de categorías y productos

```csharp
public class ImportCatalogRequest
{
    public List<CategoryNode> Categories { get; set; } = new();
}

public class CategoryNode
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<ProductNode> Products { get; set; } = new();
    public List<CategoryNode> Subcategories { get; set; } = new();
}

public class ProductNode
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public List<string> Images { get; set; } = new();
}
```

```csharp
public class ProductNodeValidator : AbstractValidator<ProductNode>
{
    public ProductNodeValidator()
    {
        RuleFor(x => x.Sku)
            .NotEmpty()
            .Matches(@"^[A-Z0-9-]{3,20}$")
                .WithMessage("El SKU debe tener entre 3 y 20 caracteres alfanuméricos o guiones.");

        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);

        RuleFor(x => x.Price).GreaterThan(0m).MaxDecimalPlaces(2);

        RuleFor(x => x.Images)
            .MaxCount(10)
                .WithMessage("Un producto puede tener máximo 10 imágenes.");

        RuleForEach(x => x.Images)
            .NotEmpty()
            .Url()
                .WithMessage("Cada imagen debe ser una URL válida.");
    }
}

public class CategoryNodeValidator : AbstractValidator<CategoryNode>
{
    public CategoryNodeValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .Matches(@"^[A-Z0-9_]{2,20}$")
                .WithMessage("El código de categoría debe ser alfanumérico en mayúsculas.");

        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);

        // Valida cada producto dentro de la categoría
        RuleForEach(x => x.Products)
            .SetValidator(new ProductNodeValidator());

        // Valida subcategorías recursivamente — máximo 1 nivel de profundidad
        RuleFor(x => x.Subcategories)
            .MaxCount(20)
                .WithMessage("Una categoría puede tener máximo 20 subcategorías.");

        RuleForEach(x => x.Subcategories)
            .Must(sub => sub.Subcategories.Count == 0)
                .WithMessage("Las subcategorías no pueden tener subcategorías anidadas.")
            .SetValidator(new CategoryNodeValidator());
    }
}

public class ImportCatalogValidator : AbstractValidator<ImportCatalogRequest>
{
    public ImportCatalogValidator()
    {
        RuleFor(x => x.Categories)
            .NotEmptyCollection()
                .WithMessage("El catálogo debe tener al menos una categoría.")
            .MaxCount(100)
                .WithMessage("El catálogo no puede tener más de 100 categorías raíz.");

        RuleForEach(x => x.Categories)
            .SetValidator(new CategoryNodeValidator());
    }
}
```

---

## Validador con estado calculado

En algunos casos necesitas calcular un valor una vez y usarlo en múltiples reglas. Puedes hacerlo en el constructor del validador:

```csharp
public class CreateSubscriptionValidator : AbstractValidator<CreateSubscriptionRequest>
{
    public CreateSubscriptionValidator(ISubscriptionPlanRepository plans)
    {
        // No puedes hacer llamadas async aquí, pero sí puedes inyectar
        // servicios para usar en MustAsync

        RuleFor(x => x.PlanId)
            .NotEmpty()
            .MustAsync(async (req, planId, ct) =>
            {
                var plan = await plans.GetByIdAsync(planId, ct);
                return plan != null && plan.IsActive;
            })
            .WithMessage("El plan de suscripción no existe o no está activo.")
            .StopOnFirstFailure();

        RuleFor(x => x.BillingCycle)
            .NotEmpty()
            .In(new[] { "monthly", "annual" })
                .WithMessage("El ciclo de facturación debe ser 'monthly' o 'annual'.");

        RuleFor(x => x.PaymentMethodId)
            .NotEmpty()
            .MustAsync(async (req, pmId, ct) =>
            {
                var plan = await plans.GetByIdAsync(req.PlanId, ct);
                if (plan == null) return true; // Ya validado arriba

                // Plan gratuito no requiere método de pago
                return plan.Price == 0 || !string.IsNullOrEmpty(pmId);
            })
            .WithMessage("Los planes de pago requieren un método de pago registrado.")
            .WhenAsync(async (req, ct) =>
            {
                // Solo validar si el plan existe
                return await plans.ExistsAsync(req.PlanId, ct);
            });
    }
}
```

---

## Reutilización de reglas con métodos de extensión

Puedes crear extensiones para `IRuleBuilder` que encapsulen reglas comunes a múltiples validadores:

```csharp
public static class ValiValidationExtensions
{
    // Validación de NIF/NIE español
    public static IRuleBuilder<T, string> IsValidSpanishId<T>(
        this IRuleBuilder<T, string> builder)
    {
        return builder
            .NotEmpty()
            .Matches(@"^[0-9]{8}[A-Z]$|^[XYZ][0-9]{7}[A-Z]$")
                .WithMessage("El documento de identidad no tiene formato válido (DNI/NIE).");
    }

    // Validación de IBAN español
    public static IRuleBuilder<T, string> IsValidSpanishIban<T>(
        this IRuleBuilder<T, string> builder)
    {
        return builder
            .NotEmpty()
            .StartsWith("ES")
            .LengthBetween(24, 24)
                .WithMessage("El IBAN español debe tener exactamente 24 caracteres.")
            .IsNumeric(); // Los últimos 22 son numéricos (simplificado)
    }

    // Validación de precio con moneda
    public static IRuleBuilder<T, decimal> IsValidPrice<T>(
        this IRuleBuilder<T, decimal> builder,
        decimal maxPrice = 999999.99m)
    {
        return builder
            .GreaterThan(0m)
                .WithMessage("El precio debe ser mayor que 0.")
            .LessThanOrEqualTo(maxPrice)
                .WithMessage($"El precio no puede superar {maxPrice:C}.")
            .MaxDecimalPlaces(2)
                .WithMessage("El precio no puede tener más de 2 decimales.");
    }
}

// Uso en validadores
public class BankAccountValidator : AbstractValidator<BankAccount>
{
    public BankAccountValidator()
    {
        RuleFor(x => x.HolderNationalId).IsValidSpanishId();
        RuleFor(x => x.Iban).IsValidSpanishIban();
    }
}

public class ProductCatalogEntryValidator : AbstractValidator<ProductCatalogEntry>
{
    public ProductCatalogEntryValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Price).IsValidPrice(maxPrice: 50000m);
        RuleFor(x => x.CostPrice).IsValidPrice(maxPrice: 25000m);
    }
}
```

---

## Switch/Case: validación polimórfica

`RuleSwitch` y `SwitchOn` permiten modelar formularios donde la estructura de validación varía según un discriminador. Los patrones siguientes muestran cómo usarlos en escenarios reales.

### Escenario 1: formulario de envío con tipos de entrega distintos

Un pedido puede enviarse a domicilio, recogerse en tienda o depositarse en una consigna automática. Cada opción requiere campos diferentes.

```csharp
public class ShipmentRequest
{
    public string ShippingType { get; set; } = string.Empty;
    // "home_delivery" | "store_pickup" | "locker"

    // Regla global: siempre obligatorio
    public string ContactPhone { get; set; } = string.Empty;

    // Campos para entrega a domicilio
    public string? RecipientName { get; set; }
    public string? Street { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }

    // Campos para recogida en tienda
    public string? StoreId { get; set; }
    public string? PickupName { get; set; }

    // Campos para consigna automática
    public string? LockerId { get; set; }
    public string? LockerAccessCode { get; set; }
}

public class ShipmentValidator : AbstractValidator<ShipmentRequest>
{
    public ShipmentValidator()
    {
        // Regla global: se ejecuta siempre
        RuleFor(x => x.ContactPhone)
            .NotEmpty()
                .WithMessage("El teléfono de contacto es obligatorio.")
            .PhoneNumber();

        RuleFor(x => x.ShippingType)
            .NotEmpty()
            .In(new[] { "home_delivery", "store_pickup", "locker" })
                .WithMessage("El tipo de envío no es válido.")
            .StopOnFirstFailure();

        // Solo un caso se ejecuta según ShippingType
        RuleSwitch(x => x.ShippingType)
            .Case("home_delivery", rules =>
            {
                rules.RuleFor(x => x.RecipientName)
                    .NotEmpty()
                        .WithMessage("El nombre del destinatario es obligatorio para envíos a domicilio.");

                rules.RuleFor(x => x.Street)
                    .NotEmpty()
                        .WithMessage("La calle es obligatoria.")
                    .MaximumLength(200);

                rules.RuleFor(x => x.City)
                    .NotEmpty()
                        .WithMessage("La ciudad es obligatoria.");

                rules.RuleFor(x => x.PostalCode)
                    .NotEmpty()
                        .WithMessage("El código postal es obligatorio.")
                    .Matches(@"^\d{5}$")
                        .WithMessage("El código postal debe tener 5 dígitos.");
            })
            .Case("store_pickup", rules =>
            {
                rules.RuleFor(x => x.StoreId)
                    .NotEmpty()
                        .WithMessage("Debes seleccionar una tienda.");

                rules.RuleFor(x => x.PickupName)
                    .NotEmpty()
                        .WithMessage("El nombre de recogida es obligatorio.")
                    .MaximumLength(200);
            })
            .Case("locker", rules =>
            {
                rules.RuleFor(x => x.LockerId)
                    .NotEmpty()
                        .WithMessage("Debes seleccionar una consigna.");

                rules.RuleFor(x => x.LockerAccessCode)
                    .NotEmpty()
                        .WithMessage("El código de acceso a la consigna es obligatorio.")
                    .MinimumLength(4)
                    .MaximumLength(10)
                        .WithMessage("El código debe tener entre 4 y 10 caracteres.");
            });
    }
}
```

### Escenario 2: notificaciones por canal

La entidad `Notification` envía mensajes por email, SMS o notificación push. Cada canal requiere campos distintos para el destinatario y el contenido tiene restricciones de longitud diferentes.

```csharp
public class NotificationRequest
{
    public string Channel { get; set; } = string.Empty; // "email" | "sms" | "push"
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;

    // Datos del destinatario (según canal)
    public string? RecipientEmail { get; set; }
    public string? RecipientPhone { get; set; }
    public string? DeviceToken { get; set; }

    // Metadatos
    public bool IsUrgent { get; set; }
}

public class NotificationValidator : AbstractValidator<NotificationRequest>
{
    public NotificationValidator()
    {
        RuleFor(x => x.Channel)
            .NotEmpty()
            .In(new[] { "email", "sms", "push" })
                .WithMessage("El canal debe ser 'email', 'sms' o 'push'.")
            .StopOnFirstFailure();

        // El asunto siempre es obligatorio
        RuleFor(x => x.Subject)
            .NotEmpty()
                .WithMessage("El asunto de la notificación es obligatorio.")
            .MaximumLength(200);

        // El cuerpo varía en longitud según el canal
        RuleFor(x => x.Body)
            .SwitchOn(x => x.Channel)
            .Case("email",  b => b
                .NotEmpty()
                    .WithMessage("El cuerpo del email es obligatorio.")
                .MaximumLength(50000)
                    .WithMessage("El cuerpo del email no puede superar 50.000 caracteres."))
            .Case("sms",    b => b
                .NotEmpty()
                    .WithMessage("El mensaje SMS es obligatorio.")
                .MaximumLength(160)
                    .WithMessage("Los SMS no pueden superar 160 caracteres."))
            .Case("push",   b => b
                .NotEmpty()
                    .WithMessage("El cuerpo de la notificación push es obligatorio.")
                .MaximumLength(256)
                    .WithMessage("Las notificaciones push no pueden superar 256 caracteres."));

        // Destinatario: un campo diferente por canal
        RuleSwitch(x => x.Channel)
            .Case("email", rules =>
            {
                rules.RuleFor(x => x.RecipientEmail)
                    .NotEmpty()
                        .WithMessage("El email del destinatario es obligatorio.")
                    .Email()
                        .WithMessage("El email del destinatario no tiene formato válido.");
            })
            .Case("sms", rules =>
            {
                rules.RuleFor(x => x.RecipientPhone)
                    .NotEmpty()
                        .WithMessage("El teléfono del destinatario es obligatorio.")
                    .PhoneNumber()
                        .WithMessage("El teléfono del destinatario no tiene formato válido.");
            })
            .Case("push", rules =>
            {
                rules.RuleFor(x => x.DeviceToken)
                    .NotEmpty()
                        .WithMessage("El token de dispositivo es obligatorio.")
                    .MinimumLength(32)
                        .WithMessage("El token de dispositivo parece inválido.");
            });
    }
}
```

Este ejemplo combina ambas herramientas:
- `SwitchOn` sobre `Body` para aplicar límites de longitud distintos según el canal.
- `RuleSwitch` para validar el campo de destinatario correcto según el canal.

### Reglas globales + RuleSwitch

Las reglas definidas fuera del `RuleSwitch` se ejecutan siempre. El switch solo controla el bloque condicional. Puedes definir reglas antes, después o intercaladas con el switch:

```csharp
public class PaymentValidator : AbstractValidator<PaymentDto>
{
    public PaymentValidator()
    {
        // Siempre se validan — independientes del método de pago
        RuleFor(x => x.Amount).Positive();
        RuleFor(x => x.Currency).NotEmpty().CurrencyCode();

        // Solo un caso del switch se ejecuta
        RuleSwitch(x => x.Method)
            .Case("credit_card", rules => { /* ... */ })
            .Case("bank_transfer", rules => { /* ... */ })
            .Default(rules => { /* ... */ });

        // También siempre se valida — tras el switch
        RuleFor(x => x.Description).MaximumLength(500);
    }
}
```

---

## Combinando las nuevas reglas en escenarios reales

Los siguientes ejemplos muestran cómo integrar las nuevas reglas en formularios reales, combinándolas con las reglas existentes para una validación completa.

### Formulario de reserva: fechas cruzadas y capacidad

```csharp
public class CreateBookingRequest
{
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public int Guests { get; set; }
    public int RoomCapacity { get; set; }
    public bool HasBreakfast { get; set; }
    public decimal? BreakfastRate { get; set; }
    public decimal RoomRate { get; set; }
}

public class CreateBookingValidator : AbstractValidator<CreateBookingRequest>
{
    public CreateBookingValidator()
    {
        RuleFor(x => x.CheckIn)
            .FutureDate()
                .WithMessage("La fecha de entrada debe ser en el futuro.")
            .IsWeekday()
                .WithMessage("Las entradas solo se procesan en días laborables.");

        RuleFor(x => x.CheckOut)
            .GreaterThanProperty(x => x.CheckIn)
                .WithMessage("La fecha de salida debe ser posterior a la fecha de entrada.")
            .WithinNext(TimeSpan.FromDays(365))
                .WithMessage("No se pueden hacer reservas con más de un año de antelación.");

        RuleFor(x => x.Guests)
            .GreaterThan(0)
                .WithMessage("Debe haber al menos 1 huésped.")
            .LessThanOrEqualToProperty(x => x.RoomCapacity)
                .WithMessage("El número de huéspedes supera la capacidad de la habitación.");

        // La tarifa de desayuno es obligatoria si se incluye desayuno
        RuleFor(x => x.BreakfastRate)
            .RequiredIf(x => x.HasBreakfast)
                .WithMessage("La tarifa de desayuno es obligatoria si se incluye desayuno.")
            .Positive()
                .WithMessage("La tarifa de desayuno debe ser un valor positivo.")
            .When(x => x.HasBreakfast);

        RuleFor(x => x.RoomRate)
            .Positive()
            .Precision(8, 2);
    }
}
```

### Registro de usuario: mayoría de edad, política de contraseña y código de país

```csharp
public class RegisterUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public DateTime BirthDate { get; set; }
    public string CountryCode { get; set; } = string.Empty;
    public string? ReferralCode { get; set; }
}

public class RegisterUserValidator : AbstractValidator<RegisterUserRequest>
{
    public RegisterUserValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .Email()
            .MaximumLength(320);

        RuleFor(x => x.Username)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(50)
            .IsAlphanumeric()
            .Slug()
                .WithMessage("El nombre de usuario solo puede contener letras minúsculas, números y guiones.");

        // Política de contraseña todo-en-uno
        RuleFor(x => x.Password)
            .NotEmpty()
            .PasswordPolicy(minLength: 10, requireUppercase: true, requireLowercase: true,
                requireDigit: true, requireSpecialChar: true)
                .WithMessage("La contraseña debe tener al menos 10 caracteres, mayúscula, minúscula, número y carácter especial.")
            .NotEqualToProperty(x => x.Username)
                .WithMessage("La contraseña no puede ser igual al nombre de usuario.");

        RuleFor(x => x.ConfirmPassword)
            .EqualToProperty(x => x.Password)
                .WithMessage("Las contraseñas no coinciden.");

        // Restricción de mayoría de edad
        RuleFor(x => x.BirthDate)
            .PastDate()
                .WithMessage("La fecha de nacimiento debe estar en el pasado.")
            .MinAge(18)
                .WithMessage("Debes tener al menos 18 años para registrarte.");

        // Código de país ISO 3166-1 alpha-2
        RuleFor(x => x.CountryCode)
            .NotEmpty()
            .CountryCode()
                .WithMessage("El código de país debe ser un código ISO válido (ej: PE, ES, US).");

        // Código de referido: sin HTML ni SQL injection
        RuleFor(x => x.ReferralCode)
            .NoHtmlTags()
            .NoSqlInjectionPatterns()
            .MaximumLength(20)
            .When(x => x.ReferralCode != null);
    }
}
```

### Formulario financiero: precisión decimal, porcentaje e IBAN

```csharp
public class CreateTransactionRequest
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public decimal TaxRate { get; set; }
    public string DestinationIban { get; set; } = string.Empty;
    public string OriginCountry { get; set; } = string.Empty;
    public DateTime ValueDate { get; set; }
    public string? Metadata { get; set; }
}

public class CreateTransactionValidator : AbstractValidator<CreateTransactionRequest>
{
    public CreateTransactionValidator()
    {
        RuleFor(x => x.Amount)
            .Positive()
                .WithMessage("El importe debe ser mayor que 0.")
            // Máximo 12 dígitos totales con 2 decimales (equivalente a DECIMAL(12,2) en SQL)
            .Precision(12, 2)
                .WithMessage("El importe no puede tener más de 2 decimales ni superar 10 dígitos enteros.");

        RuleFor(x => x.Currency)
            .CurrencyCode()
                .WithMessage("El código de moneda debe ser un código ISO 4217 válido (ej: USD, EUR, PEN).");

        RuleFor(x => x.TaxRate)
            .NonNegative()
                .WithMessage("La tasa impositiva no puede ser negativa.")
            .Percentage()
                .WithMessage("La tasa impositiva debe ser un porcentaje entre 0 y 100.");

        RuleFor(x => x.DestinationIban)
            .NotEmpty()
            .Iban()
                .WithMessage("El IBAN de destino no es válido.");

        RuleFor(x => x.OriginCountry)
            .CountryCode()
                .WithMessage("El código de país de origen no es válido.");

        // La fecha valor debe ser un día laborable dentro del próximo mes
        RuleFor(x => x.ValueDate)
            .IsWeekday()
                .WithMessage("La fecha valor debe ser un día laborable.")
            .WithinNext(TimeSpan.FromDays(30))
                .WithMessage("La fecha valor debe estar dentro del próximo mes.");

        // El campo Metadata debe ser JSON válido si se proporciona
        RuleFor(x => x.Metadata)
            .IsValidJson()
                .WithMessage("El campo Metadata debe ser un JSON válido.")
            .When(x => x.Metadata != null);
    }
}
```

### Petición a API: campo de metadatos JSON y campo de configuración Base64

```csharp
public class ApiWebhookRequest
{
    public string EndpointUrl { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public string? SignatureBase64 { get; set; }
    public string? FilterMetadata { get; set; }
    public string SourceIpAddress { get; set; } = string.Empty;
}

public class ApiWebhookValidator : AbstractValidator<ApiWebhookRequest>
{
    private static readonly string[] AllowedEvents = new[]
    {
        "order.created", "order.updated", "payment.completed", "user.registered"
    };

    public ApiWebhookValidator()
    {
        RuleFor(x => x.EndpointUrl)
            .NotEmpty()
            .Url()
                .WithMessage("La URL del endpoint debe ser una URL HTTP/HTTPS válida.")
            .StartsWith("https://")
                .WithMessage("El endpoint de webhook debe usar HTTPS.");

        RuleFor(x => x.EventType)
            .NotEmpty()
            .In(AllowedEvents)
                .WithMessage("El tipo de evento no es reconocido.");

        // El payload debe ser JSON válido y sin patrones de inyección
        RuleFor(x => x.Payload)
            .NotEmpty()
            .IsValidJson()
                .WithMessage("El payload debe ser un objeto JSON válido.")
            .NoSqlInjectionPatterns()
                .WithMessage("El payload contiene patrones no permitidos.");

        // La firma digital debe ser Base64 válido si se proporciona
        RuleFor(x => x.SignatureBase64)
            .IsValidBase64()
                .WithMessage("La firma digital debe estar codificada en Base64.")
            .When(x => x.SignatureBase64 != null);

        // El filtro de metadatos es JSON opcional
        RuleFor(x => x.FilterMetadata)
            .IsValidJson()
                .WithMessage("El campo FilterMetadata debe ser un JSON válido.")
            .When(x => x.FilterMetadata != null);

        // La IP de origen puede ser IPv4 o IPv6
        RuleFor(x => x.SourceIpAddress)
            .IPv4()
                .WithMessage("La IP de origen no es válida.")
            .When(x => !x.SourceIpAddress.Contains(':'));

        RuleFor(x => x.SourceIpAddress)
            .IPv6()
                .WithMessage("La dirección IPv6 de origen no es válida.")
            .When(x => x.SourceIpAddress.Contains(':'));
    }
}
```

---

## Siguientes pasos

Una vez dominados estos patrones avanzados, puedes explorar:

- **[CascadeMode](08-cascade-mode.md)** — Control fino del flujo de validación en casos complejos
- **[Reglas avanzadas](06-reglas-avanzadas.md)** — Custom y Transform para casos especiales
- **[Integración con Vali-Mediator](14-integracion-valimediator.md)** — Pipeline behavior con Result\<T\>
