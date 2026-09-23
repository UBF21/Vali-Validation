namespace Vali_Validation.Core.Localization;

/// <summary>
/// A rule's message: a localizable <see cref="MessageKey"/> (resolved against
/// <see cref="LanguageManager"/> at validation time), a raw string set via <c>.WithMessage()</c>
/// (never localized — preserves today's exact behavior for custom messages), or a caller-supplied
/// dictionary (see <see cref="FromDictionary"/>) resolved against the active language at validation
/// time. Either way, carries <see cref="Args"/>: named runtime values (e.g. <c>{"length": 5}</c>)
/// substituted into the resolved template's <c>{length}</c>-style placeholders at resolution time,
/// alongside the existing <c>{PropertyName}</c>/<c>{PropertyValue}</c> substitution.
/// </summary>
internal readonly struct MessageSpec
{
    private readonly MessageKey? _key;
    private readonly string? _rawMessage;
    private readonly IReadOnlyDictionary<string, string>? _customMessages;

    public IReadOnlyDictionary<string, object>? Args { get; }

    private MessageSpec(MessageKey? key, string? rawMessage, IReadOnlyDictionary<string, string>? customMessages, IReadOnlyDictionary<string, object>? args)
    {
        _key = key;
        _rawMessage = rawMessage;
        _customMessages = customMessages;
        Args = args;
    }

    public static MessageSpec Localized(MessageKey key, IReadOnlyDictionary<string, object>? args = null)
        => new(key, null, null, args);

    public static MessageSpec Raw(string text, IReadOnlyDictionary<string, object>? args = null)
        => new(null, text, null, args);

    /// <summary>
    /// A message resolved against a caller-supplied dictionary (language code → text), e.g. an
    /// <see cref="Vali_Validation.Core.Rules.IPropertyValidator{TProperty}"/>'s own <c>Messages</c>.
    /// Resolution is deferred to <see cref="ResolveTemplate"/> — NOT baked in at construction time —
    /// so it correctly re-resolves on every <c>Validate()</c> call, honoring per-call language
    /// overrides even when the same rule-builder chain runs only once (e.g. inside a Singleton
    /// validator's constructor).
    /// </summary>
    public static MessageSpec FromDictionary(IReadOnlyDictionary<string, string> messages, IReadOnlyDictionary<string, object>? args = null)
        => new(null, null, messages, args);

    internal string ResolveTemplate(string language)
    {
        if (_customMessages != null)
            return _customMessages.TryGetValue(language, out var text) ? text : _customMessages.Values.First();
        return _rawMessage ?? LanguageManager.GetTemplate(_key!.Value, language);
    }
}
