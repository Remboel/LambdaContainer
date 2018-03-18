   __                 _         _           ___            _        _                 
  / /  __ _ _ __ ___ | |__   __| | __ _    / __\___  _ __ | |_ __ _(_)_ __   ___ _ __ 
 / /  / _` | '_ ` _ \| '_ \ / _` |/ _` |  / /  / _ \| '_ \| __/ _` | | '_ \ / _ \ '__|
/ /__| (_| | | | | | | |_) | (_| | (_| | / /__| (_) | | | | || (_| | | | | |  __/ |   
\____/\__,_|_| |_| |_|_.__/ \__,_|\__,_| \____/\___/|_| |_|\__\__,_|_|_| |_|\___|_|   
                                                                                      
Thank you for installing Lambda Container Web API Integration.
Lambda Container is a lightweigt Inversion of Control container written in F# for all .Net clients.
This package allows you to install the lambda container as WebAPI's dependency resolver in one line of code.

Visit the project's website to view the code and/or browse the documentation: https://lambdacontainer.codeplex.com

Code example -> configuring web api hive and setting the IoC container on web api http config:
	LambdaContainerBootstrapper
		.Create()
        .ConfigureForWebApi()
        .Run()
        .EnableAsWebApiDependencyResolver(httpConfiguration);

Release notes:
- 1.0.0:  Changed dependency to draw on V1 assemblies
- 0.10.1: Fixed namespace
- 0.10.0: Rewritten package in F#
		  - Extension method is now located in LambdaContainer.Integrations.WebApi.Extensions
- 0.9.0 : Changed package name to LambdaContainer.Integrations.WebApi
		  - Changed core dependency to 0.13.0
- 0.7.0 : Updated runtime dependencies.
			- Changed dependency: .NET framework 4.5.2 to 4.5
			- Changed dependency: F# runtime from 3.1 to 4.0
			- Changed dependency: Core >= 0.9.0
- 0.6.0 : Depends on Core >= 0.8.0.
- 0.5.0 : Depends on Core >= 0.5.0. Dependency/disposal scopes are now supported.
- 0.4.0 : Depends on Core >= 0.3.0
- 0.3.1 : Fixed readme code example
- 0.3.0 : Added 2 extensions. One "ConfigureForWebApi" that reconfigures the bootstrapper for web api and one "EnableAsWebApiDependencyResolver" which enables the lc as depenency resolver for web api.
- 0.2.0 : Fixed failed resolutions when not registered.
- 0.0.1 : Initial alpha release of the Lambda Container Core library.