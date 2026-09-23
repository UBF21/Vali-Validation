using Vali_Validation.Core.Results;
using Vali_Validation.Core.Validators;
using Xunit;

namespace Vali_Validation.Tests;

public class AsyncModifiersDto
{
    public string? Email { get; set; }
    public string? Username { get; set; }
    public string? ReferralCode { get; set; }
    public string? Name { get; set; }
}

public class AsyncRuleModifiersTests
{
    [Fact]
    public async Task WithSeverity_AfterMustAsync_ActuallyAppliesToTheAsyncRule()
    {
        var validator = new WithSeverityAfterMustAsyncValidator();
        var result = await validator.ValidateAsync(new AsyncModifiersDto { Email = "anything" });

        Assert.True(result.IsValid); // Warning doesn't block IsValid
        var failure = Assert.Single(result.Failures);
        Assert.Equal(Severity.Warning, failure.Severity);
    }

    private class WithSeverityAfterMustAsyncValidator : AbstractValidator<AsyncModifiersDto>
    {
        public WithSeverityAfterMustAsyncValidator()
            => RuleFor(x => x.Email).MustAsync(async _ => { await Task.Yield(); return false; }).WithSeverity(Severity.Warning);
    }

    [Fact]
    public async Task WithErrorCode_AfterMustAsync_ActuallyAppliesToTheAsyncRule()
    {
        var validator = new WithErrorCodeAfterMustAsyncValidator();
        var result = await validator.ValidateAsync(new AsyncModifiersDto { Email = "anything" });

        var failure = Assert.Single(result.Failures);
        Assert.Equal("EMAIL_TAKEN", failure.ErrorCode);
    }

    private class WithErrorCodeAfterMustAsyncValidator : AbstractValidator<AsyncModifiersDto>
    {
        public WithErrorCodeAfterMustAsyncValidator()
            => RuleFor(x => x.Email).MustAsync(async _ => { await Task.Yield(); return false; }).WithErrorCode("EMAIL_TAKEN");
    }

    [Fact]
    public async Task WithMessage_AfterMustAsync_ActuallyAppliesToTheAsyncRule_NotAnEarlierSyncRule()
    {
        var validator = new WithMessageAfterMustAsyncValidator();

        // Non-empty email: NotEmpty passes, only MustAsync fails — its message must be the custom one.
        var result = await validator.ValidateAsync(new AsyncModifiersDto { Email = "taken@example.com" });
        var failure = Assert.Single(result.Failures);
        Assert.Equal("Email is already taken.", failure.Message);

        // Empty email: NotEmpty fails too (both rules run independently). NotEmpty's own message
        // must be untouched by WithMessage() — proving it did not silently reassign to that
        // earlier sync rule instead of the intended MustAsync rule.
        var resultEmpty = await validator.ValidateAsync(new AsyncModifiersDto { Email = "" });
        Assert.Contains(resultEmpty.Failures, f => f.PropertyName == "Email" && f.Message != "Email is already taken.");
        Assert.Contains(resultEmpty.Failures, f => f.Message == "Email is already taken.");
    }

