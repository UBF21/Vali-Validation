using System.Net;
using System.Net.Sockets;
using Vali_Validation.Core.Utils;

namespace Vali_Validation.Core.Rules;

public partial class RuleBuilder<T, TProperty> where T : class
{
    public IRuleBuilder<T, TProperty> Email()
    {
        _currentCondition = value =>
        {
            string? str = value?.ToString();
            return !string.IsNullOrWhiteSpace(str) && RegularExpressions.IsValidEmail(str);
        };
        _currentMessage = $"The {_propertyName} field must be a valid email address.";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> Url()
    {
        _currentCondition = value =>
        {
            string? str = value?.ToString();
            return Uri.TryCreate(str, UriKind.Absolute, out var uriResult)
                   && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        };
        _currentMessage = $"The {_propertyName} field must be a valid URL.";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> IsAlpha()
    {
        _currentCondition = value =>
        {
            string? str = value?.ToString();
            return !string.IsNullOrWhiteSpace(str) && RegularExpressions.IsValidAlpha(str);
        };
        _currentMessage = $"The {_propertyName} field must only contain alphabetic characters.";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> IsAlphanumeric()
    {
        _currentCondition = value =>
        {
            string? str = value?.ToString();
            return !string.IsNullOrWhiteSpace(str) && RegularExpressions.IsValidAlphaNumeric(str);
        };
        _currentMessage = $"The {_propertyName} field must only contain alphanumeric characters.";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> IsNumeric()
    {
        _currentCondition = value =>
        {
            string? str = value?.ToString();
            return !string.IsNullOrWhiteSpace(str) && RegularExpressions.IsValidNumber(str);
        };
        _currentMessage = $"The {_propertyName} field must only contain numbers.";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> IsEnum<TEnum>() where TEnum : struct, Enum
    {
        _currentCondition = value => value != null && Enum.IsDefined(typeof(TEnum), value);
        _currentMessage = $"The {_propertyName} field must be a valid {typeof(TEnum).Name} value.";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> Guid()
    {
        _currentCondition = value => System.Guid.TryParse(value?.ToString(), out _);
        _currentMessage = $"The {_propertyName} field must be a valid GUID.";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> NotEmptyGuid()
    {
        _currentCondition = value =>
            value != null && System.Guid.TryParse(value.ToString(), out var guid) && guid != System.Guid.Empty;
        _currentMessage = $"The {_propertyName} field must not be an empty GUID.";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> PhoneNumber()
    {
        _currentCondition = value =>
        {
            string? str = value?.ToString();
            return !string.IsNullOrWhiteSpace(str) && RegularExpressions.IsValidPhone(str);
        };
        _currentMessage = $"The {_propertyName} field must be a valid phone number.";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> IPv4()
    {
        _currentCondition = value =>
        {
            string? str = value?.ToString();
            return !string.IsNullOrWhiteSpace(str) && RegularExpressions.IsValidIPv4(str);
        };
        _currentMessage = $"The {_propertyName} field must be a valid IPv4 address.";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> IPv6()
    {
        _currentCondition = value =>
        {
            string? str = value?.ToString();
            return !string.IsNullOrWhiteSpace(str) &&
                   IPAddress.TryParse(str, out var addr) &&
                   addr.AddressFamily == AddressFamily.InterNetworkV6;
        };
        _currentMessage = $"The {_propertyName} field must be a valid IPv6 address.";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> MacAddress()
    {
        _currentCondition = value =>
        {
            string? str = value?.ToString();
            return !string.IsNullOrWhiteSpace(str) && RegularExpressions.IsValidMacAddress(str);
        };
        _currentMessage = $"The {_propertyName} field must be a valid MAC address.";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> CreditCard()
    {
        _currentCondition = value =>
        {
            string? str = value?.ToString();
            return !string.IsNullOrWhiteSpace(str) && RegularExpressions.IsValidCreditCard(str);
        };
        _currentMessage = $"The {_propertyName} field must be a valid credit card number.";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> Latitude()
    {
        _currentCondition = value => NumericConversion.TryToDouble(value, out var d) && d >= -90.0 && d <= 90.0;
        _currentMessage = $"The {_propertyName} field must be a valid latitude (-90 to 90).";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> Longitude()
    {
        _currentCondition = value => NumericConversion.TryToDouble(value, out var d) && d >= -180.0 && d <= 180.0;
        _currentMessage = $"The {_propertyName} field must be a valid longitude (-180 to 180).";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> CountryCode()
    {
        _currentCondition = value =>
        {
            string? str = value?.ToString();
            return !string.IsNullOrWhiteSpace(str) && RegularExpressions.IsValidCountryCode(str);
        };
        _currentMessage = $"The {_propertyName} field must be a valid ISO 3166-1 alpha-2 country code (e.g. US, PE, ES).";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> CurrencyCode()
    {
        _currentCondition = value =>
        {
            string? str = value?.ToString();
            return !string.IsNullOrWhiteSpace(str) && RegularExpressions.IsValidCurrencyCode(str);
        };
        _currentMessage = $"The {_propertyName} field must be a valid ISO 4217 currency code (e.g. USD, EUR, PEN).";
        AddCurrentCondition();
        return this;
    }
}
