# Validadores

## AbstractValidator\<T\>

`AbstractValidator<T>` es la clase base que debes heredar para crear cualquier validador. Es donde defines todas las reglas usando el método `RuleFor`.

### Estructura básica

```csharp
public class OrderValidator : AbstractValidator<Order>
{
    public OrderValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.TotalAmount).GreaterThan(0);
        RuleFor(x => x.Items).NotEmptyCollection();
    }
}
```

La restricción `where T : class` implica que no puedes crear validadores para tipos valor (structs, int, etc.) directamente. Si necesitas validar un tipo valor, envuélvelo en un objeto wrapper.

### Inyección de dependencias en el constructor

Los validadores pueden recibir dependencias en el constructor. Esto es útil cuando una regla necesita acceder a la base de datos u otros servicios (por ejemplo, verificar unicidad de email):

```csharp
public class CreateUserValidator : AbstractValidator<CreateUserRequest>
{
    private readonly IUserRepository _userRepository;

    public CreateUserValidator(IUserRepository userRepository)
    {
        _userRepository = userRepository;

        RuleFor(x => x.Email)
            .NotEmpty()
            .Email()
            .MustAsync(async email =>
            {
                var exists = await _userRepository.ExistsByEmailAsync(email);
                return !exists;
            })
            .WithMessage("Ya existe un usuario con ese email.");

        RuleFor(x => x.Username)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(50);
    }
}
```

> **Importante:** Si el validador recibe servicios por DI, regístralo como `Scoped` (no `Transient`) cuando esos servicios sean `Scoped` (como `DbContext`). Ver [Inyección de dependencias](11-inyeccion-dependencias.md).

---

## RuleFor

`RuleFor` acepta una expresión que selecciona la propiedad a validar. Devuelve un `IRuleBuilder<T, TProperty>` que permite encadenar reglas.

```csharp
RuleFor(x => x.Email)
    .NotEmpty()
    .Email()
    .MaximumLength(320);
```

La expresión puede acceder a propiedades anidadas:

```csharp
// Propiedad directa
RuleFor(x => x.Name).NotEmpty();

// Propiedad anidada
RuleFor(x => x.Address.Street).NotEmpty();

// Propiedad de propiedad
RuleFor(x => x.ContactInfo.PrimaryEmail).Email();
```

> **Nota:** Para expresiones complejas como `x => x.Name.ToLower()`, el nombre de la propiedad extraído puede no ser el esperado. En esos casos, usa `OverridePropertyName("Name")` para controlar la clave en el resultado. Ver [Modificadores](07-modificadores.md).

---

## RuleForEach

`RuleForEach` valida cada elemento de una colección. Las claves de error usan índices: `"Items[0]"`, `"Items[1]"`, etc.

```csharp
public class CreateInvoiceValidator : AbstractValidator<CreateInvoiceRequest>
{
    public CreateInvoiceValidator()
    {
        RuleFor(x => x.InvoiceNumber)
            .NotEmpty()
            .Matches(@"^INV-\d{6}$")
                .WithMessage("El número de factura debe tener el formato INV-XXXXXX.");

        RuleFor(x => x.Lines)
            .NotEmptyCollection()
                .WithMessage("La factura debe tener al menos una línea.");

        // Valida cada línea individualmente
        RuleForEach(x => x.Lines)
            .Must(line => line.Quantity > 0)
                .WithMessage("La cantidad debe ser mayor que 0.")
            .Must(line => line.UnitPrice > 0)
                .WithMessage("El precio unitario debe ser mayor que 0.");
    }
}
```

Si los errores se producen en la línea con índice 2:

```json
{
  "Lines[2]": ["La cantidad debe ser mayor que 0."]
}
```

### RuleForEach con SetValidator

Para validar objetos complejos dentro de una colección, combina `RuleForEach` con `SetValidator`:

