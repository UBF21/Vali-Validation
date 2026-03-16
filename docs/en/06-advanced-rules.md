# Advanced Rules

This document covers rules that go beyond static checks: custom predicates, async logic, cross-property dependencies, transformations and nested validators.

---

## Must

`Must` lets you define any synchronous predicate. It is the escape valve when no predefined rule covers your case.

```csharp
RuleFor(x => x.BirthDate)
    .Must(date => date.Year >= 1900)
        .WithMessage("The birth date cannot be before 1900.")
    .Must(date => DateTime.Today.Year - date.Year >= 18)
        .WithMessage("You must be at least 18 years old.");
```

`Must` can also access the full root object through an overload with two parameters:

```csharp
RuleFor(x => x.EndDate)
    .Must((request, endDate) => endDate > request.StartDate)
        .WithMessage("The end date must be after the start date.");

RuleFor(x => x.MaximumDiscount)
    .Must((request, maxDiscount) => maxDiscount < request.OriginalPrice)
        .WithMessage("The maximum discount cannot exceed the original price.");
```

### Real Example: Date Range Validation

```csharp
public class CreateCampaignValidator : AbstractValidator<CreateCampaignRequest>
{
    public CreateCampaignValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);

        RuleFor(x => x.StartDate)
            .FutureDate()
                .WithMessage("The campaign must start in the future.");

        RuleFor(x => x.EndDate)
            .FutureDate()
                .WithMessage("The campaign must end in the future.")
            .Must((req, endDate) => endDate > req.StartDate)
                .WithMessage("The end date must be after the start date.")
            .Must((req, endDate) => (endDate - req.StartDate).TotalDays <= 365)
                .WithMessage("The campaign duration cannot exceed 365 days.");

        RuleFor(x => x.Budget)
            .GreaterThan(0)
                .WithMessage("The budget must be positive.")
            .Must((req, budget) => budget <= req.MaxBudget)
                .WithMessage("The budget cannot exceed the authorized maximum.")
            .When(x => x.Budget.HasValue);
    }
}
```

---

## MustAsync

`MustAsync` is the async version of `Must`. Use it when validation requires I/O: database queries, external API calls, etc.

### Without CancellationToken

```csharp
RuleFor(x => x.Email)
    .MustAsync(async email =>
    {
        var exists = await _userRepository.ExistsByEmailAsync(email);
        return !exists;
    })
    .WithMessage("A user is already registered with that email.");
```

### With CancellationToken

```csharp
RuleFor(x => x.Email)
    .MustAsync(async (email, ct) =>
    {
        var exists = await _userRepository.ExistsByEmailAsync(email, ct);
        return !exists;
    })
    .WithMessage("A user is already registered with that email.");
```

### With Access to the Root Object

```csharp
RuleFor(x => x.ProductId)
    .MustAsync(async (request, productId, ct) =>
    {
        // Verify that the product belongs to the specified category
        var product = await _productRepository.GetByIdAsync(productId, ct);
        return product?.CategoryId == request.CategoryId;
    })
    .WithMessage("The product does not belong to the specified category.");
```

### Complete Example: User Registration

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
            .WithMessage("That username is already in use.");

        RuleFor(x => x.Email)
            .NotEmpty()
            .Email()
            .MustAsync(async (email, ct) =>
            {
                return !await _users.EmailExistsAsync(email, ct);
            })
            .WithMessage("That email is already registered.")
            .MustAsync(async (email, ct) =>
            {
                return !await _blacklist.IsBlacklistedAsync(email, ct);
            })
            .WithMessage("Registration is not allowed with that email provider.");
    }
}
```

> **Performance:** If you have multiple independent `MustAsync` calls, consider using `ValidateParallelAsync` to execute them in parallel. See [Validators](04-validators.md).

---

## DependentRuleAsync

`DependentRuleAsync` defines an async rule that depends on **two properties** of the same object. It is useful when validating one property depends on the value of another and requires async logic.

```csharp
DependentRuleAsync(
    x => x.ProductId,    // first property
    x => x.WarehouseId,  // second property
    async (productId, warehouseId) =>
    {
        return await _inventory.IsProductAvailableInWarehouseAsync(productId, warehouseId);
    }
)
.WithMessage("The product is not available in the specified warehouse.");
```

### Example: Valid Discount for a Product

```csharp
public class ApplyCouponValidator : AbstractValidator<ApplyCouponRequest>
{
    private readonly ICouponService _couponService;

