using System.Reflection;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Vali_Validation.Core.Extensions;

namespace Vali_Validation.MediatR;

/// <summary>
/// Extension methods for integrating Vali-Validation with MediatR.
/// </summary>
public static class ValiValidationMediatRExtension
{
    /// <summary>
    /// Registers the Vali-Validation pipeline behavior into MediatR and scans
    /// <paramref name="validatorsAssembly"/> for validator implementations.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="mediatRConfig">Action to configure MediatR (e.g. register handlers).</param>
    /// <param name="validatorsAssembly">Assembly to scan for <c>IValidator&lt;T&gt;</c> implementations.</param>
    /// <param name="validatorsLifetime">Lifetime for validators. Defaults to <see cref="ServiceLifetime.Transient"/>.</param>
    /// <example>
    /// <code>
    /// builder.Services.AddMediatRWithValidation(
    ///     cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly),
    ///     typeof(Program).Assembly);
    /// </code>
    /// </example>
    public static IServiceCollection AddMediatRWithValidation(
        this IServiceCollection services,
        Action<MediatRServiceConfiguration> mediatRConfig,
        Assembly validatorsAssembly,
        ServiceLifetime validatorsLifetime = ServiceLifetime.Transient)
    {
        services.AddMediatR(cfg =>
        {
            mediatRConfig(cfg);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidationsFromAssembly(validatorsAssembly, validatorsLifetime);
        return services;
    }

    /// <summary>
    /// Registers only the <see cref="ValidationBehavior{TRequest,TResponse}"/> as an open-generic
    /// MediatR behavior, without scanning for validators.
    /// Use this when you prefer to configure MediatR separately.
    /// </summary>
    /// <example>
    /// <code>
    /// builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(...));
    /// builder.Services.AddValiValidationBehavior();
    /// builder.Services.AddValidationsFromAssembly(typeof(Program).Assembly);
    /// </code>
    /// </example>
    public static IServiceCollection AddValiValidationBehavior(this IServiceCollection services)
    {
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        return services;
    }
}
