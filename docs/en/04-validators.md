# Validators

## AbstractValidator\<T\>

`AbstractValidator<T>` is the base class you must inherit to create any validator. It is where you define all rules using the `RuleFor` method.

### Basic Structure

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

The `where T : class` constraint means you cannot create validators for value types (structs, int, etc.) directly. If you need to validate a value type, wrap it in a wrapper object.

### Dependency Injection in the Constructor

Validators can receive dependencies in the constructor. This is useful when a rule needs to access the database or other services (for example, checking email uniqueness):

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
            .WithMessage("A user with that email already exists.");

        RuleFor(x => x.Username)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(50);
    }
}
```

> **Important:** If the validator receives services via DI, register it as `Scoped` (not `Transient`) when those services are `Scoped` (such as `DbContext`). See [Dependency Injection](11-dependency-injection.md).

---

## RuleFor

`RuleFor` accepts an expression that selects the property to validate. It returns an `IRuleBuilder<T, TProperty>` that allows chaining rules.

```csharp
RuleFor(x => x.Email)
    .NotEmpty()
    .Email()
    .MaximumLength(320);
```

The expression can access nested properties:

```csharp
// Direct property
RuleFor(x => x.Name).NotEmpty();

// Nested property
RuleFor(x => x.Address.Street).NotEmpty();

// Property of property
RuleFor(x => x.ContactInfo.PrimaryEmail).Email();
```

> **Note:** For complex expressions like `x => x.Name.ToLower()`, the extracted property name may not be what you expect. In those cases, use `OverridePropertyName("Name")` to control the key in the result. See [Modifiers](07-modifiers.md).

---

## RuleForEach

`RuleForEach` validates each element of a collection. Error keys use indexes: `"Items[0]"`, `"Items[1]"`, etc.

```csharp
public class CreateInvoiceValidator : AbstractValidator<CreateInvoiceRequest>
{
    public CreateInvoiceValidator()
    {
        RuleFor(x => x.InvoiceNumber)
            .NotEmpty()
            .Matches(@"^INV-\d{6}$")
                .WithMessage("The invoice number must have the format INV-XXXXXX.");

        RuleFor(x => x.Lines)
            .NotEmptyCollection()
                .WithMessage("The invoice must have at least one line.");

        // Validates each line individually
        RuleForEach(x => x.Lines)
            .Must(line => line.Quantity > 0)
                .WithMessage("The quantity must be greater than 0.")
            .Must(line => line.UnitPrice > 0)
                .WithMessage("The unit price must be greater than 0.");
    }
}
```

If errors occur on line at index 2:

```json
{
  "Lines[2]": ["The quantity must be greater than 0."]
}
```

### RuleForEach with SetValidator

To validate complex objects inside a collection, combine `RuleForEach` with `SetValidator`:

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

Errors appear as `"Lines[0].ProductId"`, `"Lines[1].UnitPrice"`, etc.

---

## Include

`Include` copies all rules from another validator into the current validator. It is useful for rule inheritance or for composing validators from separate parts.

### Use Case: Common Base Rules

```csharp
// Rules common to all auditable entities
public class AuditableValidator<T> : AbstractValidator<T> where T : IAuditable
{
    protected AuditableValidator()
    {
        RuleFor(x => x.CreatedBy).NotEmpty();
        RuleFor(x => x.CreatedAt).PastDate();
    }
}

// Product-specific rules
public class ProductBaseValidator : AbstractValidator<Product>
{
    public ProductBaseValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Price).GreaterThan(0);
    }
}

// Complete validator that combines both
public class CreateProductValidator : AbstractValidator<Product>
{
    public CreateProductValidator()
    {
        Include(new ProductBaseValidator());

        // Additional rules specific to creation
        RuleFor(x => x.Stock).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Category).NotEmpty();
    }
}
```

### Use Case: Separation of Responsibilities

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
        // Identity
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BirthDate).PastDate();

        // Includes section validators
        Include(new AddressValidator());
        Include(new ContactValidator());
    }
}
```

> **Caution:** `Include` is not the same as `SetValidator`. `Include` takes rules from another validator for the **same type T** and adds them to the current validator. `SetValidator` delegates validation of a **nested property** to a validator of a different type.

---

## RuleSwitch — Case-based Conditional Validation

`RuleSwitch` lets you apply **completely different sets of rules across multiple properties** depending on the value of a discriminator field. It is the right tool when the type or state of an object determines which fields are relevant at all — not just what format a single field should have.

### Syntax

```csharp
RuleSwitch(x => x.DiscriminatorProperty)
    .Case(value1, rules => { /* configure rules on 'rules' */ })
    .Case(value2, rules => { /* configure rules on 'rules' */ })
    .Default(rules =>        { /* fallback when no case matches */ });
```

