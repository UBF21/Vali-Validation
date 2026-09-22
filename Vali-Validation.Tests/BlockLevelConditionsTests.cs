using Vali_Validation.Core.Validators;
using Xunit;

namespace Vali_Validation.Tests;

public class BlockConditionDto
{
    public bool IsPromo { get; set; }
    public bool IsInternational { get; set; }
    public string? PromoCode { get; set; }
    public string? Country { get; set; }
    public string? AlwaysChecked { get; set; }
}

public class BlockConditionValidator : AbstractValidator<BlockConditionDto>
{
    public int PromoCodeInvocations;
    public int CountryInvocations;
    public int AlwaysCheckedInvocations;

    public BlockConditionValidator()
    {
        When(x => x.IsPromo, () =>
        {
            RuleFor(x => x.PromoCode).Must(_ =>
            {
                PromoCodeInvocations++;
                return true;
            });

            RuleFor(x => x.Country).Must(_ =>
            {
                CountryInvocations++;
                return true;
            });
        });

        RuleFor(x => x.AlwaysChecked).Must(_ =>
        {
            AlwaysCheckedInvocations++;
            return true;
        });
    }
}

public class NestedBlockConditionValidator : AbstractValidator<BlockConditionDto>
{
    public int NestedRuleInvocations;

    public NestedBlockConditionValidator()
    {
        When(x => x.IsPromo, () =>
        {
            When(x => x.IsInternational, () =>
            {
                RuleFor(x => x.PromoCode).Must(_ =>
                {
                    NestedRuleInvocations++;
                    return true;
                });
            });
        });
    }
}

public class UnlessBlockConditionValidator : AbstractValidator<BlockConditionDto>
{
    public int RuleInvocations;

    public UnlessBlockConditionValidator()
    {
        Unless(x => x.IsPromo, () =>
        {
            RuleFor(x => x.PromoCode).Must(_ =>
            {
                RuleInvocations++;
                return true;
            });
        });
    }
}

public class BlockLevelConditionsTests
{
    [Fact]
    public void When_WithBlockConditionTrue_RunsBothRulesInBlock()
    {
        var validator = new BlockConditionValidator();

        validator.Validate(new BlockConditionDto { IsPromo = true });

        Assert.Equal(1, validator.PromoCodeInvocations);
        Assert.Equal(1, validator.CountryInvocations);
    }

    [Fact]
    public void When_WithBlockConditionFalse_SkipsBothRulesInBlock()
    {
        var validator = new BlockConditionValidator();

        validator.Validate(new BlockConditionDto { IsPromo = false });

        Assert.Equal(0, validator.PromoCodeInvocations);
        Assert.Equal(0, validator.CountryInvocations);
    }

    [Fact]
    public void When_WithBlockConditionFalse_DoesNotAffectRulesOutsideTheBlock()
    {
        var validator = new BlockConditionValidator();

        validator.Validate(new BlockConditionDto { IsPromo = false });

        Assert.Equal(1, validator.AlwaysCheckedInvocations);
    }

    [Fact]
    public void Unless_WithBlockConditionTrue_SkipsRulesInBlock()
    {
        var validator = new UnlessBlockConditionValidator();

        validator.Validate(new BlockConditionDto { IsPromo = true });

        Assert.Equal(0, validator.RuleInvocations);
    }

    [Fact]
    public void Unless_WithBlockConditionFalse_RunsRulesInBlock()
    {
        var validator = new UnlessBlockConditionValidator();

        validator.Validate(new BlockConditionDto { IsPromo = false });

        Assert.Equal(1, validator.RuleInvocations);
    }

    [Fact]
    public void When_Nested_RequiresBothOuterAndInnerConditionTrue()
    {
        var validator = new NestedBlockConditionValidator();

        validator.Validate(new BlockConditionDto { IsPromo = true, IsInternational = false });
        Assert.Equal(0, validator.NestedRuleInvocations);

        var validator2 = new NestedBlockConditionValidator();
        validator2.Validate(new BlockConditionDto { IsPromo = true, IsInternational = true });
        Assert.Equal(1, validator2.NestedRuleInvocations);
    }

    [Fact]
    public void When_WithNullCondition_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ThrowingNullConditionValidator());
    }

    private class ThrowingNullConditionValidator : AbstractValidator<BlockConditionDto>
    {
        public ThrowingNullConditionValidator()
        {
            When(null!, () => { });
        }
    }
}
