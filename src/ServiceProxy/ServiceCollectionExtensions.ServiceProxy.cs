using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace ServiceProxy;
public static partial class ServiceCollectionExtensions
{
    private static readonly Dictionary<ServiceDescriptor, ServiceProxyDescriptor> _proxyDescriptors = [];

    /// <summary>
    /// Wraps an already registered service and replaces it with a matching type from the calling assembly.
    /// </summary>
    /// <param name="types">One or more types that act as proxies for registered services</param>
    public static IServiceCollection AddServiceProxy(this IServiceCollection services)
    {
        return services.AddServiceProxy(Assembly.GetCallingAssembly());
    }

    /// <summary>
    /// Wraps an already registered service and replaces it with a matching type from <paramref name="assembly"/>.
    /// </summary>
    /// <param name="types">One or more types that act as proxies for registered services</param>
    public static IServiceCollection AddServiceProxy(this IServiceCollection services, Assembly assembly)
    {
        return AddServiceProxy(services, assembly.GetTypes());
    }

    /// <summary>
    /// Wraps an already registered service and replaces it with a matching type from <paramref name="types"/>.  Given a type, if its a class and not abstract, find all of its open generic interfaces.
    /// It then looks through registered services that have a service type of an interface that matches one found above.  The proxy is expected to have a constructor that takes in an instance of the same interface 
    /// for the purposes of wrapping the functionality of the already registered service.
    /// </summary>
    /// <param name="types">One or more types that act as proxies for registered services</param>
    public static IServiceCollection AddServiceProxy(this IServiceCollection services, params Type[] types)
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
                var proxyDescriptor = services.AddServiceProxy(serviceDescriptor, implementationsByInterface);
                if (proxyDescriptor != null)
                {
                    _proxyDescriptors.Add(proxyDescriptor.ServiceProxy, proxyDescriptor);
                }
            }
        }

        return services;
    }

    private static ServiceProxyDescriptor? AddServiceProxy(this IServiceCollection services, ServiceDescriptor serviceDescriptor, Dictionary<Type, List<Type>> implementationsByInterface)
    {
        ServiceProxyDescriptor? proxied = null;
        var serviceType = serviceDescriptor.ServiceType;
        if (!implementationsByInterface.TryGetValue(serviceType, out var implementationTypes) && serviceDescriptor.ServiceType.IsGenericType)
        {
            serviceType = serviceDescriptor.ServiceType.GetGenericTypeDefinition();
        }
        if (implementationsByInterface.TryGetValue(serviceType, out implementationTypes))
        {
            ServiceDescriptor? newServiceDescriptor = null;
            var implementationType = implementationTypes.Single();

            if (!IsServiceInProxyPipeline(serviceDescriptor, implementationType))
            {
                if (serviceDescriptor.ImplementationInstance != null && serviceDescriptor.ImplementationInstance.GetType() != implementationType)
                {
                    object implementationFactory(IServiceProvider serviceProvider)
                    {
                        return ActivatorUtilities.CreateInstance(serviceProvider, implementationType, serviceDescriptor.ImplementationInstance);
                    }
                    newServiceDescriptor = new ServiceDescriptor(serviceType, implementationFactory, ServiceLifetime.Singleton);
                }
                else if (serviceDescriptor.ImplementationType != null && serviceDescriptor.ImplementationType != implementationType)
                {
                    newServiceDescriptor = AddServiceProxyFromImplementationType(serviceDescriptor, implementationType);
                }
                else if (serviceDescriptor.ImplementationFactory != null)
                {
                    newServiceDescriptor = AddServiceProxyFromImplementationFactory(serviceDescriptor, serviceType, implementationType);
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

    private static ServiceDescriptor? AddServiceProxyFromImplementationFactory(ServiceDescriptor serviceDescriptor, Type serviceType, Type implementationType)
    {
        ServiceDescriptor? newServiceDescriptor = null;
        if (implementationType.IsGenericTypeDefinition)
        {
            var implementedGenericType = GetImplementedGenericType(serviceDescriptor.ServiceType, implementationType);

            if (implementedGenericType != null)
            {
                object implementationFactory(IServiceProvider serviceProvider)
                {
                    var proxyTarget = serviceDescriptor.ImplementationFactory!(serviceProvider);
                    return ActivatorUtilities.CreateInstance(serviceProvider, implementedGenericType, proxyTarget);
                }
                newServiceDescriptor = new ServiceDescriptor(serviceDescriptor.ServiceType, implementationFactory, serviceDescriptor.Lifetime);
            }
        }
        else
        {
            object implementationFactory(IServiceProvider serviceProvider)
            {
                var proxyTarget = serviceDescriptor.ImplementationFactory!(serviceProvider);
                return ActivatorUtilities.CreateInstance(serviceProvider, implementationType, proxyTarget);
            }
            newServiceDescriptor = new ServiceDescriptor(serviceType, implementationFactory, serviceDescriptor.Lifetime);
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

    private static ServiceDescriptor? AddServiceProxyFromImplementationType(ServiceDescriptor serviceDescriptor, Type implementationType)
    {
        ServiceDescriptor? newServiceDescriptor = null;
        if (implementationType.IsGenericTypeDefinition)
        {
            var implementedGenericType = GetImplementedGenericType(serviceDescriptor.ServiceType, implementationType);

            if (implementedGenericType != null)
            {
                object implementationFactory(IServiceProvider serviceProvider)
                {
                    var proxyTarget = ActivatorUtilities.CreateInstance(serviceProvider, serviceDescriptor.ImplementationType!);
                    return ActivatorUtilities.CreateInstance(serviceProvider, implementedGenericType, proxyTarget);
                }
                newServiceDescriptor = new ServiceDescriptor(serviceDescriptor.ServiceType, implementationFactory, serviceDescriptor.Lifetime);
            }
        }
        else
        {
            object implementationFactory(IServiceProvider serviceProvider)
            {
                var proxyTarget = ActivatorUtilities.CreateInstance(serviceProvider, serviceDescriptor.ImplementationType!);
                return ActivatorUtilities.CreateInstance(serviceProvider, implementationType, proxyTarget);
            }
            newServiceDescriptor = new ServiceDescriptor(serviceDescriptor.ServiceType, implementationFactory, serviceDescriptor.Lifetime);
        }

        return newServiceDescriptor;
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
