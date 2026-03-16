# CascadeMode

`CascadeMode` controls what happens when a rule fails: whether the remaining rules are still evaluated or evaluation is stopped. There are two levels of control:

1. **Per property** — using `.StopOnFirstFailure()` in the builder
2. **Global (entire validator)** — overriding `GlobalCascadeMode`

---

## The Two Modes

```csharp
public enum CascadeMode
{
    Continue,          // Default: evaluates all rules even if some fail
    StopOnFirstFailure // Stops on first failure per property or per validator
}
```

---

## Per-Property CascadeMode

### StopOnFirstFailure Per Property

When you use `.StopOnFirstFailure()` in a builder, evaluation of that property stops after the first failure. Other properties in the validator **are still evaluated**.

```csharp
public class UserRegistrationValidator : AbstractValidator<UserRegistrationRequest>
{
    private readonly IUserRepository _users;

    public UserRegistrationValidator(IUserRepository users)
    {
        _users = users;

        RuleFor(x => x.Email)
            .NotEmpty()
                .WithMessage("The email is required.")         // Rule 1
            .Email()
                .WithMessage("The email does not have a valid format.") // Rule 2
            .MustAsync(async (email, ct) =>
                !await _users.EmailExistsAsync(email, ct))
                .WithMessage("That email is already registered.")    // Rule 3
            .StopOnFirstFailure();

        // Email and Password are evaluated independently:
        // If Email fails on rule 1, rules 2 and 3 for Email are not evaluated.
        // But Password IS evaluated normally.
        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8);
    }
}
```

### Why It Is Important

Without `StopOnFirstFailure`, if email is `null`:

```
Email: The email is required.
Email: The email does not have a valid format.
Email: <MustAsync throws NullReferenceException because email is null>
```

With `StopOnFirstFailure`, if email is `null`:

```
Email: The email is required.
```

Only the first error. The following rules do not execute, which is safer and more useful for the user.

### Real Example: Credit Card Validation

```csharp
public class PaymentValidator : AbstractValidator<PaymentRequest>
{
    public PaymentValidator()
    {
        // Without StopOnFirstFailure:
        // If CardNumber is null → "is required", "is invalid (Luhn)", etc.
        // With StopOnFirstFailure:
        // If CardNumber is null → only "is required"
        RuleFor(x => x.CardNumber)
            .NotEmpty()
                .WithMessage("The card number is required.")
            .NoWhitespace()
                .WithMessage("The card number must not contain spaces.")
            .IsNumeric()
                .WithMessage("The card number can only contain digits.")
            .LengthBetween(13, 19)
                .WithMessage("The card number must have between 13 and 19 digits.")
            .CreditCard()
                .WithMessage("The card number is not valid.")
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

## Global CascadeMode

The global mode applies to **the entire validator**. When `StopOnFirstFailure` is activated globally:

- If the first **property** evaluated has an error, subsequent properties **are not evaluated**.
- This is different from per-property mode, where only that specific property stops.

### Activating Global Mode

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

        // If CardNumber fails, Amount is not evaluated
        // If CardNumber passes but Amount fails, the validator stops here
    }
}
```

### Visual Difference Between Modes

Given this invalid request:

```csharp
var request = new PaymentRequest
{
    CardNumber = "",    // Fails
    Amount = -10,       // Also fails
    Currency = ""       // Also fails
};
```

**With `CascadeMode.Continue` (default):**

```json
{
  "CardNumber": ["The card number is required."],
  "Amount": ["The amount must be positive."],
  "Currency": ["The currency is required."]
}
```

**With `GlobalCascadeMode = CascadeMode.StopOnFirstFailure`:**

```json
{
  "CardNumber": ["The card number is required."]
}
```

Only the error from the first property that fails. The validator stops there.

---

## Combining Both Levels

You can use both levels simultaneously. For example, global `Continue` mode but `StopOnFirstFailure` on specific properties:

```csharp
public class CreateOrderValidator : AbstractValidator<CreateOrderRequest>
{
    // Global mode: evaluates all properties even if some fail
    protected override CascadeMode GlobalCascadeMode => CascadeMode.Continue;

    public CreateOrderValidator()
    {
        // This property stops on first failure (avoids DB call if empty)
        RuleFor(x => x.CustomerId)
            .NotEmpty()
            .MustAsync(async id => await _customers.ExistsAsync(id))
            .StopOnFirstFailure();

        // This property also stops (avoids NRE if Card is null)
        RuleFor(x => x.CardNumber)
            .NotEmpty()
            .CreditCard()
            .StopOnFirstFailure();

        // This property evaluates all its rules (default mode)
        RuleFor(x => x.Notes)
            .MaximumLength(500)
            .NoWhitespace();
    }
}
```