    public ApplyCouponValidator(ICouponService couponService)
    {
        _couponService = couponService;

        RuleFor(x => x.CouponCode).NotEmpty();
        RuleFor(x => x.ProductId).NotEmpty();

        // The dependent rule validates the coupon + product combination
        DependentRuleAsync(
            x => x.CouponCode,
            x => x.ProductId,
            async (couponCode, productId) =>
            {
                return await _couponService.IsValidForProductAsync(couponCode, productId);
            }
        )
        .WithMessage("The coupon is not valid for this product.");
    }
}
```

---

## Custom

`Custom` gives full control over validation. You receive the property value and a `CustomContext<T>` that allows you to add errors in a granular way, including errors for other properties or with error codes.

### CustomContext\<T\> Interface

```csharp
// ctx.Instance — the full T object
// ctx.AddFailure(message) — adds an error to the current property
// ctx.AddFailure(property, message) — adds an error to a specific property
// ctx.AddFailure(property, message, errorCode) — with error code
```

### Example: Complex Cross-Property Validation

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
                // Access the full request via ctx.Instance
                var request = ctx.Instance;

                var sourceAccount = await _accounts.GetByIdAsync(sourceId);
                if (sourceAccount == null)
                {
                    ctx.AddFailure("SourceAccountId", "The source account does not exist.", "ACCOUNT_NOT_FOUND");
                    return;
                }

                if (sourceAccount.OwnerId != request.RequestingUserId)
                {
                    ctx.AddFailure("SourceAccountId", "You do not have permissions over the source account.", "UNAUTHORIZED");
                    return;
                }

                if (sourceAccount.Balance < request.Amount)
                {
                    // Adds the error to Amount, not to SourceAccountId
                    ctx.AddFailure("Amount", $"Insufficient balance. Available: {sourceAccount.Balance:C}.", "INSUFFICIENT_FUNDS");
                }

                if (!sourceAccount.AllowsTransfers)
                {
                    ctx.AddFailure("SourceAccountId", "This account does not allow transfers.", "TRANSFERS_DISABLED");
                }
            });
    }
}
```

### Example: Business Hours Validation

```csharp
RuleFor(x => x.ScheduledTime)
    .Custom((scheduledTime, ctx) =>
    {
        var request = ctx.Instance;

        if (scheduledTime.DayOfWeek == DayOfWeek.Saturday ||
            scheduledTime.DayOfWeek == DayOfWeek.Sunday)
        {
            ctx.AddFailure("ScheduledTime",
                "Tasks cannot be scheduled on weekends.",
                "WEEKEND_NOT_ALLOWED");
            return;
        }

        if (scheduledTime.Hour < 8 || scheduledTime.Hour >= 18)
        {
            ctx.AddFailure("ScheduledTime",
                "Tasks can only be scheduled between 8:00 and 18:00.",
                "OUTSIDE_BUSINESS_HOURS");
        }

        if (request.Priority == TaskPriority.Low && scheduledTime.Hour < 10)
        {
            ctx.AddFailure("ScheduledTime",
                "Low-priority tasks cannot be scheduled before 10:00.",
                "LOW_PRIORITY_TOO_EARLY");
        }
    });
```

---

## SwitchOn — Conditional Rules by Value on a Property

`SwitchOn` lets you apply **different rules to the same property** depending on the value of another property. It is chained directly on a `RuleFor` builder and replaces the pattern of writing separate `RuleFor(...).When(...)` blocks for every possible variant.

### Syntax

```csharp
RuleFor(x => x.TargetProperty)
    .SwitchOn(x => x.DiscriminatorProperty)
    .Case(value1, b => b.Rule1().Rule2())
    .Case(value2, b => b.Rule3())
    .Default(     b => b.FallbackRule());
```

`ISwitchOnBuilder<T, TProperty, TKey>` interface:

```csharp
ISwitchOnBuilder<T, TProperty, TKey> Case(TKey value, Action<IRuleBuilder<T, TProperty>> configure)
ISwitchOnBuilder<T, TProperty, TKey> Default(Action<IRuleBuilder<T, TProperty>> configure)
```

