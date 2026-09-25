# Property Validators

`IPropertyValidator<TProperty>` is a first-class, reusable alternative to writing an inline `.Must(...)` predicate or a custom `IRuleBuilder` extension method (see [Advanced Patterns — IRuleBuilder Extensions](15-advanced-patterns.md)). Use it when a custom rule is worth unit-testing in isolation, or when you want to share it across validators without exposing the package's internal rule-registration machinery.

---

## When to Use SetPropertyValidator vs Must vs an Extension Method

| Approach | Best for |
|---|---|
| `.Must(predicate)` | A one-off check, used in a single validator, no test in isolation needed |
| `IRuleBuilder<T, TProperty>` extension method | A reusable rule you want to call fluently, e.g. `.PasswordPolicy()` |
| `IPropertyValidator<TProperty>` + `.SetPropertyValidator(...)` | A reusable rule you want to unit-test as a standalone class, independent of any `AbstractValidator<T>` |

All three integrate with the rest of the modifier chain (`WithMessage`, `WithErrorCode`, `WithSeverity`, `When`, `Unless`) identically — `SetPropertyValidator` is just another rule method on `IRuleBuilder<T, TProperty>`.

---

## The IPropertyValidator\<TProperty\> Contract

```csharp
namespace Vali_Validation.Core.Rules;

public interface IPropertyValidator<TProperty>
{
    bool IsValid(TProperty value);

    IReadOnlyDictionary<string, string> Messages { get; }
}
```

- `IsValid` — the predicate. Return `true` if the value is valid.
- `Messages` — a language code → default message text dictionary, used when the rule fails and no `.WithMessage()` override is set on the rule. Must contain at least one entry. See [Localization](19-localization.md#custom-rules-ipropertyvalidator-messages) for exactly how it's resolved against the active language.

### Writing One Directly

```csharp
public class EvenNumberValidator : IPropertyValidator<int>
{
    public bool IsValid(int value) => value % 2 == 0;

    public IReadOnlyDictionary<string, string> Messages { get; } = new Dictionary<string, string>
    {
        ["en"] = "The {PropertyName} field must be an even number.",
        ["es"] = "El campo {PropertyName} debe ser un número par."
    };
}
```

```csharp
RuleFor(x => x.Age).SetPropertyValidator(new EvenNumberValidator());
```

---

## PropertyValidator\<TProperty\>: the Convenience Base Class

If you don't need multiple languages or want a quick default message, extend `PropertyValidator<TProperty>` instead of implementing the interface directly — it supplies a default English-only `Messages` for you:

```csharp
namespace Vali_Validation.Core.Rules;

public abstract class PropertyValidator<TProperty> : IPropertyValidator<TProperty>
{
    public abstract bool IsValid(TProperty value);

    // Default: { ["en"] = "The {PropertyName} field is invalid." }
    public virtual IReadOnlyDictionary<string, string> Messages { get; }
}
```

```csharp
public class EvenNumberValidator : PropertyValidator<int>
{
    public override bool IsValid(int value) => value % 2 == 0;

    // Override only if the default English message isn't good enough
    public override IReadOnlyDictionary<string, string> Messages { get; } = new Dictionary<string, string>
    {
        ["en"] = "The {PropertyName} field must be an even number."
    };
}
```

If you don't override `Messages` at all, a failure renders as: `"The Age field is invalid."`

---

## Attaching with SetPropertyValidator

`.SetPropertyValidator(IPropertyValidator<TProperty>)` attaches the validator as the current rule:

```csharp
public class ProductValidator : AbstractValidator<Product>
{
    public ProductValidator()
    {
        RuleFor(x => x.Sku)
            .NotEmpty()
            .SetPropertyValidator(new SkuFormatValidator())
                .WithErrorCode("INVALID_SKU_FORMAT");

        RuleFor(x => x.Quantity)
            .SetPropertyValidator(new EvenNumberValidator())
                .WithSeverity(Severity.Warning)
                .WithMessage("Quantity is usually shipped in pairs — double-check this order.");
    }
}
```

`SetPropertyValidator` throws `ArgumentNullException` if `validator` is `null`.

---

## Real Example: a Domain Rule Shared Across Validators

```csharp
// Shared, independently testable
public class BusinessHoursValidator : PropertyValidator<TimeOnly>
{
    private static readonly TimeOnly Open = new(8, 0);
    private static readonly TimeOnly Close = new(18, 0);

    public override bool IsValid(TimeOnly value) => value >= Open && value < Close;

    public override IReadOnlyDictionary<string, string> Messages { get; } = new Dictionary<string, string>
    {
        ["en"] = "The {PropertyName} must be between 08:00 and 18:00.",
        ["es"] = "El campo {PropertyName} debe estar entre las 08:00 y las 18:00."
    };
}

public class ScheduleDeliveryValidator : AbstractValidator<ScheduleDeliveryRequest>
{
    public ScheduleDeliveryValidator()
    {
        RuleFor(x => x.RequestedTime)
            .SetPropertyValidator(new BusinessHoursValidator());
    }
}

public class ScheduleTechnicianVisitValidator : AbstractValidator<ScheduleTechnicianVisitRequest>
{
    public ScheduleTechnicianVisitValidator()
    {
        // Same rule, reused, without duplicating the 08:00–18:00 check
        RuleFor(x => x.VisitTime)
            .SetPropertyValidator(new BusinessHoursValidator());
    }
}
```

### Unit-Testing the Validator in Isolation

Because `BusinessHoursValidator` is a plain class with no dependency on `AbstractValidator<T>` or a `RuleFor` chain, it's trivial to unit-test on its own:

```csharp
public class BusinessHoursValidatorTests
{
    private readonly BusinessHoursValidator _validator = new();

    [Theory]
    [InlineData(8, 0, true)]
    [InlineData(17, 59, true)]
    [InlineData(7, 59, false)]
    [InlineData(18, 0, false)]
    public void IsValid_ReturnsExpectedResult(int hour, int minute, bool expected)
    {
        Assert.Equal(expected, _validator.IsValid(new TimeOnly(hour, minute)));
    }
}
```

---

## Placeholders in Messages

Only `{PropertyName}` and `{PropertyValue}` are substituted into an `IPropertyValidator`'s `Messages` text — there's no equivalent of the `Args`-style custom `{token}` placeholders used internally by some built-in rules.

```csharp
["en"] = "The {PropertyName} field ('{PropertyValue}') must be an even number."
```

---

## Next Steps

- **[Localization](19-localization.md#custom-rules-ipropertyvalidator-messages)** — exactly how `Messages` is resolved against the active language
- **[Advanced Patterns](15-advanced-patterns.md)** — the `IRuleBuilder` extension-method alternative for reusable rules
- **[Severity](17-severity.md)** — `.WithSeverity(...)` works on `SetPropertyValidator` rules like any other
