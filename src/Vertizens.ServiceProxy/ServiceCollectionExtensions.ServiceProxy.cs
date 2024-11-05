using Microsoft.Extensions.DependencyInjection;

namespace Vertizens.ServiceProxy;
public static partial class ServiceCollectionExtensions
{
    private static readonly Dictionary<ServiceDescriptor, ServiceProxyDescriptor> _proxyDescriptors = [];

    /// <summary>
    /// Wraps an already registered service and replaces it with a matching type from <paramref name="types"/>.  Given a type, if its a class and not abstract, find all of its open generic interfaces.
    /// It then looks through registered services that have a service type of an interface that matches one found above.  The proxy is expected to have a constructor that takes in an instance of the same interface 
    /// for the purposes of wrapping the functionality of the already registered service.
    /// </summary>
    /// <param name="types">One or more types that act as proxies for registered services</param>
    public static IServiceCollection AddServiceProxy(this IServiceCollection services, params Type[] types)
    {
        return services.AddServiceProxy(null, types);
    }
    /// <summary>
    /// Wraps an already registered service and replaces it with a matching type from <paramref name="types"/>.  Given a type, if its a class and not abstract, find all of its open generic interfaces.
    /// It then looks through registered services that have a service type of an interface that matches one found above.  The proxy is expected to have a constructor that takes in an instance of the same interface 
    /// for the purposes of wrapping the functionality of the already registered service.
    /// </summary>
    /// <param name="types">One or more types that act as proxies for registered services</param>
    /// <param name="filter">Provided filter for whether or not to Proxy a particular ServiceType</param>
    public static IServiceCollection AddServiceProxy(this IServiceCollection services, Func<Type, Type, bool>? filter = null, params Type[] types)
    {
        var implementationsByInterface = types.Where(x => x.IsClass && !x.IsAbstract).SelectMany(x => x.GetInterfaces()
            .Select(i =>
            {
                var interfaceType = i;
                if (i.IsGenericType && i.GetGenericArguments().Any(x => x.IsGenericParameter))
                {
                    interfaceType = i.GetGenericTypeDefinition();
                }
                return new KeyValuePair<Type, Type>(interfaceType, x);
            })).GroupBy(x => x.Key, x => x.Value).ToDictionary(x => x.Key, x => x.ToList());

        foreach (var serviceDescriptor in services.ToList())
        {
            if (serviceDescriptor.ServiceType.IsInterface)
            {
                var proxyDescriptor = services.AddServiceProxy(serviceDescriptor, implementationsByInterface, filter);
                if (proxyDescriptor != null)
                {
                    _proxyDescriptors.Add(proxyDescriptor.ServiceProxy, proxyDescriptor);
                }
            }
        }

        return services;
    }

    private static ServiceProxyDescriptor? AddServiceProxy(this IServiceCollection services,
        ServiceDescriptor serviceDescriptor,
        Dictionary<Type, List<Type>> implementationsByInterface,
        Func<Type, Type, bool>? filter = null)
    {
        ServiceProxyDescriptor? proxied = null;
        var serviceType = serviceDescriptor.ServiceType;
        if (!implementationsByInterface.TryGetValue(serviceType, out _) && serviceDescriptor.ServiceType.IsGenericType)
        {
            serviceType = serviceDescriptor.ServiceType.GetGenericTypeDefinition();
        }

        if (implementationsByInterface.TryGetValue(serviceType, out var implementationTypes))
        {
            ServiceDescriptor? newServiceDescriptor = null;
            var implementationType = implementationTypes.Single();

            if (!IsServiceInProxyPipeline(serviceDescriptor, implementationType))
            {
                if ((!serviceDescriptor.IsKeyedService && serviceDescriptor.ImplementationType != null && serviceDescriptor.ImplementationType != implementationType) ||
                    (serviceDescriptor.IsKeyedService && serviceDescriptor.KeyedImplementationType != null && serviceDescriptor.KeyedImplementationType != implementationType))
                {
                    newServiceDescriptor = AddServiceProxyFromImplementationType(serviceDescriptor, implementationType, filter);
                }
                else if ((!serviceDescriptor.IsKeyedService && serviceDescriptor.ImplementationFactory != null) ||
                    (serviceDescriptor.IsKeyedService && serviceDescriptor.KeyedImplementationFactory != null))
                {
                    newServiceDescriptor = AddServiceProxyFromImplementationFactory(serviceDescriptor, implementationType, filter);
                }
                else if ((!serviceDescriptor.IsKeyedService && serviceDescriptor.ImplementationInstance != null && serviceDescriptor.ImplementationInstance.GetType() != implementationType) ||
                    (serviceDescriptor.IsKeyedService && serviceDescriptor.KeyedImplementationInstance != null && serviceDescriptor.KeyedImplementationInstance.GetType() != implementationType))
                {
                    newServiceDescriptor = AddServiceProxyFromImplementationInstance(serviceDescriptor, implementationType, filter);
                }
            }

            if (newServiceDescriptor != null)
            {
                services.Remove(serviceDescriptor);
                services.Add(newServiceDescriptor);
                proxied = new ServiceProxyDescriptor { ImplementationType = implementationType, ProxiedService = serviceDescriptor, ServiceProxy = newServiceDescriptor };
            }
        }

        return proxied;
    }