```csharp
public class InvoiceLineValidator : AbstractValidator<InvoiceLine>
{
    public InvoiceLineValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.UnitPrice).GreaterThan(0);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

public class CreateInvoiceValidator : AbstractValidator<CreateInvoiceRequest>
{
    public CreateInvoiceValidator()
    {
        RuleFor(x => x.InvoiceNumber).NotEmpty();
        RuleFor(x => x.Lines).NotEmptyCollection();

        RuleForEach(x => x.Lines)
            .SetValidator(new InvoiceLineValidator());
    }
}
```

Los errores aparecen como `"Lines[0].ProductId"`, `"Lines[1].UnitPrice"`, etc.

---

## Include

`Include` copia todas las reglas de otro validador en el validador actual. Es útil para herencia de reglas o para componer validadores de partes distintas.

### Caso de uso: base común de reglas

```csharp
// Reglas comunes a todas las entidades auditables
public class AuditableValidator<T> : AbstractValidator<T> where T : IAuditable
{
    protected AuditableValidator()
    {
        RuleFor(x => x.CreatedBy).NotEmpty();
        RuleFor(x => x.CreatedAt).PastDate();
    }
}

// Reglas específicas del producto
public class ProductBaseValidator : AbstractValidator<Product>
{
    public ProductBaseValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Price).GreaterThan(0);
    }
}

// Validador completo que combina ambos
public class CreateProductValidator : AbstractValidator<Product>
{
    public CreateProductValidator()
    {
        Include(new ProductBaseValidator());

        // Reglas adicionales específicas de creación
        RuleFor(x => x.Stock).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Category).NotEmpty();
    }
}
```

### Caso de uso: separación por responsabilidades

```csharp
public class AddressValidator : AbstractValidator<UserRequest>
{
    public AddressValidator()
    {
        RuleFor(x => x.Street).NotEmpty();
        RuleFor(x => x.City).NotEmpty();
        RuleFor(x => x.PostalCode).Matches(@"^\d{5}$");
    }
}

public class ContactValidator : AbstractValidator<UserRequest>
{
    public ContactValidator()
    {
        RuleFor(x => x.Email).NotEmpty().Email();
        RuleFor(x => x.PhoneNumber).PhoneNumber().When(x => x.PhoneNumber != null);
    }
}

public class UserRequestValidator : AbstractValidator<UserRequest>
{
    public UserRequestValidator()
    {
        // Identidad
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BirthDate).PastDate();

        // Incluye validadores de secciones
        Include(new AddressValidator());
        Include(new ContactValidator());
    }
}
```

> **Precaución:** `Include` no es lo mismo que `SetValidator`. `Include` toma reglas de otro validador sobre el **mismo tipo T** y las agrega al validador actual. `SetValidator` delega validación de una **propiedad anidada** a un validador de tipo diferente.

---

## RuleSwitch — Validación condicional por casos

`RuleSwitch` permite definir grupos de reglas que se activan según el valor de una propiedad discriminadora del objeto. Solo se ejecuta **un caso por validación**: el primero que coincide con el valor en tiempo de ejecución.

### Cuándo usarlo

Úsalo cuando el **tipo o estado** del objeto determina qué conjuntos de campos son obligatorios o válidos. Por ejemplo, un formulario de pago donde los campos requeridos cambian completamente según el método elegido.

Compáralo con `When`/`Unless`: estos modificadores trabajan de forma independiente sobre una sola propiedad; `RuleSwitch` agrupa reglas sobre **múltiples propiedades** de forma exclusiva.

### Sintaxis

```csharp
RuleSwitch(x => x.PropiedadDiscriminadora)
    .Case(valor1, rules =>
    {
        rules.RuleFor(x => x.Campo1).NotEmpty();
        rules.RuleFor(x => x.Campo2).MinimumLength(3);
    })
    .Case(valor2, rules =>
    {
        rules.RuleFor(x => x.CampoDistinto).NotEmpty().Email();
    })
    .Default(rules =>
    {
        rules.RuleFor(x => x.CampoGenerico).NotEmpty();
    });
```

`ICaseBuilder<T, TKey>` expone dos métodos:

```csharp
ICaseBuilder<T, TKey> Case(TKey value, Action<AbstractValidator<T>> configure)
ICaseBuilder<T, TKey> Default(Action<AbstractValidator<T>> configure)
```

