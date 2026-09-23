using Vali_Validation.Core.Localization;

namespace Vali_Validation.Core.Rules;

public partial class RuleBuilder<T, TProperty> where T : class
{
    public IRuleBuilder<T, TProperty> HasCount(int count)
    {
        _currentCondition = value =>
            value is System.Collections.IEnumerable enumerable &&
            enumerable.Cast<object>().Count() == count;
        _currentMessageSpec = MessageSpec.Localized(MessageKey.HasCount,
            new Dictionary<string, object> { ["count"] = count });
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> NotEmptyCollection()
    {
        _currentCondition = value =>
            value is System.Collections.IEnumerable enumerable &&
            enumerable.Cast<object>().Any();
        _currentMessageSpec = MessageSpec.Localized(MessageKey.NotEmptyCollection);
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> MinCount(int min)
    {
        _currentCondition = value => value is System.Collections.IEnumerable e && e.Cast<object>().Count() >= min;
        _currentMessageSpec = MessageSpec.Localized(MessageKey.MinCount,
            new Dictionary<string, object> { ["min"] = min });
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> MaxCount(int max)
    {
        _currentCondition = value => value is System.Collections.IEnumerable e && e.Cast<object>().Count() <= max;
        _currentMessageSpec = MessageSpec.Localized(MessageKey.MaxCount,
            new Dictionary<string, object> { ["max"] = max });
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
        _currentMessageSpec = MessageSpec.Localized(MessageKey.Unique);
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> AllSatisfy(Func<object, bool> predicate)
    {
        _currentCondition = value => value is System.Collections.IEnumerable e && e.Cast<object>().All(predicate);
        _currentMessageSpec = MessageSpec.Localized(MessageKey.AllSatisfy);
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> AnySatisfy(Func<object, bool> predicate)
    {
        _currentCondition = value => value is System.Collections.IEnumerable e && e.Cast<object>().Any(predicate);
        _currentMessageSpec = MessageSpec.Localized(MessageKey.AnySatisfy);
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> In(IEnumerable<TProperty> values)
    {
        _currentCondition = values.Contains;
        _currentMessageSpec = MessageSpec.Localized(MessageKey.In);
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> NotIn(IEnumerable<TProperty> values)
    {
        var list = values.ToList();
        _currentCondition = value => !list.Contains(value);
        _currentMessageSpec = MessageSpec.Localized(MessageKey.NotIn);
        AddCurrentCondition();
        return this;
    }
}
