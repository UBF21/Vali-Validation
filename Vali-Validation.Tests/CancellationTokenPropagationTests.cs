using Vali_Validation.Core.Validators;
using Vali_Validation.Tests.Models;
using Xunit;

namespace Vali_Validation.Tests;

public class CancellationTokenPropagationValidator : AbstractValidator<PersonDto>
{
    public CancellationTokenPropagationValidator(Action<CancellationToken> onTokenObserved)
    {
        RuleFor(x => x.Name).MustAsync((value, ct) =>
        {
            onTokenObserved(ct);
            return Task.FromResult(true);
        });
    }
}

public class CancellationTokenPropagationTests
{
    [Fact]
    public async Task ValidateAsync_PropagatesCancellationToken_ToMustAsyncPredicate()
    {
        CancellationToken observedToken = default;
        var validator = new CancellationTokenPropagationValidator(ct => observedToken = ct);
        using var cts = new CancellationTokenSource();

        await validator.ValidateAsync(new PersonDto { Name = "Ana", Age = 1 }, cts.Token);

        Assert.Equal(cts.Token, observedToken);
    }

    [Fact]
    public async Task ValidateParallelAsync_PropagatesCancellationToken_ToMustAsyncPredicate()
    {
        CancellationToken observedToken = default;
        var validator = new CancellationTokenPropagationValidator(ct => observedToken = ct);
        using var cts = new CancellationTokenSource();

        await validator.ValidateParallelAsync(new PersonDto { Name = "Ana", Age = 1 }, cts.Token);

        Assert.Equal(cts.Token, observedToken);
    }

    [Fact]
    public async Task ValidateAsync_WhenTokenAlreadyCancelled_StillPassesItThrough()
    {
        CancellationToken observedToken = default;
        var validator = new CancellationTokenPropagationValidator(ct => observedToken = ct);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await validator.ValidateAsync(new PersonDto { Name = "Ana", Age = 1 }, cts.Token);

        Assert.True(observedToken.IsCancellationRequested);
    }
}