El bloque `Default` es opcional. Si no se define y ningún caso coincide con el valor en tiempo de ejecución, no se ejecuta ninguna regla de ese bloque.

### Ejemplo: pasarela de pago con múltiples métodos

```csharp
public class PaymentDto
{
    public string Method { get; set; } = string.Empty; // "credit_card" | "bank_transfer" | "paypal"
    public decimal Amount { get; set; }

    // Campos para tarjeta
    public string? CardNumber { get; set; }
    public string? Cvv { get; set; }
    public string? CardHolder { get; set; }

    // Campos para transferencia bancaria
    public string? Iban { get; set; }
    public string? BankName { get; set; }

    // Campos para PayPal
    public string? PaypalEmail { get; set; }

    // Campos de fallback
    public string? Reference { get; set; }
}

public class PaymentValidator : AbstractValidator<PaymentDto>
{
    public PaymentValidator()
    {
        // Regla global: aplica siempre, independientemente del método de pago
        RuleFor(x => x.Amount).Positive();

        // Solo se ejecuta el caso que coincide con Method en tiempo de ejecución
        RuleSwitch(x => x.Method)
            .Case("credit_card", rules =>
            {
                rules.RuleFor(x => x.CardNumber).NotEmpty();
                rules.RuleFor(x => x.Cvv).NotEmpty().MinimumLength(3).MaximumLength(4);
                rules.RuleFor(x => x.CardHolder).NotEmpty();
            })
            .Case("bank_transfer", rules =>
            {
                rules.RuleFor(x => x.Iban).NotEmpty().Iban();
                rules.RuleFor(x => x.BankName).NotEmpty();
            })
            .Case("paypal", rules =>
            {
                rules.RuleFor(x => x.PaypalEmail).NotEmpty().Email();
            })
            .Default(rules =>
            {
                rules.RuleFor(x => x.Reference).NotEmpty();
            });
    }
}
```

**Comportamiento:**
- Si `Method = "credit_card"`, se validan `CardNumber`, `Cvv` y `CardHolder`. Los campos de transferencia y PayPal se ignoran.
- Si `Method = "paypal"`, solo se valida `PaypalEmail`.
- Si `Method` no coincide con ningún caso (por ejemplo, un valor inesperado), se ejecuta el bloque `Default` y se valida `Reference`.
- Si no hay `Default` y el valor no coincide, no se ejecuta ninguna regla del switch.
- La regla `Amount.Positive()` se ejecuta **siempre**, independientemente del caso activo.

> **Nota:** Las reglas definidas dentro de los casos de `RuleSwitch` conviven con las reglas globales del validador. Define siempre las reglas globales (campos obligatorios para todos los casos) antes o después del switch, fuera de él.

---

## Métodos de validación

### Validate (síncrono)

```csharp
ValidationResult result = validator.Validate(instance);
```

El método síncrono ejecuta únicamente las reglas síncronas. Las reglas definidas con `MustAsync`, `WhenAsync` o `DependentRuleAsync` se omiten. Úsalo solo cuando tengas certeza de que no hay reglas asíncronas.

```csharp
var validator = new LoginRequestValidator();
var result = validator.Validate(new LoginRequest { Email = "bad-email", Password = "" });

if (!result.IsValid)
{
    foreach (var error in result.ToFlatList())
        Console.WriteLine(error);
    // Email: El formato de email no es válido.
    // Password: La contraseña es obligatoria.
}
```

### ValidateAsync

```csharp
// Sin CancellationToken
ValidationResult result = await validator.ValidateAsync(instance);

// Con CancellationToken
ValidationResult result = await validator.ValidateAsync(instance, cancellationToken);
```

Ejecuta todas las reglas, incluyendo las asíncronas. Es el método recomendado en aplicaciones ASP.NET Core.

```csharp
public async Task<IActionResult> Register(
    [FromBody] RegisterRequest request,
    [FromServices] IValidator<RegisterRequest> validator)
{
    var result = await validator.ValidateAsync(request, HttpContext.RequestAborted);
    if (!result.IsValid)
        return BadRequest(result.Errors);

    await _userService.RegisterAsync(request);
    return Ok();
}
```

