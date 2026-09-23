using Vali_Validation.Core.Results;
using Vali_Validation.Core.Rules;
using Vali_Validation.Core.Validators;
using Xunit;

namespace Vali_Validation.Tests.Localization;

public class DateRulesLocalizationDto
{
    public DateTime? Value { get; set; }
}

public class DateRulesLocalizationTests
{
    private static ValidationResult ValidateWith(Action<IRuleBuilder<DateRulesLocalizationDto, DateTime?>> configure, DateTime? value)
    {
        var v = new InlineValidator(configure);
        return v.Validate(new DateRulesLocalizationDto { Value = value }, opts => opts.WithLanguage("en"));
    }

    private class InlineValidator : AbstractValidator<DateRulesLocalizationDto>
    {
        public InlineValidator(Action<IRuleBuilder<DateRulesLocalizationDto, DateTime?>> configure)
            => configure(RuleFor(x => x.Value));
    }

    [Fact] public void FutureDate_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must be a future date.",
            ValidateWith(r => r.FutureDate(), DateTime.Now.AddDays(-1)).Errors["Value"][0]);

    [Fact] public void PastDate_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must be a past date.",
            ValidateWith(r => r.PastDate(), DateTime.Now.AddDays(1)).Errors["Value"][0]);

    [Fact] public void Today_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must be today's date.",
            ValidateWith(r => r.Today(), DateTime.Today.AddDays(-1)).Errors["Value"][0]);

    [Fact] public void MinAge_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field requires a minimum age of 18 years.",
            ValidateWith(r => r.MinAge(18), DateTime.Today.AddYears(-10)).Errors["Value"][0]);

    [Fact] public void MaxAge_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must correspond to a maximum age of 18 years.",
            ValidateWith(r => r.MaxAge(18), DateTime.Today.AddYears(-30)).Errors["Value"][0]);

    [Fact] public void DateBetween_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must be between 2020-01-01 and 2020-12-31.",
            ValidateWith(r => r.DateBetween(new DateTime(2020, 1, 1), new DateTime(2020, 12, 31)), new DateTime(2021, 1, 1)).Errors["Value"][0]);

    [Fact] public void NotExpired_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must not be expired.",
            ValidateWith(r => r.NotExpired(), DateTime.Now.AddDays(-1)).Errors["Value"][0]);

    [Fact] public void WithinNext_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must be within the next 5 days.",
            ValidateWith(r => r.WithinNext(TimeSpan.FromDays(5)), DateTime.Now.AddDays(10)).Errors["Value"][0]);

    [Fact] public void WithinLast_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must be within the last 5 days.",
            ValidateWith(r => r.WithinLast(TimeSpan.FromDays(5)), DateTime.Now.AddDays(-10)).Errors["Value"][0]);

    [Fact] public void IsWeekday_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must be a weekday.",
            ValidateWith(r => r.IsWeekday(), NextDayOfWeek(DayOfWeek.Saturday)).Errors["Value"][0]);

    [Fact] public void IsWeekend_Default_MatchesOriginalEnglishText() =>
        Assert.Equal("The Value field must be a weekend day.",
            ValidateWith(r => r.IsWeekend(), NextDayOfWeek(DayOfWeek.Monday)).Errors["Value"][0]);

    private static DateTime NextDayOfWeek(DayOfWeek dayOfWeek)
    {
        var date = DateTime.Today;
        while (date.DayOfWeek != dayOfWeek) date = date.AddDays(1);
        return date;
    }

    // Spanish spot-checks
    [Fact] public void FutureDate_Spanish_MatchesSpanishCatalog()
    {
        var v = new InlineValidator(r => r.FutureDate());
        var result = v.Validate(new DateRulesLocalizationDto { Value = DateTime.Now.AddDays(-1) }, opts => opts.WithLanguage("es"));
        Assert.Equal("El campo Value debe ser una fecha futura.", result.Errors["Value"][0]);
    }

    [Fact] public void DateBetween_Spanish_MatchesSpanishCatalog()
    {
        var v = new InlineValidator(r => r.DateBetween(new DateTime(2020, 1, 1), new DateTime(2020, 12, 31)));
        var result = v.Validate(new DateRulesLocalizationDto { Value = new DateTime(2021, 1, 1) }, opts => opts.WithLanguage("es"));
        Assert.Equal("El campo Value debe estar entre 2020-01-01 y 2020-12-31.", result.Errors["Value"][0]);
    }
}
