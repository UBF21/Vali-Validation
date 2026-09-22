using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Vali_Validation.Core.Extensions;
using Vali_Validation.Core.Rules;
using Vali_Validation.Core.Validators;
using Xunit;

namespace Vali_Validation.Tests;

public class InjectAddressDto
{
    public string? Street { get; set; }
}

public class InjectAddressValidator : AbstractValidator<InjectAddressDto>
{
    public InjectAddressValidator()
    {
        RuleFor(x => x.Street).NotEmpty();
    }
}

public class InjectOrderDto
{
    public InjectAddressDto? Address { get; set; }
}

public class InjectOrderValidatorViaDi : AbstractValidator<InjectOrderDto>
{
    public InjectOrderValidatorViaDi(IServiceProvider serviceProvider)
    {
        RuleFor(x => x.Address).InjectValidator(serviceProvider);
    }
}

public class InjectOrderValidatorManual : AbstractValidator<InjectOrderDto>
{
    public InjectOrderValidatorManual()
    {
        RuleFor(x => x.Address).SetValidator(new InjectAddressValidator());
    }
}

public class InjectValidatorTests
{
    private static IServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddValidationsFromAssembly(Assembly.GetExecutingAssembly());
        return services.BuildServiceProvider();
    }

    [Fact]
    public void InjectValidator_ResolvesFromDi_SameResultAsManualSetValidator()
    {
        var serviceProvider = BuildServiceProvider();
        var diValidator = new InjectOrderValidatorViaDi(serviceProvider);
        var manualValidator = new InjectOrderValidatorManual();
        var order = new InjectOrderDto { Address = new InjectAddressDto { Street = null } };

        var diResult = diValidator.Validate(order);
        var manualResult = manualValidator.Validate(order);

        Assert.False(diResult.IsValid);
        Assert.Equal(manualResult.ErrorsFor("Address.Street"), diResult.ErrorsFor("Address.Street"));
    }

    [Fact]
    public void InjectValidator_WithValidNestedInstance_ProducesNoErrors()
    {
        var serviceProvider = BuildServiceProvider();
        var diValidator = new InjectOrderValidatorViaDi(serviceProvider);
        var order = new InjectOrderDto { Address = new InjectAddressDto { Street = "Main St" } };

        var result = diValidator.Validate(order);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void InjectValidator_WithNullServiceProvider_Throws()
    {
        var builder = new InjectOrderValidatorManual().RuleFor(x => x.Address);

        Assert.Throws<ArgumentNullException>(() => builder.InjectValidator(null!));
    }

    [Fact]
    public void InjectValidator_WithNoRegisteredValidator_ThrowsFromDi()
    {
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var builder = new InjectOrderValidatorManual().RuleFor(x => x.Address);

        Assert.Throws<InvalidOperationException>(() => builder.InjectValidator(serviceProvider));
    }
}
