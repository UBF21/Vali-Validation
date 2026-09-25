# Localización

Cada regla incorporada (`NotEmpty`, `Email`, `Between`, `MinAge`, y alrededor de otras 90) incluye una plantilla de mensaje por defecto en **inglés** y en **español**. El idioma usado para renderizar el mensaje de un fallo se resuelve automáticamente — no requiere configuración en una app ASP.NET Core estándar que establece `CurrentUICulture` a partir de la solicitud.

Esta página cubre los catálogos incorporados, cómo se resuelve el idioma y cómo agregar tu propio idioma.

---

## Funciona automáticamente por defecto

```csharp
// Sin ninguna configuración de idioma
var result = validator.Validate(dto);
```

Si el `CultureInfo.CurrentUICulture` del hilo actual es `"es-PE"` (o cualquier cultura cuyo nombre ISO de dos letras sea `"es"`), los mensajes de las reglas incorporadas se renderizan en español. En caso contrario, se renderizan en inglés. En ASP.NET Core, `CurrentUICulture` normalmente la establece por solicitud el middleware de localización (header `Accept-Language`, valor de ruta, cookie, etc.) — Vali-Validation lo lee, sin nada extra que conectar.

---

## Sobrescribir el idioma para una llamada

Pasa `WithLanguage` sobre el lambda de `ValidationOptions` que aceptan `Validate`/`ValidateAsync`:

```csharp
var result = validator.Validate(dto, o => o.WithLanguage("es"));

var result = await validator.ValidateAsync(dto, o => o.WithLanguage("es"), cancellationToken);
```

Esto tiene prioridad sobre `CurrentUICulture` solo para esa llamada específica — no cambia la cultura ambiente ni afecta a ninguna otra llamada de validación.

Combínalo con el filtrado por conjuntos de reglas (ver [Conjuntos de reglas](18-conjuntos-de-reglas.md)) sobre el mismo objeto de opciones:

```csharp
var result = validator.Validate(dto, o => o
    .IncludeRuleSets("checkout")
    .WithLanguage("es"));
```

---

## Orden de precedencia de resolución

El idioma usado para renderizar los mensajes incorporados de una llamada de validación dada se resuelve en este orden, gana la primera coincidencia:

1. **Override explícito** — `ValidationOptions.WithLanguage("...")` pasado a esa llamada específica de `Validate`/`ValidateAsync`.
2. **`CultureInfo.CurrentUICulture`** — pero solo si su nombre ISO de dos letras (p. ej. `"es"` para `"es-PE"`) tiene un catálogo registrado. Una cultura no registrada (p. ej. `"fr-FR"` sin catálogo `"fr"` registrado) se omite, cayendo al siguiente paso — en esta etapa *no* recae silenciosamente en inglés.
3. **`ValiValidationOptions.Global.DefaultLanguage`** — el valor por defecto de toda la app (ver [Configuración global](20-configuracion-global.md)). Por defecto es `"en"`.
4. **Fallback fijo a inglés** — se usa si, por alguna razón, incluso el `DefaultLanguage` configurado no tiene una entrada de catálogo para una determinada clave de mensaje.

```csharp
// Ejemplo: la app establece un valor por defecto global, el middleware de cultura por solicitud
// establece CurrentUICulture, y una llamada específica sobrescribe ambos.
ValiValidationOptions.Global.DefaultLanguage = "es"; // fallback del paso 3

// La solicitud llega con Accept-Language: fr — no hay catálogo "fr" registrado, cae al paso 3 → "es"
var result = validator.Validate(dto);

// Esta llamada ignora todo lo anterior y fuerza inglés sin importar la cultura/valor por defecto global
var resultEn = validator.Validate(dto, o => o.WithLanguage("en"));
```

---

## MessageKey y los catálogos incorporados

Internamente, el mensaje por defecto de cada regla incorporada se busca mediante un `MessageKey` — un enum estable con un valor por cada regla (`NotEmpty`, `Email`, `GreaterThan`, `MinAge`, `MustAsync`, etc., alrededor de 90 en total entre reglas de string, numéricas, de fecha, de colección, de formato y de contraseña). No necesitas referenciar `MessageKey` directamente a menos que estés registrando un nuevo idioma (ver abajo).

Los catálogos incorporados viven bajo `Vali_Validation.Core.Localization.Catalogs` — `EnglishCatalog` (siempre completo, el fallback final) y `SpanishCatalog`.

---

## Agregar un nuevo idioma

Registra un catálogo para cualquier código de idioma con `LanguageManager.RegisterLanguage` — hazlo una vez, al inicio de la aplicación:

