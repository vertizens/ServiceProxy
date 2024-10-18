namespace ServiceProxy.Tests;
public interface ITestGeneric<T>
{
    T DoStuff();
}

internal class TestGenericLongImplementation : ITestGeneric<long>
{
    public long DoStuff()
    {
        return 123;
    }
}

internal class TestGenericStringImplementation : ITestGeneric<string>
{
    public string DoStuff()
    {
        return "123";
    }
}

internal class TestGenericParamImplementation<K> : ITestGeneric<K> where K : class, new()
{
    public K DoStuff()
    {
        return new K();
    }
}

internal abstract class TestGenericAbstractImplementation<K> : ITestGeneric<K>
{
    public abstract K DoStuff();
}

internal class ProxyImplementation<T>(
    ITestGeneric<T> proxiedService
    ) : ITestGeneric<T>
{
    public T DoStuff()
    {
        //pre proxy things
        //authenticator, don't call proxied Service if not authorized 
        //validator, don't call proxied Service if invalid
        //cache layer, don't call proxied Service if in cache
        //etc...

        var proxiedReturn = proxiedService.DoStuff();

        //post proxy things

        return proxiedReturn;
    }
}

internal class SecondProxyImplementation<T>(
    ITestGeneric<T> proxiedService
    ) : ITestGeneric<T>
{
    public T DoStuff()
    {
        //pre proxy things
        //authenticator, don't call proxied Service if not authorized 
        //validator, don't call proxied Service if invalid
        //cache layer, don't call proxied Service if in cache
        //etc...

        var proxiedReturn = proxiedService.DoStuff();

        //post proxy things

        return proxiedReturn;
    }
}