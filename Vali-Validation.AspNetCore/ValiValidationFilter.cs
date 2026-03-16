using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Vali_Validation.Core.Validators;

namespace Vali_Validation.AspNetCore;

/// <summary>
/// Minimal API endpoint filter that validates a request argument of type <typeparamref name="T"/>
/// before the endpoint handler executes.
/// Returns HTTP 400 with a structured validation error response when validation fails.
/// </summary>
/// <typeparam name="T">The type to validate. Must have a registered <see cref="IValidator{T}"/>.</typeparam>
/// <example>
/// <code>
/// app.MapPost("/users", (CreateUserDto dto) => ...)
///    .AddEndpointFilter&lt;ValiValidationFilter&lt;CreateUserDto&gt;&gt;();
/// // Or using the extension:
/// app.MapPost("/users", (CreateUserDto dto) => ...)
///    .WithValiValidation&lt;CreateUserDto&gt;();
/// </code>
/// </example>
public sealed class ValiValidationFilter<T> : IEndpointFilter where T : class
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var validator = context.HttpContext.RequestServices.GetService<IValidator<T>>();
        if (validator is null)
            return await next(context);

        var argument = context.Arguments.OfType<T>().FirstOrDefault();
        if (argument is null)
            return await next(context);

        var result = await validator.ValidateAsync(argument);
        if (result.IsValid)
            return await next(context);

        var errors = result.Errors.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.ToArray());

        return Results.ValidationProblem(errors);
    }
}
