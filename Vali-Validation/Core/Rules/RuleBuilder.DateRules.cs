using Vali_Validation.Core.Localization;

namespace Vali_Validation.Core.Rules;

public partial class RuleBuilder<T, TProperty> where T : class
{
    public IRuleBuilder<T, TProperty> FutureDate()
    {
        _currentCondition = value => value is DateTime date && date > DateTime.Now;
        _currentMessageSpec = MessageSpec.Localized(MessageKey.FutureDate);
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> PastDate()
    {
        _currentCondition = value => value is DateTime date && date < DateTime.Now;
        _currentMessageSpec = MessageSpec.Localized(MessageKey.PastDate);
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> Today()
    {
        _currentCondition = value => value is DateTime date && date.Date == DateTime.Today;
        _currentMessageSpec = MessageSpec.Localized(MessageKey.Today);
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> MinAge(int years)
    {
        _currentCondition = value => value is DateTime dob && DateTime.Today >= dob.Date.AddYears(years);
        _currentMessageSpec = MessageSpec.Localized(MessageKey.MinAge,
            new Dictionary<string, object> { ["years"] = years });
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> MaxAge(int years)
    {
        _currentCondition = value => value is DateTime dob && DateTime.Today <= dob.Date.AddYears(years);
        _currentMessageSpec = MessageSpec.Localized(MessageKey.MaxAge,
            new Dictionary<string, object> { ["years"] = years });
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> DateBetween(DateTime from, DateTime to)
    {
        _currentCondition = value => value is DateTime d && d >= from && d <= to;
        _currentMessageSpec = MessageSpec.Localized(MessageKey.DateBetween, new Dictionary<string, object>
        {
            ["from"] = from.ToString("yyyy-MM-dd"),
            ["to"] = to.ToString("yyyy-MM-dd")
        });
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> NotExpired()
    {
        _currentCondition = value => value is DateTime d && d >= DateTime.Now;
        _currentMessageSpec = MessageSpec.Localized(MessageKey.NotExpired);
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> WithinNext(TimeSpan span)
    {
        _currentCondition = value => value is DateTime d && d > DateTime.Now && d <= DateTime.Now.Add(span);
        _currentMessageSpec = MessageSpec.Localized(MessageKey.WithinNext,
            new Dictionary<string, object> { ["days"] = span.TotalDays.ToString("0") });
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> WithinLast(TimeSpan span)
    {
        _currentCondition = value => value is DateTime d && d < DateTime.Now && d >= DateTime.Now.Subtract(span);
        _currentMessageSpec = MessageSpec.Localized(MessageKey.WithinLast,
            new Dictionary<string, object> { ["days"] = span.TotalDays.ToString("0") });
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> IsWeekday()
    {
        _currentCondition = value =>
            value is DateTime d && d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday;
        _currentMessageSpec = MessageSpec.Localized(MessageKey.IsWeekday);
        AddCurrentCondition();
        return this;
    }

    public IRuleBuilder<T, TProperty> IsWeekend()
    {
        _currentCondition = value =>
            value is DateTime d && (d.DayOfWeek == DayOfWeek.Saturday || d.DayOfWeek == DayOfWeek.Sunday);
        _currentMessageSpec = MessageSpec.Localized(MessageKey.IsWeekend);
        AddCurrentCondition();
        return this;
    }
}
