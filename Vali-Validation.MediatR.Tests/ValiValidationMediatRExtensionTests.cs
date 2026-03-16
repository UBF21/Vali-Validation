using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Vali_Validation.Core.Validators;
using Xunit;

namespace Vali_Validation.MediatR.Tests;

public class ValiValidationMediatRExtensionTests
{
    [Fact]
    public void AddValiValidationBehavior_RegistersBehaviorAsOpenGeneric()
    {
        var services = new ServiceCollection();
        services.AddValiValidationBehavior();

        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IPipelineBehavior<,>) &&
            d.ImplementationType == typeof(ValidationBehavior<,>));

        Assert.NotNull(descriptor);
    }

    [Fact]
    public void AddMediatRWithValidation_RegistersValidatorsFromAssembly()
    {
        var services = new ServiceCollection();
        services.AddMediatRWithValidation(
            cfg => cfg.RegisterServicesFromAssembly(typeof(CreateUserCommandValidator).Assembly),
            typeof(CreateUserCommandValidator).Assembly);

        var sp = services.BuildServiceProvider();
        var validator = sp.GetService<IValidator<CreateUserCommand>>();

        Assert.NotNull(validator);
    }
}
