using Vali_Validation.Core.Validators;
using Xunit;

namespace Vali_Validation.Tests;

public class CascadeEquivalenceDto
{
    public string? Value { get; set; }
}

public class RuleLevelCascadeValidator : AbstractValidator<CascadeEquivalenceDto>
{
    public int SecondRuleInvocations;

    public RuleLevelCascadeValidator()
    {
        RuleFor(x => x.Value)
            .NotEmpty()
            .Must(_ =>
            {
                SecondRuleInvocations++;
                return true;
            })
            .StopOnFirstFailure();
    }
}

public class ClassLevelCascadeDto
{
    public string? A { get; set; }
    public string? B { get; set; }
}

public class ClassLevelCascadeValidator : AbstractValidator<ClassLevelCascadeDto>
{
    public int SecondPropertyRuleInvocations;

    protected override CascadeMode GlobalCascadeMode => CascadeMode.StopOnFirstFailure;

    public ClassLevelCascadeValidator()
    {
        RuleFor(x => x.A).NotEmpty();
        RuleFor(x => x.B).Must(_ =>
        {
            SecondPropertyRuleInvocations++;
            return true;
        });
    }
}

public class CascadeModeEquivalenceTests
{
    [Fact]
    public void StopOnFirstFailure_MatchesFluentValidation_RuleLevelCascadeStop()
    {
        // FluentValidation's rule-level CascadeMode.Stop: once one validator on a property
        // fails, later validators chained on the SAME property never run.
        var validator = new RuleLevelCascadeValidator();

        var result = validator.Validate(new CascadeEquivalenceDto { Value = "" });

        Assert.False(result.IsValid);
        Assert.Equal(0, validator.SecondRuleInvocations);
    }

    [Fact]
    public void GlobalCascadeMode_StopOnFirstFailure_MatchesFluentValidation_ClassLevelCascadeStop()
    {
        // FluentValidation's class-level CascadeMode.Stop: once any rule anywhere in the
        // validator fails, rules on OTHER properties never run.
        var validator = new ClassLevelCascadeValidator();

        var result = validator.Validate(new ClassLevelCascadeDto { A = "", B = "anything" });

        Assert.False(result.IsValid);
        Assert.Equal(0, validator.SecondPropertyRuleInvocations);
    }
}
