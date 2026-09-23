namespace Vali_Validation.Core.Localization;

/// <summary>
/// A rule's message, either a localizable <see cref="MessageKey"/> (resolved against
/// <see cref="LanguageManager"/> at validation time) or a raw string set via <c>.WithMessage()</c>
/// (never localized — preserves today's exact behavior for custom messages). Either way, carries
/// <see cref="Args"/>: named runtime values (e.g. <c>{"length": 5}</c>) substituted into the
/// resolved template's <c>{length}</c>-style placeholders at resolution time, alongside the
/// existing <c>{PropertyName}</c>/<c>{PropertyValue}</c> substitution.
/// </summary>
internal readonly struct MessageSpec
{
    private readonly MessageKey? _key;
    private readonly string? _rawMessage;

    public IReadOnlyDictionary<string, object>? Args { get; }

    private MessageSpec(MessageKey? key, string? rawMessage, IReadOnlyDictionary<string, object>? args)
    {
        _key = key;
        _rawMessage = rawMessage;
        Args = args;
    }

    public static MessageSpec Localized(MessageKey key, IReadOnlyDictionary<string, object>? args = null)
        => new(key, null, args);

    public static MessageSpec Raw(string text, IReadOnlyDictionary<string, object>? args = null)
        => new(null, text, args);

    internal string ResolveTemplate(string language) =>
        _rawMessage ?? LanguageManager.GetTemplate(_key!.Value, language);
}
