using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Vali_Mediator.Core.General.Behavior;
using Vali_Mediator.Core.General.Extension;
using Vali_Validation.Core.Extensions;

namespace Vali_Validation.ValiMediator;

/// <summary>
/// Extension methods for integrating Vali-Validation with Vali-Mediator.
/// </summary>
public static class ValiValidationMediatorExtension
{
    /// <summary>
    /// Registers the <see cref="ValidationBehavior{TRequest,TResponse}"/> in the mediator pipeline.
    /// Call this inside <c>AddValiMediator(config => ...)</c>.
    /// </summary>
    /// <param name="config">The mediator configuration object.</param>
    /// <param name="lifetime">Lifetime for the behavior. Defaults to <see cref="ServiceLifetime.Scoped"/>.</param>
    /// <example>
    /// <code>
    /// builder.Services.AddValiMediator(config =>
    /// {
    ///     config.RegisterServicesFromAssembly(typeof(Program).Assembly);
    ///     config.AddValiValidationBehavior();
    /// });
    /// builder.Services.AddValidationsFromAssembly(typeof(Program).Assembly);
    /// </code>
    /// </example>
    public static ValiMediatorConfiguration AddValiValidationBehavior(
        this ValiMediatorConfiguration config,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        config.AddBehavior(
            typeof(IPipelineBehavior<,>),
            typeof(ValidationBehavior<,>),
            lifetime);
        return config;
    }

    /// <summary>
    /// Convenience overload that registers both validators from <paramref name="validatorsAssembly"/>
    /// and the <see cref="ValidationBehavior{TRequest,TResponse}"/> in a single call.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="mediatorConfig">An action to configure the mediator.</param>
    /// <param name="validatorsAssembly">Assembly to scan for validator implementations.</param>
    /// <param name="validatorsLifetime">Lifetime for validators. Defaults to <see cref="ServiceLifetime.Transient"/>.</param>
    /// <example>
    /// <code>
    /// builder.Services.AddValiMediatorWithValidation(
    ///     config => config.RegisterServicesFromAssembly(typeof(Program).Assembly),
    ///     typeof(Program).Assembly);
    /// </code>
    /// </example>
    public static IServiceCollection AddValiMediatorWithValidation(
        this IServiceCollection services,
        Action<ValiMediatorConfiguration> mediatorConfig,
        Assembly validatorsAssembly,
        ServiceLifetime validatorsLifetime = ServiceLifetime.Transient)
    {
        services.AddValiMediator(config =>
        {
            mediatorConfig(config);
            config.AddValiValidationBehavior();
        });

        services.AddValidationsFromAssembly(validatorsAssembly, validatorsLifetime);
        return services;
    }
}
