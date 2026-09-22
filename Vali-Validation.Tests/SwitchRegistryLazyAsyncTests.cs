using Vali_Validation.Core.Validators;
using Xunit;

namespace Vali_Validation.Tests;

public class SwitchDto
{
    public string Type { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public class SyncOnlySwitchValidator : AbstractValidator<SwitchDto>
{
    public int AsyncRuleCount => AsyncRules.Count;

    public SyncOnlySwitchValidator()
    {
        RuleSwitch(x => x.Type)
            .Case("A", v => v.RuleFor(x => x.Value).NotEmpty())
            .Case("B", v => v.RuleFor(x => x.Value).MinimumLength(3))
            .Default(v => v.RuleFor(x => x.Value).NotEmpty());
    }
}

public class MixedSwitchValidator : AbstractValidator<SwitchDto>
{
    public int AsyncRuleCount => AsyncRules.Count;

    public MixedSwitchValidator()
    {
        RuleSwitch(x => x.Type)
            .Case("A", v => v.RuleFor(x => x.Value).NotEmpty())
            .Case("B", v => v.RuleFor(x => x.Value).MustAsync(val => System.Threading.Tasks.Task.FromResult(val.Length > 2)))
            .Default(v => v.RuleFor(x => x.Value).NotEmpty());
    }
}

public class SwitchRegistryLazyAsyncTests
{
    [Fact]
    public void SwitchWithNoAsyncCases_DoesNotRegisterAnAsyncRule()
    {
        var validator = new SyncOnlySwitchValidator();

        Assert.Equal(0, validator.AsyncRuleCount);
    }

    [Fact]
    public void SwitchWithAtLeastOneAsyncCase_RegistersExactlyOneAsyncRule()
    {
        var validator = new MixedSwitchValidator();

        Assert.Equal(1, validator.AsyncRuleCount);
    }

    [Fact]
    public void SyncOnlySwitch_StillValidatesCorrectlyViaValidate()
    {
        var validator = new SyncOnlySwitchValidator();

        var validResult = validator.Validate(new SwitchDto { Type = "A", Value = "x" });
        var invalidResult = validator.Validate(new SwitchDto { Type = "B", Value = "x" });

        Assert.True(validResult.IsValid);
        Assert.False(invalidResult.IsValid);
    }

    [Fact]
    public async System.Threading.Tasks.Task MixedSwitch_AsyncCaseStillValidatesCorrectly()
    {
        var validator = new MixedSwitchValidator();

        var result = await validator.ValidateAsync(new SwitchDto { Type = "B", Value = "x" });

        Assert.False(result.IsValid);
    }
}
