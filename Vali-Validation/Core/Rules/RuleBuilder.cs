using System.Globalization;
using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using Vali_Validation.Core.Results;
using Vali_Validation.Core.Utils;
using Vali_Validation.Core.Validators;

namespace Vali_Validation.Core.Rules;

public class RuleBuilder<T, TProperty> : IRuleBuilder<T, TProperty> where T : class
{
    private readonly AbstractValidator<T> _validator;

    // Single-value mode: exactly one of these is non-null
    private readonly Func<T, TProperty>? _propertyFunc;
    private readonly Func<T, IEnumerable<TProperty>>? _collectionFunc;

    private readonly List<(Func<TProperty, bool> condition, string message, Func<T, bool>? when, string? code)> _rules = new();
    private readonly List<(Func<T, bool> instanceCondition, string message, Func<T, bool>? when)> _instanceRules = new();
    private readonly string _propertyName;

    private string _effectivePropertyName;
    private string? _currentMessage;
    private Func<TProperty, bool>? _currentCondition;
    private bool _isRuleAdded;
    private bool _stopOnFirstFailure;

    // -------------------------------------------------------------------------
    // Constructors
    // -------------------------------------------------------------------------

    // Single-value mode
    public RuleBuilder(AbstractValidator<T> validator, Func<T, TProperty> propertyFunc, string propertyName)
    {
        _validator = validator;
        _propertyFunc = propertyFunc;
        _propertyName = propertyName;
        _effectivePropertyName = propertyName;
    }

    // Collection mode (used by AbstractValidator.RuleForEach)
    internal RuleBuilder(AbstractValidator<T> validator, Func<T, IEnumerable<TProperty>> collectionFunc, string propertyName)
    {
        _validator = validator;
        _collectionFunc = collectionFunc;
        _propertyName = propertyName;
        _effectivePropertyName = propertyName;
    }

    // -------------------------------------------------------------------------
    // Internal helpers for extensions (e.g. SetValidator)
    // -------------------------------------------------------------------------

    internal string EffectivePropertyName => _effectivePropertyName;
    internal Func<T, TProperty>? PropertyFunc => _propertyFunc;

    internal void AddAsyncRule(Func<T, Task<ValidationResult>> rule) => _validator.AddRule(rule);

    // -------------------------------------------------------------------------
    // Core registration
    // -------------------------------------------------------------------------

    private void EnsureRegistered()
    {
        if (_isRuleAdded) return;

        if (_collectionFunc != null)
        {
            _validator.AddRule(instance =>
            {
                var result = new ValidationResult();
                var collection = _collectionFunc(instance);
                if (collection == null) return result;

                int index = 0;
                foreach (var element in collection)
                {
                    string key = $"{_effectivePropertyName}[{index}]";
                    foreach (var (condition, message, when, code) in _rules)
                    {
                        if (when != null && !when(instance)) continue;
                        if (element != null && !condition(element))
                        {
                            string resolved = message
                                .Replace("{PropertyName}", key)
                                .Replace("{PropertyValue}", element?.ToString() ?? "null");
                            result.AddError(key, resolved, code);
                            if (_stopOnFirstFailure) break;
                        }
                    }
                    index++;
                }
                return result;
            });
        }
        else
        {
            _validator.AddRule(instance =>
            {
                var result = new ValidationResult();
                TProperty value = _propertyFunc!(instance);
                foreach (var (condition, message, when, code) in _rules)
                {
                    if (when != null && !when(instance)) continue;
                    if (!condition(value))
                    {
                        string resolved = message
                            .Replace("{PropertyName}", _effectivePropertyName)
                            .Replace("{PropertyValue}", value?.ToString() ?? "null");
                        result.AddError(_effectivePropertyName, resolved, code);
                        if (_stopOnFirstFailure) break;
                    }
                }
                foreach (var (instanceCondition, message, when) in _instanceRules)
                {
                    if (when != null && !when(instance)) continue;
                    if (!instanceCondition(instance))
                    {
                        result.AddError(_effectivePropertyName, message);
                        if (_stopOnFirstFailure) break;
                    }
                }
                return result;
            });
        }

        _isRuleAdded = true;
    }

