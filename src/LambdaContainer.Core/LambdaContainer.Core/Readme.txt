   __                 _         _           ___            _        _                 
  / /  __ _ _ __ ___ | |__   __| | __ _    / __\___  _ __ | |_ __ _(_)_ __   ___ _ __ 
 / /  / _` | '_ ` _ \| '_ \ / _` |/ _` |  / /  / _ \| '_ \| __/ _` | | '_ \ / _ \ '__|
/ /__| (_| | | | | | | |_) | (_| | (_| | / /__| (_) | | | | || (_| | | | | |  __/ |   
\____/\__,_|_| |_| |_|_.__/ \__,_|\__,_| \____/\___/|_| |_|\__\__,_|_|_| |_|\___|_|   
                                                                                      
Thank you for installing Lambda Container.
Lambda Container is a lightweigt Inversion of Control container written in F# for all .Net clients.

Visit the project's website to view the code and/or browse the documentation: https://lambdacontainer.codeplex.com

Release notes:
- 1.0.0: 
	Extended registration API with convention based registrations.
- 0.14.1: Resolution improvements
- 0.14.0: Extended ILambdaContainerScoping
		  - Added WithCustomizedResolution which adds a set of registrations for the following instance resolution only.
- 0.13.0: Changed namespaces - removed LayZWriter prefix.
		  Nuget package name changed to LambdaContainer
		  Changed from 2D array to jagged array in buildplanner.
		  Refactored bootloader.	
		  Allows custom scope specific registrations to be created when creating a sub scope.
			- Use CreateSubScopeWith : recordAdditionalRegistrations : Action<ILambdaContainerRegistrationsRecorder> -> ILambdaContainer
			- This will pave the road for the cupcoming owin integration
		  LambdaContainerBootstrapper:
			- Breaking signature change: WithRegistrationsFrom (registrations : LambdaRegistrationsCommand<'registrationsType>) changed to WithRegistrationsFrom (withRecordingFrom : Action<ILambdaContainerRegistrationsRecorder>)
			- Moved namespace to LambdaContainer.Core.Setup
		  Removed IFactoryFSharpRegistrations - use IFactoryRegistrations
		  Changed signature of IFactoryRegistrations to take a tuple in order to enable type inference of fsharp functions as well as enable registration chaining.
		  Replaced all registry types by ILambdaContainerRegistry
- 0.12.0: Changed configuration API
		  - Registration override mode naming has been changed
		  - - RegistrationOverrideModes -> RegistrationCollisionModes
		  - - Skip -> Ignore
		  - - Accept -> Override
		  - Inner refactorings regarding reflection based type exploration replacement
- 0.11.2: Refactor in dynamic type resolver:
- 0.11.1: Changes to bootloader:
		  - Default disposal scope is now "SubScope".
		  - The LambdaContainerInstance.Current has been removed.
- 0.11.0: Bug fix:
		  - Injections of all registrations of a type by expecting an array of the type now works. Ctor example: MyType(IMyService[] services)
- 0.10.2: Inner restructuring
- 0.10.1: Changes in dynamic type resolver:
	      - Saves one function call in generated factory method by operating directly on FsharpFunc objects in stead of wrapping them in a Func.
		  - Generated factory methods are now shared across threads in a concurrent dictionary. Before this, each thread had a local store of all generated factory methods.
- 0.10.0: ResolveByName with unregistered concrete type now won't attempt to perform type build up since the client code expects a sepecific version, not just "a" version.
		  Types without factory functions (Type registratoins / unregistered concrete types) are now resolved faster due to switch from reflection to dynamic generation of factory functions.
- 0.9.2 : Removed fsharp core dependency.
- 0.9.1 : Updated nuspec dependencies to pull in fsharp core 4.0.0.1
- 0.9.0 : Updated runtime dependencies.	
			- Changed dependency: .NET framework 4.5.2 to 4.5
			- Changed dependency: F# runtime from 3.1 to 4.0
- 0.8.0 :	Changed bootloader design to support other boot loading scenarios besides assembly scanning.
			- Configuration of the LC is now more flexible. The old LambdaContainerConfiguration is replaced by:
				* LambdaContainerAssemblyScannerConfiguration: Configure assembly scanning specifics. The assembly scanner can also be turned off now for small/POC solutions.
				* LambdaContainerConfiguration: Container specific settings.
			- When configuring the bootstrapper it is now also possible to supplement/replace assembly scanning with registration commands, 
			  that offer the same registration API as the one found in the registry implementations used for assembly scanning.
			  The functionality is accessible throug <yourBootstrapperInstance>.WithRegistrationsFrom(..)
- 0.7.0 :	Added method/property injection for instances built using type registrations.:
			- Usage: Mark publich method and/or property with the LambdaContainerInjection Attribute, and it's arguments will be invoked during type buildup.
- 0.6.2 :	Internal refactoring.
- 0.6.1 :	Added reraise of dependency cycle if when factory is invoked
- 0.6.0 :	Added dependency cycle detection
- 0.5.0 :	Added dependency scopes / child containers. LC will now ignore ctors with primitive type arguments during injection ctor scanning.
			To create a dependency scope cast the container to ILambdaContainerScoping and invoke "CreateSubScope".
			Registrations with state ([thread]singleton) with disposal scope = SubScope will get their own result lifetime scope and will be disposed when the sub scope is disposed.
			Registrations with state ([thread]singleton) with disposal scope = Container will share the instances with the parent container and only when the root container is disposed, they will be disposed.
- 0.4.1 : Improved boot loader performance
- 0.4.0 : All public interfaces formerly suffixed with 'registrar' are now renamed to 'registrations'
- 0.3.0 : Renamed properties in bootstrapper config
- 0.2.0 : Replaced the old registrations API with a fluent interface. This makes way for introduction of dependency scopes.
- 0.1.1 : Refactored self retrieval
- 0.1.0 : Allowed GetInstance<ILambdaContainer> as well as ctor injection of the active container.
- 0.0.4 : Added reconfigurable bootstrapper.
- 0.0.3 : Internal refactorings.
- 0.0.2 : Fixed suppressed registration errors. Invalid registrations should stop the bootstrapper.
- 0.0.1 : Initial alpha release of the Lambda Container Core library.