`ICaseBuilder<T, TKey>` interface:

```csharp
ICaseBuilder<T, TKey> Case(TKey value, Action<AbstractValidator<T>> configure)
ICaseBuilder<T, TKey> Default(Action<AbstractValidator<T>> configure)
```

Each `Action<AbstractValidator<T>>` receives the same validator instance and can call `RuleFor`, `RuleForEach`, `SetValidator`, etc., exactly as you would in the constructor.

### When to Use It

Use `RuleSwitch` when the object has a **type discriminator** field and different variants of the object require entirely different fields. Typical scenarios:

- A payment form where the required fields depend on the payment method
- A notification entity where `Channel` ("email", "sms", "push") controls which address fields must be present
- A shipment record where `ShippingType` determines whether a home address or a locker code is needed

### Complete Example: Payment Validator

```csharp
public class PaymentDto
{
    public decimal Amount { get; set; }
    public string Method { get; set; } = string.Empty; // "credit_card" | "bank_transfer" | "paypal"

    // credit_card fields
    public string? CardNumber { get; set; }
    public string? Cvv { get; set; }
    public string? CardHolder { get; set; }

    // bank_transfer fields
    public string? Iban { get; set; }
    public string? BankName { get; set; }

    // paypal fields
    public string? PaypalEmail { get; set; }

    // fallback fields
    public string? Reference { get; set; }
}

public class PaymentValidator : AbstractValidator<PaymentDto>
{
    public PaymentValidator()
    {
        // This rule always applies, regardless of payment method
        RuleFor(x => x.Amount).Positive();

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

### Default Case

`.Default(...)` is optional. If omitted and no case matches the discriminator value, **no rules from the switch are applied** — the object passes that portion of validation silently. This is intentional: if you want to reject unknown values, add a rule on the discriminator property itself before the switch:

```csharp
RuleFor(x => x.Method)
    .NotEmpty()
    .In(new[] { "credit_card", "bank_transfer", "paypal" })
        .WithMessage("The payment method is not supported.");

RuleSwitch(x => x.Method)
    .Case("credit_card", rules => { ... })
    .Case("bank_transfer", rules => { ... })
    .Case("paypal", rules => { ... });
    // No Default needed — invalid values are already rejected above
```

### Behavior: Only One Case Executes

`RuleSwitch` evaluates the discriminator once and runs **at most one** case. Cases are not cumulative. If `Method == "credit_card"`, only the `"credit_card"` case is evaluated; the `"bank_transfer"` and `"paypal"` cases are completely skipped.

Rules declared outside of `RuleSwitch` (such as `RuleFor(x => x.Amount).Positive()` above) always run independently.

---

## Validation Methods

### Validate (synchronous)

```csharp
ValidationResult result = validator.Validate(instance);
```

The synchronous method executes only synchronous rules. Rules defined with `MustAsync`, `WhenAsync` or `DependentRuleAsync` are skipped. Use it only when you are certain there are no async rules.

```csharp
var validator = new LoginRequestValidator();
var result = validator.Validate(new LoginRequest { Email = "bad-email", Password = "" });

if (!result.IsValid)
{
    foreach (var error in result.ToFlatList())
        Console.WriteLine(error);
    // Email: The email format is not valid.
    // Password: The password is required.
}
```

### ValidateAsync

```csharp
// Without CancellationToken
ValidationResult result = await validator.ValidateAsync(instance);

// With CancellationToken
ValidationResult result = await validator.ValidateAsync(instance, cancellationToken);
```

Executes all rules, including async ones. This is the recommended method in ASP.NET Core applications.

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

Executes all async rules **in parallel**. Useful when there are multiple `MustAsync` calls that make independent database or external service calls.

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

        // These three calls will execute in parallel with ValidateParallelAsync
        RuleFor(x => x.CustomerId)
            .MustAsync(async id => await _customers.ExistsAsync(id))
                .WithMessage("The customer does not exist.");

        RuleForEach(x => x.Items)
            .MustAsync(async item => await _products.ExistsAsync(item.ProductId))
                .WithMessage("The product does not exist.");

        RuleFor(x => x.CouponCode)
            .MustAsync(async code => await _coupons.IsValidAsync(code))
                .WithMessage("The coupon is not valid.")
            .When(x => x.CouponCode != null);
    }
}

// Usage: ValidateParallelAsync executes all three queries simultaneously
var result = await validator.ValidateParallelAsync(order, ct);
```

> **Warning:** Do not use `ValidateParallelAsync` if rules have side effects or dependencies between them. Use it only when rules are independent and pure (read-only).

