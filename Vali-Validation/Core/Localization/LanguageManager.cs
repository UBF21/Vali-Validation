using System.Collections.Concurrent;
using Vali_Validation.Core.Localization.Catalogs;

namespace Vali_Validation.Core.Localization;

/// <summary>
/// In-memory catalog of per-language message templates, keyed by <see cref="MessageKey"/>.
/// Ships with "en" (English, always complete — the final fallback) and "es" (Spanish).
/// Call <see cref="RegisterLanguage"/> to add or replace a language's catalog at startup.
/// </summary>
public static class LanguageManager
{
    private static readonly ConcurrentDictionary<string, IReadOnlyDictionary<MessageKey, string>> _catalogs =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = EnglishCatalog.Messages,
            ["es"] = SpanishCatalog.Messages
        };

    /// <summary>
    /// Registers or replaces the message catalog for <paramref name="languageCode"/> (e.g. "pt", "fr").
    /// A partial catalog is fine — any <see cref="MessageKey"/> missing from it falls back to English.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="languageCode"/> or <paramref name="messages"/> is null.</exception>
    public static void RegisterLanguage(string languageCode, IReadOnlyDictionary<MessageKey, string> messages)
    {
        if (languageCode == null) throw new ArgumentNullException(nameof(languageCode));
        if (messages == null) throw new ArgumentNullException(nameof(messages));
        _catalogs[languageCode] = new Dictionary<MessageKey, string>(messages);
    }

    internal static string GetTemplate(MessageKey key, string languageCode)
    {
        if (_catalogs.TryGetValue(languageCode, out var catalog) && catalog.TryGetValue(key, out var template))
            return template;
        return _catalogs["en"][key];
    }

    /// <summary>
    /// Whether <paramref name="languageCode"/> has a registered catalog (built-in or via
    /// <see cref="RegisterLanguage"/>). Used by <see cref="Validators.AbstractValidator{T}.ActiveLanguage"/>
    /// to decide whether <see cref="System.Globalization.CultureInfo.CurrentUICulture"/> actually
    /// resolves to a known language before using it, rather than passing through any non-empty ISO
    /// code and silently landing on <see cref="GetTemplate"/>'s hardcoded English safety net.
    /// </summary>
    internal static bool HasLanguage(string languageCode) => _catalogs.ContainsKey(languageCode);
}
