using System;
using System.Collections.Generic;
using Vali_Validation.Core.Validators;
using Vali_Validation.Tests.Models;
using Xunit;

namespace Vali_Validation.Tests;

// ---------------------------------------------------------------------------
// DTOs used by the new tests
// ---------------------------------------------------------------------------

public class DateDto
{
    public DateTime BirthDate { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime ExpiryDate { get; set; }
}

public class OrderDto
{
    public decimal Price { get; set; }
    public decimal Discount { get; set; }
    public string? Iban { get; set; }
    public string? CountryCode { get; set; }
    public string? CurrencyCode { get; set; }
    public string? Slug { get; set; }
    public string? Metadata { get; set; }
    public string? FileContent { get; set; }
    public string? MacAddress { get; set; }
    public string? IpV6 { get; set; }
    public string? HtmlContent { get; set; }
    public string? SearchQuery { get; set; }
    public string? Password { get; set; }
    public double Lat { get; set; }
    public double Lng { get; set; }
    public int Stock { get; set; }
    public decimal TaxRate { get; set; }
    public decimal Factor { get; set; }
    public decimal Amount { get; set; }
}

public class CrossFieldDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? OldPassword { get; set; }
    public string? NewPassword { get; set; }
    public bool IsCompany { get; set; }
    public string? CompanyName { get; set; }
    public int Min { get; set; }
    public int Max { get; set; }
}

// ---------------------------------------------------------------------------
// Cross-property comparison tests
// ---------------------------------------------------------------------------

public class CrossPropertyComparisonTests
{
    private class StartEndValidator : AbstractValidator<CrossFieldDto>
    {
        public StartEndValidator()
        {
            RuleFor(x => x.StartDate).LessThanProperty(x => x.EndDate);
        }
    }

    private class EndStartValidator : AbstractValidator<CrossFieldDto>
    {
        public EndStartValidator()
        {
            RuleFor(x => x.EndDate).GreaterThanProperty(x => x.StartDate);
        }
    }

    private class MinMaxGeValidator : AbstractValidator<CrossFieldDto>
    {
        public MinMaxGeValidator()
        {
            RuleFor(x => x.Max).GreaterThanOrEqualToProperty(x => x.Min);
        }
    }

    private class MinMaxLeValidator : AbstractValidator<CrossFieldDto>
    {
        public MinMaxLeValidator()
        {
            RuleFor(x => x.Min).LessThanOrEqualToProperty(x => x.Max);
        }
    }

    private class PasswordNotEqualValidator : AbstractValidator<CrossFieldDto>
    {
        public PasswordNotEqualValidator()
        {
            RuleFor(x => x.NewPassword).NotEqualToProperty(x => x.OldPassword);
        }
    }

    [Fact]
    public void LessThanProperty_Valid()
    {
        var dto = new CrossFieldDto { StartDate = DateTime.Today, EndDate = DateTime.Today.AddDays(1) };
        var result = new StartEndValidator().Validate(dto);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void LessThanProperty_Invalid()
    {
        var dto = new CrossFieldDto { StartDate = DateTime.Today.AddDays(1), EndDate = DateTime.Today };
        var result = new StartEndValidator().Validate(dto);
        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor("StartDate"));
    }

