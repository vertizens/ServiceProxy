using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace ServiceProxy;
public static partial class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all non-abstract classes that implement an interface <typeparamref name="TInterface"/>.  Uses imlpementation types from calling assembly.
    /// </summary>
    /// <typeparam name="TInterface">Interface classes must implement</typeparam>
    public static IServiceCollection AddInterfaceTypes<TInterface>(this IServiceCollection services, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        return services.AddInterfaceTypes<TInterface>(Assembly.GetCallingAssembly(), serviceLifetime);
    }

    /// <summary>
    /// Adds all non-abstract classes that implement an interface <typeparamref name="TInterface"/>.  Uses  imlpementation types from passed in assembly.
    /// </summary>
    /// <typeparam name="TInterface">Interface classes must implement</typeparam>
    /// <param name="assembly">Assembly types to register</param>
    public static IServiceCollection AddInterfaceTypes<TInterface>(this IServiceCollection services, Assembly assembly, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        return services.AddInterfaceTypes(typeof(TInterface), assembly, serviceLifetime);
    }

    /// <summary>
    /// Adds all non-abstract classes that implement an interface <typeparamref name="TInterface"/>.  Uses imlpementation types from calling assembly.
    /// </summary>
    /// <param name="interfaceType">Interface classes must implement</param>
    public static IServiceCollection AddInterfaceTypes(this IServiceCollection services, Type interfaceType, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        return services.AddInterfaceTypes(interfaceType, Assembly.GetCallingAssembly(), serviceLifetime);
    }

    /// <summary>
    /// Adds all non-abstract classes that implement an interface <typeparamref name="TInterface"/>.  Uses  imlpementation types from passed in assembly.
    /// </summary>
    /// <param name="interfaceType">Interface classes must implement</param>
    /// <param name="assembly">Assembly types to register</param>
    public static IServiceCollection AddInterfaceTypes(this IServiceCollection services, Type interfaceType, Assembly assembly, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        return services.AddInterfaceTypes(interfaceType, serviceLifetime, assembly.GetTypes());
    }

    /// <summary>
    /// Adds all non-abstract classes that implement an interface <typeparamref name="TInterface"/>.  Uses  imlpementation types from <paramref name="types"/>.
    /// </summary>
    /// <param name="interfaceType">Interface classes must implement</param>
    /// <param name="serviceLifetime"></param>
    public static IServiceCollection AddInterfaceTypes(this IServiceCollection services, Type interfaceType, ServiceLifetime serviceLifetime = ServiceLifetime.Transient, params Type[] types)
    {
        var implementationTypes = types.Where(x => x.IsClass && !x.IsAbstract);

        foreach (var implementationType in implementationTypes)
        {
            if (!(implementationType.IsGenericType && implementationType.GetGenericArguments().Any(a => a.IsGenericTypeParameter)))
            {
                var customInterfaces = implementationType.GetInterfaces().Where(i => i == interfaceType || (i.IsGenericType && i.GetGenericTypeDefinition() == interfaceType));
                foreach (var customInterface in customInterfaces)
                {
                    services.Add(new ServiceDescriptor(customInterface, implementationType, serviceLifetime));
                }
            }
        }

        return services;
    }
}
