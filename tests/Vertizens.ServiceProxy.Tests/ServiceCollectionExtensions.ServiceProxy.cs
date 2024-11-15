using Microsoft.Extensions.DependencyInjection;

namespace Vertizens.ServiceProxy.Tests;
public partial class ServiceCollectionExtensions
{
    [Fact]
    public void AddServiceProxy_WrapImplementationType()
    {
        var services = new ServiceCollection();
        services.AddInterfaceTypes(typeof(ITestGeneric<>));
        services.AddServiceProxy(typeof(ProxyImplementation<>));

        var testServiceLong = services.SingleOrDefault(x => x.ServiceType == typeof(ITestGeneric<long>));
        Assert.NotNull(testServiceLong);
        Assert.False(testServiceLong.IsKeyedService);
        Assert.NotNull(testServiceLong.ImplementationFactory);
        Assert.Equal(ServiceLifetime.Transient, testServiceLong.Lifetime);

        var testServiceString = services.SingleOrDefault(x => x.ServiceType == typeof(ITestGeneric<string>));
        Assert.NotNull(testServiceString);
        Assert.True(testServiceString.IsKeyedService);
        Assert.NotNull(testServiceString.KeyedImplementationFactory);
        Assert.Equal(ServiceLifetime.Transient, testServiceString.Lifetime);

        var serviceProvider = services.BuildServiceProvider();

        var testServiceLongInstance = serviceProvider.GetRequiredService<ITestGeneric<long>>();
        Assert.IsType<ProxyImplementation<long>>(testServiceLongInstance);

        var testServiceStringInstance = serviceProvider.GetRequiredKeyedService<ITestGeneric<string>>(KeyedServiceConstants.SpecialKeyedService);
        Assert.IsType<ProxyImplementation<string>>(testServiceStringInstance);
    }

    [Fact]
    public void AddServiceProxy_WrapImplementationInstance()
    {
        var services = new ServiceCollection();
        services.AddSingleton(typeof(ITestGeneric<long>), new TestGenericLongImplementation());
        services.AddServiceProxy(typeof(ProxyImplementation<>));

        var testServiceLong = services.SingleOrDefault(x => x.ServiceType == typeof(ITestGeneric<long>));
        Assert.NotNull(testServiceLong);
        Assert.False(testServiceLong.IsKeyedService);
        Assert.NotNull(testServiceLong.ImplementationFactory);
        Assert.Equal(ServiceLifetime.Singleton, testServiceLong.Lifetime);

        var serviceProvider = services.BuildServiceProvider();

        var testServiceLongInstance = serviceProvider.GetRequiredService<ITestGeneric<long>>();
        Assert.IsType<ProxyImplementation<long>>(testServiceLongInstance);
    }

    [Fact]
    public void AddServiceProxy_WrapImplementationFactory()
    {
        var services = new ServiceCollection();
        services.AddTransient(typeof(ITestGeneric<long>), sp => new TestGenericLongImplementation());
        services.AddServiceProxy(typeof(ProxyImplementation<>));

        var testServiceLong = services.SingleOrDefault(x => x.ServiceType == typeof(ITestGeneric<long>));
        Assert.NotNull(testServiceLong);
        Assert.False(testServiceLong.IsKeyedService);
        Assert.NotNull(testServiceLong.ImplementationFactory);
        Assert.Equal(ServiceLifetime.Transient, testServiceLong.Lifetime);

        var serviceProvider = services.BuildServiceProvider();

        var testServiceLongInstance = serviceProvider.GetRequiredService<ITestGeneric<long>>();
        Assert.IsType<ProxyImplementation<long>>(testServiceLongInstance);
    }

    [Fact]
    public void AddServiceProxy_Success_Stops_Pipeline_Duplicate()
    {
        var services = new ServiceCollection();
        services.AddInterfaceTypes(typeof(ITestGeneric<>));
        services.AddServiceProxy(typeof(ProxyImplementation<>));
        services.AddServiceProxy(typeof(SecondProxyImplementation<>));
        services.AddServiceProxy(typeof(ProxyImplementation<>));

        var testServiceLong = services.SingleOrDefault(x => x.ServiceType == typeof(ITestGeneric<long>));
        Assert.NotNull(testServiceLong);
        Assert.NotNull(testServiceLong.ImplementationFactory);
        Assert.Equal(ServiceLifetime.Transient, testServiceLong.Lifetime);

        var testServiceString = services.SingleOrDefault(x => x.ServiceType == typeof(ITestGeneric<string>));
        Assert.NotNull(testServiceString);
        Assert.True(testServiceString.IsKeyedService);
        Assert.NotNull(testServiceString.KeyedImplementationFactory);
        Assert.Equal(ServiceLifetime.Transient, testServiceString.Lifetime);

        var serviceProvider = services.BuildServiceProvider();

        var testServiceLongInstance = serviceProvider.GetRequiredService<ITestGeneric<long>>();
        Assert.IsType<SecondProxyImplementation<long>>(testServiceLongInstance);

        var testServiceStringInstance = serviceProvider.GetRequiredKeyedService<ITestGeneric<string>>(KeyedServiceConstants.SpecialKeyedService);
        Assert.IsType<SecondProxyImplementation<string>>(testServiceStringInstance);
    }

    [Fact]
    public void AddServiceProxy_WrapImplementationType_Filter()
    {
        var services = new ServiceCollection();
        services.AddInterfaceTypes(typeof(ITestGeneric<>));
        services.AddServiceProxy((s, i) => s.GetGenericArguments()[0] == typeof(long), typeof(ProxyImplementation<>));

        var testServiceLong = services.SingleOrDefault(x => x.ServiceType == typeof(ITestGeneric<long>));
        Assert.NotNull(testServiceLong);
        Assert.False(testServiceLong.IsKeyedService);
        Assert.NotNull(testServiceLong.ImplementationFactory);
        Assert.Equal(ServiceLifetime.Transient, testServiceLong.Lifetime);

        var testServiceString = services.SingleOrDefault(x => x.ServiceType == typeof(ITestGeneric<string>));
        Assert.NotNull(testServiceString);
        Assert.True(testServiceString.IsKeyedService);
        Assert.NotNull(testServiceString.KeyedImplementationType);
        Assert.Equal(ServiceLifetime.Transient, testServiceString.Lifetime);

        var serviceProvider = services.BuildServiceProvider();

        var testServiceLongInstance = serviceProvider.GetRequiredService<ITestGeneric<long>>();
        Assert.IsType<ProxyImplementation<long>>(testServiceLongInstance);

        var testServiceStringInstance = serviceProvider.GetRequiredKeyedService<ITestGeneric<string>>(KeyedServiceConstants.SpecialKeyedService);
        Assert.IsType<TestGenericStringImplementation>(testServiceStringInstance);
    }
}
