using Microsoft.Extensions.DependencyInjection;
using Vali_Mediator.Core.Request;
using Vali_Mediator.Core.Result;
using Vali_Validation.Core.Exceptions;
using Vali_Validation.Core.Validators;
using Vali_Validation.ValiMediator;
using Xunit;

namespace Vali_Validation.ValiMediator.Tests;

// --- Test models ---

public class PlaceOrderCommand : IRequest<Result<string>>
{
    public string? ProductName { get; set; }
}

public class PlaceOrderCommandValidator : AbstractValidator<PlaceOrderCommand>
{
    public PlaceOrderCommandValidator()
    {
        RuleFor(x => x.ProductName).NotEmpty();
    }
}

public class DeleteCommand : IRequest<string>
{
    public string? Id { get; set; }
}

public class DeleteCommandValidator : AbstractValidator<DeleteCommand>
{
    public DeleteCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class VoidCommand : IRequest<Vali_Mediator.Core.Result.Result>
{
    public string? Name { get; set; }
}

public class VoidCommandValidator : AbstractValidator<VoidCommand>
{
    public VoidCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
    }
}

public class AnotherCommand : IRequest<Result<int>>
{
    public int Value { get; set; }
}

public class AnotherCommandValidator : AbstractValidator<AnotherCommand>
{
    public AnotherCommandValidator()
    {
        RuleFor(x => x.Value).GreaterThan(0);
    }
}

// --- Tests ---

public class ValidationBehaviorValiMediatorTests
{
    private static IServiceProvider BuildServices(bool withValidator, bool isResultType)
    {
        var services = new ServiceCollection();
        if (withValidator)
        {
            if (isResultType)
                services.AddTransient<IValidator<PlaceOrderCommand>, PlaceOrderCommandValidator>();
            else
                services.AddTransient<IValidator<DeleteCommand>, DeleteCommandValidator>();
        }
        return services.BuildServiceProvider();
    }

