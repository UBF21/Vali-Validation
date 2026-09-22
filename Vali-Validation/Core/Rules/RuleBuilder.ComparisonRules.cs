using System.Linq.Expressions;
using Vali_Validation.Core.Results;
using Vali_Validation.Core.Validators;

namespace Vali_Validation.Core.Rules;

public partial class RuleBuilder<T, TProperty> where T : class
{
    public IRuleBuilder<T, TProperty> Must(Func<TProperty, bool>? predicate)
    {
        _currentCondition = predicate ?? (_ => true);
        _currentMessage = $"The {_propertyName} field does not meet the specified condition.";
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

    public IRuleBuilder<T, TProperty> NotEqual(TProperty other)
    {
        _currentCondition = value => !Equals(value, other);
        _currentMessage = $"The {_propertyName} field must not be equal to '{other}'.";
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
}
