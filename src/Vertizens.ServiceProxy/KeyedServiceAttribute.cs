namespace Vertizens.ServiceProxy;

/// <summary>
/// Defines this class to be registered as keyed service.
/// </summary>
/// <param name="_key">Used to set the key when registering in ServiceCollection as Keyed service</param>
[AttributeUsage(AttributeTargets.Class)]
public class KeyedServiceAttribute(object _key) : Attribute
{
    /// <summary>
    /// Key to use for registration and usage
    /// </summary>
    public object Key { get { return _key; } }
}
