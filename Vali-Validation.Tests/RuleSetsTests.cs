using System.Collections.Generic;
using Vali_Validation.Core.Validators;
using Xunit;

namespace Vali_Validation.Tests;

public class RuleSetsDto
{
    public string? Name { get; set; }
    public string? PromoCode { get; set; }
}

public class RuleSetsValidator : AbstractValidator<RuleSetsDto>
{
    public int DefaultRuleInvocations;
    public int CreateRuleInvocations;
    public int UpdateRuleInvocations;

    public RuleSetsValidator()
    {
        RuleFor(x => x.Name).Must(_ =>
        {
            DefaultRuleInvocations++;
            return true;
        });

        RuleFor(x => x.PromoCode).Must(_ =>
        {
            CreateRuleInvocations++;
            return true;
        }).InRuleSet("Create");

        RuleFor(x => x.PromoCode).Must(_ =>
        {
            UpdateRuleInvocations++;
            return true;
        }).InRuleSet("Update");
    }
}

public class RuleSetsTests
{
    [Fact]
    public void Validate_WithoutOptions_RunsEveryRuleRegardlessOfRuleSet()
    {
        var validator = new RuleSetsValidator();

        validator.Validate(new RuleSetsDto());

        Assert.Equal(1, validator.DefaultRuleInvocations);
        Assert.Equal(1, validator.CreateRuleInvocations);
        Assert.Equal(1, validator.UpdateRuleInvocations);
    }

    [Fact]
    public void Validate_WithIncludeRuleSets_RunsOnlyMatchingRuleSetRulesNotDefault()
    {
        var validator = new RuleSetsValidator();

        validator.Validate(new RuleSetsDto(), opts => opts.IncludeRuleSets("Create"));

        Assert.Equal(0, validator.DefaultRuleInvocations);
        Assert.Equal(1, validator.CreateRuleInvocations);
        Assert.Equal(0, validator.UpdateRuleInvocations);
    }

    [Fact]
    public void Validate_WithIncludeRuleSetsDefaultAndCreate_RunsBoth()
    {
        var validator = new RuleSetsValidator();

        validator.Validate(new RuleSetsDto(), opts => opts.IncludeRuleSets("default", "Create"));

        Assert.Equal(1, validator.DefaultRuleInvocations);
        Assert.Equal(1, validator.CreateRuleInvocations);
        Assert.Equal(0, validator.UpdateRuleInvocations);
    }

    [Fact]
    public async Task ValidateAsync_WithIncludeRuleSets_RunsOnlyMatchingRuleSetRules()
    {
        var validator = new RuleSetsValidator();

        await validator.ValidateAsync(new RuleSetsDto(), opts => opts.IncludeRuleSets("Update"));

        Assert.Equal(0, validator.DefaultRuleInvocations);
        Assert.Equal(0, validator.CreateRuleInvocations);
        Assert.Equal(1, validator.UpdateRuleInvocations);
    }

    [Fact]
    public void IncludeRuleSets_WithNoArguments_Throws()
    {
        var options = new ValidationOptions();

        Assert.Throws<ArgumentException>(() => options.IncludeRuleSets());
    }

    [Fact]
    public void InRuleSet_WithNoArguments_Throws()
    {
        var validator = new RuleSetsValidator();

        Assert.Throws<ArgumentException>(() =>
            validator.RuleFor(x => x.Name).NotEmpty().InRuleSet());
    }
}
