using Vali_Validation.Core.Localization;

namespace Vali_Validation.Core.Localization.Catalogs;

internal static class EnglishCatalog
{
    public static readonly IReadOnlyDictionary<MessageKey, string> Messages = new Dictionary<MessageKey, string>
    {
        [MessageKey.RuleBuilderDefault] = "The {PropertyName} field is invalid.",
        [MessageKey.NotEmpty] = "The {PropertyName} field cannot be empty.",
        [MessageKey.MinimumLength] = "The {PropertyName} field must be at least {length} characters long.",
    };
}
