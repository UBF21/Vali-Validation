using System.Text.RegularExpressions;

namespace Vali_Validation.Core.Utils;

public static class RegularExpressions
{
    private static readonly Regex ExpressionEmail = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    private static readonly Regex ExpressionAlphaNumeric = new("^[a-zA-Z0-9_]+$");
    private static readonly Regex ExpressionNumber = new(@"^\d+$");
    private static readonly Regex ExpressionAlpha = new("^[a-zA-ZáéíóúÁÉÍÓÚñÑ\\s]+$");

    private static readonly Regex ExpressionPhone = new(@"^\+?[1-9]\d{1,14}$");
    private static readonly Regex ExpressionIPv4 = new(@"^((25[0-5]|2[0-4]\d|[01]?\d\d?)\.){3}(25[0-5]|2[0-4]\d|[01]?\d\d?)$");
    private static readonly Regex ExpressionNoWhitespace = new(@"^\S+$");

    public static bool IsValidEmail(string email) => ExpressionEmail.IsMatch(email);
    public static bool IsValidAlphaNumeric(string str) => ExpressionAlphaNumeric.IsMatch(str);
    public static bool IsValidNumber(string str) => ExpressionNumber.IsMatch(str);
    public static bool IsValidAlpha(string str) => ExpressionAlpha.IsMatch(str);
    public static bool IsValidPhone(string value) => ExpressionPhone.IsMatch(value);
    public static bool IsValidIPv4(string value) => ExpressionIPv4.IsMatch(value);
    public static bool HasNoWhitespace(string value) => ExpressionNoWhitespace.IsMatch(value);

    public static bool HasUppercaseLetter(string value) => value.Any(char.IsUpper);
    public static bool HasLowercaseLetter(string value) => value.Any(char.IsLower);
    public static bool HasDigitChar(string value) => value.Any(char.IsDigit);
    public static bool HasSpecialCharacter(string value) => value.Any(c => !char.IsLetterOrDigit(c));

    public static bool IsValidCreditCard(string value)
    {
        string digits = new string(value.Where(char.IsDigit).ToArray());
        if (digits.Length < 13 || digits.Length > 19) return false;
        int sum = 0;
        bool alternate = false;
        for (int i = digits.Length - 1; i >= 0; i--)
        {
            int n = digits[i] - '0';
            if (alternate) { n *= 2; if (n > 9) n -= 9; }
            sum += n;
            alternate = !alternate;
        }
        return sum % 10 == 0;
    }

    private static readonly Regex ExpressionSlug = new(@"^[a-z0-9]+(?:-[a-z0-9]+)*$");
    private static readonly Regex ExpressionMac = new(@"^([0-9A-Fa-f]{2}[:\-]){5}([0-9A-Fa-f]{2})$");
    private static readonly Regex ExpressionHtmlTag = new(@"<[^>]+>");
    private static readonly Regex ExpressionSqlInjection = new(@"('(''|[^'])*')|(;)|(--)|(\b(SELECT|INSERT|UPDATE|DELETE|DROP|CREATE|ALTER|EXEC|UNION|SCRIPT)\b)", RegexOptions.IgnoreCase);
    private static readonly Regex ExpressionCountryCode = new(@"^[A-Z]{2}$");
    private static readonly Regex ExpressionCurrencyCode = new(@"^[A-Z]{3}$");

    public static bool IsValidSlug(string s) => ExpressionSlug.IsMatch(s);
    public static bool IsValidMacAddress(string s) => ExpressionMac.IsMatch(s);
    public static bool HasNoHtmlTags(string s) => !ExpressionHtmlTag.IsMatch(s);
    public static bool HasNoSqlInjection(string s) => !ExpressionSqlInjection.IsMatch(s);
    public static bool IsValidCountryCode(string s) => ExpressionCountryCode.IsMatch(s);
    public static bool IsValidCurrencyCode(string s) => ExpressionCurrencyCode.IsMatch(s);
}
