# Global Configuration

`ValiValidationOptions.Global` holds app-wide defaults that apply across every validator, unless a specific call or validator overrides them. Configure it once, at startup — before any concurrent validation runs.

```csharp
using Vali_Validation.Core.Configuration;

ValiValidationOptions.Global.DefaultCascadeMode = CascadeMode.StopOnFirstFailure;
ValiValidationOptions.Global.DefaultLanguage = "es";
ValiValidationOptions.Global.DisplayNameResolver = name => name;
ValiValidationOptions.Global.PropertyNameResolver = name => name;
```

> **Thread safety:** these are plain mutable statics with no internal locking — the same convention .NET itself uses for `JsonSerializerOptions.Default`. Set them once during app startup (e.g. the top of `Program.cs`), before the app starts handling concurrent requests. Mutating them at runtime while validations are in flight is not supported.

---

## DefaultCascadeMode

Applies to any validator whose class doesn't override `GlobalCascadeMode` itself. Default: `CascadeMode.Continue` — preserves the library's behavior with no configuration at all.

```csharp
ValiValidationOptions.Global.DefaultCascadeMode = CascadeMode.StopOnFirstFailure;
```

An individual validator can still opt out and set its own mode:

```csharp
public class OrderValidator : AbstractValidator<Order>
{
    // Overrides the global default for this validator only
    protected override CascadeMode GlobalCascadeMode => CascadeMode.Continue;
}
```

See [CascadeMode](08-cascade-mode.md) for the full explanation of what `Continue` vs `StopOnFirstFailure` mean at the validator level (distinct from the per-property `.StopOnFirstFailure()` modifier covered in [Modifiers](07-modifiers.md)).

---

## DefaultLanguage

The fallback language code used to resolve built-in rule messages when `CultureInfo.CurrentUICulture` has no matching registered catalog and no per-call explicit override is set. Default: `"en"`.

```csharp
ValiValidationOptions.Global.DefaultLanguage = "es";
```

This is the 3rd-precedence fallback in the language resolution order — see [Localization](19-localization.md#resolution-precedence) for the full four-step chain (explicit `WithLanguage` > `CurrentUICulture` > `DefaultLanguage` > hardcoded English).

---

## DisplayNameResolver

Transforms the text substituted into the `{PropertyName}` placeholder inside a message — for the rule's own property, and for any other property referenced by name in a cross-property rule (e.g. the `otherName` side of `EqualToProperty`, or `dependentPropertyName` in `DependentRuleAsync`). Default: identity — no transform.

```csharp
// Turn PascalCase property names into a more human-readable form for messages
ValiValidationOptions.Global.DisplayNameResolver = name =>
    string.Concat(name.Select((c, i) => i > 0 && char.IsUpper(c) ? " " + c : c.ToString()));

// "PostalCode" -> "Postal Code" in message text, e.g.:
// "The Postal Code field cannot be empty."
```

**`DisplayNameResolver` does NOT change the key used in `ValidationResult.Errors`/`Failures`** — only the text substituted into `{PropertyName}` inside message strings. If you also want the dictionary key itself to change, configure `PropertyNameResolver` too (see below) — they're independent settings for independent purposes.

---

## PropertyNameResolver

Transforms the actual property name used as the key in `ValidationResult.Errors`/`Failures` — applied **once**, when a property expression is first resolved (e.g. the moment `RuleFor(x => x.Foo)` extracts `"Foo"` as the property name). Default: identity.

```csharp
// Convert PascalCase property names to snake_case keys, e.g. for a snake_case JSON API
ValiValidationOptions.Global.PropertyNameResolver = name =>
    string.Concat(name.Select((c, i) => i > 0 && char.IsUpper(c) ? "_" + char.ToLower(c) : char.ToLower(c).ToString()));

// RuleFor(x => x.PostalCode) now produces errors under the key "postal_code" instead of "PostalCode"
```

```csharp
var result = validator.Validate(dto);
// result.Errors.ContainsKey("postal_code")  == true
// result.Errors.ContainsKey("PostalCode")   == false
```

When both resolvers are configured, `DisplayNameResolver` receives the **already-transformed** name from `PropertyNameResolver` — never the raw, untransformed one. Keep this in mind if your two resolvers expect different input casing.

`PropertyNameResolver` is a global, app-wide setting — it's not the same thing as the per-rule `.OverridePropertyName(...)` modifier (see [Modifiers](07-modifiers.md#overridepropertyname)), which changes the key for one specific rule chain. Use `.OverridePropertyName(...)` for a one-off exception; use `PropertyNameResolver` when you want every validator in the app to follow the same naming convention.

---

## Typical Startup Configuration

```csharp
// Program.cs
using Vali_Validation.Core.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Configure Vali-Validation defaults before the app starts serving requests
ValiValidationOptions.Global.DefaultCascadeMode = CascadeMode.StopOnFirstFailure;
ValiValidationOptions.Global.DefaultLanguage = "en";
ValiValidationOptions.Global.PropertyNameResolver = name => JsonNamingPolicy.CamelCase.ConvertName(name);

builder.Services.AddValidationsFromAssembly(typeof(Program).Assembly);

var app = builder.Build();
// ...
app.Run();
```

---

## Next Steps

- **[Localization](19-localization.md)** — the full language resolution chain that `DefaultLanguage` participates in
- **[CascadeMode](08-cascade-mode.md)** — `Continue` vs `StopOnFirstFailure` at the validator level
- **[Modifiers](07-modifiers.md)** — `.OverridePropertyName(...)`, the per-rule counterpart to `PropertyNameResolver`
