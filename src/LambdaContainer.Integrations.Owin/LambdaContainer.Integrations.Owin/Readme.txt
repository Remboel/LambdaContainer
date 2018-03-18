   __                 _         _           ___            _        _                 
  / /  __ _ _ __ ___ | |__   __| | __ _    / __\___  _ __ | |_ __ _(_)_ __   ___ _ __ 
 / /  / _` | '_ ` _ \| '_ \ / _` |/ _` |  / /  / _ \| '_ \| __/ _` | | '_ \ / _ \ '__|
/ /__| (_| | | | | | | |_) | (_| | (_| | / /__| (_) | | | | || (_| | | | | |  __/ |   
\____/\__,_|_| |_| |_|_.__/ \__,_|\__,_| \____/\___/|_| |_|\__\__,_|_|_| |_|\___|_|   
                                                                                      
Thank you for downloading the owin integration for LambdaContainer.
To add lambda container to the owin pipeline use the extension method located in:

LambdaContainer.Integrations.Owin.AppBuilder.AppBuilderExtensions.WithLambdaContainer(builder, container)

Once added, each request will receive a sub scope of the lambdacontainer where the IOwinContext, IOwinRequest and IOwinResponse objects are registered.
To retrieve the lambda container from the owin context, use the extension method:

LambdaContainer.Integrations.Owin.OwinContext.OwinContextExtensions.GetLambdaContainer(context)

To enable dependency injection in custom middleware, register them in the owin pipeline by wrapping the type in the
LambdaContainerMiddleware<'a>

..or simply by using the Extension method: builder.UseLambdaContainerManagedMiddleware<'a>()

Example:
builder.Use<LambdaContainerMiddleware<SomeMiddleware>>()

Make sure that 'WithLambdaContainer' has been added earlier in the pipeline.

Release notes:
- 1.0.0: Updated core lib reference
- 0.0.2: Added LambdaContainer.Integrations.Owin.Middleware.External.LambdaContainerMiddleware<'a>
		 - Change dependency on LambdaContainer to 0.14.0
		 - Added extension method: UseLambdaContainerManagedMiddleware<'a>(builder)
- 0.0.1: Initial alpha
		 - Added app builder extension which sets container sub scope in context and registers current owin context within