    private void AddCurrentCondition()
    {
        if (_currentCondition != null)
        {
            string message = _currentMessage ?? $"The {_effectivePropertyName} field is invalid.";
            _rules.Add((_currentCondition, message, null, null));
            _currentCondition = null;
            _currentMessage = null;
        }

        EnsureRegistered();
    }

    private void AddInstanceCondition(Func<T, bool> condition, string message)
    {
        _instanceRules.Add((condition, message, null));
        EnsureRegistered();
    }

    // -------------------------------------------------------------------------
    // Modifiers
    // -------------------------------------------------------------------------

    public IRuleBuilder<T, TProperty> WithMessage(string? message)
    {
        if (_rules.Count > 0)
        {
            int last = _rules.Count - 1;
            var (condition, _, when, code) = _rules[last];
            _rules[last] = (condition, message ?? $"The {_effectivePropertyName} field is invalid.", when, code);
        }
        return this;
    }

    public IRuleBuilder<T, TProperty> WithErrorCode(string code)
    {
        if (_rules.Count > 0)
        {
            int last = _rules.Count - 1;
            var (cond, msg, when, _) = _rules[last];
            _rules[last] = (cond, msg, when, code);
        }
        return this;
    }

    public IRuleBuilder<T, TProperty> OverridePropertyName(string name)
    {
        _effectivePropertyName = name;
        return this;
    }

    public IRuleBuilder<T, TProperty> StopOnFirstFailure()
    {
        _stopOnFirstFailure = true;
        return this;
    }

    /// <summary>
    /// Applies a guard condition to ALL rules defined so far in this builder.
    /// Rules are skipped when <paramref name="condition"/> returns false.
    /// </summary>
    public IRuleBuilder<T, TProperty> When(Func<T, bool> condition)
    {
        for (int i = 0; i < _rules.Count; i++)
        {
            var (cond, msg, _, code) = _rules[i];
            _rules[i] = (cond, msg, condition, code);
        }
        for (int i = 0; i < _instanceRules.Count; i++)
        {
            var (cond, msg, _) = _instanceRules[i];
            _instanceRules[i] = (cond, msg, condition);
        }
        return this;
    }

    public IRuleBuilder<T, TProperty> Unless(Func<T, bool> condition)
        => When(instance => !condition(instance));

    // -------------------------------------------------------------------------
    // Sync rules
    // -------------------------------------------------------------------------

