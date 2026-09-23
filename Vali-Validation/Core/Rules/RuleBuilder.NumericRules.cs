using System.Globalization;
using Vali_Validation.Core.Localization;

namespace Vali_Validation.Core.Rules;

public partial class RuleBuilder<T, TProperty> where T : class
{
    public IRuleBuilder<T, TProperty> Positive()
    {
        _currentCondition = value => NumericConversion.TryToDecimal(value, out var d) && d > 0;
        _currentMessageSpec = MessageSpec.Localized(MessageKey.Positive);
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> Negative()
    {
        _currentCondition = value => NumericConversion.TryToDecimal(value, out var d) && d < 0;
        _currentMessageSpec = MessageSpec.Localized(MessageKey.Negative);
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> NotZero()
    {
        _currentCondition = value => value is IComparable comparable && comparable.CompareTo(0) != 0;
        _currentMessageSpec = MessageSpec.Localized(MessageKey.NotZero);
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> NonNegative()
    {
        _currentCondition = value => NumericConversion.TryToDouble(value, out var d) && d >= 0;
        _currentMessageSpec = MessageSpec.Localized(MessageKey.NonNegative);
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> Percentage()
    {
        _currentCondition = value => NumericConversion.TryToDouble(value, out var d) && d >= 0 && d <= 100;
        _currentMessageSpec = MessageSpec.Localized(MessageKey.Percentage);
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
        _currentMessageSpec = MessageSpec.Localized(MessageKey.Precision,
            new Dictionary<string, object> { ["totalDigits"] = totalDigits, ["decimalPlaces"] = decimalPlaces });
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> MultipleOf(decimal factor)
    {
        _currentCondition = value => NumericConversion.TryToDecimal(value, out var d) && factor != 0 && d % factor == 0;
        _currentMessageSpec = MessageSpec.Localized(MessageKey.MultipleOf,
            new Dictionary<string, object> { ["factor"] = factor });
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> MultipleOfProperty(System.Linq.Expressions.Expression<Func<T, TProperty>> otherExpression)
    {
        var otherName = Validators.AbstractValidator<T>.GetPropertyName(otherExpression.Body);
        var otherFunc = otherExpression.Compile();
        MessageSpec spec = MessageSpec.Localized(MessageKey.MultipleOfProperty,
            new Dictionary<string, object> { ["otherName"] = otherName });
        AddInstanceCondition(instance =>
        {
            TProperty value = _propertyFunc != null ? _propertyFunc(instance) : default!;
            TProperty other = otherFunc(instance);
            if (!NumericConversion.TryToDecimal(value, out decimal dv)) return false;
            if (!NumericConversion.TryToDecimal(other, out decimal dOther)) return false;
            return dOther != 0 && dv % dOther == 0;
        }, spec);
        return this;
    }

    public IRuleBuilder<T, TProperty> Odd()
    {
        _currentCondition = value => NumericConversion.TryToInt64(value, out var l) && l % 2 != 0;
        _currentMessageSpec = MessageSpec.Localized(MessageKey.Odd);
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> Even()
    {
        _currentCondition = value => NumericConversion.TryToInt64(value, out var l) && l % 2 == 0;
        _currentMessageSpec = MessageSpec.Localized(MessageKey.Even);
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
        _currentMessageSpec = MessageSpec.Localized(MessageKey.MaxDecimalPlaces,
            new Dictionary<string, object> { ["decimalPlaces"] = decimalPlaces });
        AddCurrentCondition();
        return this;
    }
}
