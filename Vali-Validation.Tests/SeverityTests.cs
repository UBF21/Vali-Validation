using System.Text.Json;
using Vali_Validation.Core.Results;
using Vali_Validation.Core.Rules;
using Vali_Validation.Core.Validators;
using Xunit;

namespace Vali_Validation.Tests;

public class SeverityDto
{
    public string? Email { get; set; }
    public decimal Discount { get; set; }
    public string? Name { get; set; }
}

public class SeverityValidator : AbstractValidator<SeverityDto>
{
    public SeverityValidator()
    {
        RuleFor(x => x.Email).NotEmpty();
        RuleFor(x => x.Discount).LessThanOrEqualTo(50m).WithSeverity(Severity.Warning)
            .WithMessage("Discount above 50% requires manager approval.");
        RuleFor(x => x.Name).RequiredIf(x => x.Discount > 0);
    }
}

public class SeverityTests
{
    [Fact]
    public void ValidationResult_SerializesToSingleStructuredFailuresArray_WithDefaultJsonOptions()
    {
        var result = new ValidationResult();
        result.AddFailure("Email", "The Email field must be a valid email address.", Severity.Error);
        result.AddFailure("Discount", "Discount above 50% requires manager approval.", Severity.Warning);

        string json = JsonSerializer.Serialize(result);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.False(root.GetProperty("isValid").GetBoolean());

        var failures = root.GetProperty("failures");
        Assert.Equal(2, failures.GetArrayLength());

        var first = failures[0];
        Assert.Equal("Email", first.GetProperty("property").GetString());
        Assert.Equal("The Email field must be a valid email address.", first.GetProperty("message").GetString());
        Assert.Equal("Error", first.GetProperty("severity").GetString());
        Assert.False(first.TryGetProperty("errorCode", out _));

        var second = failures[1];
        Assert.Equal("Discount", second.GetProperty("property").GetString());
        Assert.Equal("Warning", second.GetProperty("severity").GetString());
    }

    [Fact]
    public void ValidationResult_Serialization_IncludesErrorCodeWhenPresent()
    {
        var result = new ValidationResult();
        result.AddFailure("Email", "Invalid.", Severity.Error, "EMAIL_INVALID");

        string json = JsonSerializer.Serialize(result);
        using var doc = JsonDocument.Parse(json);

        Assert.Equal("EMAIL_INVALID", doc.RootElement.GetProperty("failures")[0].GetProperty("errorCode").GetString());
    }

    [Fact]
    public void ValidationResult_WithNoFailures_SerializesIsValidTrueAndEmptyFailuresArray()
    {
        var result = new ValidationResult();

        string json = JsonSerializer.Serialize(result);
        using var doc = JsonDocument.Parse(json);

        Assert.True(doc.RootElement.GetProperty("isValid").GetBoolean());
        Assert.Equal(0, doc.RootElement.GetProperty("failures").GetArrayLength());
    }

    [Fact]
    public void WithSeverity_Warning_DoesNotBlockIsValid_ButAppearsInFailures()
    {
        var validator = new SeverityValidator();
        var dto = new SeverityDto { Email = "a@b.com", Discount = 75, Name = "x" };

        var result = validator.Validate(dto);

        Assert.True(result.IsValid);
        Assert.Contains(result.Failures, f => f.PropertyName == "Discount" && f.Severity == Severity.Warning);
    }

