using Vali_Validation.Core.Rules;
using Vali_Validation.Core.Validators;
using Xunit;

namespace Vali_Validation.Tests;

public class WhenAsyncCancellationDto
{
    public string? Name { get; set; }
}

public class NestedWhenAsyncChildDto
{
    public string? Code { get; set; }
}

public class NestedWhenAsyncChildValidator : AbstractValidator<NestedWhenAsyncChildDto>
{
    public CancellationToken? ObservedToken;

    public NestedWhenAsyncChildValidator()
    {
        // WhenAsync only — no true async rule — so AsyncRules.Count == 0 and SetValidator
        // routes this validator through the sync Validate() path.
        RuleFor(x => x.Code).NotEmpty().WhenAsync(async (instance, ct) =>
        {
            ObservedToken = ct;
            await Task.Yield();
            return true;
        });
    }
}

public class NestedWhenAsyncParentDto
{
    public NestedWhenAsyncChildDto? Child { get; set; }
}

public class NestedWhenAsyncParentValidator : AbstractValidator<NestedWhenAsyncParentDto>
{
    public readonly NestedWhenAsyncChildValidator ChildValidator = new();

    public NestedWhenAsyncParentValidator()
    {
        RuleFor(x => x.Child).SetValidator(ChildValidator);
    }
}

public class ParallelWhenAsyncDto
{
    public string? A { get; set; }
    public string? B { get; set; }
}

public class ParallelWhenAsyncValidator : AbstractValidator<ParallelWhenAsyncDto>
{
    public CancellationToken? ObservedTokenA;
    public CancellationToken? ObservedTokenB;

    public ParallelWhenAsyncValidator()
    {
        RuleFor(x => x.A).NotEmpty().WhenAsync(async (instance, ct) =>
        {
            ObservedTokenA = ct;
            await Task.Yield();
            return true;
        });
        RuleFor(x => x.B).NotEmpty().WhenAsync(async (instance, ct) =>
        {
            ObservedTokenB = ct;
            await Task.Yield();
            return true;
        });
    }
}

public class WhenAsyncCancellationValidator : AbstractValidator<WhenAsyncCancellationDto>
{
    public CancellationToken? ObservedToken;

    public WhenAsyncCancellationValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WhenAsync(async (instance, ct) =>
        {
            ObservedToken = ct;
            await Task.Yield();
            return true;
        });
    }
}

public class ConcurrentWhenAsyncValidator : AbstractValidator<WhenAsyncCancellationDto>
{
    public CancellationToken ObservedTokenA;
    public CancellationToken ObservedTokenB;

    public ConcurrentWhenAsyncValidator(bool isA)
    {
        RuleFor(x => x.Name).NotEmpty().WhenAsync(async (instance, ct) =>
        {
            await Task.Delay(50, CancellationToken.None); // force interleaving with the other concurrent call
            if (isA) ObservedTokenA = ct; else ObservedTokenB = ct;
            return true;
        });
    }
}

public class WhenAsyncCancellationTests
{
    [Fact]
    public async Task WhenAsync_ObservesTheRealCancellationTokenPassedToValidateAsync()
    {
        var validator = new WhenAsyncCancellationValidator();
        using var cts = new CancellationTokenSource();

        await validator.ValidateAsync(new WhenAsyncCancellationDto { Name = "Ana" }, cts.Token);

        Assert.Equal(cts.Token, validator.ObservedToken);
    }

    [Fact]
    public void WhenAsync_UnderSyncValidate_ObservesCancellationTokenNone()
    {
        var validator = new WhenAsyncCancellationValidator();

        validator.Validate(new WhenAsyncCancellationDto { Name = "Ana" });

        Assert.Equal(CancellationToken.None, validator.ObservedToken);
    }

    [Fact]
    public async Task WhenAsync_ConcurrentValidateAsyncCallsOnDifferentInstances_DoNotCrossContaminateTokens()
    {
        // Same validator TYPE (shares the static-scoped machinery this design relies on),
        // different instances, called concurrently with different tokens — each call must
        // observe only its own token, never the other's.
        var validatorA = new ConcurrentWhenAsyncValidator(isA: true);
        var validatorB = new ConcurrentWhenAsyncValidator(isA: false);
        using var ctsA = new CancellationTokenSource();
        using var ctsB = new CancellationTokenSource();

        var taskA = validatorA.ValidateAsync(new WhenAsyncCancellationDto { Name = "Ana" }, ctsA.Token);
        var taskB = validatorB.ValidateAsync(new WhenAsyncCancellationDto { Name = "Beto" }, ctsB.Token);
        await Task.WhenAll(taskA, taskB);

        Assert.Equal(ctsA.Token, validatorA.ObservedTokenA);
        Assert.Equal(ctsB.Token, validatorB.ObservedTokenB);
        Assert.NotEqual(validatorA.ObservedTokenA, validatorB.ObservedTokenB);
    }

    [Fact]
    public async Task WhenAsync_TokenDoesNotLeakPastValidateAsyncCall()
    {
        var validator = new WhenAsyncCancellationValidator();
        using var cts = new CancellationTokenSource();

        await validator.ValidateAsync(new WhenAsyncCancellationDto { Name = "Ana" }, cts.Token);

        // A second, unrelated sync Validate() call on the same instance, made AFTER the
        // async call above completed, must not observe the leaked-forward token from the
        // prior async call — it should see CancellationToken.None, exactly as if the async
        // call had never happened.
        validator.ObservedToken = null;
        validator.Validate(new WhenAsyncCancellationDto { Name = "Ana" });

        Assert.Equal(CancellationToken.None, validator.ObservedToken);
    }

    [Fact]
    public async Task WhenAsync_OnNestedSetValidatorChild_ObservesTheOuterValidateAsyncToken()
    {
        // The child validator's WhenAsync is its ONLY async-flavored construct (no MustAsync),
        // so nestedValidator.AsyncRules.Count == 0 and SetValidator routes it through the sync
        // Validate() path. AbstractValidator<NestedWhenAsyncChildDto> has its own, separate
        // AsyncLocal field from AbstractValidator<NestedWhenAsyncParentDto> — without forwarding
        // the ambient token across that type boundary, the child would always observe None.
        var parentValidator = new NestedWhenAsyncParentValidator();
        using var cts = new CancellationTokenSource();

        await parentValidator.ValidateAsync(
            new NestedWhenAsyncParentDto { Child = new NestedWhenAsyncChildDto { Code = "ABC" } },
            cts.Token);

        Assert.Equal(cts.Token, parentValidator.ChildValidator.ObservedToken);
    }

    [Fact]
    public async Task WhenAsync_UnderValidateParallelAsync_ObservesTheRealTokenAcrossMultipleAsyncRules()
    {
        // WhenAsync-gated rules run as sync rules (WhenAsync only decorates an existing rule's
        // guard condition; it never itself becomes an _asyncRules entry — only MustAsync/
        // DependentRuleAsync/SetValidator's async branch do). The AsyncLocal is set for the
        // whole ValidateParallelAsync call, so both the sync-rules phase and the Task.WhenAll
        // fan-out observe the same ambient token; this asserts that holds for two independent
        // WhenAsync-gated rules evaluated under ValidateParallelAsync.
        var validator = new ParallelWhenAsyncValidator();
        using var cts = new CancellationTokenSource();

        await validator.ValidateParallelAsync(
            new ParallelWhenAsyncDto { A = "x", B = "y" }, cts.Token);

        Assert.Equal(cts.Token, validator.ObservedTokenA);
        Assert.Equal(cts.Token, validator.ObservedTokenB);
    }
}
