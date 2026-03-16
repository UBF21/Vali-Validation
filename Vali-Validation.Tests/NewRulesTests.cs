using Vali_Validation.Core.Results;
using Vali_Validation.Core.Validators;
using Xunit;

namespace Vali_Validation.Tests;

// ---------------------------------------------------------------------------
// Models used only in this test file
// ---------------------------------------------------------------------------

public class SampleDto
{
    public string? Name { get; set; }
    public string? Password { get; set; }
    public string? PasswordConfirm { get; set; }
    public string? Phone { get; set; }
    public string? IpAddress { get; set; }
    public string? CreditCardNumber { get; set; }
    public decimal Price { get; set; }
    public int Count { get; set; }
    public string? GuidValue { get; set; }
    public bool IsActive { get; set; }
}

// ---------------------------------------------------------------------------
// Validators
// ---------------------------------------------------------------------------

internal class GreaterThanOrEqualToValidator : AbstractValidator<SampleDto>
{
    public GreaterThanOrEqualToValidator(int min)
    {
        RuleFor(x => x.Count).GreaterThanOrEqualTo(min);
    }
}

internal class LessThanOrEqualToValidator : AbstractValidator<SampleDto>
{
    public LessThanOrEqualToValidator(int max)
    {
        RuleFor(x => x.Count).LessThanOrEqualTo(max);
    }
}

internal class EqualToPropertyValidator : AbstractValidator<SampleDto>
{
    public EqualToPropertyValidator()
    {
        RuleFor(x => x.PasswordConfirm).EqualToProperty(x => x.Password);
    }
}

internal class NotContainsValidator : AbstractValidator<SampleDto>
{
    public NotContainsValidator()
    {
        RuleFor(x => x.Name).NotContains("bad");
    }
}

internal class NoWhitespaceValidator : AbstractValidator<SampleDto>
{
    public NoWhitespaceValidator()
    {
        RuleFor(x => x.Name).NoWhitespace();
    }
}

internal class PhoneNumberValidator : AbstractValidator<SampleDto>
{
    public PhoneNumberValidator()
    {
        RuleFor(x => x.Phone).PhoneNumber();
    }
}

internal class IPv4Validator : AbstractValidator<SampleDto>
{
    public IPv4Validator()
    {
        RuleFor(x => x.IpAddress).IPv4();
    }
}

internal class CreditCardValidator : AbstractValidator<SampleDto>
{
    public CreditCardValidator()
    {
        RuleFor(x => x.CreditCardNumber).CreditCard();
    }
}

internal class MaxDecimalPlacesValidator : AbstractValidator<SampleDto>
{
    public MaxDecimalPlacesValidator(int places)
    {
        RuleFor(x => x.Price).MaxDecimalPlaces(places);
    }
}

internal class MultipleOfValidator : AbstractValidator<SampleDto>
{
    public MultipleOfValidator(decimal factor)
    {
        RuleFor(x => x.Price).MultipleOf(factor);
    }
}

internal class OddValidator : AbstractValidator<SampleDto>
{
    public OddValidator()
    {
        RuleFor(x => x.Count).Odd();
    }
}

internal class EvenValidator : AbstractValidator<SampleDto>
{
    public EvenValidator()
    {
        RuleFor(x => x.Count).Even();
    }
}

internal class NotEmptyGuidValidator : AbstractValidator<SampleDto>
{
    public NotEmptyGuidValidator()
    {
        RuleFor(x => x.GuidValue).NotEmptyGuid();
    }
}

internal class WhenAsyncValidator : AbstractValidator<SampleDto>
{
    public WhenAsyncValidator()
    {
        // Rule only runs when IsActive == true
        RuleFor(x => x.Name)
            .NotEmpty()
            .WhenAsync((instance, _) => Task.FromResult(instance.IsActive));
    }
}

