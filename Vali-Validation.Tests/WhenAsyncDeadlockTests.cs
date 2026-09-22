using Vali_Validation.Core.Validators;
using Vali_Validation.Tests.Models;
using Xunit;

namespace Vali_Validation.Tests;

// A minimal single-threaded SynchronizationContext that queues continuations,
// reproducing the classic ASP.NET-classic/WPF/WinForms deadlock shape:
// a blocking .GetAwaiter().GetResult() on this context deadlocks if the
// continuation also needs to run on this same context.
public sealed class QueuingSynchronizationContext : SynchronizationContext
{
    private readonly Queue<(SendOrPostCallback callback, object? state)> _queue = new();

    public override void Post(SendOrPostCallback d, object? state) => _queue.Enqueue((d, state));

    public void RunQueued()
    {
        while (_queue.Count > 0)
        {
            var (callback, state) = _queue.Dequeue();
            callback(state);
        }
    }
}

public class WhenAsyncBlockingValidator : AbstractValidator<PersonDto>
{
    public WhenAsyncBlockingValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WhenAsync(async (instance, ct) =>
            {
                await Task.Delay(1, ct);
                return true;
            });
    }
}

public class WhenAsyncDeadlockTests
{
    [Fact]
    public void WhenAsync_UnderCapturedSynchronizationContext_DoesNotDeadlock()
    {
        var previousContext = SynchronizationContext.Current;
        var queuingContext = new QueuingSynchronizationContext();
        SynchronizationContext.SetSynchronizationContext(queuingContext);
        try
        {
            var validator = new WhenAsyncBlockingValidator();
            var task = Task.Run(() => validator.Validate(new PersonDto { Name = "", Age = 1 }));

            // Pump the captured context's queue while waiting — mirrors what a
            // real UI/classic-ASP.NET message loop does. If WhenAsync's
            // internal await captured this context, the Task above would
            // never complete without this pump running concurrently; if it
            // deadlocks despite the pump (old Task.Run-less implementation
            // could still hang the thread that owns the context), the test
            // times out instead of hanging the whole suite.
            var completed = task.Wait(TimeSpan.FromSeconds(5));
            queuingContext.RunQueued();

            Assert.True(completed, "Validate() did not complete within 5 seconds — likely deadlocked.");
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previousContext);
        }
    }

    [Fact]
    public void WhenAsync_TrueCondition_RunsGuardedRules()
    {
        var validator = new WhenAsyncBlockingValidator();

        var result = validator.Validate(new PersonDto { Name = "", Age = 1 });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, kvp => kvp.Key == "Name");
    }
}
