# Rule Modifiers

Modifiers are chained after a rule to change its behavior or error message. They apply to the **last rule** defined in the chain.

---

## WithMessage

`WithMessage` replaces the default error message of the last rule.

```csharp
RuleFor(x => x.Email)
    .NotEmpty()
        .WithMessage("The email is required.")
    .Email()
        .WithMessage("The email format is not valid.");
```

### Placeholders

`WithMessage` supports two placeholders that are substituted at runtime:

| Placeholder | Value |
|---|---|
| `{PropertyName}` | Property name (or the value of `OverridePropertyName`) |
| `{PropertyValue}` | Current value of the property |

```csharp
RuleFor(x => x.Username)
    .MaximumLength(50)
        .WithMessage("The field {PropertyName} cannot exceed 50 characters. Current value: '{PropertyValue}'.");
```

If the username is `"this_username_is_way_too_long"`, the error will be:

```
The field Username cannot exceed 50 characters. Current value: 'this_username_is_way_too_long'.
```

### Example with Multiple Messages

```csharp
public class CreateProductValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
                .WithMessage("The product name is required.")
            .MinimumLength(3)
                .WithMessage("The name '{PropertyValue}' is too short (minimum 3 characters).")
            .MaximumLength(200)
                .WithMessage("The name cannot exceed 200 characters.");

        RuleFor(x => x.Price)
            .GreaterThan(0m)
                .WithMessage("The price must be greater than 0. Value received: {PropertyValue}.")
            .MaxDecimalPlaces(2)
                .WithMessage("The price cannot have more than 2 decimal places.");

        RuleFor(x => x.Stock)
            .GreaterThanOrEqualTo(0)
                .WithMessage("The field {PropertyName} cannot be negative.");
    }
}
```

### Anti-pattern: Generic Message

```csharp
// Incorrect: a single WithMessage for all rules
RuleFor(x => x.Email)
    .NotEmpty()
    .Email()
    .MaximumLength(320)
    .WithMessage("Invalid email."); // Only applies to MaximumLength, not all previous rules
```

`WithMessage` always applies to the **last rule** before it. For per-rule messages, place `WithMessage` immediately after each rule.

---

## WithErrorCode

`WithErrorCode` assigns an error code to the last rule. Codes appear in `ValidationResult.ErrorCodes`, which is useful for structured API responses or for client-side localization.

```csharp
RuleFor(x => x.Email)
    .NotEmpty()
        .WithErrorCode("EMAIL_REQUIRED")
    .Email()
        .WithErrorCode("EMAIL_INVALID_FORMAT")
    .MustAsync(async (email, ct) => !await _users.ExistsByEmailAsync(email, ct))
        .WithErrorCode("EMAIL_ALREADY_EXISTS")
        .WithMessage("An account with that email already exists.");
```

You can combine `WithMessage` and `WithErrorCode` in any order:

```csharp
RuleFor(x => x.Age)
    .GreaterThanOrEqualTo(18)
        .WithMessage("You must be at least 18 years old.")
        .WithErrorCode("AGE_BELOW_MINIMUM");
```

### Usage in the API Response

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

Response:

```json
{
  "errors": {
    "Email": ["An account with that email already exists."]
  },
  "errorCodes": {
    "Email": ["EMAIL_ALREADY_EXISTS"]
  }
}
```

### Recommended Standard Codes

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
                .WithMessage("The product does not exist.");
    }
}
```

---

## OverridePropertyName

`OverridePropertyName` changes the key that appears in `ValidationResult.Errors`. Useful when the C# property name is not client-friendly.

```csharp
public class UpdateUserDtoValidator : AbstractValidator<UpdateUserDto>
{
    public UpdateUserDtoValidator()
    {
        // In the DTO the property is called Email, but the client expects "emailAddress"
        RuleFor(x => x.Email)
            .NotEmpty()
            .Email()
            .OverridePropertyName("emailAddress");

        // Nested property — change the full name
        RuleFor(x => x.Address.PostalCode)
            .Matches(@"^\d{5}$")
            .OverridePropertyName("postalCode"); // Instead of "Address.PostalCode"
    }
}
```

Result with `OverridePropertyName`:

```json
{
  "emailAddress": ["The email field is not valid."],
  "postalCode": ["The postal code must have 5 digits."]
}
```

### Complex Expressions

If you use `RuleFor` with expressions that are not simple member access (e.g. `x => x.Name.ToLower()`), the extracted property name may be incorrect. Use `OverridePropertyName` to fix this:

```csharp
RuleFor(x => x.Name.Trim())
    .NotEmpty()
    .OverridePropertyName("Name"); // Without this, the name could be incorrect
```

---

## StopOnFirstFailure

`StopOnFirstFailure` stops evaluation of that property's rules as soon as one fails. Subsequent rules are not evaluated.

```csharp
RuleFor(x => x.Email)
    .NotNull()
        .WithMessage("The email is required.")
    .NotEmpty()
        .WithMessage("The email cannot be empty.")
    .Email()
        .WithMessage("The email does not have a valid format.")
    .MustAsync(async email => !await _users.ExistsByEmailAsync(email))
        .WithMessage("That email is already registered.")
    .StopOnFirstFailure(); // If NotNull fails, the following rules are not evaluated