internal class UnlessAsyncValidator : AbstractValidator<SampleDto>
{
    public UnlessAsyncValidator()
    {
        // Rule runs unless IsActive == true
        RuleFor(x => x.Name)
            .NotEmpty()
            .UnlessAsync((instance, _) => Task.FromResult(instance.IsActive));
    }
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

public class NewRulesTests
{
    // GreaterThanOrEqualTo
    [Fact]
    public void GreaterThanOrEqualTo_WhenEqual_IsValid()
    {
        var v = new GreaterThanOrEqualToValidator(5);
        var result = v.Validate(new SampleDto { Count = 5 });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void GreaterThanOrEqualTo_WhenAbove_IsValid()
    {
        var v = new GreaterThanOrEqualToValidator(5);
        var result = v.Validate(new SampleDto { Count = 10 });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void GreaterThanOrEqualTo_WhenBelow_IsInvalid()
    {
        var v = new GreaterThanOrEqualToValidator(5);
        var result = v.Validate(new SampleDto { Count = 4 });
        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor("Count"));
    }

    // LessThanOrEqualTo
    [Fact]
    public void LessThanOrEqualTo_WhenEqual_IsValid()
    {
        var v = new LessThanOrEqualToValidator(10);
        var result = v.Validate(new SampleDto { Count = 10 });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void LessThanOrEqualTo_WhenBelow_IsValid()
    {
        var v = new LessThanOrEqualToValidator(10);
        var result = v.Validate(new SampleDto { Count = 3 });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void LessThanOrEqualTo_WhenAbove_IsInvalid()
    {
        var v = new LessThanOrEqualToValidator(10);
        var result = v.Validate(new SampleDto { Count = 11 });
        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor("Count"));
    }

    // EqualToProperty
    [Fact]
    public void EqualToProperty_WhenEqual_IsValid()
    {
        var v = new EqualToPropertyValidator();
        var result = v.Validate(new SampleDto { Password = "secret", PasswordConfirm = "secret" });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void EqualToProperty_WhenNotEqual_IsInvalid()
    {
        var v = new EqualToPropertyValidator();
        var result = v.Validate(new SampleDto { Password = "secret", PasswordConfirm = "other" });
        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor("PasswordConfirm"));
    }

    // NotContains
    [Fact]
    public void NotContains_WhenAbsent_IsValid()
    {
        var v = new NotContainsValidator();
        var result = v.Validate(new SampleDto { Name = "goodvalue" });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void NotContains_WhenPresent_IsInvalid()
    {
        var v = new NotContainsValidator();
        var result = v.Validate(new SampleDto { Name = "thisbadword" });
        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor("Name"));
    }

    [Fact]
    public void NotContains_IsCaseInsensitiveByDefault()
    {
        var v = new NotContainsValidator();
        var result = v.Validate(new SampleDto { Name = "thisBadWord" });
        Assert.False(result.IsValid);
    }

    // NoWhitespace
    [Fact]
    public void NoWhitespace_WhenNoSpaces_IsValid()
    {
        var v = new NoWhitespaceValidator();
        var result = v.Validate(new SampleDto { Name = "nospaces" });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void NoWhitespace_WhenHasSpace_IsInvalid()
    {
        var v = new NoWhitespaceValidator();
        var result = v.Validate(new SampleDto { Name = "has space" });
        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor("Name"));
    }

    [Fact]
    public void NoWhitespace_WhenNull_IsInvalid()
    {
        var v = new NoWhitespaceValidator();
        var result = v.Validate(new SampleDto { Name = null });
        Assert.False(result.IsValid);
    }

    // PhoneNumber
    [Theory]
    [InlineData("+12025551234")]
    [InlineData("+442071234567")]
    [InlineData("12025551234")]
    public void PhoneNumber_ValidE164_IsValid(string phone)
    {
        var v = new PhoneNumberValidator();
        var result = v.Validate(new SampleDto { Phone = phone });
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("+0123456")]
    [InlineData("")]
    public void PhoneNumber_Invalid_IsInvalid(string phone)
    {
        var v = new PhoneNumberValidator();
        var result = v.Validate(new SampleDto { Phone = phone });
        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor("Phone"));
    }

    // IPv4
    [Theory]
    [InlineData("192.168.1.1")]
    [InlineData("0.0.0.0")]
    [InlineData("255.255.255.255")]
    public void IPv4_Valid_IsValid(string ip)
    {
        var v = new IPv4Validator();
        var result = v.Validate(new SampleDto { IpAddress = ip });
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("256.0.0.1")]
    [InlineData("192.168.1")]
    [InlineData("not-an-ip")]
    public void IPv4_Invalid_IsInvalid(string ip)
    {
        var v = new IPv4Validator();
        var result = v.Validate(new SampleDto { IpAddress = ip });
        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor("IpAddress"));
    }

