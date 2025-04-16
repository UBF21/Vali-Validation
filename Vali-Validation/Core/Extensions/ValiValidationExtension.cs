using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Vali_Validation.Core.Validators;

namespace Vali_Validation.Core.Extensions;

/// <summary>
/// Provides extension methods for registering Vali-Validation validators into the service collection.
/// </summary>
public static class ValiValidationExtension
{
    
    /// <summary>
    /// Registers all validator implementations from the specified assembly into the dependency injection container.
    /// 
    /// It scans the provided assembly for all non-abstract, non-interface classes that implement
    /// the <c>IValidator&lt;T&gt;</c> interface and registers them with <c>Transient</c> lifetime.
    /// 
    /// This method enables automatic discovery and registration of validation rules following
    /// the Vali-Validation pattern, similar to FluentValidation's registration behavior.
    /// </summary>
    /// <param name="services">The service collection to add validators to.</param>
    /// <param name="assembly">The assembly to scan for validator implementations.</param>
    /// <returns>The modified <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddValidationsFromAssembly(this IServiceCollection services, Assembly assembly)
    {
        var validatorTypes = assembly
            .GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidator<>))
                .Select(i => new { InterfaceType = i, ImplementationType = t }));

        foreach (var validator in validatorTypes)
        {
            services.AddTransient(validator.InterfaceType, validator.ImplementationType);
        }

        return services;
    }
}