```

Without `StopOnFirstFailure`, if `Email` is `null`:
- Fails `NotNull` → "The email is required."
- Fails `NotEmpty` → "The email cannot be empty."
- Fails `Email` → "The email does not have a valid format."
- `MustAsync` is called with `null` → possible exception

With `StopOnFirstFailure`, if `Email` is `null`:
- Fails `NotNull` → "The email is required."
- The remaining rules are not evaluated

> This is the per-property modifier. To stop all properties after the first failure, use the global `CascadeMode`. See [CascadeMode](08-cascade-mode.md).

### When It Is Essential

`StopOnFirstFailure` is especially important when:

1. Later rules assume earlier ones passed (e.g. `Email()` assumes the string is not null)
2. Async rules are expensive and you don't want to make unnecessary DB calls
3. Accumulated error messages would be confusing for the user (e.g. "is null" AND "format is invalid")

```csharp
RuleFor(x => x.ProductId)
    .NotEmpty()
        .WithMessage("The product ID is required.")
    .MustAsync(async id => await _products.ExistsAsync(id))
        .WithMessage("The product does not exist.")
    .MustAsync(async id => await _products.IsActiveAsync(id))
        .WithMessage("The product is not active.")
    .StopOnFirstFailure(); // Avoids calling the DB if the ID is empty
```

---

## When

`When` applies the preceding rules in the chain **only if the condition is true**. If the condition is false, the rules are skipped without generating errors.

```csharp
RuleFor(x => x.VatNumber)
    .NotEmpty()
        .WithMessage("The VAT number is required for companies.")
    .Matches(@"^[A-Z]{2}[A-Z0-9]{9}$")
        .WithMessage("The VAT number does not have the correct format.")
    .When(x => x.CustomerType == CustomerType.Company);
```

### When Applies to ALL Previous Rules in the Builder

`When` applies to all rules defined in that `RuleFor`. In the example above, both `NotEmpty()` and `Matches()` are only evaluated if `CustomerType == Company`.

If you want `When` to apply only to a specific rule, place it after that rule and before the next one:

```csharp
// When only applies to Matches, not to NotEmpty
RuleFor(x => x.VatNumber)
    .NotEmpty()
        .WithMessage("The VAT number is required.");  // Always evaluated

// Separate builder for the conditional rule
RuleFor(x => x.VatNumber)
    .Matches(@"^[A-Z]{2}[A-Z0-9]{9}$")
        .WithMessage("The VAT number format is incorrect.")
    .When(x => x.CustomerType == CustomerType.Company);
```

### Practical Examples

```csharp
public class CreateListingValidator : AbstractValidator<CreateListingRequest>
{
    public CreateListingValidator()
    {
        // Basic fields (always)
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Price).GreaterThan(0);

        // Auction fields (only for auctions)
        RuleFor(x => x.AuctionEndDate)
            .NotNull()
                .WithMessage("The auction must have a closing date.")
            .FutureDate()
                .WithMessage("The closing date must be in the future.")
            .When(x => x.ListingType == ListingType.Auction);

        // Reserve price (only in auctions with reserve price)
        RuleFor(x => x.ReservePrice)
            .GreaterThan(0)
            .Must((req, reserve) => reserve < req.Price)
                .WithMessage("The reserve price must be less than the starting price.")
            .When(x => x.ListingType == ListingType.Auction && x.HasReservePrice);

        // Discount (only if there is an active discount)
        RuleFor(x => x.DiscountPercentage)
            .Between(1m, 90m)
                .WithMessage("The discount must be between 1% and 90%.")
            .When(x => x.HasDiscount);

        // Weight and dimensions (only for physical products)
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

`Unless` is the negation of `When`. It applies rules if the condition is **false**.

```csharp
RuleFor(x => x.AlternativeEmail)
    .NotEmpty()
        .WithMessage("If you have no phone number, the alternative email is required.")
    .Email()
    .Unless(x => !string.IsNullOrEmpty(x.PhoneNumber));

// Equivalent with When:
RuleFor(x => x.AlternativeEmail)
    .NotEmpty()
    .Email()
    .When(x => string.IsNullOrEmpty(x.PhoneNumber));
```

### Example: Optional Fields with Restrictions

```csharp
public class UpdateProfileValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileValidator()
    {
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(100);

        // Bio is optional, but if provided must have real content
        RuleFor(x => x.Bio)
            .NotEmpty()
                .WithMessage("If you include a bio, it cannot be empty.")
            .MaximumLength(500)
            .Unless(x => x.Bio == null); // Only validate if not null

        // Website is optional, but if provided must be a valid URL
        RuleFor(x => x.Website)
            .Url()
                .WithMessage("The website URL is not valid.")
            .Unless(x => x.Website == null);

        // Phone is only required if there is no other contact method
        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
                .WithMessage("You need at least one contact method (email or phone).")
            .Unless(x => !string.IsNullOrEmpty(x.AlternativeEmail));
    }
}
```

---

## WhenAsync

`WhenAsync` is the async version of `When`. It allows conditions that require I/O.

```csharp
RuleFor(x => x.NewEmail)
    .NotEmpty()
    .Email()
    .MustAsync(async (email, ct) => !await _users.ExistsByEmailAsync(email, ct))
        .WithMessage("That email is already in use.")
    .WhenAsync(async (request, ct) =>
    {
        // Only validate the new email if the user wants to change it
        var currentUser = await _users.GetByIdAsync(request.UserId, ct);
        return currentUser?.Email != request.NewEmail;
    });
```

> **Note:** `WhenAsync` only works when `ValidateAsync` is called. If `Validate` (synchronous) is called, rules with `WhenAsync` are skipped.

### Example: Conditional Permissions

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
                .WithMessage("The role does not exist.");