### ValidateParallelAsync

```csharp
ValidationResult result = await validator.ValidateParallelAsync(instance);
ValidationResult result = await validator.ValidateParallelAsync(instance, cancellationToken);
```

Ejecuta todas las reglas asíncronas **en paralelo**. Útil cuando hay múltiples `MustAsync` que hacen llamadas independientes a la base de datos o servicios externos.

```csharp
public class CreateOrderValidator : AbstractValidator<CreateOrderRequest>
{
    private readonly IProductRepository _products;
    private readonly ICustomerRepository _customers;
    private readonly ICouponRepository _coupons;

    public CreateOrderValidator(
        IProductRepository products,
        ICustomerRepository customers,
        ICouponRepository coupons)
    {
        _products = products;
        _customers = customers;
        _coupons = coupons;

        // Estas tres llamadas se ejecutarán en paralelo con ValidateParallelAsync
        RuleFor(x => x.CustomerId)
            .MustAsync(async id => await _customers.ExistsAsync(id))
                .WithMessage("El cliente no existe.");

        RuleForEach(x => x.Items)
            .MustAsync(async item => await _products.ExistsAsync(item.ProductId))
                .WithMessage("El producto no existe.");

        RuleFor(x => x.CouponCode)
            .MustAsync(async code => await _coupons.IsValidAsync(code))
                .WithMessage("El cupón no es válido.")
            .When(x => x.CouponCode != null);
    }
}

// Uso: ValidateParallelAsync ejecuta las tres queries simultáneamente
var result = await validator.ValidateParallelAsync(order, ct);
```

> **Advertencia:** No uses `ValidateParallelAsync` si las reglas tienen efectos secundarios o dependencias entre sí. Úsalo solo cuando las reglas sean independientes y puras (solo lectura).

### ValidateAndThrow / ValidateAndThrowAsync

```csharp
// Lanza ValidationException si la validación falla
validator.ValidateAndThrow(instance);
await validator.ValidateAndThrowAsync(instance);
await validator.ValidateAndThrowAsync(instance, cancellationToken);
```

Equivalente a `ValidateAsync` + comprobación de `IsValid` + lanzamiento de `ValidationException`. Útil en capas de servicio donde prefieres propagar la excepción:

```csharp
public class OrderService
{
    private readonly IValidator<CreateOrderRequest> _validator;
    private readonly IOrderRepository _repository;

    public OrderService(IValidator<CreateOrderRequest> validator, IOrderRepository repository)
    {
        _validator = validator;
        _repository = repository;
    }

    public async Task<Order> CreateAsync(CreateOrderRequest request, CancellationToken ct)
    {
        // Lanza ValidationException con todos los errores si algo falla
        await _validator.ValidateAndThrowAsync(request, ct);

        // Si llegamos aquí, el request es válido
        return await _repository.CreateAsync(request, ct);
    }
}
```

Ver [Excepciones](10-excepciones.md) para más detalles sobre `ValidationException`.

---

## IValidator\<T\> — Interfaz para DI

`IValidator<T>` es la interfaz que se registra en el contenedor DI. Tiene los mismos métodos que `AbstractValidator<T>` excepto `RuleFor`, `RuleForEach` e `Include` (que son internos a la implementación).

```csharp
public interface IValidator<T>
{
    ValidationResult Validate(T instance);
    Task<ValidationResult> ValidateAsync(T instance);
    Task<ValidationResult> ValidateAsync(T instance, CancellationToken ct);
    Task<ValidationResult> ValidateParallelAsync(T instance);
    Task<ValidationResult> ValidateParallelAsync(T instance, CancellationToken ct);
    void ValidateAndThrow(T instance);
    Task ValidateAndThrowAsync(T instance);
    Task ValidateAndThrowAsync(T instance, CancellationToken ct);
}
```

Siempre inyecta `IValidator<T>` en lugar de la implementación concreta. Esto facilita el testing (puedes inyectar un mock) y desacopla el código consumidor del validador específico:

