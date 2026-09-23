using System.Linq.Expressions;
using Vali_Validation.Core.Localization;
using Vali_Validation.Core.Results;
using Vali_Validation.Core.Validators;

namespace Vali_Validation.Core.Rules;

public partial class RuleBuilder<T, TProperty> where T : class
{
    public IRuleBuilder<T, TProperty> Must(Func<TProperty, bool>? predicate)
    {
        _currentCondition = predicate ?? (_ => true);
        _currentMessageSpec = MessageSpec.Localized(MessageKey.Must);
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> EqualTo(TProperty other)
    {
        _currentCondition = value => Equals(value, other);
        _currentMessageSpec = MessageSpec.Localized(MessageKey.EqualTo,
            new Dictionary<string, object> { ["other"] = other! });
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> NotEqual(TProperty other)
    {
        _currentCondition = value => !Equals(value, other);
        _currentMessageSpec = MessageSpec.Localized(MessageKey.NotEqual,
            new Dictionary<string, object> { ["other"] = other! });
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> GreaterThan(IComparable threshold)
    {
        _currentCondition = value => (value as IComparable)?.CompareTo(threshold) > 0;
        _currentMessageSpec = MessageSpec.Localized(MessageKey.GreaterThan,
            new Dictionary<string, object> { ["threshold"] = threshold });
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> LessThan(IComparable threshold)
    {
        _currentCondition = value => (value as IComparable)?.CompareTo(threshold) < 0;
        _currentMessageSpec = MessageSpec.Localized(MessageKey.LessThan,
            new Dictionary<string, object> { ["threshold"] = threshold });
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> GreaterThanOrEqualTo(IComparable threshold)
    {
        _currentCondition = value => (value as IComparable)?.CompareTo(threshold) >= 0;
        _currentMessageSpec = MessageSpec.Localized(MessageKey.GreaterThanOrEqualTo,
            new Dictionary<string, object> { ["threshold"] = threshold });
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> LessThanOrEqualTo(IComparable threshold)
    {
        _currentCondition = value => (value as IComparable)?.CompareTo(threshold) <= 0;
        _currentMessageSpec = MessageSpec.Localized(MessageKey.LessThanOrEqualTo,
            new Dictionary<string, object> { ["threshold"] = threshold });
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
        _currentMessageSpec = MessageSpec.Localized(MessageKey.Between,
            new Dictionary<string, object> { ["min"] = min!, ["max"] = max! });
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
        _currentMessageSpec = MessageSpec.Localized(MessageKey.ExclusiveBetween,
            new Dictionary<string, object> { ["min"] = min!, ["max"] = max! });
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
        _currentMessageSpec = MessageSpec.Localized(MessageKey.LengthBetween,
            new Dictionary<string, object> { ["min"] = min, ["max"] = max });
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> NotNull()
    {
        _currentCondition = value => value != null;
        _currentMessageSpec = MessageSpec.Localized(MessageKey.NotNull);
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> Null()
    {
        _currentCondition = value => value == null;
        _currentMessageSpec = MessageSpec.Localized(MessageKey.Null);
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> Empty()
    {
        _currentCondition = value => string.IsNullOrEmpty(value?.ToString());
        _currentMessageSpec = MessageSpec.Localized(MessageKey.Empty);
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> EqualToProperty(Expression<Func<T, TProperty>> otherExpression)
    {
        var otherName = AbstractValidator<T>.GetPropertyName(otherExpression.Body);
        var otherFunc = otherExpression.Compile();
        MessageSpec spec = MessageSpec.Localized(MessageKey.EqualToProperty,
            new Dictionary<string, object> { ["otherName"] = otherName });
        AddSyncRule(instance =>
        {
            var result = new ValidationResult();
            TProperty value = _propertyFunc != null ? _propertyFunc(instance) : default!;
            TProperty otherValue = otherFunc(instance);
            if (!Equals(value, otherValue))
                result.AddError(_effectivePropertyName, ResolveMessage(spec, _effectivePropertyName, null));
            return result;
        });
        return this;
    }

    public IRuleBuilder<T, TProperty> GreaterThanProperty(Expression<Func<T, TProperty>> otherExpression)
    {
        var otherName = AbstractValidator<T>.GetPropertyName(otherExpression.Body);
        var otherFunc = otherExpression.Compile();
        MessageSpec spec = MessageSpec.Localized(MessageKey.GreaterThanProperty,
            new Dictionary<string, object> { ["otherName"] = otherName });
        AddInstanceCondition(instance =>
        {
            TProperty value = _propertyFunc != null ? _propertyFunc(instance) : default!;
            TProperty other = otherFunc(instance);
            return (value as IComparable)?.CompareTo(other) > 0;
        }, spec);
        return this;
    }

    public IRuleBuilder<T, TProperty> GreaterThanOrEqualToProperty(Expression<Func<T, TProperty>> otherExpression)
    {
        var otherName = AbstractValidator<T>.GetPropertyName(otherExpression.Body);
        var otherFunc = otherExpression.Compile();
        MessageSpec spec = MessageSpec.Localized(MessageKey.GreaterThanOrEqualToProperty,
            new Dictionary<string, object> { ["otherName"] = otherName });
        AddInstanceCondition(instance =>
        {
            TProperty value = _propertyFunc != null ? _propertyFunc(instance) : default!;
            TProperty other = otherFunc(instance);
            int cmp = (value as IComparable)?.CompareTo(other) ?? -1;
            return cmp >= 0;
        }, spec);
        return this;
    }

    public IRuleBuilder<T, TProperty> LessThanProperty(Expression<Func<T, TProperty>> otherExpression)
    {
        var otherName = AbstractValidator<T>.GetPropertyName(otherExpression.Body);
        var otherFunc = otherExpression.Compile();
        MessageSpec spec = MessageSpec.Localized(MessageKey.LessThanProperty,
            new Dictionary<string, object> { ["otherName"] = otherName });
        AddInstanceCondition(instance =>
        {
            TProperty value = _propertyFunc != null ? _propertyFunc(instance) : default!;
            TProperty other = otherFunc(instance);
            return (value as IComparable)?.CompareTo(other) < 0;
        }, spec);
        return this;
    }

    public IRuleBuilder<T, TProperty> LessThanOrEqualToProperty(Expression<Func<T, TProperty>> otherExpression)
    {
        var otherName = AbstractValidator<T>.GetPropertyName(otherExpression.Body);
        var otherFunc = otherExpression.Compile();
        MessageSpec spec = MessageSpec.Localized(MessageKey.LessThanOrEqualToProperty,
            new Dictionary<string, object> { ["otherName"] = otherName });
        AddInstanceCondition(instance =>
        {
            TProperty value = _propertyFunc != null ? _propertyFunc(instance) : default!;
            TProperty other = otherFunc(instance);
            int cmp = (value as IComparable)?.CompareTo(other) ?? 1;
            return cmp <= 0;
        }, spec);
        return this;
    }

    public IRuleBuilder<T, TProperty> NotEqualToProperty(Expression<Func<T, TProperty>> otherExpression)
    {
        var otherName = AbstractValidator<T>.GetPropertyName(otherExpression.Body);
        var otherFunc = otherExpression.Compile();
        MessageSpec spec = MessageSpec.Localized(MessageKey.NotEqualToProperty,
            new Dictionary<string, object> { ["otherName"] = otherName });
        AddInstanceCondition(instance =>
        {
            TProperty value = _propertyFunc != null ? _propertyFunc(instance) : default!;
            TProperty other = otherFunc(instance);
            return !Equals(value, other);
        }, spec);
        return this;
    }

    public IRuleBuilder<T, TProperty> RequiredIf(Func<T, bool> condition)
    {
        MessageSpec spec = MessageSpec.Localized(MessageKey.RequiredIf);
        AddInstanceCondition(instance =>
        {
            if (!condition(instance)) return true;
            TProperty value = _propertyFunc != null ? _propertyFunc(instance) : default!;
            return value != null && !string.IsNullOrWhiteSpace(value.ToString());
        }, spec);
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