    public IRuleBuilder<T, TProperty> NotEmpty()
    {
        _currentCondition = value => value != null && !string.IsNullOrWhiteSpace(value.ToString());
        _currentMessage = $"The {_propertyName} field cannot be empty.";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> Must(Func<TProperty, bool>? predicate)
    {
        _currentCondition = predicate ?? (_ => true);
        _currentMessage = $"The {_propertyName} field does not meet the specified condition.";
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

    public IRuleBuilder<T, TProperty> Matches(string pattern)
    {
        _currentCondition = value => Regex.IsMatch(value?.ToString() ?? "", pattern);
        _currentMessage = $"The {_propertyName} field is not in the correct format.";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> EqualTo(TProperty other)
    {
        _currentCondition = value => Equals(value, other);
        _currentMessage = $"The {_propertyName} field must be equal to '{other}'.";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> GreaterThan(IComparable threshold)
    {
        _currentCondition = value => (value as IComparable)?.CompareTo(threshold) > 0;
        _currentMessage = $"The {_propertyName} field must be greater than {threshold}.";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> LessThan(IComparable threshold)
    {
        _currentCondition = value => (value as IComparable)?.CompareTo(threshold) < 0;
        _currentMessage = $"The {_propertyName} field must be less than {threshold}.";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> NotNull()
    {
        _currentCondition = value => value != null;
        _currentMessage = $"The {_propertyName} field cannot be null.";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> Null()
    {
        _currentCondition = value => value == null;
        _currentMessage = $"The {_propertyName} field must be null.";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> Empty()
    {
        _currentCondition = value => string.IsNullOrEmpty(value?.ToString());
        _currentMessage = $"The {_propertyName} field must be empty.";
        AddCurrentCondition();
        return this;
    }

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

    public IRuleBuilder<T, TProperty> Between<TComparable>(TComparable min, TComparable max)
        where TComparable : IComparable
    {
        _currentCondition = value =>
            value is TComparable comparable &&
            comparable.CompareTo(min) >= 0 &&
            comparable.CompareTo(max) <= 0;
        _currentMessage = $"The {_propertyName} field must be between {min} and {max}.";
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

    public IRuleBuilder<T, TProperty> In(IEnumerable<TProperty> values)
    {
        _currentCondition = values.Contains;
        _currentMessage = $"The {_propertyName} field must be in the list of allowed values.";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> Positive()
    {
        _currentCondition = value =>
        {
            if (value == null) return false;
            try { return Convert.ToDecimal(value, CultureInfo.InvariantCulture) > 0; }
            catch { return false; }
        };
        _currentMessage = $"The {_propertyName} field must be a positive number.";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> Negative()
    {
        _currentCondition = value =>
        {
            if (value == null) return false;
            try { return Convert.ToDecimal(value, CultureInfo.InvariantCulture) < 0; }
            catch { return false; }
        };
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

    public IRuleBuilder<T, TProperty> FutureDate()
    {
        _currentCondition = value => value is DateTime date && date > DateTime.Now;
        _currentMessage = $"The {_propertyName} field must be a future date.";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> PastDate()
    {
        _currentCondition = value => value is DateTime date && date < DateTime.Now;
        _currentMessage = $"The {_propertyName} field must be a past date.";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> Today()
    {
        _currentCondition = value => value is DateTime date && date.Date == DateTime.Today;
        _currentMessage = $"The {_propertyName} field must be today's date.";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> HasCount(int count)
    {
        _currentCondition = value =>
            value is System.Collections.IEnumerable enumerable &&
            enumerable.Cast<object>().Count() == count;
        _currentMessage = $"The {_propertyName} field must contain exactly {count} items.";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> NotEmptyCollection()
    {
        _currentCondition = value =>
            value is System.Collections.IEnumerable enumerable &&
            enumerable.Cast<object>().Any();
        _currentMessage = $"The {_propertyName} field must not be an empty collection.";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> NotEqual(TProperty other)
    {
        _currentCondition = value => !Equals(value, other);
        _currentMessage = $"The {_propertyName} field must not be equal to '{other}'.";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> LengthBetween(int min, int max)
    {
        _currentCondition = value =>
        {
            int? len = value?.ToString()?.Length;
            return len.HasValue && len.Value >= min && len.Value <= max;
        };
        _currentMessage = $"The {_propertyName} field must be between {min} and {max} characters long.";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> ExclusiveBetween<TComparable>(TComparable min, TComparable max)
        where TComparable : IComparable
    {
        _currentCondition = value =>
            value is TComparable comparable &&
            comparable.CompareTo(min) > 0 &&
            comparable.CompareTo(max) < 0;
        _currentMessage = $"The {_propertyName} field must be exclusively between {min} and {max}.";
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

    public IRuleBuilder<T, TProperty> GreaterThanOrEqualTo(IComparable threshold)
    {
        _currentCondition = value => (value as IComparable)?.CompareTo(threshold) >= 0;
        _currentMessage = $"The {_propertyName} field must be greater than or equal to {threshold}.";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> LessThanOrEqualTo(IComparable threshold)
    {
        _currentCondition = value => (value as IComparable)?.CompareTo(threshold) <= 0;
        _currentMessage = $"The {_propertyName} field must be less than or equal to {threshold}.";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> EqualToProperty(Expression<Func<T, TProperty>> otherExpression)
    {
        var otherName = AbstractValidator<T>.GetPropertyName(otherExpression.Body);
        var otherFunc = otherExpression.Compile();
        string message = $"The {_propertyName} field must equal {otherName}.";
        _validator.AddRule(instance =>
        {
            var result = new ValidationResult();
            TProperty value = _propertyFunc != null ? _propertyFunc(instance) : default!;
            TProperty otherValue = otherFunc(instance);
            if (!Equals(value, otherValue))
                result.AddError(_effectivePropertyName, message);
            return result;
        });
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

    public IRuleBuilder<T, TProperty> MultipleOf(decimal factor)
    {
        _currentCondition = value =>
        {
            if (value == null) return false;
            try
            {
                decimal d = Convert.ToDecimal(value);
                return factor != 0 && d % factor == 0;
            }
            catch { return false; }
        };
        _currentMessage = $"The {_propertyName} field must be a multiple of {factor}.";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> Odd()
    {
        _currentCondition = value =>
        {
            try { return Convert.ToInt64(value) % 2 != 0; }
            catch { return false; }
        };
        _currentMessage = $"The {_propertyName} field must be an odd number.";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> Even()
    {
        _currentCondition = value =>
        {
            try { return Convert.ToInt64(value) % 2 == 0; }
            catch { return false; }
        };
        _currentMessage = $"The {_propertyName} field must be an even number.";
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

    // -------------------------------------------------------------------------
    // Password strength rules
    // -------------------------------------------------------------------------

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

    // -------------------------------------------------------------------------
    // Collection and string rules
    // -------------------------------------------------------------------------

    public IRuleBuilder<T, TProperty> NotIn(IEnumerable<TProperty> values)
    {
        var list = values.ToList();
        _currentCondition = value => !list.Contains(value);
        _currentMessage = $"The {_propertyName} field must not be in the list of disallowed values.";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> MinCount(int min)
    {
        _currentCondition = value => value is System.Collections.IEnumerable e && e.Cast<object>().Count() >= min;
        _currentMessage = $"The {_propertyName} field must contain at least {min} items.";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> MaxCount(int max)
    {
        _currentCondition = value => value is System.Collections.IEnumerable e && e.Cast<object>().Count() <= max;
        _currentMessage = $"The {_propertyName} field must contain at most {max} items.";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> Unique()
    {
        _currentCondition = value =>
        {
            if (value is not System.Collections.IEnumerable e) return false;
            var items = e.Cast<object>().ToList();
            return items.Count == items.Distinct().Count();
        };
        _currentMessage = $"The {_propertyName} field must not contain duplicate values.";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> AllSatisfy(Func<object, bool> predicate)
    {
        _currentCondition = value => value is System.Collections.IEnumerable e && e.Cast<object>().All(predicate);
        _currentMessage = $"The {_propertyName} field: all items must satisfy the condition.";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> AnySatisfy(Func<object, bool> predicate)
    {
        _currentCondition = value => value is System.Collections.IEnumerable e && e.Cast<object>().Any(predicate);
        _currentMessage = $"The {_propertyName} field: at least one item must satisfy the condition.";
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

    // -------------------------------------------------------------------------
    // Custom rule
    // -------------------------------------------------------------------------

    public IRuleBuilder<T, TProperty> Custom(Action<TProperty, CustomValidationContext<T>> action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));
        _validator.AddRule(instance =>
        {
            var result = new ValidationResult();
            TProperty value = _propertyFunc != null ? _propertyFunc(instance) : default!;
            var context = new CustomValidationContext<T>(instance, result, _effectivePropertyName);
            action(value, context);
            return result;
        });
        return this;
    }

    // -------------------------------------------------------------------------
    // Transform
    // -------------------------------------------------------------------------

    public RuleBuilder<T, TNew> Transform<TNew>(Func<TProperty, TNew> transform)
    {
        if (transform == null) throw new ArgumentNullException(nameof(transform));
        if (_propertyFunc == null)
            throw new InvalidOperationException("Transform is not supported in collection mode.");
        Func<T, TNew> newFunc = instance => transform(_propertyFunc(instance));
        return new RuleBuilder<T, TNew>(_validator, newFunc, _propertyName);
    }

    // -------------------------------------------------------------------------
    // Async rules
    // -------------------------------------------------------------------------

    public IRuleBuilder<T, TProperty> MustAsync(Func<TProperty, Task<bool>> predicateAsync)
    {
        if (predicateAsync == null) throw new ArgumentNullException(nameof(predicateAsync));

        string message = _currentMessage ?? $"The {_propertyName} field does not meet the specified condition.";
        _currentMessage = null;

        _validator.AddRule(async instance =>
        {
            var result = new ValidationResult();
            TProperty value = _propertyFunc != null ? _propertyFunc(instance) : default!;
            bool isValid = await predicateAsync(value).ConfigureAwait(false);
            if (!isValid) result.AddError(_effectivePropertyName, message);
            return result;
        });

        return this;
    }

    public IRuleBuilder<T, TProperty> MustAsync(Func<TProperty, CancellationToken, Task<bool>> predicateAsync)
    {
        if (predicateAsync == null) throw new ArgumentNullException(nameof(predicateAsync));

        string message = _currentMessage ?? $"The {_propertyName} field does not meet the specified condition.";
        _currentMessage = null;

        _validator.AddRule(async instance =>
        {
            var result = new ValidationResult();
            TProperty value = _propertyFunc != null ? _propertyFunc(instance) : default!;
            bool isValid = await predicateAsync(value, CancellationToken.None).ConfigureAwait(false);
            if (!isValid) result.AddError(_effectivePropertyName, message);
            return result;
        });

        return this;
    }

    public IRuleBuilder<T, TProperty> DependentRuleAsync<TDependent>(
        Expression<Func<T, TProperty>> propertyExpression,
        Expression<Func<T, TDependent>> dependentPropertyExpression,
        Func<TProperty, TDependent, Task<bool>> predicateAsync)
    {
        if (predicateAsync == null) throw new ArgumentNullException(nameof(predicateAsync));
        if (propertyExpression == null) throw new ArgumentNullException(nameof(propertyExpression));
        if (dependentPropertyExpression == null) throw new ArgumentNullException(nameof(dependentPropertyExpression));

        var propertyName = AbstractValidator<T>.GetPropertyName(propertyExpression.Body);
        var propertyFunc = propertyExpression.Compile();

        var dependentPropertyName = AbstractValidator<T>.GetPropertyName(dependentPropertyExpression.Body);
        var dependentFunc = dependentPropertyExpression.Compile();

        string message = _currentMessage ?? $"The field {propertyName} does not meet the dependent condition of {dependentPropertyName}.";
        _currentMessage = null;

        _validator.AddRule(async instance =>
        {
            var result = new ValidationResult();
            TProperty value = propertyFunc(instance);
            TDependent dependentValue = dependentFunc(instance);

            bool isValid = await predicateAsync(value, dependentValue).ConfigureAwait(false);
            if (!isValid) result.AddError(propertyName, message);
            return result;
        });

        return this;
    }

    public IRuleBuilder<T, TProperty> WhenAsync(Func<T, CancellationToken, Task<bool>> condition)
        => When(instance => condition(instance, CancellationToken.None).GetAwaiter().GetResult());

    public IRuleBuilder<T, TProperty> UnlessAsync(Func<T, CancellationToken, Task<bool>> condition)
        => Unless(instance => condition(instance, CancellationToken.None).GetAwaiter().GetResult());

    // -------------------------------------------------------------------------
    // Cross-property comparison rules
    // -------------------------------------------------------------------------

    public IRuleBuilder<T, TProperty> GreaterThanProperty(Expression<Func<T, TProperty>> otherExpression)
    {
        var otherName = AbstractValidator<T>.GetPropertyName(otherExpression.Body);
        var otherFunc = otherExpression.Compile();
        string message = $"The {_propertyName} field must be greater than {otherName}.";
        AddInstanceCondition(instance =>
        {
            TProperty value = _propertyFunc != null ? _propertyFunc(instance) : default!;
            TProperty other = otherFunc(instance);
            return (value as IComparable)?.CompareTo(other) > 0;
        }, message);
        return this;
    }

    public IRuleBuilder<T, TProperty> GreaterThanOrEqualToProperty(Expression<Func<T, TProperty>> otherExpression)
    {
        var otherName = AbstractValidator<T>.GetPropertyName(otherExpression.Body);
        var otherFunc = otherExpression.Compile();
        string message = $"The {_propertyName} field must be greater than or equal to {otherName}.";
        AddInstanceCondition(instance =>
        {
            TProperty value = _propertyFunc != null ? _propertyFunc(instance) : default!;
            TProperty other = otherFunc(instance);
            int cmp = (value as IComparable)?.CompareTo(other) ?? -1;
            return cmp >= 0;
        }, message);
        return this;
    }

    public IRuleBuilder<T, TProperty> LessThanProperty(Expression<Func<T, TProperty>> otherExpression)
    {
        var otherName = AbstractValidator<T>.GetPropertyName(otherExpression.Body);
        var otherFunc = otherExpression.Compile();
        string message = $"The {_propertyName} field must be less than {otherName}.";
        AddInstanceCondition(instance =>
        {
            TProperty value = _propertyFunc != null ? _propertyFunc(instance) : default!;
            TProperty other = otherFunc(instance);
            return (value as IComparable)?.CompareTo(other) < 0;
        }, message);
        return this;
    }

    public IRuleBuilder<T, TProperty> LessThanOrEqualToProperty(Expression<Func<T, TProperty>> otherExpression)
    {
        var otherName = AbstractValidator<T>.GetPropertyName(otherExpression.Body);
        var otherFunc = otherExpression.Compile();
        string message = $"The {_propertyName} field must be less than or equal to {otherName}.";
        AddInstanceCondition(instance =>
        {
            TProperty value = _propertyFunc != null ? _propertyFunc(instance) : default!;
            TProperty other = otherFunc(instance);
            int cmp = (value as IComparable)?.CompareTo(other) ?? 1;
            return cmp <= 0;
        }, message);
        return this;
    }

    public IRuleBuilder<T, TProperty> NotEqualToProperty(Expression<Func<T, TProperty>> otherExpression)
    {
        var otherName = AbstractValidator<T>.GetPropertyName(otherExpression.Body);
        var otherFunc = otherExpression.Compile();
        string message = $"The {_propertyName} field must not be equal to {otherName}.";
        AddInstanceCondition(instance =>
        {
            TProperty value = _propertyFunc != null ? _propertyFunc(instance) : default!;
            TProperty other = otherFunc(instance);
            return !Equals(value, other);
        }, message);
        return this;
    }

    // -------------------------------------------------------------------------
    // RequiredIf / RequiredUnless
    // -------------------------------------------------------------------------

    public IRuleBuilder<T, TProperty> RequiredIf(Func<T, bool> condition)
    {
        string message = $"The {_propertyName} field is required.";
        AddInstanceCondition(instance =>
        {
            if (!condition(instance)) return true;
            TProperty value = _propertyFunc != null ? _propertyFunc(instance) : default!;
            return value != null && !string.IsNullOrWhiteSpace(value.ToString());
        }, message);
        return this;
    }

    public IRuleBuilder<T, TProperty> RequiredIf<TOther>(Expression<Func<T, TOther>> otherProperty, TOther expectedValue)
    {
        var otherFunc = otherProperty.Compile();
        return RequiredIf(instance => Equals(otherFunc(instance), expectedValue));
    }

    public IRuleBuilder<T, TProperty> RequiredUnless(Func<T, bool> condition)
        => RequiredIf(instance => !condition(instance));

    // -------------------------------------------------------------------------
    // Date rules
    // -------------------------------------------------------------------------

    public IRuleBuilder<T, TProperty> MinAge(int years)
    {
        _currentCondition = value => value is DateTime dob && DateTime.Today >= dob.Date.AddYears(years);
        _currentMessage = $"The {_propertyName} field requires a minimum age of {years} years.";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> MaxAge(int years)
    {
        _currentCondition = value => value is DateTime dob && DateTime.Today <= dob.Date.AddYears(years);
        _currentMessage = $"The {_propertyName} field must correspond to a maximum age of {years} years.";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> DateBetween(DateTime from, DateTime to)
    {
        _currentCondition = value => value is DateTime d && d >= from && d <= to;
        _currentMessage = $"The {_propertyName} field must be between {from:yyyy-MM-dd} and {to:yyyy-MM-dd}.";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> NotExpired()
    {
        _currentCondition = value => value is DateTime d && d >= DateTime.Now;
        _currentMessage = $"The {_propertyName} field must not be expired.";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> WithinNext(TimeSpan span)
    {
        _currentCondition = value => value is DateTime d && d > DateTime.Now && d <= DateTime.Now.Add(span);
        _currentMessage = $"The {_propertyName} field must be within the next {span.TotalDays:0} days.";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> WithinLast(TimeSpan span)
    {
        _currentCondition = value => value is DateTime d && d < DateTime.Now && d >= DateTime.Now.Subtract(span);
        _currentMessage = $"The {_propertyName} field must be within the last {span.TotalDays:0} days.";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> IsWeekday()
    {
        _currentCondition = value =>
            value is DateTime d && d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday;
        _currentMessage = $"The {_propertyName} field must be a weekday.";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> IsWeekend()
    {
        _currentCondition = value =>
            value is DateTime d && (d.DayOfWeek == DayOfWeek.Saturday || d.DayOfWeek == DayOfWeek.Sunday);
        _currentMessage = $"The {_propertyName} field must be a weekend day.";
        AddCurrentCondition();
        return this;
    }

    // -------------------------------------------------------------------------
    // Numeric rules
    // -------------------------------------------------------------------------

    public IRuleBuilder<T, TProperty> NonNegative()
    {
        _currentCondition = value =>
        {
            if (value == null) return false;
            try { return Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture) >= 0; }
            catch { return false; }
        };
        _currentMessage = $"The {_propertyName} field must be non-negative (zero or greater).";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> Percentage()
    {
        _currentCondition = value =>
        {
            if (value == null) return false;
            try
            {
                double d = Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture);
                return d >= 0 && d <= 100;
            }
            catch { return false; }
        };
        _currentMessage = $"The {_propertyName} field must be a valid percentage between 0 and 100.";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> Precision(int totalDigits, int decimalPlaces)
    {
        _currentCondition = value =>
        {
            if (value == null) return true;
            decimal d;
            try { d = Convert.ToDecimal(value, CultureInfo.InvariantCulture); }
            catch { return true; }
            string str = d.ToString(CultureInfo.InvariantCulture);
            int dotIndex = str.IndexOf('.');
            string intPart = dotIndex < 0 ? str : str.Substring(0, dotIndex);
            string fracPart = dotIndex < 0 ? string.Empty : str.Substring(dotIndex + 1);
            if (fracPart.Length > decimalPlaces) return false;
            if (intPart.TrimStart('-').Length + fracPart.Length > totalDigits) return false;
            return true;
        };
        _currentMessage = $"The {_propertyName} field must have at most {totalDigits} total digits and {decimalPlaces} decimal places.";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> MultipleOfProperty(Expression<Func<T, TProperty>> otherExpression)
    {
        var otherName = AbstractValidator<T>.GetPropertyName(otherExpression.Body);
        var otherFunc = otherExpression.Compile();
        string message = $"The {_propertyName} field must be a multiple of {otherName}.";
        AddInstanceCondition(instance =>
        {
            TProperty value = _propertyFunc != null ? _propertyFunc(instance) : default!;
            TProperty other = otherFunc(instance);
            if (value == null) return false;
            try
            {
                decimal dv = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
                decimal dOther = Convert.ToDecimal(other, CultureInfo.InvariantCulture);
                return dOther != 0 && dv % dOther == 0;
            }
            catch { return false; }
        }, message);
        return this;
    }

    // -------------------------------------------------------------------------
    // String / format rules
    // -------------------------------------------------------------------------

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

    public IRuleBuilder<T, TProperty> Latitude()
    {
        _currentCondition = value =>
        {
            if (value == null) return false;
            try
            {
                double d = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                return d >= -90.0 && d <= 90.0;
            }
            catch { return false; }
        };
        _currentMessage = $"The {_propertyName} field must be a valid latitude (-90 to 90).";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> Longitude()
    {
        _currentCondition = value =>
        {
            if (value == null) return false;
            try
            {
                double d = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                return d >= -180.0 && d <= 180.0;
            }
            catch { return false; }
        };
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
            try { Convert.FromBase64String(str); return true; }
            catch { return false; }
        };
        _currentMessage = $"The {_propertyName} field must be a valid Base64 encoded string.";
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

    public IRuleBuilder<T, TProperty> Iban()
    {
        _currentCondition = value =>
        {
            string? str = value?.ToString();
            if (string.IsNullOrWhiteSpace(str)) return false;
            string iban = str.Replace(" ", string.Empty).ToUpperInvariant();
            if (iban.Length < 15 || iban.Length > 34) return false;
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
        };
        _currentMessage = $"The {_propertyName} field must be a valid IBAN.";
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

    // -------------------------------------------------------------------------
    // Property-level switch/case
    // -------------------------------------------------------------------------

    public ISwitchOnBuilder<T, TProperty, TKey> SwitchOn<TKey>(Expression<Func<T, TKey>> keyExpression)
    {
        var keyFunc = keyExpression.Compile();
        return new SwitchOnBuilder<T, TProperty, TKey>(_validator, _propertyFunc!, _effectivePropertyName, keyFunc);
    }
}