Each action receives the **same `IRuleBuilder` for the property**, so you can chain any combination of rules inside a case.

### When to Use It

Use `SwitchOn` when the **format or constraints of a single field** depend on the value of another field. Examples:

- A document number whose format differs by document type (passport, DNI, RUC)
- A measurement value whose precision and range differ by unit
- A configuration value whose data type differs by setting key

The key distinction from `RuleSwitch`: `SwitchOn` is about one property with multiple formats, while `RuleSwitch` is about multiple properties that are active for one variant.

### Complete Example: Document Validator

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
                .WithMessage("The passport number must have the format AA999999."))
            .Case("dni",      b => b.NotEmpty().IsNumeric().MinimumLength(8).MaximumLength(8)
                .WithMessage("The DNI must be exactly 8 digits."))
            .Case("ruc",      b => b.NotEmpty().IsNumeric().MinimumLength(11).MaximumLength(11)
                .WithMessage("The RUC must be exactly 11 digits."))
            .Default(         b => b.NotEmpty());
    }
}
```

### Additional Example: Measurement Value by Unit

Different measurement units impose different constraints on the value field:

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
                    .WithMessage("The weight cannot be negative.")
                .MaxDecimalPlaces(3)
                    .WithMessage("Weight values support at most 3 decimal places."))
            .Case("percentage", b => b
                .NotEmpty()
                .Transform(v => decimal.TryParse(v, out var d) ? d : -1m)
                .Percentage()
                    .WithMessage("The percentage must be between 0 and 100."))
            .Case("text", b => b
                .NotEmpty()
                .MaximumLength(500)
                    .WithMessage("Text values cannot exceed 500 characters."))
            .Default(b => b.NotEmpty());
    }
}
```

### Difference from When/Unless

| | `SwitchOn` | `When` / `Unless` |
|---|---|---|
| Exclusivity | Only **one** case runs | Each `When` block is evaluated **independently** |
| Multiple variants | One construct covers all variants | Requires one `RuleFor(...).When(...)` per variant |
| Overlap | Impossible by design | Possible if conditions are not mutually exclusive |

Use `SwitchOn` when the variants are **mutually exclusive** and exhaustive. Use `When`/`Unless` when you need independent guards that may overlap or when the condition is more complex than a simple equality check.

---

## Transform

`Transform` converts the property value before applying rules. It returns a new `IRuleBuilder<T, TNew>` for subsequent rules.

### Use Case: Normalize Before Validating

```csharp
public class SearchProductsValidator : AbstractValidator<SearchProductsRequest>
{
    public SearchProductsValidator()
    {
        // Transforms the string to lowercase before validating length and content
        RuleFor(x => x.SearchTerm)
            .Transform(term => term?.Trim().ToLowerInvariant())
            .MinimumLength(2)
                .WithMessage("The search term must have at least 2 characters.")
            .MaximumLength(100)
            .When(x => x.SearchTerm != null);

        // Transforms the date to UTC before validating it is in the future
        RuleFor(x => x.FromDate)
            .Transform(date => date.ToUniversalTime())
            .FutureDate()
            .When(x => x.FromDate != default);
    }
}
```

### Use Case: Extract Part of the Value

```csharp
public class DocumentValidator : AbstractValidator<DocumentRequest>
{
    public DocumentValidator()
    {
        // Validates only the file name extension
        RuleFor(x => x.FileName)
            .Transform(name => Path.GetExtension(name).ToLowerInvariant())
            .In(new[] { ".pdf", ".docx", ".xlsx", ".png", ".jpg" })
                .WithMessage("The file format is not allowed.");

        // Validates only the email domain
        RuleFor(x => x.Email)
            .Transform(email => email.Split('@').LastOrDefault() ?? "")
            .NotIn(new[] { "mailinator.com", "guerrillamail.com", "tempmail.com" })
                .WithMessage("Temporary email domains are not accepted.");
    }
}
```

### Use Case: Type Conversion

