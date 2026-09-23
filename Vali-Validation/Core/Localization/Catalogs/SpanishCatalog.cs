using Vali_Validation.Core.Localization;

namespace Vali_Validation.Core.Localization.Catalogs;

internal static class SpanishCatalog
{
    public static readonly IReadOnlyDictionary<MessageKey, string> Messages = new Dictionary<MessageKey, string>
    {
        [MessageKey.RuleBuilderDefault] = "El campo {PropertyName} no es válido.",
        [MessageKey.NotEmpty] = "El campo {PropertyName} no puede estar vacío.",
        [MessageKey.MinimumLength] = "El campo {PropertyName} debe tener al menos {length} caracteres.",

        // String rules (RuleBuilder.StringRules.cs)
        [MessageKey.MustContain] = "El campo {PropertyName} debe contener '{substring}'.",
        [MessageKey.MaximumLength] = "El campo {PropertyName} no debe tener más de {length} caracteres.",
        [MessageKey.Matches] = "El campo {PropertyName} no tiene el formato correcto.",
        [MessageKey.StartsWith] = "El campo {PropertyName} debe comenzar con '{prefix}'.",
        [MessageKey.EndsWith] = "El campo {PropertyName} debe terminar con '{suffix}'.",
        [MessageKey.NotContains] = "El campo {PropertyName} no debe contener '{substring}'.",
        [MessageKey.NoWhitespace] = "El campo {PropertyName} no debe contener espacios en blanco.",
        [MessageKey.Lowercase] = "El campo {PropertyName} debe estar en minúsculas.",
        [MessageKey.Uppercase] = "El campo {PropertyName} debe estar en mayúsculas.",
        [MessageKey.MinWords] = "El campo {PropertyName} debe contener al menos {min} palabras.",
        [MessageKey.MaxWords] = "El campo {PropertyName} debe contener como máximo {max} palabras.",

        // Comparison rules (RuleBuilder.ComparisonRules.cs)
        [MessageKey.Must] = "El campo {PropertyName} no cumple con la condición especificada.",
        [MessageKey.EqualTo] = "El campo {PropertyName} debe ser igual a '{other}'.",
        [MessageKey.NotEqual] = "El campo {PropertyName} no debe ser igual a '{other}'.",
        [MessageKey.GreaterThan] = "El campo {PropertyName} debe ser mayor que {threshold}.",
        [MessageKey.LessThan] = "El campo {PropertyName} debe ser menor que {threshold}.",
        [MessageKey.GreaterThanOrEqualTo] = "El campo {PropertyName} debe ser mayor o igual que {threshold}.",
        [MessageKey.LessThanOrEqualTo] = "El campo {PropertyName} debe ser menor o igual que {threshold}.",
        [MessageKey.Between] = "El campo {PropertyName} debe estar entre {min} y {max}.",
        [MessageKey.ExclusiveBetween] = "El campo {PropertyName} debe estar exclusivamente entre {min} y {max}.",
        [MessageKey.LengthBetween] = "El campo {PropertyName} debe tener entre {min} y {max} caracteres.",
        [MessageKey.NotNull] = "El campo {PropertyName} no puede ser nulo.",
        [MessageKey.Null] = "El campo {PropertyName} debe ser nulo.",
        [MessageKey.Empty] = "El campo {PropertyName} debe estar vacío.",
        [MessageKey.EqualToProperty] = "El campo {PropertyName} debe ser igual a {otherName}.",
        [MessageKey.GreaterThanProperty] = "El campo {PropertyName} debe ser mayor que {otherName}.",
        [MessageKey.GreaterThanOrEqualToProperty] = "El campo {PropertyName} debe ser mayor o igual que {otherName}.",
        [MessageKey.LessThanProperty] = "El campo {PropertyName} debe ser menor que {otherName}.",
        [MessageKey.LessThanOrEqualToProperty] = "El campo {PropertyName} debe ser menor o igual que {otherName}.",
        [MessageKey.NotEqualToProperty] = "El campo {PropertyName} no debe ser igual a {otherName}.",
        [MessageKey.RequiredIf] = "El campo {PropertyName} es obligatorio.",
        [MessageKey.RequiredUnless] = "El campo {PropertyName} es obligatorio.",

        // Numeric rules (RuleBuilder.NumericRules.cs)
        [MessageKey.Positive] = "El campo {PropertyName} debe ser un número positivo.",
        [MessageKey.Negative] = "El campo {PropertyName} debe ser un número negativo.",
        [MessageKey.NotZero] = "El campo {PropertyName} no debe ser cero.",
        [MessageKey.NonNegative] = "El campo {PropertyName} debe ser no negativo (cero o mayor).",
        [MessageKey.Percentage] = "El campo {PropertyName} debe ser un porcentaje válido entre 0 y 100.",
        [MessageKey.Precision] = "El campo {PropertyName} debe tener como máximo {totalDigits} dígitos totales y {decimalPlaces} decimales.",
        [MessageKey.MultipleOf] = "El campo {PropertyName} debe ser un múltiplo de {factor}.",
        [MessageKey.MultipleOfProperty] = "El campo {PropertyName} debe ser un múltiplo de {otherName}.",
        [MessageKey.Odd] = "El campo {PropertyName} debe ser un número impar.",
        [MessageKey.Even] = "El campo {PropertyName} debe ser un número par.",
        [MessageKey.MaxDecimalPlaces] = "El campo {PropertyName} debe tener como máximo {decimalPlaces} decimales.",

        // Date rules (RuleBuilder.DateRules.cs)
        [MessageKey.FutureDate] = "El campo {PropertyName} debe ser una fecha futura.",
        [MessageKey.PastDate] = "El campo {PropertyName} debe ser una fecha pasada.",
        [MessageKey.Today] = "El campo {PropertyName} debe ser la fecha de hoy.",
        [MessageKey.MinAge] = "El campo {PropertyName} requiere una edad mínima de {years} años.",
        [MessageKey.MaxAge] = "El campo {PropertyName} debe corresponder a una edad máxima de {years} años.",
        [MessageKey.DateBetween] = "El campo {PropertyName} debe estar entre {from} y {to}.",
        [MessageKey.NotExpired] = "El campo {PropertyName} no debe estar vencido.",
        [MessageKey.WithinNext] = "El campo {PropertyName} debe estar dentro de los próximos {days} días.",
        [MessageKey.WithinLast] = "El campo {PropertyName} debe estar dentro de los últimos {days} días.",
        [MessageKey.IsWeekday] = "El campo {PropertyName} debe ser un día hábil.",
        [MessageKey.IsWeekend] = "El campo {PropertyName} debe ser un día de fin de semana.",

        // Collection rules (RuleBuilder.CollectionRules.cs)
        [MessageKey.HasCount] = "El campo {PropertyName} debe contener exactamente {count} elementos.",
        [MessageKey.NotEmptyCollection] = "El campo {PropertyName} no debe ser una colección vacía.",
        [MessageKey.MinCount] = "El campo {PropertyName} debe contener al menos {min} elementos.",
        [MessageKey.MaxCount] = "El campo {PropertyName} debe contener como máximo {max} elementos.",
        [MessageKey.Unique] = "El campo {PropertyName} no debe contener valores duplicados.",
        [MessageKey.AllSatisfy] = "El campo {PropertyName}: todos los elementos deben cumplir la condición.",
        [MessageKey.AnySatisfy] = "El campo {PropertyName}: al menos un elemento debe cumplir la condición.",
        [MessageKey.In] = "El campo {PropertyName} debe estar en la lista de valores permitidos.",
        [MessageKey.NotIn] = "El campo {PropertyName} no debe estar en la lista de valores no permitidos.",

        // Format rules (RuleBuilder.FormatRules.cs)
        [MessageKey.Email] = "El campo {PropertyName} debe ser una dirección de correo electrónico válida.",
        [MessageKey.Url] = "El campo {PropertyName} debe ser una URL válida.",
        [MessageKey.IsAlpha] = "El campo {PropertyName} solo debe contener caracteres alfabéticos.",
        [MessageKey.IsAlphanumeric] = "El campo {PropertyName} solo debe contener caracteres alfanuméricos.",
        [MessageKey.IsNumeric] = "El campo {PropertyName} solo debe contener números.",
        [MessageKey.IsEnum] = "El campo {PropertyName} debe ser un valor válido de {enumType}.",
        [MessageKey.Guid] = "El campo {PropertyName} debe ser un GUID válido.",
        [MessageKey.NotEmptyGuid] = "El campo {PropertyName} no debe ser un GUID vacío.",
        [MessageKey.PhoneNumber] = "El campo {PropertyName} debe ser un número de teléfono válido.",
        [MessageKey.IPv4] = "El campo {PropertyName} debe ser una dirección IPv4 válida.",
        [MessageKey.IPv6] = "El campo {PropertyName} debe ser una dirección IPv6 válida.",
        [MessageKey.MacAddress] = "El campo {PropertyName} debe ser una dirección MAC válida.",
        [MessageKey.CreditCard] = "El campo {PropertyName} debe ser un número de tarjeta de crédito válido.",
        [MessageKey.Latitude] = "El campo {PropertyName} debe ser una latitud válida (-90 a 90).",
        [MessageKey.Longitude] = "El campo {PropertyName} debe ser una longitud válida (-180 a 180).",
        [MessageKey.CountryCode] = "El campo {PropertyName} debe ser un código de país ISO 3166-1 alpha-2 válido (ej. US, PE, ES).",
        [MessageKey.CurrencyCode] = "El campo {PropertyName} debe ser un código de moneda ISO 4217 válido (ej. USD, EUR, PEN).",

        // Format rules part 2 / password (RuleBuilder.FormatRules.Password.cs)
        [MessageKey.IsValidJson] = "El campo {PropertyName} debe ser una cadena JSON válida.",
        [MessageKey.IsValidBase64] = "El campo {PropertyName} debe ser una cadena Base64 válida.",
        [MessageKey.Iban] = "El campo {PropertyName} debe ser un IBAN válido.",
        [MessageKey.HasUppercase] = "El campo {PropertyName} debe contener al menos una letra mayúscula.",
        [MessageKey.HasLowercase] = "El campo {PropertyName} debe contener al menos una letra minúscula.",
        [MessageKey.HasDigit] = "El campo {PropertyName} debe contener al menos un dígito.",
        [MessageKey.HasSpecialChar] = "El campo {PropertyName} debe contener al menos un carácter especial.",
        [MessageKey.Slug] = "El campo {PropertyName} debe ser un slug de URL válido (solo minúsculas, números y guiones).",
        [MessageKey.NoHtmlTags] = "El campo {PropertyName} no debe contener etiquetas HTML.",
        [MessageKey.NoSqlInjectionPatterns] = "El campo {PropertyName} contiene contenido potencialmente inseguro.",

        // Async rules (RuleBuilder.AsyncRules.cs)
        [MessageKey.MustAsync] = "El campo {PropertyName} no cumple con la condición especificada.",
        [MessageKey.DependentRuleAsync] = "El campo {PropertyName} no cumple con la condición dependiente de {dependentPropertyName}.",
    };
}
