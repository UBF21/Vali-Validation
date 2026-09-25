# Severity (Warnings vs Errors)

By default, every rule failure blocks validation: it makes `ValidationResult.IsValid` `false` and shows up in `Errors`/`ErrorCodes`. `Severity` lets you mark a rule as **non-blocking** — the failure is still recorded, but it never fails validation and never appears in the legacy error surface.

This is useful for business rules that deserve the caller's attention without rejecting the request: "this discount is unusually high," "this field is deprecated, please migrate," "this order is close to the customer's credit limit."

---

## The Severity Enum

```csharp
namespace Vali_Validation.Core.Results;

public enum Severity
{
    Error = 0,   // Default. Blocks validation.
    Warning = 1, // Non-blocking. Recorded, doesn't fail IsValid.
    Info = 2     // Non-blocking. Same as Warning, different semantic weight.
}
```

Every rule is `Severity.Error` unless you explicitly say otherwise.

---

## WithSeverity

`WithSeverity` is a modifier, exactly like `WithMessage` or `WithErrorCode`: it applies to the **last rule** in the chain.

```csharp
public class OrderValidator : AbstractValidator<Order>
{
    public OrderValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty(); // Severity.Error (default) — blocks IsValid

        RuleFor(x => x.Discount)
            .LessThanOrEqualTo(50)
                .WithSeverity(Severity.Warning)
                .WithMessage("Discount above 50% requires manager approval.");
    }
}
```

If `Discount` is `60`, the resulting `ValidationResult`:

```csharp
result.IsValid;                          // true — a Warning never blocks
result.Failures.Count;                   // 1
result.Failures[0].Severity;             // Severity.Warning
result.Errors.ContainsKey("Discount");   // false — Warning failures don't appear in Errors
```

If `Email` is also empty, `IsValid` becomes `false` because of the `Error`-severity failure — the `Warning` on `Discount` is irrelevant to that decision either way.

### Combining with Other Modifiers

`WithSeverity` composes with `WithMessage`, `WithErrorCode`, `When`/`Unless` in any order, same as the rest of the modifier chain:

```csharp
RuleFor(x => x.Inventory)
    .LessThan(10)
        .WithSeverity(Severity.Warning)
        .WithErrorCode("LOW_STOCK")
        .WithMessage("Inventory is running low ({PropertyValue} units left).")
    .When(x => x.TrackInventory);
```

---

## ValidationResult.Failures: the Source of Truth

`Failures` is the full, severity-aware list of everything a validation run produced — `Errors`/`ErrorCodes` are just filtered views over it (`Severity.Error` only), kept for backward compatibility.

```csharp
public sealed class ValidationFailure
{
    public string PropertyName { get; }
    public string Message { get; }
    public Severity Severity { get; }
    public string? ErrorCode { get; }
}
```

```csharp
var result = validator.Validate(order);

foreach (var failure in result.Failures)
{
    Console.WriteLine($"[{failure.Severity}] {failure.PropertyName}: {failure.Message}");
}
// [Error] Email: The Email field cannot be empty.
// [Warning] Discount: Discount above 50% requires manager approval.
```

Filter by severity when you need to separate the two:

```csharp
var blocking = result.Failures.Where(f => f.Severity == Severity.Error).ToList();
var warnings = result.Failures.Where(f => f.Severity == Severity.Warning).ToList();

if (warnings.Count > 0)
{
    _logger.LogWarning("Order passed validation with {Count} warning(s): {Messages}",
        warnings.Count, string.Join(" | ", warnings.Select(w => w.Message)));
}
```

### AddFailure vs AddError

`ValidationResult` exposes both a general-purpose method and a shorthand for the common case:

```csharp
// Explicit severity and (optional) error code
result.AddFailure("Discount", "Discount exceeds the recommended threshold.", Severity.Warning);
result.AddFailure("Email", "The email is already in use.", Severity.Error, "EMAIL_ALREADY_EXISTS");

// AddError is exactly AddFailure(property, message, Severity.Error, errorCode)
result.AddError("Email", "The email is already in use.", "EMAIL_ALREADY_EXISTS");
```

---

## Serialization

`ValidationResult` serializes as `isValid` + a single `failures` array via `System.Text.Json` with default options — no custom converter or naming policy required. `Severity` serializes as its string name.

```csharp
var json = JsonSerializer.Serialize(result);
```

```json
{
  "isValid": false,
  "failures": [
    { "property": "Email", "message": "The Email field must be a valid email address.", "severity": "Error" },
    { "property": "Discount", "message": "Discount above 50% requires manager approval.", "severity": "Warning" }
  ]
}
```

`errorCode` is included only when set (it's omitted from the JSON entirely when `null`, not serialized as `null`).

> The legacy `Errors`/`ErrorCodes` dictionaries are marked `[JsonIgnore]` — they don't appear in the serialized output. If existing client code parses the old `errors`/`errorCodes` shape, see [Migrating from v2.x](#migrating-from-v2x) below before upgrading a shared API contract.

---

## Where Severity Does NOT Apply

`WithSeverity` follows the same "last rule in the chain" rule as `WithMessage`/`WithErrorCode`, with two gaps worth knowing about:

1. **Cross-property rules** (`RequiredIf`, `RequiredUnless`, `EqualToProperty`, and the other `*Property` rules) are unaffected by a preceding or following `.WithSeverity()` call — they always produce `Severity.Error`.
2. **`MustAsync` and `DependentRuleAsync`** always produce `Severity.Error` regardless of any `.WithSeverity()` chained after them. Calling `.WithSeverity(...)` right after one of these either silently has no effect, or silently reassigns the severity of a different, earlier synchronous rule in the same chain if one exists.

```csharp
// This does NOT make the MustAsync failure a Warning — it stays Severity.Error.
RuleFor(x => x.Email)
    .MustAsync(async (email, ct) => !await _users.ExistsByEmailAsync(email, ct))
        .WithMessage("That email is already in use.")
        .WithSeverity(Severity.Warning); // No effect on the MustAsync rule
```

If you need a non-blocking async check, add the failure manually with `result.AddFailure(...)` (see [Advanced Rules — Custom](06-advanced-rules.md#custom)) instead of relying on `WithSeverity` after `MustAsync`.

---

## Migrating from v2.x

- If you never need `Warning`/`Info` severities, **no code changes are required**. `Errors`, `ErrorCodes`, `ErrorsFor(...)`, `HasErrorFor(...)`, `FirstError(...)` and `IsValid` all behave exactly as before — they only ever reflect `Severity.Error` failures.
- If code directly mutated `result.Errors`/`result.ErrorCodes`, or assigned them to a variable typed `Dictionary<string, List<string>>`, that now fails to compile — both are `IReadOnlyDictionary<string, List<string>>`. Read-only usage (indexer reads, iteration, `ContainsKey`) is unaffected.
- To read the new severity-aware data instead of the old dictionaries:

  ```csharp
  // Old (v2.x)
  var emailErrors = result.Errors["Email"];

  // New (v3.x) — equivalent, but goes through Failures
  var emailErrors = result.Failures
      .Where(f => f.PropertyName == "Email")
      .Select(f => f.Message);
  ```

---

## Next Steps

- **[Validation Result](09-validation-result.md)** — the rest of `ValidationResult`: `Errors`, `ErrorCodes`, `Merge`, testing
- **[Modifiers](07-modifiers.md)** — `WithMessage`, `WithErrorCode`, `When`/`Unless` and how they combine with `WithSeverity`
- **[Rule Sets](18-rule-sets.md)** — restricting which rules run per validation call
