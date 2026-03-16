using System.IO;
using System.Text;
using Microsoft.AspNetCore.Http;
using Vali_Validation.Core.Exceptions;
using Vali_Validation.Core.Results;
using Xunit;

namespace Vali_Validation.AspNetCore.Tests;

public class ValiValidationMiddlewareTests
{
    private static ValidationResult BuildFailingResult(string property = "Name", string message = "The Name field cannot be empty.")
    {
        var result = new ValidationResult();
        result.AddError(property, message);
        return result;
    }

    [Fact]
    public async Task InvokeAsync_WhenNoException_PassesThrough()
    {
        bool nextCalled = false;
        var middleware = new ValiValidationMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
        Assert.Equal(200, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WhenValidationException_Returns400()
    {
        var validationResult = BuildFailingResult();
        var middleware = new ValiValidationMiddleware(_ => throw new ValidationException(validationResult));

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal(400, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WhenValidationException_ContentTypeIsProblemJson()
    {
        var validationResult = BuildFailingResult();
        var middleware = new ValiValidationMiddleware(_ => throw new ValidationException(validationResult));

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal("application/problem+json", context.Response.ContentType);
    }

    [Fact]
    public async Task InvokeAsync_WhenValidationException_BodyContainsErrors()
    {
        var validationResult = BuildFailingResult("Email", "Invalid email.");
        var middleware = new ValiValidationMiddleware(_ => throw new ValidationException(validationResult));

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        string body = await new StreamReader(context.Response.Body, Encoding.UTF8).ReadToEndAsync();

        Assert.Contains("Email", body);
        Assert.Contains("Invalid email.", body);
    }

    [Fact]
    public async Task InvokeAsync_WhenValidationException_BodyContainsStatus400()
    {
        var validationResult = BuildFailingResult();
        var middleware = new ValiValidationMiddleware(_ => throw new ValidationException(validationResult));

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        string body = await new StreamReader(context.Response.Body, Encoding.UTF8).ReadToEndAsync();

        Assert.Contains("400", body);
        Assert.Contains("Validation Failed", body);
    }

    [Fact]
    public async Task InvokeAsync_WhenOtherException_Rethrows()
    {
        var middleware = new ValiValidationMiddleware(_ => throw new InvalidOperationException("unexpected"));

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(context));
    }
}