    private class WithMessageAfterMustAsyncValidator : AbstractValidator<AsyncModifiersDto>
    {
        public WithMessageAfterMustAsyncValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .MustAsync(async _ => { await Task.Yield(); return false; })
                .WithMessage("Email is already taken.");
        }
    }

    [Fact]
    public async Task When_FluentPerRule_AfterMustAsync_ActuallyGatesTheAsyncRule()
    {
        var validator = new WhenAfterMustAsyncValidator();

        // Gate false: the async rule never runs, so it never fails even though the predicate would return false.
        var gated = await validator.ValidateAsync(new AsyncModifiersDto { Email = "x", Name = null });
        Assert.True(gated.IsValid);

        // Gate true: the async rule runs and fails.
        var ungated = await validator.ValidateAsync(new AsyncModifiersDto { Email = "x", Name = "gate-on" });
        Assert.False(ungated.IsValid);
    }

    private class WhenAfterMustAsyncValidator : AbstractValidator<AsyncModifiersDto>
    {
        public WhenAfterMustAsyncValidator()
        {
            RuleFor(x => x.Email)
                .MustAsync(async _ => { await Task.Yield(); return false; })
                .When(x => x.Name == "gate-on");
        }
    }

    [Fact]
    public async Task Unless_FluentPerRule_AfterMustAsync_ActuallyGatesTheAsyncRule()
    {
        var validator = new UnlessAfterMustAsyncValidator();

        var gated = await validator.ValidateAsync(new AsyncModifiersDto { Email = "x", Name = "skip" });
        Assert.True(gated.IsValid);

        var ungated = await validator.ValidateAsync(new AsyncModifiersDto { Email = "x", Name = "run" });
        Assert.False(ungated.IsValid);
    }

    private class UnlessAfterMustAsyncValidator : AbstractValidator<AsyncModifiersDto>
    {
        public UnlessAfterMustAsyncValidator()
        {
            RuleFor(x => x.Email)
                .MustAsync(async _ => { await Task.Yield(); return false; })
                .Unless(x => x.Name == "skip");
        }
    }

    [Fact]
    public async Task WithMessage_AfterALaterSyncRuleFollowingMustAsync_TargetsTheSyncRule_NotStaleAsyncState()
    {
        // MustAsync first, then a SYNC rule (NotEmpty), then WithMessage — must target NotEmpty,
        // proving the "last addition was async" tracking correctly resets on the next sync rule.
        var validator = new LaterSyncRuleAfterAsyncValidator();
        var result = await validator.ValidateAsync(new AsyncModifiersDto { Email = "x", Username = "" });

        Assert.Contains(result.Failures, f => f.PropertyName == "Username" && f.Message == "Custom username message.");
    }

    private class LaterSyncRuleAfterAsyncValidator : AbstractValidator<AsyncModifiersDto>
    {
        public LaterSyncRuleAfterAsyncValidator()
        {
            RuleFor(x => x.Email).MustAsync(async _ => { await Task.Yield(); return true; });
            RuleFor(x => x.Username).NotEmpty().WithMessage("Custom username message.");
        }
    }

    [Fact]
    public async Task WithMessage_AfterCustomFollowingMustAsync_DoesNotHijackTheStaleAsyncState()
    {
        // Custom() bypasses AddCurrentCondition entirely — it must still reset the "last addition
        // was async" tracking (via the shared AddSyncRule choke point), otherwise WithMessage here
        // would silently reassign to the earlier MustAsync rule instead of no-op'ing (Custom rules
        // don't support WithMessage today) or applying to Custom.
        var validator = new WithMessageAfterCustomFollowingMustAsyncValidator();
        var result = await validator.ValidateAsync(new AsyncModifiersDto { Email = "x" });

        Assert.Contains(result.Failures, f => f.Message == "custom failed");
        Assert.DoesNotContain(result.Failures, f => f.Message == "this must not hijack MustAsync");
    }

    private class WithMessageAfterCustomFollowingMustAsyncValidator : AbstractValidator<AsyncModifiersDto>
    {
        public WithMessageAfterCustomFollowingMustAsyncValidator()
        {
            RuleFor(x => x.Email)
                .MustAsync(async _ => { await Task.Yield(); return false; })
                .Custom((_, ctx) => ctx.AddFailure("custom failed"))
                .WithMessage("this must not hijack MustAsync");
        }
    }

    [Fact]
    public async Task DependentRuleAsync_WithSeverityErrorCodeAndMessage_AllApplyCorrectly()
    {
        var validator = new DependentRuleAsyncModifiersValidator();
        var result = await validator.ValidateAsync(new AsyncModifiersDto { ReferralCode = "BOGUS", Username = "someone" });

        var failure = Assert.Single(result.Failures);
        Assert.Equal(Severity.Warning, failure.Severity);
        Assert.Equal("BAD_REFERRAL", failure.ErrorCode);
        Assert.Equal("Referral code is invalid.", failure.Message);
        Assert.True(result.IsValid); // Warning doesn't block IsValid
    }

    private class DependentRuleAsyncModifiersValidator : AbstractValidator<AsyncModifiersDto>
    {
        public DependentRuleAsyncModifiersValidator()
        {
            RuleFor(x => x.ReferralCode)
                .DependentRuleAsync(
                    x => x.ReferralCode,
                    x => x.Username,
                    async (code, _) => { await Task.Yield(); return code != "BOGUS"; })
                .WithSeverity(Severity.Warning)
                .WithErrorCode("BAD_REFERRAL")
                .WithMessage("Referral code is invalid.");
        }
    }

    [Fact]
    public async Task WithMessage_AfterDependentRuleAsync_StillSubstitutesDependentPropertyNamePlaceholder()
    {
        // Confirms Args (dependentPropertyName) survive a WithMessage() override, same guarantee
        // already proven for sync rules' {length}/{min}/etc. placeholders.
        var validator = new DependentRuleAsyncCustomMessageWithPlaceholderValidator();
        var result = await validator.ValidateAsync(new AsyncModifiersDto { ReferralCode = "BOGUS", Username = "Alice" });

        var failure = Assert.Single(result.Failures);
        Assert.Equal("ReferralCode is invalid given Username.", failure.Message);
    }

    private class DependentRuleAsyncCustomMessageWithPlaceholderValidator : AbstractValidator<AsyncModifiersDto>
    {
        public DependentRuleAsyncCustomMessageWithPlaceholderValidator()
        {
            RuleFor(x => x.ReferralCode)
                .DependentRuleAsync(
                    x => x.ReferralCode,
                    x => x.Username,
                    async (code, _) => { await Task.Yield(); return code != "BOGUS"; })
                .WithMessage("{PropertyName} is invalid given {dependentPropertyName}.");
        }
    }
}
