using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Vali_Validation.Core.Results;
using Vali_Validation.Core.Utils;
using Vali_Validation.Core.Validators;

namespace Vali_Validation.Core.Rules;

public class RuleBuilder<T, TProperty> : IRuleBuilder<T, TProperty> where T : class
{
    private readonly AbstractValidator<T> _validator;
    private readonly Func<T, TProperty> _propertyFunc;
    private readonly List<(Func<TProperty, bool> condition, string message)> _rules = new();
    private readonly string _propertyName;

    private string? _currentMessage;
    private bool _isRuleAdded;

    private Func<TProperty, bool>? _currentCondition;


    public RuleBuilder(AbstractValidator<T> validator, Func<T, TProperty> propertyFunc, string propertyName)
    {
        _validator = validator;
        _propertyFunc = propertyFunc;
        _propertyName = propertyName;
    }

    private void AddCurrentCondition()
    {
        if (_currentCondition != null)
        {
            string message = _currentMessage ?? $"The {_propertyName} field is invalid.";
            _rules.Add((_currentCondition, message));
            _currentCondition = null;
            _currentMessage = null;
        }

        if (!_isRuleAdded)
        {
            _validator.AddRule(instance =>
            {
                ValidationResult result = new ValidationResult();
                TProperty value = _propertyFunc(instance);

                foreach (var (condition, message) in _rules)
                {
                    if (!condition(value)) result.AddError(_propertyName, message);
                }

                return result;
            });

            _isRuleAdded = true;
        }
    }


    public RuleBuilder<T, TProperty> WithMessage(string? message)
    {
        if (_rules.Count > Constants.Zero)
        {
            int lastIndex = _rules.Count - Constants.One;
            var (condition, _) = _rules[lastIndex];
            _rules[lastIndex] = (condition, message ?? $"The {_propertyName} field is invalid.");
        }

        return this;
    }

    public RuleBuilder<T, TProperty> NotEmpty()
    {
        _currentCondition = value => value != null && !string.IsNullOrWhiteSpace(value.ToString());
        _currentMessage = $"The {_propertyName} field cannot be empty.";
        AddCurrentCondition();
        return this;
    }

    public RuleBuilder<T, TProperty> Must(Func<TProperty, bool>? predicate)
    {
        _currentCondition = predicate ?? (_ => true);
        _currentMessage = $"The {_propertyName} field does not meet the specified condition.";
        AddCurrentCondition();
        return this;
    }

    public RuleBuilder<T, TProperty> MustContain(string substring,
        StringComparison comparison = StringComparison.OrdinalIgnoreCase)
    {
        _currentCondition = value =>
            value?.ToString()?.Contains(substring, comparison) ?? false;
        _currentMessage = $"The {_propertyName} field must contain '{substring}'.";
        AddCurrentCondition();
        return this;
    }

    public RuleBuilder<T, TProperty> MinimumLength(int length)
    {
        _currentCondition = value => value?.ToString()?.Length >= length;
        _currentMessage = $"The {_propertyName} field must be at least {length} characters long.";
        AddCurrentCondition();
        return this;
    }

    public RuleBuilder<T, TProperty> MaximumLength(int length)
    {
        _currentCondition = value => value?.ToString()?.Length <= length;
        _currentMessage = $"The {_propertyName} field must be no longer than {length} characters.";
        AddCurrentCondition();
        return this;
    }

    public RuleBuilder<T, TProperty> Matches(string pattern)
    {
        _currentCondition = value => Regex.IsMatch(value?.ToString() ?? "", pattern);
        _currentMessage = $"The {_propertyName} field is not in the correct format.";
        AddCurrentCondition();
        return this;
    }

    public RuleBuilder<T, TProperty> EqualTo(TProperty other)
    {
        _currentCondition = value => Equals(value, other);
        _currentMessage = $"The {_propertyName} field must be equal to '{other}'.";
        AddCurrentCondition();
        return this;
    }

