using Vali_Validation.Core.Utils;

namespace Vali_Validation.Core.Rules;

public partial class RuleBuilder<T, TProperty> where T : class
{
    public IRuleBuilder<T, TProperty> NotEmpty()
    {
        _currentCondition = value => value != null && !string.IsNullOrWhiteSpace(value.ToString());
        _currentMessage = $"The {_propertyName} field cannot be empty.";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> MustContain(string substring,
        StringComparison comparison = StringComparison.OrdinalIgnoreCase)
    {
        _currentCondition = value => value?.ToString()?.Contains(substring, comparison) ?? false;
        _currentMessage = $"The {_propertyName} field must contain '{substring}'.";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> MinimumLength(int length)
    {
        _currentCondition = value => value?.ToString()?.Length >= length;
        _currentMessage = $"The {_propertyName} field must be at least {length} characters long.";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> MaximumLength(int length)
    {
        _currentCondition = value => value?.ToString()?.Length <= length;
        _currentMessage = $"The {_propertyName} field must be no longer than {length} characters.";
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
        _currentMessage = $"The {_propertyName} field is not in the correct format.";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> StartsWith(string prefix)
    {
        _currentCondition = value => value?.ToString()?.StartsWith(prefix) ?? false;
        _currentMessage = $"The {_propertyName} field must begin with '{prefix}'.";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> EndsWith(string suffix)
    {
        _currentCondition = value => value?.ToString()?.EndsWith(suffix) ?? false;
        _currentMessage = $"The {_propertyName} field must end with '{suffix}'.";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> NotContains(string substring,
        StringComparison comparison = StringComparison.OrdinalIgnoreCase)
    {
        _currentCondition = value =>
            !(value?.ToString()?.Contains(substring, comparison) ?? false);
        _currentMessage = $"The {_propertyName} field must not contain '{substring}'.";
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
        _currentMessage = $"The {_propertyName} field must not contain whitespace.";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> Lowercase()
    {
        _currentCondition = value => { var s = value?.ToString(); return s != null && s == s.ToLower(); };
        _currentMessage = $"The {_propertyName} field must be all lowercase.";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> Uppercase()
    {
        _currentCondition = value => { var s = value?.ToString(); return s != null && s == s.ToUpper(); };
        _currentMessage = $"The {_propertyName} field must be all uppercase.";
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
        _currentMessage = $"The {_propertyName} field must contain at least {min} words.";
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
        _currentMessage = $"The {_propertyName} field must contain at most {max} words.";
        AddCurrentCondition();
        return this;
    }
}
