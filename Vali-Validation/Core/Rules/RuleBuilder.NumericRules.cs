using System.Globalization;

namespace Vali_Validation.Core.Rules;

public partial class RuleBuilder<T, TProperty> where T : class
{
    public IRuleBuilder<T, TProperty> Positive()
    {
        _currentCondition = value => NumericConversion.TryToDecimal(value, out var d) && d > 0;
        _currentMessage = $"The {_propertyName} field must be a positive number.";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> Negative()
    {
        _currentCondition = value => NumericConversion.TryToDecimal(value, out var d) && d < 0;
        _currentMessage = $"The {_propertyName} field must be a negative number.";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> NotZero()
    {
        _currentCondition = value => value is IComparable comparable && comparable.CompareTo(0) != 0;
        _currentMessage = $"The {_propertyName} field must not be zero.";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> NonNegative()
    {
        _currentCondition = value => NumericConversion.TryToDouble(value, out var d) && d >= 0;
        _currentMessage = $"The {_propertyName} field must be non-negative (zero or greater).";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> Percentage()
    {
        _currentCondition = value => NumericConversion.TryToDouble(value, out var d) && d >= 0 && d <= 100;
        _currentMessage = $"The {_propertyName} field must be a valid percentage between 0 and 100.";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> Precision(int totalDigits, int decimalPlaces)
    {
        _currentCondition = value =>
        {
            if (value == null) return true;
            if (!NumericConversion.TryToDecimal(value, out decimal d)) return false;

            string str = d.ToString(CultureInfo.InvariantCulture);
            int dotIndex = str.IndexOf('.');
            string intPart = dotIndex < 0 ? str : str.Substring(0, dotIndex);
            string fracPart = dotIndex < 0 ? string.Empty : str.Substring(dotIndex + 1);
            if (fracPart.Length > decimalPlaces) return false;
            return intPart.TrimStart('-').Length + fracPart.Length <= totalDigits;
        };
        _currentMessage = $"The {_propertyName} field must have at most {totalDigits} total digits and {decimalPlaces} decimal places.";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> MultipleOf(decimal factor)
    {
        _currentCondition = value => NumericConversion.TryToDecimal(value, out var d) && factor != 0 && d % factor == 0;
        _currentMessage = $"The {_propertyName} field must be a multiple of {factor}.";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> MultipleOfProperty(System.Linq.Expressions.Expression<Func<T, TProperty>> otherExpression)
    {
        var otherName = Validators.AbstractValidator<T>.GetPropertyName(otherExpression.Body);
        var otherFunc = otherExpression.Compile();
        string message = $"The {_propertyName} field must be a multiple of {otherName}.";
        AddInstanceCondition(instance =>
        {
            TProperty value = _propertyFunc != null ? _propertyFunc(instance) : default!;
            TProperty other = otherFunc(instance);
            if (!NumericConversion.TryToDecimal(value, out decimal dv)) return false;
            if (!NumericConversion.TryToDecimal(other, out decimal dOther)) return false;
            return dOther != 0 && dv % dOther == 0;
        }, message);
        return this;
    }

    public IRuleBuilder<T, TProperty> Odd()
    {
        _currentCondition = value => NumericConversion.TryToInt64(value, out var l) && l % 2 != 0;
        _currentMessage = $"The {_propertyName} field must be an odd number.";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> Even()
    {
        _currentCondition = value => NumericConversion.TryToInt64(value, out var l) && l % 2 == 0;
        _currentMessage = $"The {_propertyName} field must be an even number.";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> MaxDecimalPlaces(int decimalPlaces)
    {
        _currentCondition = value =>
        {
            if (value == null) return true;
            string str = value.ToString() ?? string.Empty;
            int dotIndex = str.IndexOf('.');
            if (dotIndex < 0) return true;
            return str.Length - dotIndex - 1 <= decimalPlaces;
        };
        _currentMessage = $"The {_propertyName} field must have at most {decimalPlaces} decimal places.";
        AddCurrentCondition();
        return this;
    }
}
