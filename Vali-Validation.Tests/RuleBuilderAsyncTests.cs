using Vali_Validation.Core.Validators;
using Vali_Validation.Tests.Models;
using Xunit;

namespace Vali_Validation.Tests;

public class MustAsyncValidator : AbstractValidator<PersonDto>
{
    private readonly Func<string?, Task<bool>> _check;

    public MustAsyncValidator(Func<string?, Task<bool>> check)
    {
        _check = check;
        RuleFor(x => x.Email).MustAsync(email => _check(email));
    }
}

public class DependentRuleValidator : AbstractValidator<PersonDto>
{
    public DependentRuleValidator()
    {
        RuleFor(x => x.Name)
            .DependentRuleAsync(
                x => x.Name,
                x => x.Email,
                async (name, email) =>
                {
                    await Task.Delay(1);
                    return !string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(email);
                });
    }
}

public class ParallelAsyncValidator : AbstractValidator<PersonDto>
{
    public ParallelAsyncValidator()
    {
        RuleFor(x => x.Name).MustAsync(async name =>
        {
            await Task.Delay(10);
            return !string.IsNullOrEmpty(name);
        });

        RuleFor(x => x.Email).MustAsync(async email =>
        {
            await Task.Delay(10);
            return !string.IsNullOrEmpty(email) && email!.Contains("@");
        });
    }
}

public class RuleBuilderAsyncTests
{
    [Fact]
    public async Task MustAsync_WhenPredicatePasses_ReturnsValid()
    {
        var validator = new MustAsyncValidator(email => Task.FromResult(true));
        var result = await validator.ValidateAsync(new PersonDto { Email = "test@example.com" });
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task MustAsync_WhenPredicateFails_ReturnsInvalid()
    {
        var validator = new MustAsyncValidator(email => Task.FromResult(false));
        var result = await validator.ValidateAsync(new PersonDto { Email = "test@example.com" });
        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor("Email"));
    }

    [Fact]
    public async Task MustAsync_WithRealAsyncCheck_Works()
    {
        var validator = new MustAsyncValidator(async email =>
        {
            await Task.Delay(1);
            return !string.IsNullOrEmpty(email) && email!.Contains("@");
        });

        var passResult = await validator.ValidateAsync(new PersonDto { Email = "user@example.com" });
        var failResult = await validator.ValidateAsync(new PersonDto { Email = "notanemail" });

        Assert.True(passResult.IsValid);
        Assert.False(failResult.IsValid);
    }

    [Fact]
    public async Task DependentRuleAsync_WhenBothPropertiesValid_Passes()
    {
        var validator = new DependentRuleValidator();
        var result = await validator.ValidateAsync(new PersonDto { Name = "Alice", Email = "alice@example.com" });
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task DependentRuleAsync_WhenDependentFails_ReturnsInvalid()
    {
        var validator = new DependentRuleValidator();
        var result = await validator.ValidateAsync(new PersonDto { Name = "Alice", Email = null });
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task ValidateAsync_RunsSyncAndAsyncRules()
    {
        var validator = new ParallelAsyncValidator();
        var result = await validator.ValidateAsync(new PersonDto { Name = "Alice", Email = "alice@example.com" });
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateAsync_CollectsAllErrors()
    {
        var validator = new ParallelAsyncValidator();
        var result = await validator.ValidateAsync(new PersonDto { Name = null, Email = "bademail" });
        Assert.False(result.IsValid);
        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public async Task ValidateParallelAsync_WhenAllValid_ReturnsValid()
    {
        var validator = new ParallelAsyncValidator();
        var result = await validator.ValidateParallelAsync(new PersonDto { Name = "Alice", Email = "alice@example.com" });
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateParallelAsync_WhenHasErrors_CollectsAllErrors()
    {
        var validator = new ParallelAsyncValidator();
        var result = await validator.ValidateParallelAsync(new PersonDto { Name = null, Email = "bademail" });
        Assert.False(result.IsValid);
        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public async Task ValidateParallelAsync_RunsFasterThanSequential()
    {
        // Both rules have 50ms delay — parallel should finish in ~50ms not ~100ms
        var parallelValidator = new AbstractParallelTestValidator();
        var start = System.Diagnostics.Stopwatch.StartNew();
        await parallelValidator.ValidateParallelAsync(new PersonDto { Name = "Alice", Email = "alice@example.com" });
        start.Stop();
        // Parallel: should be well under 150ms (both run concurrently at ~50ms each)
        Assert.True(start.ElapsedMilliseconds < 150, $"Expected < 150ms but got {start.ElapsedMilliseconds}ms");
    }
}

public class AbstractParallelTestValidator : AbstractValidator<PersonDto>
{
    public AbstractParallelTestValidator()
    {
        RuleFor(x => x.Name).MustAsync(async name =>
        {
            await Task.Delay(50);
            return !string.IsNullOrEmpty(name);
        });

        RuleFor(x => x.Email).MustAsync(async email =>
        {
            await Task.Delay(50);
            return !string.IsNullOrEmpty(email);
        });
    }
}
