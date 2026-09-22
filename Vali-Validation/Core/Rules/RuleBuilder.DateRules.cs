namespace Vali_Validation.Core.Rules;

public partial class RuleBuilder<T, TProperty> where T : class
{
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
}
