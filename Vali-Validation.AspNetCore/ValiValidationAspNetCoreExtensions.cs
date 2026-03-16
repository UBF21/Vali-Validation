using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Vali_Validation.AspNetCore;

/// <summary>
/// Extension methods for integrating Vali-Validation with ASP.NET Core.
/// </summary>
public static class ValiValidationAspNetCoreExtensions
{
    /// <summary>
    /// Registers the <see cref="ValiValidationMiddleware"/> in the request pipeline.
    /// Place this before other middleware that may throw <see cref="Vali_Validation.Core.Exceptions.ValidationException"/>.
    /// </summary>
    public static IApplicationBuilder UseValiValidationExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ValiValidationMiddleware>();
    }

    /// <summary>
    /// Optionally registers ProblemDetails services (ASP.NET Core built-in).
    /// Call this from <c>services.AddValiValidationProblemDetails()</c> in your DI setup.
    /// </summary>
    public static IServiceCollection AddValiValidationProblemDetails(this IServiceCollection services)
    {
        services.AddProblemDetails();
        return services;
    }

    /// <summary>
    /// Adds a <see cref="ValiValidationFilter{T}"/> endpoint filter that validates
    /// request arguments of type <typeparamref name="T"/> before the endpoint executes.
    /// </summary>
    /// <typeparam name="T">The request DTO type to validate.</typeparam>
    public static RouteHandlerBuilder WithValiValidation<T>(this RouteHandlerBuilder builder)
        where T : class
    {
        return builder.AddEndpointFilter<ValiValidationFilter<T>>();
    }
}
