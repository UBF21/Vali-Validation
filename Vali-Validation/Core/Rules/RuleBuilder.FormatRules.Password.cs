using Vali_Validation.Core.Utils;

namespace Vali_Validation.Core.Rules;

// Split out of RuleBuilder.FormatRules.cs to stay under the 300-line file
// guardian. Covers: password-strength rules, IsValidJson/IsValidBase64/Iban,
// plus Slug/NoHtmlTags/NoSqlInjectionPatterns — three string/format
// validators present on IRuleBuilder that the plan's per-domain mapping
// table omitted (see task-4-report.md).
public partial class RuleBuilder<T, TProperty> where T : class
{
    public IRuleBuilder<T, TProperty> IsValidJson()
    {
        _currentCondition = value =>
        {
            string? str = value?.ToString();
            if (string.IsNullOrWhiteSpace(str)) return false;
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(str);
                return true;
            }
            catch { return false; }
        };
        _currentMessage = $"The {_propertyName} field must be a valid JSON string.";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> IsValidBase64()
    {
        _currentCondition = value =>
        {
            string? str = value?.ToString();
            if (string.IsNullOrWhiteSpace(str)) return false;
            var buffer = new byte[(str.Length / 4 + 1) * 3];
            return Convert.TryFromBase64String(str, buffer, out _);
        };
        _currentMessage = $"The {_propertyName} field must be a valid Base64 encoded string.";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> Iban()
    {
        _currentCondition = value =>
        {
            string? str = value?.ToString();
            if (string.IsNullOrWhiteSpace(str)) return false;
            string iban = str.Replace(" ", string.Empty).ToUpperInvariant();
            if (iban.Length < 15 || iban.Length > 34) return false;
            return IsValidIbanChecksum(iban);
        };
        _currentMessage = $"The {_propertyName} field must be a valid IBAN.";
        AddCurrentCondition();
        return this;
    }

    // ISO 13616 mod-97 checksum. Extracted from the Iban() condition (kept
    // identical logic/order of operations to the original inline body) to
    // avoid the 4-level nesting the file-size/complexity guardian flags.
    private static bool IsValidIbanChecksum(string iban)
    {
        // Move first 4 chars to end
        string rearranged = iban.Substring(4) + iban.Substring(0, 4);
        // Replace letters with digits
        var sb = new System.Text.StringBuilder();
        foreach (char c in rearranged)
        {
            if (char.IsLetter(c))
                sb.Append(c - 'A' + 10);
            else
                sb.Append(c);
        }
        // Compute mod97 using string chunking (use long to avoid overflow)
        string numericStr = sb.ToString();
        long remainder = 0;
        int i = 0;
        while (i < numericStr.Length)
        {
            int chunkLen = Math.Min(9, numericStr.Length - i);
            string chunk = remainder.ToString() + numericStr.Substring(i, chunkLen);
            remainder = long.Parse(chunk) % 97;
            i += chunkLen;
        }
        return remainder == 1;
    }

    public IRuleBuilder<T, TProperty> HasUppercase()
    {
        _currentCondition = value => { var s = value?.ToString(); return s != null && RegularExpressions.HasUppercaseLetter(s); };
        _currentMessage = $"The {_propertyName} field must contain at least one uppercase letter.";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> HasLowercase()
    {
        _currentCondition = value => { var s = value?.ToString(); return s != null && RegularExpressions.HasLowercaseLetter(s); };
        _currentMessage = $"The {_propertyName} field must contain at least one lowercase letter.";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> HasDigit()
    {
        _currentCondition = value => { var s = value?.ToString(); return s != null && RegularExpressions.HasDigitChar(s); };
        _currentMessage = $"The {_propertyName} field must contain at least one digit.";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> HasSpecialChar()
    {
        _currentCondition = value => { var s = value?.ToString(); return s != null && RegularExpressions.HasSpecialCharacter(s); };
        _currentMessage = $"The {_propertyName} field must contain at least one special character.";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> PasswordPolicy(int minLength = 8, bool requireUppercase = true, bool requireLowercase = true, bool requireDigit = true, bool requireSpecialChar = true)
    {
        if (minLength > 0) MinimumLength(minLength);
        if (requireUppercase) HasUppercase();
        if (requireLowercase) HasLowercase();
        if (requireDigit) HasDigit();
        if (requireSpecialChar) HasSpecialChar();
        return this;
    }

    public IRuleBuilder<T, TProperty> Slug()
    {
        _currentCondition = value =>
        {
            string? str = value?.ToString();
            return !string.IsNullOrWhiteSpace(str) && RegularExpressions.IsValidSlug(str);
        };
        _currentMessage = $"The {_propertyName} field must be a valid URL slug (lowercase letters, numbers, and hyphens only).";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> NoHtmlTags()
    {
        _currentCondition = value =>
        {
            string? str = value?.ToString();
            return str != null && RegularExpressions.HasNoHtmlTags(str);
        };
        _currentMessage = $"The {_propertyName} field must not contain HTML tags.";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> NoSqlInjectionPatterns()
    {
        _currentCondition = value =>
        {
            string? str = value?.ToString();
            return str != null && RegularExpressions.HasNoSqlInjection(str);
        };
        _currentMessage = $"The {_propertyName} field contains potentially unsafe content.";
        AddCurrentCondition();
        return this;
    }
}
