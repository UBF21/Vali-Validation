using Vali_Validation.Core.Validators;
using Vali_Validation.Tests.Models;
using Xunit;

namespace Vali_Validation.Tests;

public class WhenValidator : AbstractValidator<PersonDto>
{
    public WhenValidator()
    {
        // Only validate Email when Age >= 18
        RuleFor(x => x.Email)
            .NotEmpty()
            .Email()
            .When(x => x.Age >= 18);
    }
}

public class UnlessValidator : AbstractValidator<PersonDto>
{
    public UnlessValidator()
    {
        // Skip Name validation unless Age is 0 (i.e. validate when Age != 0)
        RuleFor(x => x.Name)
            .NotEmpty()
            .Unless(x => x.Age == 0);
    }
}

public class StopOnFirstFailureValidator : AbstractValidator<PersonDto>
{
    public StopOnFirstFailureValidator()
    {
        RuleFor(x => x.Name)
            .NotNull()
            .NotEmpty()
            .MinimumLength(3)
            .StopOnFirstFailure();
    }
}

public class ConditionalRulesTests
{
    [Fact]
    public void When_ConditionTrue_RulesAreEvaluated()
    {
        var validator = new WhenValidator();
        // Age >= 18 so email rules apply; email is invalid
        var result = validator.Validate(new PersonDto { Age = 25, Email = "not-email" });
        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor("Email"));
    }

    [Fact]
    public void When_ConditionFalse_RulesAreSkipped()
    {
        var validator = new WhenValidator();
        // Age < 18 so email rules are skipped even though email is invalid
        var result = validator.Validate(new PersonDto { Age = 16, Email = "not-email" });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void When_ConditionTrue_ValidValue_Passes()
    {
        var validator = new WhenValidator();
        var result = validator.Validate(new PersonDto { Age = 25, Email = "user@example.com" });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Unless_ConditionFalse_RulesAreEvaluated()
    {
        var validator = new UnlessValidator();
        // Age != 0 so Unless(age == 0) means condition is false → rules ARE applied
        var result = validator.Validate(new PersonDto { Age = 25, Name = null });
        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor("Name"));
    }

    [Fact]
    public void Unless_ConditionTrue_RulesAreSkipped()
    {
        var validator = new UnlessValidator();
        // Age == 0 so Unless condition is true → rules are skipped
        var result = validator.Validate(new PersonDto { Age = 0, Name = null });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void StopOnFirstFailure_WhenFirstRuleFails_StopsEvaluation()
    {
        var validator = new StopOnFirstFailureValidator();
        // Name is null — NotNull fails. With StopOnFirstFailure, should only get one error.
        var result = validator.Validate(new PersonDto { Name = null });
        Assert.False(result.IsValid);
        Assert.Single(result.ErrorsFor("Name"));
    }

    [Fact]
    public void StopOnFirstFailure_WhenAllPass_IsValid()
    {
        var validator = new StopOnFirstFailureValidator();
        var result = validator.Validate(new PersonDto { Name = "Alice" });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void MultipleRules_WithoutStop_CollectsAllErrors()
    {
        // Without StopOnFirstFailure, all failures are reported
        var validator = new MultipleRulesValidator();
        var result = validator.Validate(new PersonDto { Name = null, Email = null, Age = -1 });
        Assert.False(result.IsValid);
        Assert.True(result.Errors.Count >= 2);
    }
}

public class MultipleRulesValidator : AbstractValidator<PersonDto>
{
    public MultipleRulesValidator()
    {
        RuleFor(x => x.Name).NotNull().NotEmpty();
        RuleFor(x => x.Email).NotNull().Email();
        RuleFor(x => x.Age).Positive();
    }
}
