using System.Net;
using System.Net.Sockets;
using Vali_Validation.Core.Localization;
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
        _currentMessageSpec = MessageSpec.Localized(MessageKey.Email);
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
        _currentMessageSpec = MessageSpec.Localized(MessageKey.Url);
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
        _currentMessageSpec = MessageSpec.Localized(MessageKey.IsAlpha);
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
        _currentMessageSpec = MessageSpec.Localized(MessageKey.IsAlphanumeric);
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
        _currentMessageSpec = MessageSpec.Localized(MessageKey.IsNumeric);
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> IsEnum<TEnum>() where TEnum : struct, Enum
    {
        _currentCondition = value => value != null && Enum.IsDefined(typeof(TEnum), value);
        _currentMessageSpec = MessageSpec.Localized(MessageKey.IsEnum,
            new Dictionary<string, object> { ["enumType"] = typeof(TEnum).Name });
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> Guid()
    {
        _currentCondition = value => System.Guid.TryParse(value?.ToString(), out _);
        _currentMessageSpec = MessageSpec.Localized(MessageKey.Guid);
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> NotEmptyGuid()
    {
        _currentCondition = value =>
            value != null && System.Guid.TryParse(value.ToString(), out var guid) && guid != System.Guid.Empty;
        _currentMessageSpec = MessageSpec.Localized(MessageKey.NotEmptyGuid);
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
        _currentMessageSpec = MessageSpec.Localized(MessageKey.PhoneNumber);
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
        _currentMessageSpec = MessageSpec.Localized(MessageKey.IPv4);
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
        _currentMessageSpec = MessageSpec.Localized(MessageKey.IPv6);
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
        _currentMessageSpec = MessageSpec.Localized(MessageKey.MacAddress);
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
        _currentMessageSpec = MessageSpec.Localized(MessageKey.CreditCard);
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> Latitude()
    {
        _currentCondition = value => NumericConversion.TryToDouble(value, out var d) && d >= -90.0 && d <= 90.0;
        _currentMessageSpec = MessageSpec.Localized(MessageKey.Latitude);
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> Longitude()
    {
        _currentCondition = value => NumericConversion.TryToDouble(value, out var d) && d >= -180.0 && d <= 180.0;
        _currentMessageSpec = MessageSpec.Localized(MessageKey.Longitude);
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
        _currentMessageSpec = MessageSpec.Localized(MessageKey.CountryCode);
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
        _currentMessageSpec = MessageSpec.Localized(MessageKey.CurrencyCode);
        AddCurrentCondition();
        return this;
    }
}
