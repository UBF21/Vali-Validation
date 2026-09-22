using System.Globalization;

namespace Vali_Validation.Core.Rules;

/// <summary>
/// Exception-free numeric coercion used by numeric/format rules to avoid try/catch as control
/// flow on the validation hot path — a mistyped or non-numeric input is an expected, common case
/// during validation, not an exceptional one.
/// </summary>
internal static class NumericConversion
{
    private static readonly double DecimalFromDoubleMax = (double)decimal.MaxValue;

    internal static bool TryToDecimal(object? value, out decimal result)
    {
        switch (value)
        {
            case decimal d: result = d; return true;
            case int i: result = i; return true;
            case long l: result = l; return true;
            case short s: result = s; return true;
            case byte b: result = b; return true;
            case sbyte sb: result = sb; return true;
            case uint ui: result = ui; return true;
            case ulong ul: result = ul; return true;
            case ushort us: result = us; return true;
            case double d when !double.IsNaN(d) && !double.IsInfinity(d) && Math.Abs(d) <= DecimalFromDoubleMax:
                result = (decimal)d; return true;
            case float f when !float.IsNaN(f) && !float.IsInfinity(f) && Math.Abs((double)f) <= DecimalFromDoubleMax:
                result = (decimal)f; return true;
            case string s: return decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out result);
            default: result = default; return false;
        }
    }

    internal static bool TryToDouble(object? value, out double result)
    {
        switch (value)
        {
            case double d: result = d; return true;
            case float f: result = f; return true;
            case decimal dec: result = (double)dec; return true;
            case int i: result = i; return true;
            case long l: result = l; return true;
            case short s: result = s; return true;
            case byte b: result = b; return true;
            case sbyte sb: result = sb; return true;
            case uint ui: result = ui; return true;
            case ulong ul: result = ul; return true;
            case ushort us: result = us; return true;
            case string s: return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
            default: result = default; return false;
        }
    }

    internal static bool TryToInt64(object? value, out long result)
    {
        switch (value)
        {
            case long l: result = l; return true;
            case int i: result = i; return true;
            case short s: result = s; return true;
            case byte b: result = b; return true;
            case sbyte sb: result = sb; return true;
            case uint ui: result = ui; return true;
            case ushort us: result = us; return true;
            case ulong ul when ul <= long.MaxValue: result = (long)ul; return true;
            case string s: return long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
            default: result = default; return false;
        }
    }
}