```csharp
// Correcto: depende de la interfaz
public class UserController : ControllerBase
{
    private readonly IValidator<CreateUserRequest> _validator;

    public UserController(IValidator<CreateUserRequest> validator)
    {
        _validator = validator;
    }
}

// Incorrecto: depende de la implementación concreta
public class UserController : ControllerBase
{
    private readonly CreateUserValidator _validator; // No hagas esto
}
```

---

## CascadeMode global

Por defecto, si una propiedad tiene múltiples reglas y la primera falla, todas las demás también se evalúan. Puedes cambiar este comportamiento globalmente sobreescribiendo `GlobalCascadeMode`:

```csharp
public class StrictValidator : AbstractValidator<PaymentRequest>
{
    // Detiene la evaluación de propiedades adicionales tras el primer fallo global
    protected override CascadeMode GlobalCascadeMode => CascadeMode.StopOnFirstFailure;

    public StrictValidator()
    {
        RuleFor(x => x.CardNumber)
            .NotEmpty()
            .CreditCard();

        RuleFor(x => x.ExpiryMonth)
            .GreaterThan(0)
            .LessThanOrEqualTo(12);

        // Si CardNumber falla, ExpiryMonth no se evalúa (modo StopOnFirstFailure global)
    }
}
```

Ver [CascadeMode](08-cascade-mode.md) para la comparación completa entre cascade por propiedad y cascade global.

---

## Ejemplo completo: e-commerce

```csharp
public class CheckoutRequestValidator : AbstractValidator<CheckoutRequest>
{
    private readonly IProductService _productService;
    private readonly IAddressService _addressService;

    public CheckoutRequestValidator(
        IProductService productService,
        IAddressService addressService)
    {
        _productService = productService;
        _addressService = addressService;

        // Validaciones del carrito
        RuleFor(x => x.CartItems)
            .NotEmptyCollection()
                .WithMessage("El carrito no puede estar vacío.");

        RuleForEach(x => x.CartItems)
            .Must(item => item.Quantity > 0)
                .WithMessage("La cantidad debe ser mayor que 0.")
            .MustAsync(async item =>
                await _productService.IsAvailableAsync(item.ProductId, item.Quantity))
                .WithMessage("El producto no tiene suficiente stock.");

        // Validaciones de dirección
        RuleFor(x => x.ShippingAddress)
            .NotNull()
                .WithMessage("La dirección de envío es obligatoria.");

        RuleFor(x => x.ShippingAddress.Street)
            .NotEmpty()
            .MaximumLength(200)
            .When(x => x.ShippingAddress != null);

        RuleFor(x => x.ShippingAddress.PostalCode)
            .NotEmpty()
            .Matches(@"^\d{5}$")
                .WithMessage("El código postal debe tener 5 dígitos.")
            .MustAsync(async postalCode =>
                await _addressService.IsValidPostalCodeAsync(postalCode))
                .WithMessage("El código postal no es válido para envíos.")
            .When(x => x.ShippingAddress != null);

        // Validaciones de pago
        RuleFor(x => x.PaymentMethod)
            .NotNull()
                .WithMessage("El método de pago es obligatorio.");

        RuleFor(x => x.PaymentMethod.CardNumber)
            .NotEmpty()
            .CreditCard()
                .WithMessage("El número de tarjeta no es válido.")
            .When(x => x.PaymentMethod?.Type == PaymentType.Card);

        RuleFor(x => x.PaymentMethod.CardExpiry)
            .NotEmpty()
            .Matches(@"^\d{2}/\d{2}$")
                .WithMessage("La fecha de expiración debe tener formato MM/YY.")
            .FutureDate()
                .WithMessage("La tarjeta ha expirado.")
            .When(x => x.PaymentMethod?.Type == PaymentType.Card);
    }
}
```

---

## Siguientes pasos

- **[Reglas básicas](05-reglas-basicas.md)** — Catálogo de todas las reglas disponibles
- **[Reglas avanzadas](06-reglas-avanzadas.md)** — Must, MustAsync, Custom, Transform, SetValidator, SwitchOn
- **[CascadeMode](08-cascade-mode.md)** — Control del flujo de validación