### ValidateAndThrow / ValidateAndThrowAsync

```csharp
// Throws ValidationException if validation fails
validator.ValidateAndThrow(instance);
await validator.ValidateAndThrowAsync(instance);
await validator.ValidateAndThrowAsync(instance, cancellationToken);
```

Equivalent to `ValidateAsync` + `IsValid` check + throwing `ValidationException`. Useful in service layers where you prefer to propagate the exception:

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
        // Throws ValidationException with all errors if anything fails
        await _validator.ValidateAndThrowAsync(request, ct);

        // If we get here, the request is valid
        return await _repository.CreateAsync(request, ct);
    }
}
```

See [Exceptions](10-exceptions.md) for more details about `ValidationException`.

---

## IValidator\<T\> — Interface for DI

`IValidator<T>` is the interface registered in the DI container. It has the same methods as `AbstractValidator<T>` except `RuleFor`, `RuleForEach` and `Include` (which are internal to the implementation).

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

Always inject `IValidator<T>` instead of the concrete implementation. This facilitates testing (you can inject a mock) and decouples the consuming code from the specific validator:

```csharp
// Correct: depends on the interface
public class UserController : ControllerBase
{
    private readonly IValidator<CreateUserRequest> _validator;

    public UserController(IValidator<CreateUserRequest> validator)
    {
        _validator = validator;
    }
}

// Incorrect: depends on the concrete implementation
public class UserController : ControllerBase
{
    private readonly CreateUserValidator _validator; // Don't do this
}
```

---

## Global CascadeMode

By default, if a property has multiple rules and the first one fails, all the others are still evaluated. You can change this behavior globally by overriding `GlobalCascadeMode`:

```csharp
public class StrictValidator : AbstractValidator<PaymentRequest>
{
    // Stops evaluation of additional properties after the first global failure
    protected override CascadeMode GlobalCascadeMode => CascadeMode.StopOnFirstFailure;

    public StrictValidator()
    {
        RuleFor(x => x.CardNumber)
            .NotEmpty()
            .CreditCard();

        RuleFor(x => x.ExpiryMonth)
            .GreaterThan(0)
            .LessThanOrEqualTo(12);

        // If CardNumber fails, ExpiryMonth is not evaluated (global StopOnFirstFailure mode)
    }
}
```

See [CascadeMode](08-cascade-mode.md) for the full comparison between per-property and global cascade.

---

## Complete Example: e-commerce

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

        // Cart validations
        RuleFor(x => x.CartItems)
            .NotEmptyCollection()
                .WithMessage("The cart cannot be empty.");

        RuleForEach(x => x.CartItems)
            .Must(item => item.Quantity > 0)
                .WithMessage("The quantity must be greater than 0.")
            .MustAsync(async item =>
                await _productService.IsAvailableAsync(item.ProductId, item.Quantity))
                .WithMessage("The product does not have enough stock.");

        // Address validations
        RuleFor(x => x.ShippingAddress)
            .NotNull()
                .WithMessage("The shipping address is required.");

        RuleFor(x => x.ShippingAddress.Street)
            .NotEmpty()
            .MaximumLength(200)
            .When(x => x.ShippingAddress != null);

        RuleFor(x => x.ShippingAddress.PostalCode)
            .NotEmpty()
            .Matches(@"^\d{5}$")
                .WithMessage("The postal code must have 5 digits.")
            .MustAsync(async postalCode =>
                await _addressService.IsValidPostalCodeAsync(postalCode))
                .WithMessage("The postal code is not valid for shipping.")
            .When(x => x.ShippingAddress != null);

        // Payment validations
        RuleFor(x => x.PaymentMethod)
            .NotNull()
                .WithMessage("The payment method is required.");

        RuleFor(x => x.PaymentMethod.CardNumber)
            .NotEmpty()
            .CreditCard()
                .WithMessage("The card number is not valid.")
            .When(x => x.PaymentMethod?.Type == PaymentType.Card);

        RuleFor(x => x.PaymentMethod.CardExpiry)
            .NotEmpty()
            .Matches(@"^\d{2}/\d{2}$")
                .WithMessage("The expiry date must have MM/YY format.")
            .FutureDate()
                .WithMessage("The card has expired.")
            .When(x => x.PaymentMethod?.Type == PaymentType.Card);
    }
}
```

---

## Next Steps

- **[Basic Rules](05-basic-rules.md)** — Catalog of all available rules
- **[Advanced Rules](06-advanced-rules.md)** — Must, MustAsync, Custom, Transform, SetValidator
- **[CascadeMode](08-cascade-mode.md)** — Validation flow control