    [Fact]
    public void LessThanProperty_EqualValues_Invalid()
    {
        var dto = new CrossFieldDto { StartDate = DateTime.Today, EndDate = DateTime.Today };
        var result = new StartEndValidator().Validate(dto);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void GreaterThanProperty_Valid()
    {
        var dto = new CrossFieldDto { StartDate = DateTime.Today, EndDate = DateTime.Today.AddDays(2) };
        var result = new EndStartValidator().Validate(dto);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void GreaterThanProperty_Invalid()
    {
        var dto = new CrossFieldDto { StartDate = DateTime.Today.AddDays(2), EndDate = DateTime.Today };
        var result = new EndStartValidator().Validate(dto);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void GreaterThanOrEqualToProperty_Valid_Greater()
    {
        var dto = new CrossFieldDto { Min = 5, Max = 10 };
        var result = new MinMaxGeValidator().Validate(dto);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void GreaterThanOrEqualToProperty_Valid_Equal()
    {
        var dto = new CrossFieldDto { Min = 10, Max = 10 };
        var result = new MinMaxGeValidator().Validate(dto);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void GreaterThanOrEqualToProperty_Invalid()
    {
        var dto = new CrossFieldDto { Min = 10, Max = 5 };
        var result = new MinMaxGeValidator().Validate(dto);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void LessThanOrEqualToProperty_Valid()
    {
        var dto = new CrossFieldDto { Min = 5, Max = 10 };
        var result = new MinMaxLeValidator().Validate(dto);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void LessThanOrEqualToProperty_Equal_Valid()
    {
        var dto = new CrossFieldDto { Min = 5, Max = 5 };
        var result = new MinMaxLeValidator().Validate(dto);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void LessThanOrEqualToProperty_Invalid()
    {
        var dto = new CrossFieldDto { Min = 10, Max = 5 };
        var result = new MinMaxLeValidator().Validate(dto);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void NotEqualToProperty_Valid()
    {
        var dto = new CrossFieldDto { OldPassword = "old123", NewPassword = "new456" };
        var result = new PasswordNotEqualValidator().Validate(dto);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void NotEqualToProperty_Invalid()
    {
        var dto = new CrossFieldDto { OldPassword = "same", NewPassword = "same" };
        var result = new PasswordNotEqualValidator().Validate(dto);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void NotEqualToProperty_BothNull_Invalid()
    {
        var dto = new CrossFieldDto { OldPassword = null, NewPassword = null };
        var result = new PasswordNotEqualValidator().Validate(dto);
        Assert.False(result.IsValid);
    }
}

// ---------------------------------------------------------------------------
// RequiredIf / RequiredUnless tests
// ---------------------------------------------------------------------------

public class RequiredIfTests
{
    private class RequiredIfIsCompanyValidator : AbstractValidator<CrossFieldDto>
    {
        public RequiredIfIsCompanyValidator()
        {
            RuleFor(x => x.CompanyName).RequiredIf(x => x.IsCompany);
        }
    }

    private class RequiredIfPropertyValidator : AbstractValidator<CrossFieldDto>
    {
        public RequiredIfPropertyValidator()
        {
            RuleFor(x => x.CompanyName).RequiredIf(x => x.OldPassword, "trigger");
        }
    }

    private class RequiredUnlessValidator : AbstractValidator<CrossFieldDto>
    {
        public RequiredUnlessValidator()
        {
            RuleFor(x => x.CompanyName).RequiredUnless(x => !x.IsCompany);
        }
    }

    [Fact]
    public void RequiredIf_ConditionTrue_FieldProvided_Valid()
    {
        var dto = new CrossFieldDto { IsCompany = true, CompanyName = "Acme Corp" };
        var result = new RequiredIfIsCompanyValidator().Validate(dto);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void RequiredIf_ConditionTrue_FieldMissing_Invalid()
    {
        var dto = new CrossFieldDto { IsCompany = true, CompanyName = null };
        var result = new RequiredIfIsCompanyValidator().Validate(dto);
        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor("CompanyName"));
    }

    [Fact]
    public void RequiredIf_ConditionFalse_FieldMissing_Valid()
    {
        var dto = new CrossFieldDto { IsCompany = false, CompanyName = null };
        var result = new RequiredIfIsCompanyValidator().Validate(dto);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void RequiredIf_ConditionTrue_FieldWhitespace_Invalid()
    {
        var dto = new CrossFieldDto { IsCompany = true, CompanyName = "   " };
        var result = new RequiredIfIsCompanyValidator().Validate(dto);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void RequiredIf_OtherPropertyEqualsExpected_Valid()
    {
        var dto = new CrossFieldDto { OldPassword = "trigger", CompanyName = "name" };
        var result = new RequiredIfPropertyValidator().Validate(dto);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void RequiredIf_OtherPropertyEqualsExpected_FieldMissing_Invalid()
    {
        var dto = new CrossFieldDto { OldPassword = "trigger", CompanyName = null };
        var result = new RequiredIfPropertyValidator().Validate(dto);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void RequiredUnless_ConditionTrue_FieldMissing_Valid()
    {
        // RequiredUnless(!x.IsCompany) means required when IsCompany == true
        var dto = new CrossFieldDto { IsCompany = false, CompanyName = null };
        var result = new RequiredUnlessValidator().Validate(dto);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void RequiredUnless_ConditionFalse_FieldMissing_Invalid()
    {
        var dto = new CrossFieldDto { IsCompany = true, CompanyName = null };
        var result = new RequiredUnlessValidator().Validate(dto);
        Assert.False(result.IsValid);
    }
}

// ---------------------------------------------------------------------------
// Date rules tests
// ---------------------------------------------------------------------------

public class DateRulesTests
{
    private class MinAgeValidator : AbstractValidator<DateDto>
    {
        public MinAgeValidator() { RuleFor(x => x.BirthDate).MinAge(18); }
    }

    private class MaxAgeValidator : AbstractValidator<DateDto>
    {
        public MaxAgeValidator() { RuleFor(x => x.BirthDate).MaxAge(65); }
    }

    private class DateBetweenValidator : AbstractValidator<DateDto>
    {
        private static readonly DateTime From = new DateTime(2020, 1, 1);
        private static readonly DateTime To = new DateTime(2025, 12, 31);
        public DateBetweenValidator() { RuleFor(x => x.StartDate).DateBetween(From, To); }
    }

    private class NotExpiredValidator : AbstractValidator<DateDto>
    {
        public NotExpiredValidator() { RuleFor(x => x.ExpiryDate).NotExpired(); }
    }

    private class WithinNextValidator : AbstractValidator<DateDto>
    {
        public WithinNextValidator() { RuleFor(x => x.EndDate).WithinNext(TimeSpan.FromDays(30)); }
    }

    private class WithinLastValidator : AbstractValidator<DateDto>
    {
        public WithinLastValidator() { RuleFor(x => x.StartDate).WithinLast(TimeSpan.FromDays(30)); }
    }

    private class IsWeekdayValidator : AbstractValidator<DateDto>
    {
        public IsWeekdayValidator() { RuleFor(x => x.StartDate).IsWeekday(); }
    }

    private class IsWeekendValidator : AbstractValidator<DateDto>
    {
        public IsWeekendValidator() { RuleFor(x => x.StartDate).IsWeekend(); }
    }

    [Fact]
    public void MinAge_Valid()
    {
        var dto = new DateDto { BirthDate = DateTime.Today.AddYears(-20) };
        Assert.True(new MinAgeValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void MinAge_Exactly18_Valid()
    {
        var dto = new DateDto { BirthDate = DateTime.Today.AddYears(-18) };
        Assert.True(new MinAgeValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void MinAge_Under18_Invalid()
    {
        var dto = new DateDto { BirthDate = DateTime.Today.AddYears(-17).AddDays(-1) };
        Assert.False(new MinAgeValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void MaxAge_Valid()
    {
        var dto = new DateDto { BirthDate = DateTime.Today.AddYears(-30) };
        Assert.True(new MaxAgeValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void MaxAge_Exactly65_Valid()
    {
        var dto = new DateDto { BirthDate = DateTime.Today.AddYears(-65) };
        Assert.True(new MaxAgeValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void MaxAge_Over65_Invalid()
    {
        var dto = new DateDto { BirthDate = DateTime.Today.AddYears(-66) };
        Assert.False(new MaxAgeValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void DateBetween_InRange_Valid()
    {
        var dto = new DateDto { StartDate = new DateTime(2022, 6, 15) };
        Assert.True(new DateBetweenValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void DateBetween_BeforeRange_Invalid()
    {
        var dto = new DateDto { StartDate = new DateTime(2019, 12, 31) };
        Assert.False(new DateBetweenValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void DateBetween_AtBoundary_Valid()
    {
        var dto = new DateDto { StartDate = new DateTime(2020, 1, 1) };
        Assert.True(new DateBetweenValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void NotExpired_FutureDate_Valid()
    {
        var dto = new DateDto { ExpiryDate = DateTime.Now.AddDays(10) };
        Assert.True(new NotExpiredValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void NotExpired_PastDate_Invalid()
    {
        var dto = new DateDto { ExpiryDate = DateTime.Now.AddDays(-1) };
        Assert.False(new NotExpiredValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void WithinNext_ValidFutureWithinSpan()
    {
        var dto = new DateDto { EndDate = DateTime.Now.AddDays(15) };
        Assert.True(new WithinNextValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void WithinNext_FutureBeyondSpan_Invalid()
    {
        var dto = new DateDto { EndDate = DateTime.Now.AddDays(60) };
        Assert.False(new WithinNextValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void WithinNext_PastDate_Invalid()
    {
        var dto = new DateDto { EndDate = DateTime.Now.AddDays(-1) };
        Assert.False(new WithinNextValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void WithinLast_ValidPastWithinSpan()
    {
        var dto = new DateDto { StartDate = DateTime.Now.AddDays(-10) };
        Assert.True(new WithinLastValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void WithinLast_PastBeyondSpan_Invalid()
    {
        var dto = new DateDto { StartDate = DateTime.Now.AddDays(-60) };
        Assert.False(new WithinLastValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void WithinLast_FutureDate_Invalid()
    {
        var dto = new DateDto { StartDate = DateTime.Now.AddDays(1) };
        Assert.False(new WithinLastValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void IsWeekday_Monday_Valid()
    {
        // Find next Monday
        DateTime date = DateTime.Today;
        while (date.DayOfWeek != DayOfWeek.Monday) date = date.AddDays(1);
        var dto = new DateDto { StartDate = date };
        Assert.True(new IsWeekdayValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void IsWeekday_Saturday_Invalid()
    {
        DateTime date = DateTime.Today;
        while (date.DayOfWeek != DayOfWeek.Saturday) date = date.AddDays(1);
        var dto = new DateDto { StartDate = date };
        Assert.False(new IsWeekdayValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void IsWeekend_Saturday_Valid()
    {
        DateTime date = DateTime.Today;
        while (date.DayOfWeek != DayOfWeek.Saturday) date = date.AddDays(1);
        var dto = new DateDto { StartDate = date };
        Assert.True(new IsWeekendValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void IsWeekend_Sunday_Valid()
    {
        DateTime date = DateTime.Today;
        while (date.DayOfWeek != DayOfWeek.Sunday) date = date.AddDays(1);
        var dto = new DateDto { StartDate = date };
        Assert.True(new IsWeekendValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void IsWeekend_Wednesday_Invalid()
    {
        DateTime date = DateTime.Today;
        while (date.DayOfWeek != DayOfWeek.Wednesday) date = date.AddDays(1);
        var dto = new DateDto { StartDate = date };
        Assert.False(new IsWeekendValidator().Validate(dto).IsValid);
    }
}

// ---------------------------------------------------------------------------
// Numeric rules tests
// ---------------------------------------------------------------------------

public class NumericRulesTests
{
    private class NonNegativeValidator : AbstractValidator<OrderDto>
    {
        public NonNegativeValidator() { RuleFor(x => x.Price).NonNegative(); }
    }

    private class PercentageValidator : AbstractValidator<OrderDto>
    {
        public PercentageValidator() { RuleFor(x => x.TaxRate).Percentage(); }
    }

    private class PrecisionValidator : AbstractValidator<OrderDto>
    {
        public PrecisionValidator() { RuleFor(x => x.Price).Precision(10, 2); }
    }

    private class MultipleOfPropertyValidator : AbstractValidator<OrderDto>
    {
        public MultipleOfPropertyValidator() { RuleFor(x => x.Amount).MultipleOfProperty(x => x.Factor); }
    }

    [Fact]
    public void NonNegative_Zero_Valid()
    {
        var dto = new OrderDto { Price = 0m };
        Assert.True(new NonNegativeValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void NonNegative_Positive_Valid()
    {
        var dto = new OrderDto { Price = 5m };
        Assert.True(new NonNegativeValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void NonNegative_Negative_Invalid()
    {
        var dto = new OrderDto { Price = -1m };
        Assert.False(new NonNegativeValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void Percentage_Zero_Valid()
    {
        var dto = new OrderDto { TaxRate = 0m };
        Assert.True(new PercentageValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void Percentage_100_Valid()
    {
        var dto = new OrderDto { TaxRate = 100m };
        Assert.True(new PercentageValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void Percentage_50_Valid()
    {
        var dto = new OrderDto { TaxRate = 50m };
        Assert.True(new PercentageValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void Percentage_Negative_Invalid()
    {
        var dto = new OrderDto { TaxRate = -1m };
        Assert.False(new PercentageValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void Percentage_Over100_Invalid()
    {
        var dto = new OrderDto { TaxRate = 101m };
        Assert.False(new PercentageValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void Precision_Valid()
    {
        var dto = new OrderDto { Price = 12345678.99m };
        Assert.True(new PrecisionValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void Precision_TooManyDecimalPlaces_Invalid()
    {
        var dto = new OrderDto { Price = 1.999m };
        Assert.False(new PrecisionValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void Precision_TooManyTotalDigits_Invalid()
    {
        var dto = new OrderDto { Price = 123456789.99m };
        Assert.False(new PrecisionValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void Precision_ExactlyAtLimit_Valid()
    {
        var dto = new OrderDto { Price = 12345678.99m };
        Assert.True(new PrecisionValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void MultipleOfProperty_Valid()
    {
        var dto = new OrderDto { Amount = 10m, Factor = 5m };
        Assert.True(new MultipleOfPropertyValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void MultipleOfProperty_Invalid()
    {
        var dto = new OrderDto { Amount = 11m, Factor = 5m };
        Assert.False(new MultipleOfPropertyValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void MultipleOfProperty_ZeroFactor_Invalid()
    {
        var dto = new OrderDto { Amount = 10m, Factor = 0m };
        Assert.False(new MultipleOfPropertyValidator().Validate(dto).IsValid);
    }
}

// ---------------------------------------------------------------------------
// String / format rules tests
// ---------------------------------------------------------------------------

public class StringFormatRulesTests
{
    private class SlugValidator : AbstractValidator<OrderDto>
    {
        public SlugValidator() { RuleFor(x => x.Slug).Slug(); }
    }

    private class IPv6Validator : AbstractValidator<OrderDto>
    {
        public IPv6Validator() { RuleFor(x => x.IpV6).IPv6(); }
    }

    private class MacAddressValidator : AbstractValidator<OrderDto>
    {
        public MacAddressValidator() { RuleFor(x => x.MacAddress).MacAddress(); }
    }

    private class LatitudeValidator : AbstractValidator<OrderDto>
    {
        public LatitudeValidator() { RuleFor(x => x.Lat).Latitude(); }
    }

    private class LongitudeValidator : AbstractValidator<OrderDto>
    {
        public LongitudeValidator() { RuleFor(x => x.Lng).Longitude(); }
    }

    private class CountryCodeValidator : AbstractValidator<OrderDto>
    {
        public CountryCodeValidator() { RuleFor(x => x.CountryCode).CountryCode(); }
    }

    private class CurrencyCodeValidator : AbstractValidator<OrderDto>
    {
        public CurrencyCodeValidator() { RuleFor(x => x.CurrencyCode).CurrencyCode(); }
    }

    private class JsonValidator : AbstractValidator<OrderDto>
    {
        public JsonValidator() { RuleFor(x => x.Metadata).IsValidJson(); }
    }

    private class Base64Validator : AbstractValidator<OrderDto>
    {
        public Base64Validator() { RuleFor(x => x.FileContent).IsValidBase64(); }
    }

    private class NoHtmlTagsValidator : AbstractValidator<OrderDto>
    {
        public NoHtmlTagsValidator() { RuleFor(x => x.HtmlContent).NoHtmlTags(); }
    }

    private class NoSqlInjectionValidator : AbstractValidator<OrderDto>
    {
        public NoSqlInjectionValidator() { RuleFor(x => x.SearchQuery).NoSqlInjectionPatterns(); }
    }

    private class IbanValidator : AbstractValidator<OrderDto>
    {
        public IbanValidator() { RuleFor(x => x.Iban).Iban(); }
    }

    private class PasswordPolicyValidator : AbstractValidator<OrderDto>
    {
        public PasswordPolicyValidator() { RuleFor(x => x.Password).PasswordPolicy(); }
    }

    // Slug
    [Fact]
    public void Slug_Valid()
    {
        var dto = new OrderDto { Slug = "hello-world-123" };
        Assert.True(new SlugValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void Slug_Simple_Valid()
    {
        var dto = new OrderDto { Slug = "hello" };
        Assert.True(new SlugValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void Slug_Uppercase_Invalid()
    {
        var dto = new OrderDto { Slug = "Hello-World" };
        Assert.False(new SlugValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void Slug_TrailingHyphen_Invalid()
    {
        var dto = new OrderDto { Slug = "hello-" };
        Assert.False(new SlugValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void Slug_Null_Invalid()
    {
        var dto = new OrderDto { Slug = null };
        Assert.False(new SlugValidator().Validate(dto).IsValid);
    }

    // IPv6
    [Fact]
    public void IPv6_Valid()
    {
        var dto = new OrderDto { IpV6 = "2001:0db8:85a3:0000:0000:8a2e:0370:7334" };
        Assert.True(new IPv6Validator().Validate(dto).IsValid);
    }

    [Fact]
    public void IPv6_Abbreviated_Valid()
    {
        var dto = new OrderDto { IpV6 = "::1" };
        Assert.True(new IPv6Validator().Validate(dto).IsValid);
    }

    [Fact]
    public void IPv6_IPv4Address_Invalid()
    {
        var dto = new OrderDto { IpV6 = "192.168.1.1" };
        Assert.False(new IPv6Validator().Validate(dto).IsValid);
    }

    [Fact]
    public void IPv6_Null_Invalid()
    {
        var dto = new OrderDto { IpV6 = null };
        Assert.False(new IPv6Validator().Validate(dto).IsValid);
    }

    // MAC Address
    [Fact]
    public void MacAddress_ColonSeparated_Valid()
    {
        var dto = new OrderDto { MacAddress = "00:1A:2B:3C:4D:5E" };
        Assert.True(new MacAddressValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void MacAddress_HyphenSeparated_Valid()
    {
        var dto = new OrderDto { MacAddress = "00-1A-2B-3C-4D-5E" };
        Assert.True(new MacAddressValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void MacAddress_Invalid_Format()
    {
        var dto = new OrderDto { MacAddress = "001A2B3C4D5E" };
        Assert.False(new MacAddressValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void MacAddress_Null_Invalid()
    {
        var dto = new OrderDto { MacAddress = null };
        Assert.False(new MacAddressValidator().Validate(dto).IsValid);
    }

    // Latitude
    [Fact]
    public void Latitude_Valid()
    {
        var dto = new OrderDto { Lat = 45.0 };
        Assert.True(new LatitudeValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void Latitude_NegativeValid()
    {
        var dto = new OrderDto { Lat = -90.0 };
        Assert.True(new LatitudeValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void Latitude_MaxBoundary_Valid()
    {
        var dto = new OrderDto { Lat = 90.0 };
        Assert.True(new LatitudeValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void Latitude_ExceedsMax_Invalid()
    {
        var dto = new OrderDto { Lat = 91.0 };
        Assert.False(new LatitudeValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void Latitude_BelowMin_Invalid()
    {
        var dto = new OrderDto { Lat = -91.0 };
        Assert.False(new LatitudeValidator().Validate(dto).IsValid);
    }

    // Longitude
    [Fact]
    public void Longitude_Valid()
    {
        var dto = new OrderDto { Lng = 90.0 };
        Assert.True(new LongitudeValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void Longitude_MaxBoundary_Valid()
    {
        var dto = new OrderDto { Lng = 180.0 };
        Assert.True(new LongitudeValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void Longitude_ExceedsMax_Invalid()
    {
        var dto = new OrderDto { Lng = 181.0 };
        Assert.False(new LongitudeValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void Longitude_BelowMin_Invalid()
    {
        var dto = new OrderDto { Lng = -181.0 };
        Assert.False(new LongitudeValidator().Validate(dto).IsValid);
    }

    // CountryCode
    [Fact]
    public void CountryCode_Valid()
    {
        var dto = new OrderDto { CountryCode = "US" };
        Assert.True(new CountryCodeValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void CountryCode_Peru_Valid()
    {
        var dto = new OrderDto { CountryCode = "PE" };
        Assert.True(new CountryCodeValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void CountryCode_Lowercase_Invalid()
    {
        var dto = new OrderDto { CountryCode = "us" };
        Assert.False(new CountryCodeValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void CountryCode_ThreeLetters_Invalid()
    {
        var dto = new OrderDto { CountryCode = "USA" };
        Assert.False(new CountryCodeValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void CountryCode_Null_Invalid()
    {
        var dto = new OrderDto { CountryCode = null };
        Assert.False(new CountryCodeValidator().Validate(dto).IsValid);
    }

    // CurrencyCode
    [Fact]
    public void CurrencyCode_Valid()
    {
        var dto = new OrderDto { CurrencyCode = "USD" };
        Assert.True(new CurrencyCodeValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void CurrencyCode_EUR_Valid()
    {
        var dto = new OrderDto { CurrencyCode = "EUR" };
        Assert.True(new CurrencyCodeValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void CurrencyCode_Lowercase_Invalid()
    {
        var dto = new OrderDto { CurrencyCode = "usd" };
        Assert.False(new CurrencyCodeValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void CurrencyCode_TwoLetters_Invalid()
    {
        var dto = new OrderDto { CurrencyCode = "US" };
        Assert.False(new CurrencyCodeValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void CurrencyCode_Null_Invalid()
    {
        var dto = new OrderDto { CurrencyCode = null };
        Assert.False(new CurrencyCodeValidator().Validate(dto).IsValid);
    }

    // IsValidJson
    [Fact]
    public void IsValidJson_Object_Valid()
    {
        var dto = new OrderDto { Metadata = "{\"key\":\"value\"}" };
        Assert.True(new JsonValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void IsValidJson_Array_Valid()
    {
        var dto = new OrderDto { Metadata = "[1,2,3]" };
        Assert.True(new JsonValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void IsValidJson_Invalid()
    {
        var dto = new OrderDto { Metadata = "not json" };
        Assert.False(new JsonValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void IsValidJson_Null_Invalid()
    {
        var dto = new OrderDto { Metadata = null };
        Assert.False(new JsonValidator().Validate(dto).IsValid);
    }

    // IsValidBase64
    [Fact]
    public void IsValidBase64_Valid()
    {
        var dto = new OrderDto { FileContent = Convert.ToBase64String(new byte[] { 1, 2, 3 }) };
        Assert.True(new Base64Validator().Validate(dto).IsValid);
    }

    [Fact]
    public void IsValidBase64_Invalid()
    {
        var dto = new OrderDto { FileContent = "not-base64!!!" };
        Assert.False(new Base64Validator().Validate(dto).IsValid);
    }

    [Fact]
    public void IsValidBase64_Null_Invalid()
    {
        var dto = new OrderDto { FileContent = null };
        Assert.False(new Base64Validator().Validate(dto).IsValid);
    }

    // NoHtmlTags
    [Fact]
    public void NoHtmlTags_PlainText_Valid()
    {
        var dto = new OrderDto { HtmlContent = "Hello world" };
        Assert.True(new NoHtmlTagsValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void NoHtmlTags_WithHtml_Invalid()
    {
        var dto = new OrderDto { HtmlContent = "<b>Hello</b>" };
        Assert.False(new NoHtmlTagsValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void NoHtmlTags_EmptyString_Valid()
    {
        var dto = new OrderDto { HtmlContent = "" };
        Assert.True(new NoHtmlTagsValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void NoHtmlTags_Null_Invalid()
    {
        var dto = new OrderDto { HtmlContent = null };
        Assert.False(new NoHtmlTagsValidator().Validate(dto).IsValid);
    }

    // NoSqlInjectionPatterns
    [Fact]
    public void NoSqlInjection_SafeQuery_Valid()
    {
        var dto = new OrderDto { SearchQuery = "hello world" };
        Assert.True(new NoSqlInjectionValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void NoSqlInjection_SelectStatement_Invalid()
    {
        var dto = new OrderDto { SearchQuery = "SELECT * FROM users" };
        Assert.False(new NoSqlInjectionValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void NoSqlInjection_DropTable_Invalid()
    {
        var dto = new OrderDto { SearchQuery = "DROP TABLE users" };
        Assert.False(new NoSqlInjectionValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void NoSqlInjection_Semicolon_Invalid()
    {
        var dto = new OrderDto { SearchQuery = "hello; DROP TABLE users" };
        Assert.False(new NoSqlInjectionValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void NoSqlInjection_Null_Valid()
    {
        // null.ToString() is null, not a match
        var dto = new OrderDto { SearchQuery = null };
        Assert.False(new NoSqlInjectionValidator().Validate(dto).IsValid);
    }

    // IBAN
    [Fact]
    public void Iban_ValidGB()
    {
        var dto = new OrderDto { Iban = "GB82 WEST 1234 5698 7654 32" };
        Assert.True(new IbanValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void Iban_ValidDE()
    {
        var dto = new OrderDto { Iban = "DE89370400440532013000" };
        Assert.True(new IbanValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void Iban_Invalid()
    {
        var dto = new OrderDto { Iban = "GB00INVALID000000000000" };
        Assert.False(new IbanValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void Iban_Null_Invalid()
    {
        var dto = new OrderDto { Iban = null };
        Assert.False(new IbanValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void Iban_TooShort_Invalid()
    {
        var dto = new OrderDto { Iban = "GB82" };
        Assert.False(new IbanValidator().Validate(dto).IsValid);
    }

    // PasswordPolicy
    [Fact]
    public void PasswordPolicy_StrongPassword_Valid()
    {
        var dto = new OrderDto { Password = "Str0ng!Pass" };
        Assert.True(new PasswordPolicyValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void PasswordPolicy_TooShort_Invalid()
    {
        var dto = new OrderDto { Password = "Aa1!" };
        Assert.False(new PasswordPolicyValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void PasswordPolicy_NoUppercase_Invalid()
    {
        var dto = new OrderDto { Password = "str0ng!pass" };
        Assert.False(new PasswordPolicyValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void PasswordPolicy_NoDigit_Invalid()
    {
        var dto = new OrderDto { Password = "Strong!Pass" };
        Assert.False(new PasswordPolicyValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void PasswordPolicy_NoSpecialChar_Invalid()
    {
        var dto = new OrderDto { Password = "Str0ngPass" };
        Assert.False(new PasswordPolicyValidator().Validate(dto).IsValid);
    }
}
