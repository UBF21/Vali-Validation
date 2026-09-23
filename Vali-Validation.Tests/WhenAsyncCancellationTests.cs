using Vali_Validation.Core.Validators;
using Xunit;

namespace Vali_Validation.Tests;

public class WhenAsyncCancellationDto
{
    public string? Name { get; set; }
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
}
