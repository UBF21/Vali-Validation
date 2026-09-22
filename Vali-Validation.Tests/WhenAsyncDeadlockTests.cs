using Vali_Validation.Core.Results;
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
        try
        {
            var validator = new WhenAsyncBlockingValidator();
            ValidationResult? result = null;

            // Validate() must run SYNCHRONOUSLY on a thread that has the
            // QueuingSynchronizationContext set as its ambient context at the
            // moment WhenAsync's condition awaits — that's what reproduces the
            // classic single-threaded-context deadlock shape (UI thread /
            // classic ASP.NET request thread). A dedicated Thread plays that
            // role (Task.Run would run on a thread-pool thread, which never
            // carries an ambient SynchronizationContext, defeating the repro).
            // The main test thread only joins with a timeout — it must NOT
            // pump the queue concurrently, since a live foreign-thread pump
            // would drain the queue regardless of whether the fix is present,
            // masking the very deadlock this test exists to catch.
            var worker = new Thread(() =>
            {
                SynchronizationContext.SetSynchronizationContext(queuingContext);
                result = validator.Validate(new PersonDto { Name = "", Age = 1 });
            })
            {
                IsBackground = true
            };
            worker.Start();

            var completed = worker.Join(TimeSpan.FromSeconds(5));

            // Drain any queued continuation now so a pre-fix worker thread
            // (stuck waiting on it) can unwind instead of leaking for the
            // rest of the process's life. This runs only after `completed`
            // has already been captured, so it cannot mask a real deadlock.
            queuingContext.RunQueued();

            Assert.True(completed, "Validate() did not complete within 5 seconds — likely deadlocked.");
            Assert.False(result!.IsValid);
            Assert.Contains(result.Errors, kvp => kvp.Key == "Name");
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