```csharp
public class ParsedNumberValidator : AbstractValidator<ParseNumberRequest>
{
    public ParsedNumberValidator()
    {
        // Converts the string to decimal and validates the range
        RuleFor(x => x.AmountString)
            .Transform(s => decimal.TryParse(s, out var result) ? result : -1m)
            .GreaterThan(0m)
                .WithMessage("The amount must be a positive number.")
            .MaxDecimalPlaces(2)
                .WithMessage("The amount cannot have more than 2 decimal places.");
    }
}
```

---

## SetValidator

`SetValidator` delegates validation of a complex property to another validator. Errors from the nested validator are added with the property name as a prefix.

### Basic Usage

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

        // Address errors appear as "ShippingAddress.Street", etc.
        RuleFor(x => x.ShippingAddress)
            .NotNull()
                .WithMessage("The shipping address is required.")
            .SetValidator(new AddressValidator());

        RuleFor(x => x.BillingAddress)
            .SetValidator(new AddressValidator())
            .When(x => x.BillingAddress != null);
    }
}
```

If `ShippingAddress.Street` is empty, the result will contain:

```json
{
  "ShippingAddress.Street": ["The Street field cannot be empty."]
}
```

### SetValidator with Dependencies

If the nested validator has dependencies, you can resolve it from the DI container:

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

### SetValidator with Collections (RuleForEach)

Combine `RuleForEach` with `SetValidator` to validate lists of complex objects:

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
                .WithMessage("The discount must be between 0% and 100%.")
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
                .WithMessage("The order must have at least one line.");

        // Validates each line with OrderLineValidator
        // Errors: "Lines[0].ProductId", "Lines[1].UnitPrice", etc.
        RuleForEach(x => x.Lines)
            .SetValidator(new OrderLineValidator());
    }
}
```

---

## Advanced RuleForEach

`RuleForEach` supports most modifiers available in `RuleFor`. Here are some advanced patterns:

### With Conditional When

```csharp
// Only validates digital items
RuleForEach(x => x.Items)
    .Must(item => !string.IsNullOrEmpty(item.DownloadUrl))
        .WithMessage("Digital products must have a download URL.")
    .When(x => x.DeliveryType == DeliveryType.Digital);
```

### With MustAsync and Access to Root Object

```csharp
RuleForEach(x => x.ProductIds)
    .MustAsync(async (request, productId, ct) =>
    {
        return await _products.BelongsToCatalogAsync(productId, request.CatalogId, ct);
    })
    .WithMessage("The product does not belong to the selected catalog.");
```

### With StopOnFirstFailure Per Element

```csharp
RuleForEach(x => x.Recipients)
    .NotEmpty()
        .WithMessage("The recipient cannot be empty.")
    .Email()
        .WithMessage("The recipient must be a valid email.")
    .StopOnFirstFailure(); // Stops validating the element after the first error
```

---

## Combining Advanced Rules

