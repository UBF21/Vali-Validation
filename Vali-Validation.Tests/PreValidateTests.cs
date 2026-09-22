using Vali_Validation.Core.Results;
using Vali_Validation.Core.Validators;
using Xunit;

namespace Vali_Validation.Tests;

public class PreValidateDto
{
    public string? Name { get; set; }
}

public class RejectingPreValidateValidator : AbstractValidator<PreValidateDto>
{
    public int RuleInvocations;

    public RejectingPreValidateValidator()
    {
        RuleFor(x => x.Name).Must(_ =>
        {
            RuleInvocations++;
            return true;
        });
    }

    protected override bool PreValidate(PreValidateDto instance, ValidationResult result)
    {
        if (instance.Name == null)
        {
            result.AddError("Name", "Instance name must not be null.");
            return false;
        }
        return true;
    }
}

public class SilentPassPreValidateValidator : AbstractValidator<PreValidateDto>
{
    public SilentPassPreValidateValidator()
    {
        RuleFor(x => x.Name).Must(_ => true);
    }

    protected override bool PreValidate(PreValidateDto instance, ValidationResult result)
    {
        // Dangerous: returning false WITHOUT adding an error produces a silent pass.
        if (instance.Name == null)
            return false;
        return true;
    }
}

public class PreValidateTests
{
    [Fact]
    public void Validate_WhenPreValidateReturnsFalse_SkipsRulesAndReturnsPreValidateErrors()
    {
        var validator = new RejectingPreValidateValidator();

        var result = validator.Validate(new PreValidateDto { Name = null });

        Assert.False(result.IsValid);
        Assert.Equal("Instance name must not be null.", result.FirstError("Name"));
        Assert.Equal(0, validator.RuleInvocations);
    }

    [Fact]
    public void Validate_WhenPreValidateReturnsTrue_RunsRulesNormally()
    {
        var validator = new RejectingPreValidateValidator();

        var result = validator.Validate(new PreValidateDto { Name = "Ana" });

        Assert.True(result.IsValid);
        Assert.Equal(1, validator.RuleInvocations);
    }

    [Fact]
    public async Task ValidateAsync_WhenPreValidateReturnsFalse_SkipsRulesAndReturnsPreValidateErrors()
    {
        var validator = new RejectingPreValidateValidator();

        var result = await validator.ValidateAsync(new PreValidateDto { Name = null });

        Assert.False(result.IsValid);
        Assert.Equal(0, validator.RuleInvocations);
    }

    [Fact]
    public async Task ValidateParallelAsync_WhenPreValidateReturnsFalse_SkipsRulesAndReturnsPreValidateErrors()
    {
        var validator = new RejectingPreValidateValidator();

        var result = await validator.ValidateParallelAsync(new PreValidateDto { Name = null });

        Assert.False(result.IsValid);
        Assert.Equal(0, validator.RuleInvocations);
    }

    [Fact]
    public void Validate_WhenPreValidateReturnsFalseWithoutAddingError_ProducesSilentPass()
    {
        // TRAP: If PreValidate returns false WITHOUT adding an error, Validate() returns
        // an empty, valid result — even though rule evaluation was skipped. This is the
        // most dangerous misuse of the hook and should be documented as a contract.
        var validator = new SilentPassPreValidateValidator();

        var result = validator.Validate(new PreValidateDto { Name = null });

        // Despite returning false and skipping rules, the result is valid because
        // no error was added. This is the silent-pass trap documented in PreValidate's
        // XML doc warning.
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }
}