    // CreditCard (Luhn)
    [Theory]
    [InlineData("4532015112830366")]   // Visa test number
    [InlineData("5425233430109903")]   // Mastercard test number
    public void CreditCard_ValidLuhn_IsValid(string card)
    {
        var v = new CreditCardValidator();
        var result = v.Validate(new SampleDto { CreditCardNumber = card });
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("1234567890123456")]   // Fails Luhn
    [InlineData("0000")]               // Too short
    [InlineData("notacard")]
    public void CreditCard_Invalid_IsInvalid(string card)
    {
        var v = new CreditCardValidator();
        var result = v.Validate(new SampleDto { CreditCardNumber = card });
        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor("CreditCardNumber"));
    }

    // MaxDecimalPlaces
    [Fact]
    public void MaxDecimalPlaces_WhenWithinLimit_IsValid()
    {
        var v = new MaxDecimalPlacesValidator(2);
        var result = v.Validate(new SampleDto { Price = 9.99m });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void MaxDecimalPlaces_WhenNoDecimal_IsValid()
    {
        var v = new MaxDecimalPlacesValidator(2);
        var result = v.Validate(new SampleDto { Price = 10m });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void MaxDecimalPlaces_WhenExceedsLimit_IsInvalid()
    {
        var v = new MaxDecimalPlacesValidator(2);
        var result = v.Validate(new SampleDto { Price = 9.999m });
        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor("Price"));
    }

    // MultipleOf
    [Fact]
    public void MultipleOf_WhenExact_IsValid()
    {
        var v = new MultipleOfValidator(5m);
        var result = v.Validate(new SampleDto { Price = 25m });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void MultipleOf_WhenNotMultiple_IsInvalid()
    {
        var v = new MultipleOfValidator(5m);
        var result = v.Validate(new SampleDto { Price = 27m });
        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor("Price"));
    }

    // Odd
    [Fact]
    public void Odd_WhenOdd_IsValid()
    {
        var v = new OddValidator();
        var result = v.Validate(new SampleDto { Count = 7 });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Odd_WhenEven_IsInvalid()
    {
        var v = new OddValidator();
        var result = v.Validate(new SampleDto { Count = 8 });
        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor("Count"));
    }

    // Even
    [Fact]
    public void Even_WhenEven_IsValid()
    {
        var v = new EvenValidator();
        var result = v.Validate(new SampleDto { Count = 4 });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Even_WhenOdd_IsInvalid()
    {
        var v = new EvenValidator();
        var result = v.Validate(new SampleDto { Count = 3 });
        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor("Count"));
    }

    // NotEmptyGuid
    [Fact]
    public void NotEmptyGuid_WhenRealGuid_IsValid()
    {
        var v = new NotEmptyGuidValidator();
        var result = v.Validate(new SampleDto { GuidValue = Guid.NewGuid().ToString() });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void NotEmptyGuid_WhenEmptyGuid_IsInvalid()
    {
        var v = new NotEmptyGuidValidator();
        var result = v.Validate(new SampleDto { GuidValue = Guid.Empty.ToString() });
        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor("GuidValue"));
    }

    [Fact]
    public void NotEmptyGuid_WhenNull_IsInvalid()
    {
        var v = new NotEmptyGuidValidator();
        var result = v.Validate(new SampleDto { GuidValue = null });
        Assert.False(result.IsValid);
    }

    [Fact]
    public void NotEmptyGuid_WhenNotGuid_IsInvalid()
    {
        var v = new NotEmptyGuidValidator();
        var result = v.Validate(new SampleDto { GuidValue = "notaguidestring" });
        Assert.False(result.IsValid);
    }

    // WhenAsync
    [Fact]
    public async Task WhenAsync_ConditionTrue_RuleRuns()
    {
        var v = new WhenAsyncValidator();
        // IsActive=true => rule runs => Name=null => invalid
        var result = await v.ValidateAsync(new SampleDto { IsActive = true, Name = null });
        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor("Name"));
    }

