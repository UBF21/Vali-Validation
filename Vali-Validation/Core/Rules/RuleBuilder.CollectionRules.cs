namespace Vali_Validation.Core.Rules;

public partial class RuleBuilder<T, TProperty> where T : class
{
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

    public IRuleBuilder<T, TProperty> In(IEnumerable<TProperty> values)
    {
        _currentCondition = values.Contains;
        _currentMessage = $"The {_propertyName} field must be in the list of allowed values.";
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> NotIn(IEnumerable<TProperty> values)
    {
        var list = values.ToList();
        _currentCondition = value => !list.Contains(value);
        _currentMessage = $"The {_propertyName} field must not be in the list of disallowed values.";
        AddCurrentCondition();
        return this;
    }
}