### Complex e-commerce Validator

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

        // Customer
        RuleFor(x => x.CustomerId)
            .NotEmpty()
            .MustAsync(async (id, ct) => await _customers.ExistsAsync(id, ct))
                .WithMessage("The customer does not exist.");

        // Order lines
        RuleFor(x => x.Items)
            .NotEmptyCollection();

        RuleForEach(x => x.Items)
            .SetValidator(new OrderItemValidator(_products));

        // Address
        RuleFor(x => x.ShippingAddress)
            .NotNull()
            .SetValidator(new AddressValidator());

        // Coupon (optional)
        RuleFor(x => x.CouponCode)
            .MustAsync(async (req, code, ct) =>
            {
                var coupon = await _coupons.GetByCodeAsync(code, ct);
                if (coupon == null) return false;
                if (coupon.ExpiresAt < DateTime.UtcNow) return false;
                return coupon.MinimumOrderAmount <= req.Items.Sum(i => i.TotalPrice);
            })
            .WithMessage("The coupon is not valid or does not apply to the order amount.")
            .When(x => !string.IsNullOrEmpty(x.CouponCode));

        // Custom rule for cross-validation
        RuleFor(x => x.PaymentMethod)
            .Custom((payment, ctx) =>
            {
                var request = ctx.Instance;
                if (payment.Type == PaymentType.BankTransfer && request.Items.Count > 50)
                {
                    ctx.AddFailure("PaymentMethod",
                        "Orders with more than 50 items cannot be placed with bank transfer.",
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
                .WithMessage("The maximum quantity per item is 1000.");

        RuleFor(x => x.ProductId)
            .MustAsync(async (item, productId, ct) =>
            {
                return await _products.IsAvailableAsync(productId, item.Quantity, ct);
            })
            .WithMessage("The product does not have enough stock.");
    }
}
```

---

## Cross-Property Rules

These rules compare a property against **another property on the same object** rather than a fixed constant. They eliminate boilerplate `Must((obj, val) => ...)` calls for the most common ordering and inequality checks.

### `GreaterThanProperty(Expression<Func<T, TProperty>> otherExpression)`

Verifies that the value is strictly greater than the value of another property.

```csharp
public class CreateBookingValidator : AbstractValidator<CreateBookingRequest>
{
    public CreateBookingValidator()
    {
        RuleFor(x => x.StartDate).FutureDate();

        RuleFor(x => x.EndDate)
            .GreaterThanProperty(x => x.StartDate)
                .WithMessage("The check-out date must be after the check-in date.");
    }
}
```

### `GreaterThanOrEqualToProperty(Expression<Func<T, TProperty>> otherExpression)`

Verifies that the value is greater than or equal to another property.

```csharp
RuleFor(x => x.MaxPrice)
    .GreaterThanOrEqualToProperty(x => x.MinPrice)
        .WithMessage("The maximum price must be greater than or equal to the minimum price.");
```

### `LessThanProperty(Expression<Func<T, TProperty>> otherExpression)`

Verifies that the value is strictly less than another property.

```csharp
RuleFor(x => x.StartDate)
    .LessThanProperty(x => x.EndDate)
        .WithMessage("The start date must be before the end date.");

RuleFor(x => x.DiscountPrice)
    .LessThanProperty(x => x.OriginalPrice)
        .WithMessage("The discounted price must be lower than the original price.");
```

### `LessThanOrEqualToProperty(Expression<Func<T, TProperty>> otherExpression)`

Verifies that the value is less than or equal to another property.

```csharp
RuleFor(x => x.ActualCost)
    .LessThanOrEqualToProperty(x => x.BudgetCap)
        .WithMessage("The actual cost cannot exceed the budget cap.");
```

### `NotEqualToProperty(Expression<Func<T, TProperty>> otherExpression)`

Verifies that the value is different from another property. The canonical use case is ensuring a new password does not repeat the old one.

```csharp
public class ChangePasswordValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(8)
            .NotEqualToProperty(x => x.OldPassword)
                .WithMessage("The new password must be different from the current password.");

        RuleFor(x => x.ConfirmPassword)
            .EqualToProperty(x => x.NewPassword)
                .WithMessage("The passwords do not match.");
    }
}
```

### `MultipleOfProperty(Expression<Func<T, TProperty>> otherExpression)`

Verifies that the value is a multiple of another property. See also [Basic Rules — `MultipleOfProperty`](05-basic-rules.md) for the full description. Listed here as a cross-property rule for discoverability.

```csharp
public class TradeOrderValidator : AbstractValidator<TradeOrderRequest>
{
    public TradeOrderValidator()
    {
        // The order quantity must be a multiple of the minimum lot size
        // which can vary per instrument and comes from the same request
        RuleFor(x => x.Quantity)
            .Positive()
            .MultipleOfProperty(x => x.MinLotSize)
                .WithMessage("The quantity must be a multiple of the minimum lot size.");
    }
}
```

### Real Example: Booking Form with Multiple Cross-Property Rules

```csharp
public class CreateCampaignValidator : AbstractValidator<CreateCampaignRequest>
{
    public CreateCampaignValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);

        RuleFor(x => x.StartDate).FutureDate();

        RuleFor(x => x.EndDate)
            .FutureDate()
            .GreaterThanProperty(x => x.StartDate)
                .WithMessage("The campaign end date must be after the start date.");

        RuleFor(x => x.MaxBudget).Positive();

        RuleFor(x => x.InitialBudget)
            .Positive()
            .LessThanOrEqualToProperty(x => x.MaxBudget)
                .WithMessage("The initial budget cannot exceed the maximum budget.");
    }
}
```

---

## Conditional Required

Sometimes a field is optional by default but becomes mandatory depending on the state of the rest of the object. `RequiredIf` and `RequiredUnless` express this clearly without having to write `Must((obj, val) => ...)` combinations.

### `RequiredIf(Func<T, bool> condition)`

The field is required (not null and not empty) when the provided condition evaluates to `true`.

```csharp
public class CreateCompanyUserValidator : AbstractValidator<CreateUserRequest>
{
    public CreateCompanyUserValidator()
    {
        RuleFor(x => x.Email).NotEmpty().Email();

        // Company name is required only when the customer is a company
        RuleFor(x => x.CompanyName)
            .RequiredIf(x => x.IsCompany)
                .WithMessage("The company name is required for company accounts.");

        // Tax ID is also mandatory for companies
        RuleFor(x => x.TaxId)
            .RequiredIf(x => x.IsCompany)
                .WithMessage("The tax ID is required for company accounts.");
    }
}
```

### `RequiredIf<TOther>(Expression<Func<T, TOther>> otherProperty, TOther expectedValue)`

The field is required when another specific property equals a particular value. This overload is more readable than the lambda form when the condition is a simple equality check.

```csharp
public class CheckoutValidator : AbstractValidator<CheckoutRequest>
{
    public CheckoutValidator()
    {
        RuleFor(x => x.DeliveryMethod)
            .NotEmpty()
            .In(new[] { "pickup", "delivery" });

        // Shipping address is only required when the customer chooses delivery
        RuleFor(x => x.ShippingAddress)
            .RequiredIf(x => x.DeliveryMethod, "delivery")
                .WithMessage("A shipping address is required for home delivery.");

        // Pickup location ID is only required when the customer chooses in-store pickup
        RuleFor(x => x.PickupLocationId)
            .RequiredIf(x => x.DeliveryMethod, "pickup")
                .WithMessage("Please select a pickup location.");
    }
}
```

### `RequiredUnless(Func<T, bool> condition)`

The field is required unless the condition evaluates to `true`. Semantically equivalent to `RequiredIf(x => !condition(x))` but reads more naturally when the exemption is the exceptional case.

```csharp
public class InvoiceValidator : AbstractValidator<InvoiceRequest>
{
    public InvoiceValidator()
    {
        // VAT number is required unless the customer is a private individual
        RuleFor(x => x.VatNumber)
            .RequiredUnless(x => x.CustomerType == "individual")
                .WithMessage("A VAT number is required for business invoices.");

        // Reason is required unless the order total is below the review threshold
        RuleFor(x => x.ApprovalReason)
            .RequiredUnless(x => x.OrderTotal < 10000m)
                .WithMessage("An approval reason is required for orders over 10,000.");
    }
}
```

### Real Example: Insurance Form

```csharp
public class InsuranceApplicationValidator : AbstractValidator<InsuranceApplicationRequest>
{
    public InsuranceApplicationValidator()
    {
        RuleFor(x => x.PolicyType)
            .NotEmpty()
            .In(new[] { "personal", "commercial" });

        // Business name only required for commercial policies
        RuleFor(x => x.BusinessName)
            .RequiredIf(x => x.PolicyType, "commercial")
                .WithMessage("The business name is required for commercial policies.");

        // Annual revenue only required for commercial policies
        RuleFor(x => x.AnnualRevenue)
            .RequiredIf(x => x.PolicyType, "commercial")
            .GreaterThan(0m)
                .WithMessage("Annual revenue is required for commercial policies.")
            .When(x => x.PolicyType == "commercial");

        // A co-applicant's details are required unless filing alone
        RuleFor(x => x.CoApplicantName)
            .RequiredUnless(x => !x.HasCoApplicant)
                .WithMessage("The co-applicant name is required.");

        RuleFor(x => x.CoApplicantBirthDate)
            .RequiredUnless(x => !x.HasCoApplicant)
            .MinAge(18)
                .WithMessage("The co-applicant must be at least 18 years old.")
            .When(x => x.HasCoApplicant);
    }
}
```

---

## Next Steps

- **[Modifiers](07-modifiers.md)** — WithMessage, WithErrorCode, When/Unless, OverridePropertyName
- **[Validation Result](09-validation-result.md)** — How to work with ValidationResult and ErrorCodes
- **[Advanced Patterns](15-advanced-patterns.md)** — Validator composition, inheritance, complex cases
