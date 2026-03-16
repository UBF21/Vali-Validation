using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Vali_Validation.Core.Validators;

namespace Vali_Validation.Core.Extensions;

/// <summary>
/// Extension methods for registering Vali-Validation validators into the DI container.
/// </summary>
public static class ValiValidationExtension
{
    /// <summary>
    /// Scans <paramref name="assembly"/> for all non-abstract, non-interface classes that implement
    /// <c>IValidator&lt;T&gt;</c> and registers them with the specified <paramref name="lifetime"/>.
    /// </summary>
    /// <param name="services">The service collection to add validators to.</param>
    /// <param name="assembly">The assembly to scan for validator implementations.</param>
    /// <param name="lifetime">
    /// The service lifetime to use. Defaults to <see cref="ServiceLifetime.Transient"/>,
    /// which is correct for validators since each validation is independent.
    /// </param>
    public static IServiceCollection AddValidationsFromAssembly(
        this IServiceCollection services,
        Assembly assembly,
        ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        var registrations = assembly
            .GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidator<>))
                .Select(i => new { InterfaceType = i, ImplementationType = t }));

        foreach (var reg in registrations)
            services.Add(ServiceDescriptor.Describe(reg.InterfaceType, reg.ImplementationType, lifetime));

        return services;
    }
}