    [Fact]
    public async Task WhenAsync_ConditionFalse_RuleSkipped()
    {
        var v = new WhenAsyncValidator();
        // IsActive=false => rule is skipped => Name=null but no error
        var result = await v.ValidateAsync(new SampleDto { IsActive = false, Name = null });
        Assert.True(result.IsValid);
    }

    // UnlessAsync
    [Fact]
    public async Task UnlessAsync_ConditionTrue_RuleSkipped()
    {
        var v = new UnlessAsyncValidator();
        // IsActive=true => rule is skipped => Name=null but no error
        var result = await v.ValidateAsync(new SampleDto { IsActive = true, Name = null });
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task UnlessAsync_ConditionFalse_RuleRuns()
    {
        var v = new UnlessAsyncValidator();
        // IsActive=false => rule runs => Name=null => invalid
        var result = await v.ValidateAsync(new SampleDto { IsActive = false, Name = null });
        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor("Name"));
    }

    // ValidationResult.Merge / ErrorCount / PropertyNames
    [Fact]
    public void ValidationResult_ErrorCount_CountsAllErrors()
    {
        var result = new ValidationResult();
        result.AddError("Name", "Error 1");
        result.AddError("Name", "Error 2");
        result.AddError("Email", "Error 3");
        Assert.Equal(3, result.ErrorCount);
    }

    [Fact]
    public void ValidationResult_ErrorCount_WhenNoErrors_IsZero()
    {
        var result = new ValidationResult();
        Assert.Equal(0, result.ErrorCount);
    }

    [Fact]
    public void ValidationResult_PropertyNames_ReturnsAllKeys()
    {
        var result = new ValidationResult();
        result.AddError("Name", "E1");
        result.AddError("Email", "E2");
        result.AddError("Phone", "E3");

        var names = result.PropertyNames;
        Assert.Equal(3, names.Count);
        Assert.Contains("Name", names);
        Assert.Contains("Email", names);
        Assert.Contains("Phone", names);
    }

    [Fact]
    public void ValidationResult_PropertyNames_WhenEmpty_IsEmpty()
    {
        var result = new ValidationResult();
        Assert.Empty(result.PropertyNames);
    }

    [Fact]
    public void ValidationResult_Merge_CombinesErrors()
    {
        var a = new ValidationResult();
        a.AddError("Name", "Error A1");
        a.AddError("Email", "Error A2");

        var b = new ValidationResult();
        b.AddError("Name", "Error B1");
        b.AddError("Phone", "Error B2");

        a.Merge(b);

        Assert.Equal(2, a.ErrorsFor("Name").Count);
        Assert.Contains("Error A1", a.ErrorsFor("Name"));
        Assert.Contains("Error B1", a.ErrorsFor("Name"));
        Assert.True(a.HasErrorFor("Email"));
        Assert.True(a.HasErrorFor("Phone"));
        Assert.Equal(4, a.ErrorCount);
    }

    [Fact]
    public void ValidationResult_Merge_WithEmptyOther_IsUnchanged()
    {
        var a = new ValidationResult();
        a.AddError("Name", "Error 1");

        var b = new ValidationResult();
        a.Merge(b);

        Assert.Equal(1, a.ErrorCount);
    }
}