        // Only superadmins can assign admin roles
        RuleFor(x => x.RoleId)
            .MustAsync(async (req, roleId, ct) =>
            {
                var role = await _roles.GetByIdAsync(roleId, ct);
                return !role.IsAdminRole;
            })
            .WithMessage("Only superadmins can assign admin roles.")
            .WhenAsync(async (req, ct) =>
            {
                return !await _permissions.IsSuperAdminAsync(req.AssignedBy, ct);
            });
    }
}
```

---

## UnlessAsync

`UnlessAsync` is the async negation of `WhenAsync`.

```csharp
RuleFor(x => x.Price)
    .GreaterThan(0)
    .UnlessAsync(async (request, ct) =>
    {
        // The price can be 0 if the product is in a free catalog
        return await _catalogs.IsFreeAsync(request.CatalogId, ct);
    });
```

---

## Combining Modifiers

Modifiers can be combined. The usual order is:

```
.RuleA()
    .WithMessage("message")
    .WithErrorCode("CODE")
.RuleB()
    .WithMessage("other message")
.OverridePropertyName("alternativeName")
.When(condition)
.StopOnFirstFailure()
```

> `OverridePropertyName`, `When`/`Unless`, and `StopOnFirstFailure` apply to the entire builder, not to the last rule. `WithMessage` and `WithErrorCode` apply to the last rule.

### Complete Example with All Modifiers

```csharp
public class PaymentRequestValidator : AbstractValidator<PaymentRequest>
{
    private readonly ICurrencyService _currency;

    public PaymentRequestValidator(ICurrencyService currency)
    {
        _currency = currency;

        RuleFor(x => x.Amount)
            .GreaterThan(0m)
                .WithMessage("The amount must be greater than 0.")
                .WithErrorCode("AMOUNT_MUST_BE_POSITIVE")
            .LessThanOrEqualTo(999999.99m)
                .WithMessage("The maximum amount per transaction is 999,999.99.")
                .WithErrorCode("AMOUNT_EXCEEDS_LIMIT")
            .MaxDecimalPlaces(2)
                .WithMessage("The amount cannot have more than 2 decimal places.")
            .StopOnFirstFailure();

        RuleFor(x => x.Currency)
            .NotEmpty()
                .WithErrorCode("CURRENCY_REQUIRED")
            .Uppercase()
                .WithMessage("The currency code must be uppercase (e.g.: EUR, USD).")
            .MustAsync(async (currency, ct) =>
                await _currency.IsSupportedAsync(currency, ct))
                .WithMessage("The currency '{PropertyValue}' is not supported.")
                .WithErrorCode("CURRENCY_NOT_SUPPORTED")
            .OverridePropertyName("currencyCode");

        RuleFor(x => x.CardNumber)
            .NotEmpty()
                .WithMessage("The card number is required.")
            .CreditCard()
                .WithMessage("The card number is not valid (Luhn checksum fails).")
                .WithErrorCode("INVALID_CARD_NUMBER")
            .StopOnFirstFailure()
            .When(x => x.PaymentMethod == PaymentMethod.Card);

        RuleFor(x => x.BankAccount)
            .NotEmpty()
                .WithMessage("The IBAN is required for bank transfers.")
            .Matches(@"^[A-Z]{2}\d{2}[A-Z0-9]+$")
                .WithMessage("The IBAN has an invalid format.")
                .WithErrorCode("INVALID_IBAN")
            .When(x => x.PaymentMethod == PaymentMethod.BankTransfer);
    }
}
```

---

## Quick Summary

| Modifier | Applies to | Effect |
|---|---|---|
| `WithMessage(msg)` | Last rule | Replaces the error message |
| `WithErrorCode(code)` | Last rule | Adds code to the result |
| `OverridePropertyName(name)` | Entire builder | Changes the key in Errors |
| `StopOnFirstFailure()` | Entire builder | Stops on first failure per property |
| `When(condition)` | Entire builder | Applies only if condition is true |
| `Unless(condition)` | Entire builder | Applies only if condition is false |
| `WhenAsync(condition)` | Entire builder | Same as When but async |
| `UnlessAsync(condition)` | Entire builder | Same as Unless but async |

## Next Steps

- **[CascadeMode](08-cascade-mode.md)** — Stop evaluation at the entire validator level
- **[Validation Result](09-validation-result.md)** — How to use ErrorCodes and the rest of ValidationResult
