   __                 _         _           ___            _        _                 
  / /  __ _ _ __ ___ | |__   __| | __ _    / __\___  _ __ | |_ __ _(_)_ __   ___ _ __ 
 / /  / _` | '_ ` _ \| '_ \ / _` |/ _` |  / /  / _ \| '_ \| __/ _` | | '_ \ / _ \ '__|
/ /__| (_| | | | | | | |_) | (_| | (_| | / /__| (_) | | | | || (_| | | | | |  __/ |   
\____/\__,_|_| |_| |_|_.__/ \__,_|\__,_| \____/\___/|_| |_|\__\__,_|_|_| |_|\___|_|   
                                                                                      
Thank you for downloading the owin webapi integration for LambdaContainer.

To extend the use of the Lambda Container scope used in the owin pipeline to WebApi, use the appbuilder extension method:

WithLambdaContainerForWebApi(httpConfiguration)

Full example:
let createContainer() =
	LambdaContainerBootstrapper
		.Create()
        .ConfigureForWebApi()
        .Run()

let config = new HttpConfiguration()
let container = createContainer()

app
   .WithLambdaContainer(container)
   .WithLambdaContainerForWebApi(config)
   .UseWebApi(config)

Release notes:
- 1.0.0: Updated depenencies of core lib.
- 0.0.2: Added bootstrapper extensions to configure assembly scanner for web app.
- 0.0.1: Initial alpha
		 - Added WithLambdaContainerForWebApi extension for IAppBuilder to enable reuse in webapi of the owin lambda container scope.