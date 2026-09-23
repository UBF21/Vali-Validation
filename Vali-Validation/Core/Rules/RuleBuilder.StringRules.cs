using Vali_Validation.Core.Localization;
using Vali_Validation.Core.Utils;

namespace Vali_Validation.Core.Rules;

public partial class RuleBuilder<T, TProperty> where T : class
{
    public IRuleBuilder<T, TProperty> NotEmpty()
    {
        _currentCondition = value => value != null && !string.IsNullOrWhiteSpace(value.ToString());
        _currentMessageSpec = MessageSpec.Localized(MessageKey.NotEmpty);
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> MustContain(string substring,
        StringComparison comparison = StringComparison.OrdinalIgnoreCase)
    {
        _currentCondition = value => value?.ToString()?.Contains(substring, comparison) ?? false;
        _currentMessageSpec = MessageSpec.Localized(MessageKey.MustContain,
            new Dictionary<string, object> { ["substring"] = substring });
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> MinimumLength(int length)
    {
        _currentCondition = value => value?.ToString()?.Length >= length;
        _currentMessageSpec = MessageSpec.Localized(MessageKey.MinimumLength,
            new Dictionary<string, object> { ["length"] = length });
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> MaximumLength(int length)
    {
        _currentCondition = value => value?.ToString()?.Length <= length;
        _currentMessageSpec = MessageSpec.Localized(MessageKey.MaximumLength,
            new Dictionary<string, object> { ["length"] = length });
        AddCurrentCondition();
        return this;
    }

    private static readonly TimeSpan MatchesTimeout = TimeSpan.FromMilliseconds(250);

    public IRuleBuilder<T, TProperty> Matches(string pattern)
    {
        _currentCondition = value =>
        {
            try
            {
                return System.Text.RegularExpressions.Regex.IsMatch(value?.ToString() ?? "", pattern, System.Text.RegularExpressions.RegexOptions.None, MatchesTimeout);
            }
            catch (System.Text.RegularExpressions.RegexMatchTimeoutException)
            {
                return false;
            }
        };
        _currentMessageSpec = MessageSpec.Localized(MessageKey.Matches);
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> StartsWith(string prefix)
    {
        _currentCondition = value => value?.ToString()?.StartsWith(prefix) ?? false;
        _currentMessageSpec = MessageSpec.Localized(MessageKey.StartsWith,
            new Dictionary<string, object> { ["prefix"] = prefix });
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> EndsWith(string suffix)
    {
        _currentCondition = value => value?.ToString()?.EndsWith(suffix) ?? false;
        _currentMessageSpec = MessageSpec.Localized(MessageKey.EndsWith,
            new Dictionary<string, object> { ["suffix"] = suffix });
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> NotContains(string substring,
        StringComparison comparison = StringComparison.OrdinalIgnoreCase)
    {
        _currentCondition = value =>
            !(value?.ToString()?.Contains(substring, comparison) ?? false);
        _currentMessageSpec = MessageSpec.Localized(MessageKey.NotContains,
            new Dictionary<string, object> { ["substring"] = substring });
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> NoWhitespace()
    {
        _currentCondition = value =>
        {
            string? str = value?.ToString();
            return !string.IsNullOrEmpty(str) && RegularExpressions.HasNoWhitespace(str);
        };
        _currentMessageSpec = MessageSpec.Localized(MessageKey.NoWhitespace);
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> Lowercase()
    {
        _currentCondition = value => { var s = value?.ToString(); return s != null && s == s.ToLower(); };
        _currentMessageSpec = MessageSpec.Localized(MessageKey.Lowercase);
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> Uppercase()
    {
        _currentCondition = value => { var s = value?.ToString(); return s != null && s == s.ToUpper(); };
        _currentMessageSpec = MessageSpec.Localized(MessageKey.Uppercase);
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> MinWords(int min)
    {
        _currentCondition = value =>
        {
            var s = value?.ToString();
            if (string.IsNullOrWhiteSpace(s)) return min == 0;
            return s.Split(new char[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length >= min;
        };
        _currentMessageSpec = MessageSpec.Localized(MessageKey.MinWords,
            new Dictionary<string, object> { ["min"] = min });
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> MaxWords(int max)
    {
        _currentCondition = value =>
        {
            var s = value?.ToString();
            if (string.IsNullOrWhiteSpace(s)) return true;
            return s.Split(new char[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length <= max;
        };
        _currentMessageSpec = MessageSpec.Localized(MessageKey.MaxWords,
            new Dictionary<string, object> { ["max"] = max });
        AddCurrentCondition();
        return this;
    }
}
