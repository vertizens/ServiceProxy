using Microsoft.Extensions.DependencyInjection;

namespace ServiceProxy.Tests;

public partial class ServiceCollectionExtensions
{
    [Fact]
    public void AddInterfaceTypes_SimpleInterface()
    {
        var services = new ServiceCollection();
        services.AddInterfaceTypes<ITestSimple>();

        var testServices = services.Where(x => x.ServiceType == typeof(ITestSimple));

        Assert.True(testServices.Count() == 1);

        var testService = testServices.Single();

        Assert.True(testService.ImplementationType == typeof(TestSimpleImplementation));
        Assert.True(testService.Lifetime == ServiceLifetime.Transient);

        var serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public void AddInterfaceTypes_GenericInterface()
    {
        var services = new ServiceCollection();
        services.AddInterfaceTypes(typeof(ITestGeneric<>), ServiceLifetime.Scoped);

        var testServiceLong = services.SingleOrDefault(x => x.ServiceType == typeof(ITestGeneric<long>));
        Assert.NotNull(testServiceLong);
        Assert.True(testServiceLong.ImplementationType == typeof(TestGenericLongImplementation));
        Assert.True(testServiceLong.Lifetime == ServiceLifetime.Scoped);

        var testServiceString = services.SingleOrDefault(x => x.ServiceType == typeof(ITestGeneric<string>));
        Assert.NotNull(testServiceString);
        Assert.True(testServiceString.ImplementationType == typeof(TestGenericStringImplementation));
        Assert.True(testServiceString.Lifetime == ServiceLifetime.Scoped);

        var serviceProvider = services.BuildServiceProvider();
    }
}