```csharp
using Vali_Validation.Core.Localization;

LanguageManager.RegisterLanguage("pt", new Dictionary<MessageKey, string>
{
    [MessageKey.NotEmpty] = "O campo {PropertyName} não pode estar vazio.",
    [MessageKey.Email] = "O campo {PropertyName} deve ser um endereço de email válido.",
    // ... cualquier MessageKey que no proporciones aquí recae en inglés automáticamente
});
```

Un catálogo **parcial** está bien. Cualquier `MessageKey` que falte en tu diccionario recae en inglés al momento de resolverse — no tienes que traducir las ~90 claves antes de que un nuevo idioma sea utilizable.

`RegisterLanguage` también permite **reemplazar** un catálogo incorporado (p. ej. para sobrescribir un par de mensajes en español con tu propio estilo) — pasa `"es"` como código de idioma con un diccionario que contenga solo las claves que quieres cambiar... salvo que `RegisterLanguage` reemplaza el catálogo completo para ese código, no lo combina con el catálogo incorporado existente. Si quieres sobrescribir solo algunos mensajes en español manteniendo el resto, parte de una copia de `SpanishCatalog.Messages` y cambia solo lo que necesites:

```csharp
var customSpanish = new Dictionary<MessageKey, string>(SpanishCatalog.Messages)
{
    [MessageKey.NotEmpty] = "Este campo es obligatorio." // Sobrescribe solo este
};
LanguageManager.RegisterLanguage("es", customSpanish);
```

Una vez registrado, un idioma es utilizable tanto vía `CurrentUICulture` (si su nombre ISO de dos letras coincide) como vía un `WithLanguage("pt")` explícito.

---

## Los mensajes personalizados nunca se localizan

`.WithMessage("...")` siempre gana sobre la plantilla localizada de esa regla, en el idioma en que lo hayas escrito — se trata como un string sin procesar, no se busca en ningún catálogo:

```csharp
RuleFor(x => x.Email)
    .NotEmpty()
        .WithMessage("El email es obligatorio."); // Siempre este texto exacto, sin importar CurrentUICulture
```

Si quieres un mensaje que cambie según el idioma resuelto, no uses `WithMessage` para el caso multi-idioma — o bien confía en la plantilla localizada incorporada (omite `WithMessage`), o mantén tu propio diccionario por idioma y selecciona de él explícitamente antes de llamar a `.WithMessage(...)`.

---

## Reglas personalizadas: Messages de IPropertyValidator

Un `IPropertyValidator<TProperty>` (ver [Validadores de propiedad](21-validadores-de-propiedad.md)) define su propio diccionario `Messages` (código de idioma → texto), resuelto de la misma forma contra el idioma activo — pero recayendo en la **primera entrada** del diccionario en vez del catálogo en inglés del paquete si ni el idioma activo ni el inglés están presentes en él:

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

Solo `{PropertyName}`/`{PropertyValue}` se sustituyen en el texto de mensaje de un `IPropertyValidator` — no existe un equivalente al mecanismo de placeholders `{token}` personalizado que usan internamente las reglas incorporadas.

---

## Limitación conocida: validadores anidados y WithLanguage

Un validador anidado adjuntado vía `SetValidator` (ver [Reglas avanzadas](06-reglas-avanzadas.md#setvalidator)) **no** hereda el override *explícito* `WithLanguage(...)` de una llamada externa. Cada validador — el externo y el anidado — resuelve su propio idioma activo de forma independiente, siguiendo el mismo orden de precedencia de arriba.

En la práctica esto rara vez es un problema: `CultureInfo.CurrentUICulture` es ambiental por hilo, así que ya es consistente entre la llamada externa y cualquier validador anidado que invoque, sin necesidad de propagación alguna. Solo importa si llamas al validador externo con un override **explícito** `WithLanguage(...)` y esperas que se propague a los validadores anidados — no lo hace.

```csharp
// El AddressValidator anidado aquí resuelve su PROPIO idioma desde CurrentUICulture/Global.DefaultLanguage —
// NO se vuelve automáticamente "es" solo porque la llamada externa pasó WithLanguage("es").
var result = shipmentValidator.Validate(request, o => o.WithLanguage("es"));
```

Si necesitas que un validador anidado respete un override explícito, establece `CurrentUICulture` para el alcance de la llamada en lugar de depender de `WithLanguage`, o llama al `Validate(..., o => o.WithLanguage("es"))` del propio validador anidado al validarlo directamente.

---

## Próximos pasos

- **[Configuración global](20-configuracion-global.md)** — `ValiValidationOptions.Global.DefaultLanguage` y el resto de la configuración a nivel de app
- **[Conjuntos de reglas](18-conjuntos-de-reglas.md)** — `ValidationOptions.IncludeRuleSets`, configurado sobre el mismo objeto de opciones que `WithLanguage`
- **[Validadores de propiedad](21-validadores-de-propiedad.md)** — escribir reglas personalizadas con su propio `Messages` por idioma
