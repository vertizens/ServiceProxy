# ServiceProxy

Wrap Registered .NET DI Services with Cross Cutting concerns

## What is a cross cutting concern?

Authorization, logging, validation, caching, etc can be considered 'cross cutting' because it is the same requirements regardless of the further downstream code.  They could even be considered 'layers' and it makes sense to make them generic for re-use.

## What does wrap mean?

Wrap is basically that, a method call occurs on a layer and if applicable the flow of execution goes to the next layer down.
This is synonymous with .NET middleware.  So when creating an API, how are custom domain services to fulfill a request any different?  This is the start of making them similar in a way.


### AddInterfaceTypes

Adds all non-abstract classes that implement an interface

usage:
```
services.AddInterfaceTypes<ITestSimple>();
services.AddInterfaceTypes(typeof(ITestGeneric<>));
```

Useful for patterns that have multiple implementations for a generic interface.  Register all of them with one call and stop writing service registration code.

### AddServiceProxy

Wraps an already registered service and replaces it with a matching type from a given set of types.  Given a type, if its a class and not abstract, find all of its open generic interfaces.  It then looks through registered services that have a service type of an interface that matches one found above.  The proxy is expected to have a constructor that takes in an instance of the same interface for the purposes of wrapping the functionality of the already registered service.

So basically an already registered service gets replaced with a type provided.  This effectively builds a 'pipeline' based on the same interface from the lowest layer up.

usage:
```
services.AddServiceProxy(typeof(ProxyImplementation<>));

//Any class that was registered like ITestGeneric<SomeType> gets replaced with ProxyImplementation<SomeType>
//Then that originally registered service is passed in to the proxy so it can be called.
internal class ProxyImplementation<T>(ITestGeneric<T> proxiedService) : ITestGeneric<T>
```

So combine layers by ordering them from lowest to highest.

usage:
```
services.AddServiceProxy(typeof(ValidationImplementation<>));
services.AddServiceProxy(typeof(AuthorizationImplementation<>));
```

Use the same generic interface for the domain implementation, validation, authorization and then layers are registered in order of dependency.