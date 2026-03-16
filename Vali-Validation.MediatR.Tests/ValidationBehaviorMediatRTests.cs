using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Vali_Validation.Core.Exceptions;
using Vali_Validation.Core.Validators;
using Xunit;
using System.Threading;

namespace Vali_Validation.MediatR.Tests;

// --- Test models ---

public class CreateUserCommand : IRequest<string>
{
    public string? Name { get; set; }
}

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
    }
}

public class UpdateUserCommand : IRequest<string>
{
    public string? Email { get; set; }
}

public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty();
    }
}

// --- Tests ---

public class ValidationBehaviorMediatRTests
{
    private static IServiceProvider BuildServices(bool withValidator)
    {
        var services = new ServiceCollection();
        if (withValidator)
            services.AddTransient<IValidator<CreateUserCommand>, CreateUserCommandValidator>();
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task Handle_WhenNoValidatorRegistered_CallsNext()
    {
        var sp = BuildServices(withValidator: false);
        var behavior = new ValidationBehavior<CreateUserCommand, string>(sp);

        bool nextCalled = false;
        RequestHandlerDelegate<string> next = () =>
        {
            nextCalled = true;
            return Task.FromResult("ok");
        };

        string result = await behavior.Handle(new CreateUserCommand { Name = "" }, next, default);

        Assert.True(nextCalled);
        Assert.Equal("ok", result);
    }

    [Fact]
    public async Task Handle_WhenValidationPasses_CallsNext()
    {
        var sp = BuildServices(withValidator: true);
        var behavior = new ValidationBehavior<CreateUserCommand, string>(sp);

        bool nextCalled = false;
        RequestHandlerDelegate<string> next = () =>
        {
            nextCalled = true;
            return Task.FromResult("ok");
        };

        string result = await behavior.Handle(new CreateUserCommand { Name = "Alice" }, next, default);

        Assert.True(nextCalled);
        Assert.Equal("ok", result);
    }

    [Fact]
    public async Task Handle_WhenValidationFails_ThrowsValidationException()
    {
        var sp = BuildServices(withValidator: true);
        var behavior = new ValidationBehavior<CreateUserCommand, string>(sp);

        RequestHandlerDelegate<string> next = () => Task.FromResult("ok");

        await Assert.ThrowsAsync<ValidationException>(
            () => behavior.Handle(new CreateUserCommand { Name = "" }, next, default));
    }

    [Fact]
    public async Task Handle_WhenValidationFails_ErrorContainsPropertyName()
    {
        var sp = BuildServices(withValidator: true);
        var behavior = new ValidationBehavior<CreateUserCommand, string>(sp);

        RequestHandlerDelegate<string> next = () => Task.FromResult("ok");

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => behavior.Handle(new CreateUserCommand { Name = null }, next, default));

        Assert.True(ex.ValidationResult.Errors.ContainsKey("Name"));
    }

    [Fact]
    public async Task Handle_WhenMultiplePropertiesInvalid_AllErrorsPresent()
    {
        var services = new ServiceCollection();
        services.AddTransient<IValidator<CreateUserCommand>, CreateUserCommandValidator>();
        var sp = services.BuildServiceProvider();
        var behavior = new ValidationBehavior<CreateUserCommand, string>(sp);

        RequestHandlerDelegate<string> next = () => Task.FromResult("ok");

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => behavior.Handle(new CreateUserCommand { Name = "" }, next, default));

        Assert.False(ex.ValidationResult.IsValid);
        Assert.NotEmpty(ex.ValidationResult.Errors);
    }

    // --- Additional tests ---

    [Fact]
    public async Task Handle_WhenValidationFails_ExceptionContainsCorrectErrors()
    {
        var sp = BuildServices(withValidator: true);
        var behavior = new ValidationBehavior<CreateUserCommand, string>(sp);

        RequestHandlerDelegate<string> next = () => Task.FromResult("ok");

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => behavior.Handle(new CreateUserCommand { Name = "" }, next, default));

        Assert.True(ex.ValidationResult.Errors.ContainsKey("Name"));
        Assert.NotEmpty(ex.ValidationResult.Errors["Name"]);
    }

    [Fact]
    public async Task Handle_WhenValidationPasses_ReturnsHandlerResult()
    {
        var sp = BuildServices(withValidator: true);
        var behavior = new ValidationBehavior<CreateUserCommand, string>(sp);

        RequestHandlerDelegate<string> next = () => Task.FromResult("handler-result");

        string result = await behavior.Handle(new CreateUserCommand { Name = "Alice" }, next, default);

        Assert.Equal("handler-result", result);
    }

    [Fact]
    public async Task Handle_WhenNoValidatorRegistered_HandlerResultReturned()
    {
        var sp = BuildServices(withValidator: false);
        var behavior = new ValidationBehavior<CreateUserCommand, string>(sp);

        RequestHandlerDelegate<string> next = () => Task.FromResult("no-validator-result");

        string result = await behavior.Handle(new CreateUserCommand { Name = "" }, next, default);

        Assert.Equal("no-validator-result", result);
    }

    [Fact]
    public async Task Handle_MultipleValidatorsForDifferentTypes_EachValidatedIndependently()
    {
        // CreateUserCommand validator
        var servicesA = new ServiceCollection();
        servicesA.AddTransient<IValidator<CreateUserCommand>, CreateUserCommandValidator>();
        var spA = servicesA.BuildServiceProvider();
        var behaviorA = new ValidationBehavior<CreateUserCommand, string>(spA);

        RequestHandlerDelegate<string> nextA = () => Task.FromResult("user");
        await Assert.ThrowsAsync<ValidationException>(
            () => behaviorA.Handle(new CreateUserCommand { Name = "" }, nextA, default));

        // UpdateUserCommand validator — separate DI container, different type
        var servicesB = new ServiceCollection();
        servicesB.AddTransient<IValidator<UpdateUserCommand>, UpdateUserCommandValidator>();
        var spB = servicesB.BuildServiceProvider();
        var behaviorB = new ValidationBehavior<UpdateUserCommand, string>(spB);

        RequestHandlerDelegate<string> nextB = () => Task.FromResult("update");
        await Assert.ThrowsAsync<ValidationException>(
            () => behaviorB.Handle(new UpdateUserCommand { Email = "" }, nextB, default));
    }

    [Fact]
    public async Task Handle_WhenCancellationTokenProvided_PassesThroughSuccessfully()
    {
        var sp = BuildServices(withValidator: true);
        var behavior = new ValidationBehavior<CreateUserCommand, string>(sp);

        using var cts = new CancellationTokenSource();
        RequestHandlerDelegate<string> next = () => Task.FromResult("ok");

        // Valid request, token not cancelled — should succeed
        string result = await behavior.Handle(new CreateUserCommand { Name = "Bob" }, next, cts.Token);

        Assert.Equal("ok", result);
    }
}
