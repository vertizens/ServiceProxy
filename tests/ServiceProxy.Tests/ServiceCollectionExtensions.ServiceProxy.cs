using Microsoft.Extensions.DependencyInjection;

namespace ServiceProxy.Tests;
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
        Assert.NotNull(testServiceLong.ImplementationFactory);
        Assert.True(testServiceLong.Lifetime == ServiceLifetime.Transient);

        var testServiceString = services.SingleOrDefault(x => x.ServiceType == typeof(ITestGeneric<string>));
        Assert.NotNull(testServiceString);
        Assert.NotNull(testServiceLong.ImplementationFactory);
        Assert.True(testServiceString.Lifetime == ServiceLifetime.Transient);

        var serviceProvider = services.BuildServiceProvider();

        var testServiceLongInstance = serviceProvider.GetRequiredService<ITestGeneric<long>>();
        Assert.IsType<ProxyImplementation<long>>(testServiceLongInstance);

        var testServiceStringInstance = serviceProvider.GetRequiredService<ITestGeneric<string>>();
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
        Assert.NotNull(testServiceLong.ImplementationFactory);
        Assert.True(testServiceLong.Lifetime == ServiceLifetime.Singleton);

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
        Assert.NotNull(testServiceLong.ImplementationFactory);
        Assert.True(testServiceLong.Lifetime == ServiceLifetime.Transient);

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
        Assert.True(testServiceLong.Lifetime == ServiceLifetime.Transient);

        var testServiceString = services.SingleOrDefault(x => x.ServiceType == typeof(ITestGeneric<string>));
        Assert.NotNull(testServiceString);
        Assert.NotNull(testServiceLong.ImplementationFactory);
        Assert.True(testServiceString.Lifetime == ServiceLifetime.Transient);

        var serviceProvider = services.BuildServiceProvider();

        var testServiceLongInstance = serviceProvider.GetRequiredService<ITestGeneric<long>>();
        Assert.IsType<SecondProxyImplementation<long>>(testServiceLongInstance);

        var testServiceStringInstance = serviceProvider.GetRequiredService<ITestGeneric<string>>();
        Assert.IsType<SecondProxyImplementation<string>>(testServiceStringInstance);
    }
}
