using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Vali_Validation.AspNetCore;
using Vali_Validation.Core.Validators;
using Xunit;

namespace Vali_Validation.AspNetCore.Tests;

// --- Test models ---

public class CreateProductDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
}

public class CreateProductDtoValidator : AbstractValidator<CreateProductDto>
{
    public CreateProductDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Description).NotEmpty();
    }
}

// --- Tests ---

public class ValiValidateAttributeTests
{
    private static ActionExecutingContext BuildContext(
        IServiceProvider services,
        Dictionary<string, object?> actionArguments)
    {
        var httpContext = new DefaultHttpContext
        {
            RequestServices = services
        };

        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor());

        return new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            actionArguments,
            controller: new object());
    }

    [Fact]
    public async Task OnActionExecutionAsync_WhenNoValidatorRegistered_CallsNext()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var dto = new CreateProductDto { Name = "", Description = "" };
        var context = BuildContext(services, new Dictionary<string, object?> { ["dto"] = dto });

        bool nextCalled = false;
        ActionExecutionDelegate next = () =>
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(
                context,
                new List<IFilterMetadata>(),
                controller: new object()));
        };

        var attribute = new ValiValidateAttribute();
        await attribute.OnActionExecutionAsync(context, next);

        Assert.True(nextCalled);
        Assert.Null(context.Result);
    }

    [Fact]
    public async Task OnActionExecutionAsync_WhenValidationPasses_CallsNext()
    {
        var services = new ServiceCollection()
            .AddTransient<IValidator<CreateProductDto>, CreateProductDtoValidator>()
            .BuildServiceProvider();

        var dto = new CreateProductDto { Name = "Widget", Description = "A great product" };
        var context = BuildContext(services, new Dictionary<string, object?> { ["dto"] = dto });

        bool nextCalled = false;
        ActionExecutionDelegate next = () =>
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(
                context,
                new List<IFilterMetadata>(),
                controller: new object()));
        };

        var attribute = new ValiValidateAttribute();
        await attribute.OnActionExecutionAsync(context, next);

        Assert.True(nextCalled);
        Assert.Null(context.Result);
    }

    [Fact]
    public async Task OnActionExecutionAsync_WhenValidationFails_ReturnsBadRequest()
    {
        var services = new ServiceCollection()
            .AddTransient<IValidator<CreateProductDto>, CreateProductDtoValidator>()
            .BuildServiceProvider();

        var dto = new CreateProductDto { Name = "", Description = "" };
        var context = BuildContext(services, new Dictionary<string, object?> { ["dto"] = dto });

        ActionExecutionDelegate next = () =>
            Task.FromResult(new ActionExecutedContext(
                context,
                new List<IFilterMetadata>(),
                controller: new object()));

        var attribute = new ValiValidateAttribute();
        await attribute.OnActionExecutionAsync(context, next);

        Assert.NotNull(context.Result);
        var badRequest = Assert.IsType<BadRequestObjectResult>(context.Result);
        Assert.Equal(400, badRequest.StatusCode);
    }

    [Fact]
    public async Task OnActionExecutionAsync_WhenValidationFails_ResultContainsValidationErrors()
    {
        var services = new ServiceCollection()
            .AddTransient<IValidator<CreateProductDto>, CreateProductDtoValidator>()
            .BuildServiceProvider();

        var dto = new CreateProductDto { Name = null, Description = null };
        var context = BuildContext(services, new Dictionary<string, object?> { ["dto"] = dto });

        ActionExecutionDelegate next = () =>
            Task.FromResult(new ActionExecutedContext(
                context,
                new List<IFilterMetadata>(),
                controller: new object()));

        var attribute = new ValiValidateAttribute();
        await attribute.OnActionExecutionAsync(context, next);

        Assert.NotNull(context.Result);
        var badRequest = Assert.IsType<BadRequestObjectResult>(context.Result);
        var problemDetails = Assert.IsType<ValidationProblemDetails>(badRequest.Value);
        Assert.NotEmpty(problemDetails.Errors);
    }

    [Fact]
    public async Task OnActionExecutionAsync_WhenArgumentIsNull_SkipsValidation_CallsNext()
    {
        var services = new ServiceCollection()
            .AddTransient<IValidator<CreateProductDto>, CreateProductDtoValidator>()
            .BuildServiceProvider();

        var context = BuildContext(services, new Dictionary<string, object?> { ["dto"] = null });

        bool nextCalled = false;
        ActionExecutionDelegate next = () =>
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(
                context,
                new List<IFilterMetadata>(),
                controller: new object()));
        };

        var attribute = new ValiValidateAttribute();
        await attribute.OnActionExecutionAsync(context, next);

        Assert.True(nextCalled);
    }
}
