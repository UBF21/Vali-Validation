# Localization

Every built-in rule (`NotEmpty`, `Email`, `Between`, `MinAge`, and around 90 others) ships with a default message template in **English** and **Spanish**. Which language is used to render a failure's message is resolved automatically — no configuration required in a standard ASP.NET Core app that sets `CurrentUICulture` from the request.

This page covers the built-in catalogs, how the language is resolved, and how to add your own language.

---

## It Works Automatically by Default

```csharp
// No language configuration anywhere
var result = validator.Validate(dto);
```

If the current thread's `CultureInfo.CurrentUICulture` is `"es-PE"` (or any culture whose two-letter ISO name is `"es"`), built-in rule messages render in Spanish. Otherwise, they render in English. In ASP.NET Core, `CurrentUICulture` is normally set per-request by the localization middleware (`Accept-Language` header, route value, cookie, etc.) — Vali-Validation reads it, nothing extra to wire up.

---

## Overriding the Language for One Call

Pass `WithLanguage` on the `ValidationOptions` lambda accepted by `Validate`/`ValidateAsync`:

```csharp
var result = validator.Validate(dto, o => o.WithLanguage("es"));

var result = await validator.ValidateAsync(dto, o => o.WithLanguage("es"), cancellationToken);
```

This takes precedence over `CurrentUICulture` for that single call only — it doesn't change the ambient culture or affect any other validation call.

Combine it with rule set filtering (see [Rule Sets](18-rule-sets.md)) on the same options object:

```csharp
var result = validator.Validate(dto, o => o
    .IncludeRuleSets("checkout")
    .WithLanguage("es"));
```

---

## Resolution Precedence

The language used to render a given validation call's built-in messages is resolved in this order, first match wins:

1. **Explicit override** — `ValidationOptions.WithLanguage("...")` passed to that specific `Validate`/`ValidateAsync` call.
2. **`CultureInfo.CurrentUICulture`** — but only if its two-letter ISO name (e.g. `"es"` for `"es-PE"`) has a registered catalog. An unregistered culture (e.g. `"fr-FR"` with no `"fr"` catalog registered) is skipped, falling through to the next step — it does *not* silently land on English at this stage.
3. **`ValiValidationOptions.Global.DefaultLanguage`** — the app-wide default (see [Global Configuration](20-global-configuration.md)). Defaults to `"en"`.
4. **Hardcoded English fallback** — used if, for some reason, even the configured `DefaultLanguage` has no matching catalog entry for a given message key.

```csharp
// Example: app sets a global default, per-request culture middleware sets CurrentUICulture,
// and one specific call overrides both.
ValiValidationOptions.Global.DefaultLanguage = "es"; // step 3 fallback

// Request comes in with Accept-Language: fr — no "fr" catalog registered, falls through to step 3 → "es"
var result = validator.Validate(dto);

// This call ignores everything above and forces English regardless of culture/global default
var resultEn = validator.Validate(dto, o => o.WithLanguage("en"));
```

---

## MessageKey and the Built-In Catalogs

Internally, every built-in rule's default message is looked up by a `MessageKey` — a stable enum with one value per rule (`NotEmpty`, `Email`, `GreaterThan`, `MinAge`, `MustAsync`, and so on, roughly 90 total across string, numeric, date, collection, format and password rules). You don't need to reference `MessageKey` directly unless you're registering a new language (see below).

The built-in catalogs live under `Vali_Validation.Core.Localization.Catalogs` — `EnglishCatalog` (the always-complete fallback) and `SpanishCatalog`.

---

## Adding a New Language

Register a catalog for any language code with `LanguageManager.RegisterLanguage` — do this once, at startup:

```csharp
using Vali_Validation.Core.Localization;

LanguageManager.RegisterLanguage("pt", new Dictionary<MessageKey, string>
{
    [MessageKey.NotEmpty] = "O campo {PropertyName} não pode estar vazio.",
    [MessageKey.Email] = "O campo {PropertyName} deve ser um endereço de email válido.",
    // ... any MessageKey you don't provide here falls back to English automatically
});
```

A **partial** catalog is fine. Any `MessageKey` missing from your dictionary falls back to English at resolution time — you don't have to translate all ~90 keys before a new language is usable.

`RegisterLanguage` also lets you **replace** a built-in catalog (e.g. to override a couple of Spanish messages with your own house style) — pass `"es"` as the language code with a dictionary containing just the keys you want to change... except `RegisterLanguage` replaces the whole catalog for that code, not a merge with the existing built-in one. If you want to override just a few Spanish messages while keeping the rest, start from a copy of `SpanishCatalog.Messages` and change only what you need:

```csharp
var customSpanish = new Dictionary<MessageKey, string>(SpanishCatalog.Messages)
{
    [MessageKey.NotEmpty] = "Este campo es obligatorio." // Override just this one
};
LanguageManager.RegisterLanguage("es", customSpanish);
```

Once registered, a language is usable both via `CurrentUICulture` (if its two-letter ISO name matches) and via explicit `WithLanguage("pt")`.

---

## Custom Messages Are Never Localized

`.WithMessage("...")` always wins over the localized template for that rule, in whatever language you wrote it in — it's treated as a raw string, not looked up in any catalog:

```csharp
RuleFor(x => x.Email)
    .NotEmpty()
        .WithMessage("El email es obligatorio."); // Always this exact text, regardless of CurrentUICulture
```

If you want a message that changes with the resolved language, don't use `WithMessage` for the multi-language case — either rely on the built-in localized template (omit `WithMessage`), or maintain your own per-language dictionary and select from it explicitly before calling `.WithMessage(...)`.

---

## Custom Rules: IPropertyValidator Messages

An `IPropertyValidator<TProperty>` (see [Property Validators](21-property-validators.md)) defines its own `Messages` dictionary (language code → text), resolved the same way against the active language — but falling back to the dictionary's **first entry** rather than the package's English catalog if neither the active language nor English is present in it:

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

Only `{PropertyName}`/`{PropertyValue}` are substituted into an `IPropertyValidator`'s message text — there's no equivalent of the `Args`-style custom placeholder mechanism used internally by built-in rules.

---

## Known Limitation: Nested Validators and WithLanguage

A nested validator attached via `SetValidator` (see [Advanced Rules](06-advanced-rules.md#setvalidator)) does **not** inherit an outer call's *explicit* `WithLanguage(...)` override. Each validator — outer and nested — resolves its own active language independently, following the same precedence order above.

In practice this is rarely a problem: `CultureInfo.CurrentUICulture` is thread-ambient, so it's already consistent across the outer call and any nested validator it invokes without any forwarding needed. It only matters if you call the outer validator with an **explicit** `WithLanguage(...)` override and expect that override to propagate into nested validators — it doesn't.

```csharp
// The AddressValidator nested here resolves its OWN language from CurrentUICulture/Global.DefaultLanguage —
// it does NOT automatically become "es" just because the outer call passed WithLanguage("es").
var result = shipmentValidator.Validate(request, o => o.WithLanguage("es"));
```

If you need a nested validator to honor an explicit override, set `CurrentUICulture` for the scope of the call instead of relying on `WithLanguage`, or call the nested validator's own `Validate(..., o => o.WithLanguage("es"))` when validating it directly.

---

## Next Steps

- **[Global Configuration](20-global-configuration.md)** — `ValiValidationOptions.Global.DefaultLanguage` and the rest of the app-wide settings
- **[Rule Sets](18-rule-sets.md)** — `ValidationOptions.IncludeRuleSets`, configured on the same options object as `WithLanguage`
- **[Property Validators](21-property-validators.md)** — writing custom rules with their own per-language `Messages`
