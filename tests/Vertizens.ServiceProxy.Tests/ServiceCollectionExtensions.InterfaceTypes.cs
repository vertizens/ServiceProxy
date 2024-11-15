using Microsoft.Extensions.DependencyInjection;

namespace Vertizens.ServiceProxy.Tests;

public partial class ServiceCollectionExtensions
{
    [Fact]
    public void AddInterfaceTypes_SimpleInterface()
    {
        var services = new ServiceCollection();
        services.AddInterfaceTypes<ITestSimple>();

        var testServices = services.Where(x => x.ServiceType == typeof(ITestSimple));

        Assert.Single(testServices);

        var testService = testServices.Single();

        Assert.True(testService.ImplementationType == typeof(TestSimpleImplementation));
        Assert.Equal(ServiceLifetime.Transient, testService.Lifetime);

        var serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public void AddInterfaceTypes_GenericInterface()
    {
        var services = new ServiceCollection();
        services.AddInterfaceTypes(typeof(ITestGeneric<>), ServiceLifetime.Scoped);

        var testServiceLong = services.SingleOrDefault(x => x.ServiceType == typeof(ITestGeneric<long>));
        Assert.NotNull(testServiceLong);
        Assert.False(testServiceLong.IsKeyedService);
        Assert.True(testServiceLong.ImplementationType == typeof(TestGenericLongImplementation));
        Assert.Equal(ServiceLifetime.Scoped, testServiceLong.Lifetime);

        var testServiceString = services.SingleOrDefault(x => x.ServiceType == typeof(ITestGeneric<string>));
        Assert.NotNull(testServiceString);
        Assert.True(testServiceString.IsKeyedService);
        Assert.True((string?)testServiceString.ServiceKey == KeyedServiceConstants.SpecialKeyedService);
        Assert.True(testServiceString.KeyedImplementationType == typeof(TestGenericStringImplementation));
        Assert.Equal(ServiceLifetime.Scoped, testServiceString.Lifetime);

        var serviceProvider = services.BuildServiceProvider();
    }
}