    [Fact]
    public void WithSeverity_Warning_StillFailsValidationIfAnotherRuleIsError()
    {
        var validator = new SeverityValidator();
        var dto = new SeverityDto { Email = null, Discount = 75, Name = "x" };

        var result = validator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Failures, f => f.PropertyName == "Email" && f.Severity == Severity.Error);
        Assert.Contains(result.Failures, f => f.PropertyName == "Discount" && f.Severity == Severity.Warning);
    }

    [Fact]
    public void RuleWithoutWithSeverity_DefaultsToErrorSeverity()
    {
        var validator = new SeverityValidator();
        var dto = new SeverityDto { Email = null, Discount = 10, Name = "x" };

        var result = validator.Validate(dto);

        var emailFailure = Assert.Single(result.Failures, f => f.PropertyName == "Email");
        Assert.Equal(Severity.Error, emailFailure.Severity);
    }

    [Fact]
    public void WithSeverity_HasNoEffectOnInstanceConditionRules_RequiredIfAlwaysErrorSeverity()
    {
        // Documented boundary: WithSeverity only reaches the last rule added via AddCurrentCondition
        // (the property-level _rules list) — RequiredIf/EqualToProperty/etc. use AddInstanceCondition
        // and are unaffected, exactly like WithMessage/WithErrorCode already are.
        var validator = new SeverityValidator();
        var dto = new SeverityDto { Email = "a@b.com", Discount = 5, Name = null };

        var result = validator.Validate(dto);

        var nameFailure = Assert.Single(result.Failures, f => f.PropertyName == "Name");
        Assert.Equal(Severity.Error, nameFailure.Severity);
    }

    [Fact]
    public void WithSeverity_ComposesWithWithErrorCode_RegardlessOfCallOrder()
    {
        var validator1 = new SeverityOrderValidator1();
        var result1 = validator1.Validate(new SeverityDto { Discount = 75 });
        var failure1 = Assert.Single(result1.Failures);
        Assert.Equal(Severity.Warning, failure1.Severity);
        Assert.Equal("HIGH_DISCOUNT", failure1.ErrorCode);

        var validator2 = new SeverityOrderValidator2();
        var result2 = validator2.Validate(new SeverityDto { Discount = 75 });
        var failure2 = Assert.Single(result2.Failures);
        Assert.Equal(Severity.Warning, failure2.Severity);
        Assert.Equal("HIGH_DISCOUNT", failure2.ErrorCode);
    }

    private class SeverityOrderValidator1 : AbstractValidator<SeverityDto>
    {
        public SeverityOrderValidator1()
            => RuleFor(x => x.Discount).LessThanOrEqualTo(50m).WithSeverity(Severity.Warning).WithErrorCode("HIGH_DISCOUNT");
    }

    private class SeverityOrderValidator2 : AbstractValidator<SeverityDto>
    {
        public SeverityOrderValidator2()
            => RuleFor(x => x.Discount).LessThanOrEqualTo(50m).WithErrorCode("HIGH_DISCOUNT").WithSeverity(Severity.Warning);
    }
}

public class NestedAddressDto
{
    public string? Street { get; set; }
    public decimal ShippingWeight { get; set; }
}

public class NestedAddressValidator : AbstractValidator<NestedAddressDto>
{
    public NestedAddressValidator()
    {
        RuleFor(x => x.Street).NotEmpty();
        RuleFor(x => x.ShippingWeight).LessThanOrEqualTo(20m).WithSeverity(Severity.Warning).WithErrorCode("HEAVY_PACKAGE");
    }
}

public class NestedOrderDto
{
    public NestedAddressDto? Address { get; set; }
}

public class NestedOrderValidator : AbstractValidator<NestedOrderDto>
{
    public NestedOrderValidator()
        => RuleFor(x => x.Address).SetValidator(new NestedAddressValidator());
}

public class SwitchSeverityDto
{
    public string Status { get; set; } = "";
    public decimal Amount { get; set; }
}

public class SwitchSeverityValidator : AbstractValidator<SwitchSeverityDto>
{
    public SwitchSeverityValidator()
    {
        RuleSwitch(x => x.Status)
            .Case("pending", v => v.RuleFor(x => x.Amount).LessThanOrEqualTo(1000m).WithSeverity(Severity.Warning).WithErrorCode("LARGE_PENDING_AMOUNT"));
    }
}

public class NestedAndSwitchSeverityTests
{
    [Fact]
    public void SetValidator_PreservesSeverityAndErrorCodeOfNestedWarnings()
    {
        var validator = new NestedOrderValidator();
        var order = new NestedOrderDto { Address = new NestedAddressDto { Street = "Main St", ShippingWeight = 25 } };

        var result = validator.Validate(order);

        Assert.True(result.IsValid);
        var failure = Assert.Single(result.Failures);
        Assert.Equal("Address.ShippingWeight", failure.PropertyName);
        Assert.Equal(Severity.Warning, failure.Severity);
        Assert.Equal("HEAVY_PACKAGE", failure.ErrorCode);
    }

    [Fact]
    public void RuleSwitch_PreservesSeverityAndErrorCodeOfCaseWarnings()
    {
        var validator = new SwitchSeverityValidator();
        var dto = new SwitchSeverityDto { Status = "pending", Amount = 5000 };

        var result = validator.Validate(dto);

        Assert.True(result.IsValid);
        var failure = Assert.Single(result.Failures);
        Assert.Equal(Severity.Warning, failure.Severity);
        Assert.Equal("LARGE_PENDING_AMOUNT", failure.ErrorCode);
    }
}
