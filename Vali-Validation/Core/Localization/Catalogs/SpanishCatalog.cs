using Vali_Validation.Core.Localization;

namespace Vali_Validation.Core.Localization.Catalogs;

internal static class SpanishCatalog
{
    public static readonly IReadOnlyDictionary<MessageKey, string> Messages = new Dictionary<MessageKey, string>
    {
        [MessageKey.RuleBuilderDefault] = "El campo {PropertyName} no es válido.",
        [MessageKey.NotEmpty] = "El campo {PropertyName} no puede estar vacío.",
        [MessageKey.MinimumLength] = "El campo {PropertyName} debe tener al menos {length} caracteres.",
    };
}
