using Vali_Validation.Core.Validators;
using Xunit;

namespace Vali_Validation.Tests;

public class DiscountDto
{
    public decimal Discount { get; set; }
    public bool IsPromo { get; set; }
    public bool HasCap { get; set; }
}

public class DiscountValidator : AbstractValidator<DiscountDto>
{
    public DiscountValidator()
    {
        RuleFor(x => x.Discount)
            .GreaterThan(0m).When(x => x.IsPromo)
            .LessThan(1000m).When(x => x.HasCap);
    }
}

public class RuleBuilderWhenCombinationTests
{
    [Fact]
    public void When_CalledAgainForLaterRule_PreservesEarlierRuleGuard_DoesNotOverwriteIt()
    {
        // IsPromo=false means GreaterThan(0) must NOT run, regardless of HasCap.
        // Before the fix, the second .When(HasCap) overwrote GreaterThan's guard
        // from IsPromo to HasCap, so with HasCap=true it ran anyway and failed
        // (Discount=0 is not > 0). After the fix, GreaterThan's guard becomes
        // IsPromo && HasCap = false, so it's correctly skipped.
        var validator = new DiscountValidator();
        var dto = new DiscountDto { Discount = 0, IsPromo = false, HasCap = true };

        var result = validator.Validate(dto);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void When_CalledAgainForLaterRule_BothConditionsTrue_EarlierRuleStillRuns()
    {
        // Sanity check: AND-combination must not accidentally suppress a rule when
        // both guards are true.
        var validator = new DiscountValidator();
        var dto = new DiscountDto { Discount = -5, IsPromo = true, HasCap = true };

        var result = validator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Single(result.Errors["Discount"]);
    }

    [Fact]
    public void Unless_CalledAgainForLaterRule_ComposesWithAndLikeWhen()
    {
        var dto = new DiscountDto { Discount = 0, IsPromo = true, HasCap = false };
        var validatorInline = new InlineUnlessValidator();

        var result = validatorInline.Validate(dto);

        // GreaterThan(0).Unless(x => !x.IsPromo) -> guard = IsPromo = true -> runs -> 0 > 0 fails.
        // LessThan(1000).Unless(x => x.HasCap) -> guard = !HasCap = true -> runs -> 0 < 1000 passes.
        Assert.False(result.IsValid);
        Assert.Single(result.Errors["Discount"]);
    }

    private class InlineUnlessValidator : AbstractValidator<DiscountDto>
    {
        public InlineUnlessValidator()
        {
            RuleFor(x => x.Discount)
                .GreaterThan(0m).Unless(x => !x.IsPromo)
                .LessThan(1000m).Unless(x => x.HasCap);
        }
    }
}