    public RuleBuilder<T, TProperty> GreaterThan(IComparable threshold)
    {
        _currentCondition = value => (value as IComparable)?.CompareTo(threshold) > Constants.Zero;
        _currentMessage = $"The {_propertyName} field must be greater than {threshold}.";
        AddCurrentCondition();
        return this;
    }

    public RuleBuilder<T, TProperty> LessThan(IComparable threshold)
    {
        _currentCondition = value => (value as IComparable)?.CompareTo(threshold) < Constants.Zero;
        _currentMessage = $"The {_propertyName} field must be greater than {threshold}.";
        AddCurrentCondition();
        return this;
    }

    public RuleBuilder<T, TProperty> NotNull()
    {
        _currentCondition = value => value != null;
        _currentMessage = $"The {_propertyName} field cannot be null.";
        AddCurrentCondition();
        return this;
    }

    public RuleBuilder<T, TProperty> Null()
    {
        _currentCondition = value => value == null;
        _currentMessage = $"The {_propertyName} field must be null.";
        AddCurrentCondition();
        return this;
    }

    public RuleBuilder<T, TProperty> Empty()
    {
        _currentCondition = value => string.IsNullOrEmpty(value?.ToString());
        _currentMessage = $"The {_propertyName} field must be empty.";
        AddCurrentCondition();
        return this;
    }

    public RuleBuilder<T, TProperty> Email()
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

    public RuleBuilder<T, TProperty> Url()
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

    public RuleBuilder<T, TProperty> IsAlpha()
    {
        _currentCondition = value =>
        {
            string? str = value?.ToString();
            return !string.IsNullOrWhiteSpace(str) && RegularExpressions.isValidAlpha(str);
        };
        _currentMessage = $"The {_propertyName} field must only contain alphabetic characters.";
        AddCurrentCondition();
        return this;
    }

    public RuleBuilder<T, TProperty> IsAlphanumeric()
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

    public RuleBuilder<T, TProperty> IsNumeric()
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

    public RuleBuilder<T, TProperty> Between<TComparable>(TComparable min, TComparable max)
        where TComparable : IComparable
    {
        _currentCondition = value =>
            value is TComparable comparable && (comparable.CompareTo(min) >= Constants.Zero &&
                                                comparable.CompareTo(max) <= Constants.Zero);
        _currentMessage = $"The {_propertyName} field must be between {min} and {max}.";
        AddCurrentCondition();
        return this;
    }

    public RuleBuilder<T, TProperty> StartsWith(string prefix)
    {
        _currentCondition = value => value?.ToString()?.StartsWith(prefix) ?? false;
        _currentMessage = $"The {_propertyName} field must begin with '{prefix}'.";
        AddCurrentCondition();
        return this;
    }

    public RuleBuilder<T, TProperty> EndsWith(string suffix)
    {
        _currentCondition = value => value?.ToString()?.EndsWith(suffix) ?? false;
        _currentMessage = $"The {_propertyName} field must end with '{suffix}'.";
        AddCurrentCondition();
        return this;
    }

    public RuleBuilder<T, TProperty> In(IEnumerable<TProperty> values)
    {
        _currentCondition = values.Contains;
        _currentMessage = $"The {_propertyName} field must be in the list of allowed values.";
        AddCurrentCondition();
        return this;
    }

    public RuleBuilder<T, TProperty> Positive()
    {
        _currentCondition = value =>
            value is IComparable comparable && comparable.CompareTo(Constants.Zero) >= Constants.Zero;
        _currentMessage = $"The {_propertyName} field must be a positive number.";
        AddCurrentCondition();
        return this;
    }

    public RuleBuilder<T, TProperty> Negative()
    {
        _currentCondition = value =>
            value is IComparable comparable && comparable.CompareTo(Constants.Zero) < Constants.Zero;
        _currentMessage = $"The {_propertyName} field must be a negative number.";
        AddCurrentCondition();
        return this;
    }

