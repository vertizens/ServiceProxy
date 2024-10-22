namespace Vertizens.ServiceProxy;

/// <summary>
/// Defines this class to be registered as keyed service.
/// </summary>
/// <param name="_key">Used to set the key when registering in ServiceCollection as Keyed service</param>
[AttributeUsage(AttributeTargets.Class)]
public class KeyedServiceAttribute(object _key) : Attribute
{
    public object Key { get { return _key; } }
}