    private static IServiceProvider BuildVoidServices(bool withValidator)
    {
        var services = new ServiceCollection();
        if (withValidator)
            services.AddTransient<IValidator<VoidCommand>, VoidCommandValidator>();
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task Handle_WhenNoValidatorRegistered_CallsNext()
    {
        var sp = BuildServices(withValidator: false, isResultType: true);
        var behavior = new ValidationBehavior<PlaceOrderCommand, Result<string>>(sp);

        bool nextCalled = false;
        Func<Task<Result<string>>> next = () =>
        {
            nextCalled = true;
            return Task.FromResult(Result<string>.Ok("done"));
        };

        var result = await behavior.Handle(new PlaceOrderCommand { ProductName = "" }, next, default);

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task Handle_WhenValidationPasses_CallsNext()
    {
        var sp = BuildServices(withValidator: true, isResultType: true);
        var behavior = new ValidationBehavior<PlaceOrderCommand, Result<string>>(sp);

        bool nextCalled = false;
        Func<Task<Result<string>>> next = () =>
        {
            nextCalled = true;
            return Task.FromResult(Result<string>.Ok("done"));
        };

        var result = await behavior.Handle(new PlaceOrderCommand { ProductName = "Widget" }, next, default);

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task Handle_WhenValidationFails_AndResultType_ReturnsFail()
    {
        var sp = BuildServices(withValidator: true, isResultType: true);
        var behavior = new ValidationBehavior<PlaceOrderCommand, Result<string>>(sp);

        Func<Task<Result<string>>> next = () => Task.FromResult(Result<string>.Ok("done"));

        var result = await behavior.Handle(new PlaceOrderCommand { ProductName = "" }, next, default);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_WhenValidationFails_AndResultType_ReturnsValidationError()
    {
        var sp = BuildServices(withValidator: true, isResultType: true);
        var behavior = new ValidationBehavior<PlaceOrderCommand, Result<string>>(sp);

        Func<Task<Result<string>>> next = () => Task.FromResult(Result<string>.Ok("done"));

        var result = await behavior.Handle(new PlaceOrderCommand { ProductName = null }, next, default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task Handle_WhenValidationFails_AndNonResultType_ThrowsValidationException()
    {
        var sp = BuildServices(withValidator: true, isResultType: false);
        var behavior = new ValidationBehavior<DeleteCommand, string>(sp);

        Func<Task<string>> next = () => Task.FromResult("ok");

        await Assert.ThrowsAsync<ValidationException>(
            () => behavior.Handle(new DeleteCommand { Id = "" }, next, default));
    }

    // --- New tests for non-generic Result (void) ---

    [Fact]
    public async Task Handle_WhenVoidResultType_AndValidationFails_ReturnsResultFail()
    {
        var sp = BuildVoidServices(withValidator: true);
        var behavior = new ValidationBehavior<VoidCommand, Vali_Mediator.Core.Result.Result>(sp);

        bool nextCalled = false;
        Func<Task<Vali_Mediator.Core.Result.Result>> next = () =>
        {
            nextCalled = true;
            return Task.FromResult(Vali_Mediator.Core.Result.Result.Ok());
        };

        var result = await behavior.Handle(new VoidCommand { Name = "" }, next, default);

        Assert.False(nextCalled);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_WhenVoidResultType_AndValidationFails_ReturnsValidationErrorType()
    {
        var sp = BuildVoidServices(withValidator: true);
        var behavior = new ValidationBehavior<VoidCommand, Vali_Mediator.Core.Result.Result>(sp);

        Func<Task<Vali_Mediator.Core.Result.Result>> next = () =>
            Task.FromResult(Vali_Mediator.Core.Result.Result.Ok());

        var result = await behavior.Handle(new VoidCommand { Name = null }, next, default);

        Assert.False(result.IsSuccess);
        Assert.Equal(Vali_Mediator.Core.Result.ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task Handle_WhenVoidResultType_AndValidationPasses_CallsNext()
    {
        var sp = BuildVoidServices(withValidator: true);
        var behavior = new ValidationBehavior<VoidCommand, Vali_Mediator.Core.Result.Result>(sp);

        bool nextCalled = false;
        Func<Task<Vali_Mediator.Core.Result.Result>> next = () =>
        {
            nextCalled = true;
            return Task.FromResult(Vali_Mediator.Core.Result.Result.Ok());
        };

        var result = await behavior.Handle(new VoidCommand { Name = "Alice" }, next, default);

        Assert.True(nextCalled);
        Assert.True(result.IsSuccess);
    }

    // --- ValidationErrors dictionary populated on Result<T>.Fail ---

    [Fact]
    public async Task Handle_WhenValidationFails_AndResultType_ValidationErrorsDictionaryPopulated()
    {
        var sp = BuildServices(withValidator: true, isResultType: true);
        var behavior = new ValidationBehavior<PlaceOrderCommand, Result<string>>(sp);

        Func<Task<Result<string>>> next = () => Task.FromResult(Result<string>.Ok("done"));

        var result = await behavior.Handle(new PlaceOrderCommand { ProductName = "" }, next, default);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ValidationErrors);
        Assert.True(result.ValidationErrors!.ContainsKey("ProductName"));
    }

    // --- CancellationToken is passed through to ValidateAsync ---

    [Fact]
    public async Task Handle_WhenCancellationTokenProvided_BehaviorRespectsIt()
    {
        var sp = BuildServices(withValidator: true, isResultType: true);
        var behavior = new ValidationBehavior<PlaceOrderCommand, Result<string>>(sp);

        using var cts = new CancellationTokenSource();
        Func<Task<Result<string>>> next = () => Task.FromResult(Result<string>.Ok("done"));

        // Should not throw — just passes valid token through
        var result = await behavior.Handle(new PlaceOrderCommand { ProductName = "Widget" }, next, cts.Token);

        Assert.True(result.IsSuccess);
    }

    // --- No validator registered → behavior is no-op ---

    [Fact]
    public async Task Handle_WhenNoValidatorRegistered_AndVoidResult_CallsNext()
    {
        var sp = BuildVoidServices(withValidator: false);
        var behavior = new ValidationBehavior<VoidCommand, Vali_Mediator.Core.Result.Result>(sp);

        bool nextCalled = false;
        Func<Task<Vali_Mediator.Core.Result.Result>> next = () =>
        {
            nextCalled = true;
            return Task.FromResult(Vali_Mediator.Core.Result.Result.Ok());
        };

        var result = await behavior.Handle(new VoidCommand { Name = "" }, next, default);

        Assert.True(nextCalled);
        Assert.True(result.IsSuccess);
    }
}