    public RuleBuilder<T, TProperty> NotZero()
    {
        _currentCondition = value =>
            value is IComparable comparable && comparable.CompareTo(Constants.Zero) != Constants.Zero;
        _currentMessage = $"The {_propertyName} field must not be zero.";
        AddCurrentCondition();
        return this;
    }

    public RuleBuilder<T, TProperty> FutureDate()
    {
        _currentCondition = value => value is DateTime date && date > DateTime.Now;
        _currentMessage = $"The {_propertyName} field must be a future date.";
        AddCurrentCondition();
        return this;
    }

    public RuleBuilder<T, TProperty> PastDate()
    {
        _currentCondition = value => value is DateTime date && date < DateTime.Now;
        _currentMessage = $"The {_propertyName} field must be a past date.";
        AddCurrentCondition();
        return this;
    }

    public RuleBuilder<T, TProperty> Today()
    {
        _currentCondition = value => value is DateTime date && date.Date == DateTime.Today;
        _currentMessage = $"The {_propertyName} field must be today's date.";
        AddCurrentCondition();
        return this;
    }

    public RuleBuilder<T, TProperty> HasCount(int count)
    {
        _currentCondition = value =>
            value is System.Collections.IEnumerable enumerable && enumerable.Cast<object>().Count() == count;
        _currentMessage = $"The {_propertyName} field must contain exactly {count} items.";
        AddCurrentCondition();
        return this;
    }

    public RuleBuilder<T, TProperty> NotEmptyCollection()
    {
        _currentCondition = value =>
            value is System.Collections.IEnumerable enumerable && enumerable.Cast<object>().Any();
        _currentMessage = $"The {_propertyName} field must not be an empty collection.";
        AddCurrentCondition();
        return this;
    }

    public RuleBuilder<T, TProperty> MustAsync(Func<TProperty, Task<bool>> predicateAsync)
    {
        if (predicateAsync == null) throw new ArgumentNullException(nameof(predicateAsync));
        
        _validator.AddRule(async instance =>
        {
            ValidationResult result = new ValidationResult();
            TProperty value = _propertyFunc(instance);

            bool isValid = await predicateAsync(value).ConfigureAwait(false);
            if (!isValid)
            {
                string message = _currentMessage ?? $"The field {_propertyName} does not meet the specified condition.";
                result.AddError(_propertyName, message);
            }
            return result;
        });
        AddCurrentCondition();
        return this;
    }

    public RuleBuilder<T, TProperty> DependentRuleAsync<TDependent>(
        Expression<Func<T, TProperty>> propertyExpression, 
        Expression<Func<T, TDependent>> dependentPropertyExpression,
        Func<TProperty, TDependent, Task<bool>> predicateAsync)
    {
        if (predicateAsync == null) throw new ArgumentNullException(nameof(predicateAsync));
        if (propertyExpression == null) throw new ArgumentNullException(nameof(propertyExpression));
        if (dependentPropertyExpression == null) throw new ArgumentNullException(nameof(dependentPropertyExpression));
        
        // Extraer el nombre de la propiedad principal
        var propertyName = ((MemberExpression)propertyExpression.Body).Member.Name;
        var propertyFunc = propertyExpression.Compile();
        
        // Extraer el nombre de la propiedad dependiente
        var dependentPropertyName = ((MemberExpression)dependentPropertyExpression.Body).Member.Name;
        var dependentFunc = dependentPropertyExpression.Compile();
        
        _validator.AddRule(async instance =>
        {
            ValidationResult result = new ValidationResult();
            TProperty value = propertyFunc(instance);
            TDependent dependentValue = dependentFunc(instance);

            bool isValid = await predicateAsync(value, dependentValue).ConfigureAwait(false);
            if (!isValid)
            {
                string message = _currentMessage ?? $"The field {propertyName} does not meet the dependent condition of {dependentPropertyName}.";
                result.AddError(propertyName, message);
            }

            return result;
        });
        AddCurrentCondition();
        return this;
    }
}