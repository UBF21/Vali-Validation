# Configuración global

`ValiValidationOptions.Global` contiene valores por defecto a nivel de toda la aplicación, que se aplican a todos los validadores, salvo que una llamada o un validador específico los sobrescriba. Configúralo una sola vez, al inicio — antes de que se ejecute cualquier validación concurrente.

```csharp
using Vali_Validation.Core.Configuration;

ValiValidationOptions.Global.DefaultCascadeMode = CascadeMode.StopOnFirstFailure;
ValiValidationOptions.Global.DefaultLanguage = "es";
ValiValidationOptions.Global.DisplayNameResolver = name => name;
ValiValidationOptions.Global.PropertyNameResolver = name => name;
```

> **Concurrencia:** son propiedades estáticas mutables simples, sin bloqueo interno — la misma convención que usa .NET para `JsonSerializerOptions.Default`. Configúralas una sola vez durante el arranque de la app (p. ej. al inicio de `Program.cs`), antes de que la app empiece a atender solicitudes concurrentes. Mutarlas en tiempo de ejecución mientras hay validaciones en curso no está soportado.

---

## DefaultCascadeMode

Se aplica a cualquier validador cuya clase no sobrescriba `GlobalCascadeMode` por sí misma. Por defecto: `CascadeMode.Continue` — preserva el comportamiento de la biblioteca sin ninguna configuración.

```csharp
ValiValidationOptions.Global.DefaultCascadeMode = CascadeMode.StopOnFirstFailure;
```

Un validador individual aún puede optar por no seguirlo y definir su propio modo:

```csharp
public class OrderValidator : AbstractValidator<Order>
{
    // Sobrescribe el valor por defecto global solo para este validador
    protected override CascadeMode GlobalCascadeMode => CascadeMode.Continue;
}
```

Consulta [CascadeMode](08-cascade-mode.md) para la explicación completa de qué significan `Continue` vs `StopOnFirstFailure` a nivel de validador (distinto del modificador `.StopOnFirstFailure()` por propiedad cubierto en [Modificadores](07-modificadores.md)).

---

## DefaultLanguage

El código de idioma de respaldo usado para resolver los mensajes de las reglas incorporadas cuando `CultureInfo.CurrentUICulture` no tiene un catálogo registrado coincidente y no hay un override explícito por llamada. Por defecto: `"en"`.

```csharp
ValiValidationOptions.Global.DefaultLanguage = "es";
```

Este es el fallback de 3ra precedencia en el orden de resolución de idioma — ver [Localización](19-localizacion.md#orden-de-precedencia-de-resolución) para la cadena completa de cuatro pasos (`WithLanguage` explícito > `CurrentUICulture` > `DefaultLanguage` > inglés fijo).

---

## DisplayNameResolver

Transforma el texto sustituido en el placeholder `{PropertyName}` dentro de un mensaje — tanto para la propiedad de la regla como para cualquier otra propiedad referenciada por nombre en una regla cross-property (p. ej. el lado `otherName` de `EqualToProperty`, o `dependentPropertyName` en `DependentRuleAsync`). Por defecto: identidad — sin transformación.

```csharp
// Convierte nombres de propiedad en PascalCase a una forma más legible para los mensajes
ValiValidationOptions.Global.DisplayNameResolver = name =>
    string.Concat(name.Select((c, i) => i > 0 && char.IsUpper(c) ? " " + c : c.ToString()));

// "PostalCode" -> "Postal Code" en el texto del mensaje, p. ej.:
// "The Postal Code field cannot be empty."
```

**`DisplayNameResolver` NO cambia la clave usada en `ValidationResult.Errors`/`Failures`** — solo el texto sustituido en `{PropertyName}` dentro de los strings de mensaje. Si también quieres que cambie la clave del diccionario, configura además `PropertyNameResolver` (ver abajo) — son ajustes independientes para propósitos independientes.

---

## PropertyNameResolver

Transforma el nombre de propiedad real usado como clave en `ValidationResult.Errors`/`Failures` — aplicado **una sola vez**, cuando una expresión de propiedad se resuelve por primera vez (p. ej. el momento en que `RuleFor(x => x.Foo)` extrae `"Foo"` como nombre de propiedad). Por defecto: identidad.

```csharp
// Convierte nombres de propiedad en PascalCase a claves snake_case, p. ej. para una API JSON en snake_case
ValiValidationOptions.Global.PropertyNameResolver = name =>
    string.Concat(name.Select((c, i) => i > 0 && char.IsUpper(c) ? "_" + char.ToLower(c) : char.ToLower(c).ToString()));

// RuleFor(x => x.PostalCode) ahora produce errores bajo la clave "postal_code" en vez de "PostalCode"
```

```csharp
var result = validator.Validate(dto);
// result.Errors.ContainsKey("postal_code")  == true
// result.Errors.ContainsKey("PostalCode")   == false
```

Cuando ambos resolvers están configurados, `DisplayNameResolver` recibe el nombre **ya transformado** por `PropertyNameResolver` — nunca el nombre crudo sin transformar. Tenlo en cuenta si tus dos resolvers esperan un casing de entrada distinto.

`PropertyNameResolver` es una configuración global, a nivel de toda la app — no es lo mismo que el modificador por regla `.OverridePropertyName(...)` (ver [Modificadores](07-modificadores.md#overridepropertyname)), que cambia la clave para una cadena de reglas específica. Usa `.OverridePropertyName(...)` para una excepción puntual; usa `PropertyNameResolver` cuando quieras que todos los validadores de la app sigan la misma convención de nombres.

---

## Configuración típica al inicio

```csharp
// Program.cs
using Vali_Validation.Core.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Configura los valores por defecto de Vali-Validation antes de que la app empiece a atender solicitudes
ValiValidationOptions.Global.DefaultCascadeMode = CascadeMode.StopOnFirstFailure;
ValiValidationOptions.Global.DefaultLanguage = "en";
ValiValidationOptions.Global.PropertyNameResolver = name => JsonNamingPolicy.CamelCase.ConvertName(name);

builder.Services.AddValidationsFromAssembly(typeof(Program).Assembly);

var app = builder.Build();
// ...
app.Run();
```

---

## Próximos pasos

- **[Localización](19-localizacion.md)** — la cadena completa de resolución de idioma en la que participa `DefaultLanguage`
- **[CascadeMode](08-cascade-mode.md)** — `Continue` vs `StopOnFirstFailure` a nivel de validador
- **[Modificadores](07-modificadores.md)** — `.OverridePropertyName(...)`, la contraparte por regla de `PropertyNameResolver`