    private static ServiceDescriptor? AddServiceProxyFromImplementationInstance(ServiceDescriptor serviceDescriptor, Type implementationType, Func<Type, Type, bool>? filter = null)
    {
        ServiceDescriptor? newServiceDescriptor = null;
        var newImplementationType = implementationType.IsGenericTypeDefinition ? GetImplementedGenericType(serviceDescriptor.ServiceType, implementationType) : implementationType;
        if (newImplementationType != null)
        {
            if (filter == null || filter(serviceDescriptor.ServiceType, newImplementationType))
            {
                if (serviceDescriptor.IsKeyedService)
                {
                    object implementationFactory(IServiceProvider serviceProvider, object? serviceKey)
                    {
                        return ActivatorUtilities.CreateInstance(serviceProvider, newImplementationType, serviceDescriptor.KeyedImplementationInstance!);
                    };
                    newServiceDescriptor = new ServiceDescriptor(serviceDescriptor.ServiceType, serviceDescriptor.ServiceKey, implementationFactory, ServiceLifetime.Singleton);
                }
                else
                {
                    object implementationFactory(IServiceProvider serviceProvider)
                    {
                        return ActivatorUtilities.CreateInstance(serviceProvider, newImplementationType, serviceDescriptor.ImplementationInstance!);
                    };
                    newServiceDescriptor = new ServiceDescriptor(serviceDescriptor.ServiceType, implementationFactory, ServiceLifetime.Singleton);
                }
            }
        }

        return newServiceDescriptor;
    }

    private static ServiceDescriptor? AddServiceProxyFromImplementationFactory(ServiceDescriptor serviceDescriptor, Type implementationType, Func<Type, Type, bool>? filter = null)
    {
        ServiceDescriptor? newServiceDescriptor = null;
        var newImplementationType = implementationType.IsGenericTypeDefinition ? GetImplementedGenericType(serviceDescriptor.ServiceType, implementationType) : implementationType;
        if (newImplementationType != null)
        {
            if (filter == null || filter(serviceDescriptor.ServiceType, newImplementationType))
            {
                if (serviceDescriptor.IsKeyedService)
                {
                    object implementationFactory(IServiceProvider serviceProvider, object? serviceKey)
                    {
                        var proxyTarget = serviceDescriptor.KeyedImplementationFactory!(serviceProvider, serviceKey);
                        return ActivatorUtilities.CreateInstance(serviceProvider, newImplementationType, proxyTarget);
                    }
                    newServiceDescriptor = new ServiceDescriptor(serviceDescriptor.ServiceType, serviceDescriptor.ServiceKey, implementationFactory, serviceDescriptor.Lifetime);
                }
                else
                {
                    object implementationFactory(IServiceProvider serviceProvider)
                    {
                        var proxyTarget = serviceDescriptor.ImplementationFactory!(serviceProvider);
                        return ActivatorUtilities.CreateInstance(serviceProvider, newImplementationType, proxyTarget);
                    }
                    newServiceDescriptor = new ServiceDescriptor(serviceDescriptor.ServiceType, implementationFactory, serviceDescriptor.Lifetime);
                }
            }
        }

        return newServiceDescriptor;
    }

    private static ServiceDescriptor? AddServiceProxyFromImplementationType(ServiceDescriptor serviceDescriptor, Type implementationType, Func<Type, Type, bool>? filter = null)
    {
        ServiceDescriptor? newServiceDescriptor = null;
        var newImplementationType = implementationType.IsGenericTypeDefinition ? GetImplementedGenericType(serviceDescriptor.ServiceType, implementationType) : implementationType;
        if (newImplementationType != null)
        {
            if (filter == null || filter(serviceDescriptor.ServiceType, newImplementationType))
            {
                if (serviceDescriptor.IsKeyedService)
                {
                    object implementationFactory(IServiceProvider serviceProvider, object? serviceKey)
                    {
                        var proxyTarget = ActivatorUtilities.CreateInstance(serviceProvider, serviceDescriptor.KeyedImplementationType!);
                        return ActivatorUtilities.CreateInstance(serviceProvider, newImplementationType, proxyTarget);
                    }
                    newServiceDescriptor = new ServiceDescriptor(serviceDescriptor.ServiceType, serviceDescriptor.ServiceKey, implementationFactory, serviceDescriptor.Lifetime);
                }
                else
                {
                    object implementationFactory(IServiceProvider serviceProvider)
                    {
                        var proxyTarget = ActivatorUtilities.CreateInstance(serviceProvider, serviceDescriptor.ImplementationType!);
                        return ActivatorUtilities.CreateInstance(serviceProvider, newImplementationType, proxyTarget);
                    }
                    newServiceDescriptor = new ServiceDescriptor(serviceDescriptor.ServiceType, implementationFactory, serviceDescriptor.Lifetime);
                }
            }
        }

        return newServiceDescriptor;
    }

    private static Type? GetImplementedGenericType(Type serviceType, Type implementationType)
    {
        var serviceGenericArgumentTypes = serviceType.GetGenericArguments();
        var implementationTypeGenericDef = implementationType.GetGenericTypeDefinition();
        var implementationTypeArguments = implementationTypeGenericDef.GetGenericArguments();
        implementationType = implementationType.GetGenericTypeDefinition().MakeGenericType(serviceGenericArgumentTypes.Take(implementationTypeArguments.Length).ToArray());

        var assignableInterfaces = implementationType.GetInterfaces().ToList();
        var matching = false;
        foreach (var assignableInterface in assignableInterfaces)
        {
            matching |= serviceType.IsAssignableTo(assignableInterface);
        }

        return matching ? implementationType : null;
    }

    private static bool IsServiceInProxyPipeline(ServiceDescriptor serviceDescriptor, Type implementationType)
    {
        _proxyDescriptors.TryGetValue(serviceDescriptor, out var proxyDescriptor);

        var exists = proxyDescriptor?.ImplementationType == implementationType;

        if (proxyDescriptor?.ProxiedService != null && !exists)
        {
            exists = IsServiceInProxyPipeline(proxyDescriptor.ProxiedService, implementationType);
        }

        return exists;
    }
}
