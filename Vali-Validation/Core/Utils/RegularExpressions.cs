using System.Text.RegularExpressions;

namespace Vali_Validation.Core.Utils;

public static class RegularExpressions
{
    private static readonly Regex ExpressionEmail = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    private static readonly Regex ExpressionAlphaNumeric = new("^[a-zA-Z0-9_]+$");
    private static readonly Regex ExpressionNumber = new(@"^\d+$");
    private static readonly Regex ExpressionAlpha = new("^[a-zA-ZáéíóúÁÉÍÓÚñÑ\\s]+$");

    public static bool IsValidEmail(string email)
    {
        return ExpressionEmail.IsMatch(email);
    }

    public static bool IsValidAlphaNumeric(string str)
    {
        return ExpressionAlphaNumeric.IsMatch(str);
    }

    public static bool IsValidNumber(string str)
    {
        return ExpressionNumber.IsMatch(str);
    }

    public static bool isValidAlpha(string str)
    {
        return ExpressionAlpha.IsMatch(str);
    }
}