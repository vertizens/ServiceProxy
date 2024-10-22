using Microsoft.Extensions.DependencyInjection;

namespace Vertizens.ServiceProxy;
internal class ServiceProxyDescriptor
{
    public required ServiceDescriptor ProxiedService { get; set; }
    public required ServiceDescriptor ServiceProxy { get; set; }
    public required Type ImplementationType { get; set; }
}
