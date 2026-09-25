# Rule Sets

Rule sets let a single validator selectively run a subset of its rules for one particular call, instead of always running everything. Typical use case: the same DTO is validated differently at different stages of a workflow — e.g. a "draft" save only needs a few required fields, while "checkout" needs the full set.

---

## Tagging Rules with InRuleSet

`InRuleSet` is a modifier available on `IRuleBuilder<T, TProperty>` — it tags the rule(s) defined by that builder with one or more names.

```csharp
public class CheckoutValidator : AbstractValidator<CheckoutRequest>
{
    public CheckoutValidator()
    {
        // Always runs — no InRuleSet call, implicit "default" tag
        RuleFor(x => x.CartId).NotEmpty();

        // Only runs when "checkout" is included
        RuleFor(x => x.PaymentMethod)
            .NotEmpty()
                .WithMessage("Select a payment method before checking out.")
            .InRuleSet("checkout");

        RuleFor(x => x.ShippingAddress)
            .NotNull()
            .SetValidator(new AddressValidator())
            .InRuleSet("checkout");

        // Tagged with two rule sets — runs when either is included
        RuleFor(x => x.CouponCode)
            .MustAsync(async (code, ct) => await _coupons.IsValidAsync(code, ct))
                .WithMessage("The coupon code is not valid.")
            .InRuleSet("checkout", "coupon-preview");
    }
}
```

Rules with **no** `InRuleSet` call carry the implicit tag `"default"`.

Calling `InRuleSet` more than once on the same builder is additive — it adds more tags, it doesn't replace the existing ones:

```csharp
RuleFor(x => x.TaxId)
    .NotEmpty()
    .InRuleSet("checkout")
    .InRuleSet("business-account"); // Now tagged with BOTH "checkout" and "business-account"
```

---

## Restricting a Validation Call to Specific Rule Sets

Use `IncludeRuleSets` on the `ValidationOptions` passed to `Validate`/`ValidateAsync`:

```csharp
// Only runs rules tagged "checkout" (plus... see note on "default" below)
var result = validator.Validate(request, o => o.IncludeRuleSets("checkout"));

// Async overload takes a CancellationToken like the rest of the async API
var result = await validator.ValidateAsync(request, o => o.IncludeRuleSets("checkout"), ct);

// Multiple rule sets — a rule runs if it matches ANY of the included names
var result = validator.Validate(request, o => o.IncludeRuleSets("checkout", "coupon-preview"));
```

`IncludeRuleSets` throws `ArgumentException` if called with no arguments — there's no way to pass an empty filter through this overload and get "run nothing" or "run everything" by accident.

### The Implicit "default" Rule Set

Rules with no `InRuleSet` call are **not** automatically included just because you ran a filtered validation. If you want the untagged rules to run alongside a named rule set, include `"default"` explicitly:

```csharp
// Runs ONLY rules tagged "checkout" — CartId (untagged, "default") is SKIPPED
validator.Validate(request, o => o.IncludeRuleSets("checkout"));

// Runs untagged rules AND rules tagged "checkout"
validator.Validate(request, o => o.IncludeRuleSets("default", "checkout"));
```

### Calling Validate With No Options Runs Everything

`Validate(instance)` (no `ValidationOptions` lambda) is unaffected by rule sets entirely — it runs every rule regardless of any `InRuleSet` tag, exactly like before rule sets existed:

```csharp
// Runs ALL rules — CartId, PaymentMethod, ShippingAddress, CouponCode
var result = validator.Validate(request);
```

There is no overload that lets you pass an *empty* rule set filter to `Validate(instance, configureOptions)` and get the "run everything" behavior that way — use the parameterless overload for that.

---

## Combining IncludeRuleSets with WithLanguage

`ValidationOptions` also carries `WithLanguage` (see [Localization](19-localization.md)) — both are configured on the same options object and chain fluently:

```csharp
var result = validator.Validate(request, o => o
    .IncludeRuleSets("checkout")
    .WithLanguage("es"));
```

---

## Real Example: Multi-Step Wizard

```csharp
public class CreateListingRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public decimal? Price { get; set; }
    public string? Category { get; set; }
    public List<string> Photos { get; set; } = new();
    public bool ShippingIncluded { get; set; }
    public string? ShippingCountry { get; set; }
}

public class CreateListingValidator : AbstractValidator<CreateListingRequest>
{
    public CreateListingValidator()
    {
        // Step 1: basic info — required at every step
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Category).NotEmpty();

        // Step 2: pricing — only enforced once the user reaches that step
        RuleFor(x => x.Price)
            .NotNull()
            .GreaterThan(0m)
            .InRuleSet("pricing");

        // Step 3: photos — only enforced right before publishing
        RuleFor(x => x.Photos)
            .MinCount(1)
                .WithMessage("Add at least one photo before publishing.")
            .InRuleSet("publish");

        RuleFor(x => x.ShippingCountry)
            .NotEmpty()
                .WithMessage("Select a shipping country.")
            .InRuleSet("publish")
            .When(x => x.ShippingIncluded);
    }
}
```

Usage per wizard step:

```csharp
// Step 1: only default rules (Title, Category)
var step1 = validator.Validate(request);
step1 = validator.Validate(request, o => o.IncludeRuleSets("default"));

// Step 2: default + pricing
var step2 = validator.Validate(request, o => o.IncludeRuleSets("default", "pricing"));

// Final publish: everything
var publishResult = validator.Validate(request, o => o.IncludeRuleSets("default", "pricing", "publish"));

// Equivalent to publishResult — no filter runs everything unconditionally
var publishResultAlt = validator.Validate(request);
```

---

## Rule Sets vs When/Unless

Rule sets and `When`/`Unless` solve different problems and are often combined:

| | Rule sets | `When` / `Unless` |
|---|---|---|
| Decided by | The **caller** of `Validate`/`ValidateAsync`, per call | The **data itself**, evaluated during validation |
| Typical use | "Only run checkout rules during checkout" | "Only require VAT number for company customers" |
| Granularity | Whole rules included/excluded per call | Rules always considered, condition evaluated against the instance |

They compose freely — a rule can be both tagged with `InRuleSet` and guarded with `When`:

```csharp
RuleFor(x => x.ShippingCountry)
    .NotEmpty()
    .InRuleSet("publish")
    .When(x => x.ShippingIncluded);
```

---

## Next Steps

- **[Modifiers](07-modifiers.md)** — the full modifier chain (`WithMessage`, `When`/`Unless`, etc.) that `InRuleSet` joins
- **[Localization](19-localization.md)** — `ValidationOptions.WithLanguage`, configured alongside `IncludeRuleSets`
- **[Validators](04-validators.md)** — `Validate`/`ValidateAsync` overloads in full