---

## Global StopOnFirstFailure Mode with Specific Properties

When the global mode is `StopOnFirstFailure` but you want a specific property to evaluate all its rules, you can use `StopOnFirstFailure()` only on the properties that need it — the global mode already applies it to all, but there is no way to "deactivate it" per property.

In practice, if you need this fine control, it is better to use global `Continue` mode and add `StopOnFirstFailure()` only where needed:

```csharp
// Recommended pattern: global Continue + selective StopOnFirstFailure
public class FlexibleValidator : AbstractValidator<ComplexRequest>
{
    // Continue is the default, you don't need to override if that is the behavior you want
    // protected override CascadeMode GlobalCascadeMode => CascadeMode.Continue;

    public FlexibleValidator()
    {
        // Stops on first failure: critical to avoid cascading errors
        RuleFor(x => x.UserId)
            .NotEmpty()
            .MustAsync(async id => await _users.ExistsAsync(id))
            .StopOnFirstFailure();

        // Evaluates all rules: useful to give complete feedback to the user
        RuleFor(x => x.Email)
            .NotEmpty()
                .WithMessage("The email is required.")
            .Email()
                .WithMessage("The email format is not valid.")
            .MaximumLength(320)
                .WithMessage("The email is too long.");
        // Without StopOnFirstFailure: if NotEmpty fails, Email and MaximumLength are also evaluated

        // Stops on first failure: expensive rules
        RuleFor(x => x.ExternalApiToken)
            .NotEmpty()
            .MustAsync(async token => await _api.ValidateTokenAsync(token))
            .StopOnFirstFailure();
    }
}
```

---

## When to Use Each Mode

### Use `StopOnFirstFailure` per property when:

1. **Later rules may throw exceptions** if earlier ones did not pass (e.g. `null` in a rule that expects a string)
2. **There are expensive async rules** that should not run if a basic validation already failed
3. **Accumulated messages would be confusing** (e.g. "is null" and "format is invalid" for the same field)

```csharp
// Example 1: avoid NPE
RuleFor(x => x.Address)
    .NotNull()
        .WithMessage("The address is required.")
    .Must(addr => addr.PostalCode.Length == 5) // Would throw NPE if addr is null
    .StopOnFirstFailure();

// Example 2: avoid expensive calls
RuleFor(x => x.Email)
    .NotEmpty()
    .Email()
    .MustAsync(async email => !await _users.ExistsByEmailAsync(email)) // Only if format is valid
    .StopOnFirstFailure();
```

### Use `GlobalCascadeMode = StopOnFirstFailure` when:

1. **Validation of the first field is a prerequisite** for the others (e.g. operation type defines which fields are required)
2. **Performance is critical** and you want to minimize validation work
3. **You prefer incremental feedback** (one property at a time) instead of all errors at once

```csharp
// Example: multi-step wizard where each step validates a section
public class WizardStep1Validator : AbstractValidator<WizardRequest>
{
    protected override CascadeMode GlobalCascadeMode => CascadeMode.StopOnFirstFailure;

    public WizardStep1Validator()
    {
        // If the account type is not valid, there is no point in validating the rest
        RuleFor(x => x.AccountType)
            .NotEmpty()
            .IsEnum<AccountType>();

        RuleFor(x => x.CompanyName)
            .NotEmpty()
            .MaximumLength(200);

        // ... more fields for step 1
    }
}
```

### Use `Continue` (default) when:

1. **You want to show all errors at once** (better UX in forms)
2. **Rules are independent** and there is no risk of cascading exceptions
3. **You are in an API** where the client wants to know all problems with the request

---

## Comparative Example: Registration Form

```csharp
// Form UX: show all errors to the user at once
public class RegistrationFormValidator : AbstractValidator<RegistrationForm>
{
    // Continue by default
    public RegistrationFormValidator()
    {
        // StopOnFirstFailure only where necessary
        RuleFor(x => x.Email)
            .NotEmpty().Email().StopOnFirstFailure();

        RuleFor(x => x.Password)
            .NotEmpty().MinimumLength(8).HasUppercase().HasLowercase().HasDigit().HasSpecialChar();
        // No StopOnFirstFailure: the user will see all requirements they do not meet

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

With data: `Password = "abc"` (no uppercase, no digit, no special character):

```json
{
  "Password": [
    "The password must have at least 8 characters.",
    "Must contain at least one uppercase letter.",
    "Must contain at least one number.",
    "Must contain at least one special character."
  ]
}
```

The user sees all requirements they fail in a single response.

---

## Next Steps

- **[Validation Result](09-validation-result.md)** — How to read and use ValidationResult
- **[Modifiers](07-modifiers.md)** — StopOnFirstFailure and other modifiers in detail
