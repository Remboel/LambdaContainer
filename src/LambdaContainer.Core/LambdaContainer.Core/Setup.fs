// The MIT License(MIT)
// Copyright(c) 2017 Morten Rembøl Jacobsen

// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and 
// associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, 
// and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE 
// WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR 
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, 
// ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

namespace LambdaContainer.Core.Setup
open LambdaContainer.Core.Contracts
open LambdaContainer.Core.NetFrameworkEx
open LambdaContainer.Core.FactoryContracts
open LambdaContainer.Core.Container
open LambdaContainer.Core.TypeResolvers
open LambdaContainer.Core.RepositoryConstruction
open System
open System.Collections.Generic
open System.Diagnostics
open System.Reflection
open System.IO

module internal RegistryDiscovery =
    type RegistryMatchCondition = FileInfo -> bool
    type RegistryFoundAction<'a> = 'a -> unit

    let acceptRegistryFile (registryFileLoadCondition : RegistryMatchCondition) (name : string) =
        let fi = new FileInfo(name)
        match fi.Extension.ToLowerInvariant() with
        | ".dll" | ".exe" -> 
            fi |> registryFileLoadCondition
        | _ -> 
            false

    let getRegistries<'a> (assembly : Assembly) =
      try
        assembly.GetTypes()
        |> Seq.where (fun t -> t.IsClass)
        |> Seq.where (fun t -> not t.IsAbstract)
        |> Seq.where (fun t -> typeof<'a>.IsAssignableFrom(t))
        |> Seq.where (fun t -> t.GetConstructor([||]) |> isNull |> not)
      with
          | ex -> 
              Trace.TraceWarning (ex.ToString())
              Seq.empty<Type>

    let tryLoadAssembly (path : string) =
      try
          Assembly.LoadFrom(path) |> Some
      with
          | ex -> 
              Trace.TraceWarning (ex.ToString())
              None
      
    let tryCreateInstance<'a> (assembly : Assembly) (theType : Type) =
      try
          (theType.FullName |> assembly.CreateInstance) :?> 'a |> Some
      with
          | ex -> 
              Trace.TraceError (ex.ToString())
              None

    let getRegistryContainerCandidates dir =
        Directory.GetFiles(dir,"*", SearchOption.AllDirectories)
    
    let discoverRegistries<'a> hiveDirectory (isRegistryFile : RegistryMatchCondition) (onEach : RegistryFoundAction<'a>) =
      match hiveDirectory |> Directory.Exists with
      | false -> ()
      | true ->
          query 
              {
                  //Find assembly candidates that can be loaded
                  for assemblyName in hiveDirectory |> getRegistryContainerCandidates do
                  where (acceptRegistryFile isRegistryFile assemblyName)
                  select (tryLoadAssembly assemblyName) into maybeAssembly
                  where maybeAssembly.IsSome
                  
                  //Select all registries that can be read
                  select maybeAssembly.Value into assembly
                  for registry in getRegistries<'a> assembly do
                  select (tryCreateInstance<'a> assembly registry) into maybeRegistry
                  where maybeRegistry.IsSome
                  select maybeRegistry.Value
              } 
          |> Seq.iter onEach

/// <summary>
/// Creates a new instance of LambdaContainerBootstrapper</summary>
/// <param name="containerConfig"> The Configuration used when building the LambdaContainer</param>
/// <param name="assemblyScannerConfiguration"> The Configuration used when building the LambdaContainer's assembly scanner.</param>
type public LambdaContainerBootstrapper internal(containerConfig : LambdaContainerConfiguration ,assemblyScannerConfiguration : LambdaContainerAssemblyScannerConfiguration) =
    let _registrationCommands = new List<Delegate>()

    let runAssemblyScanner repositoryBuilder =
        RegistryDiscovery.discoverRegistries<ILambdaContainerRegistry> 
            assemblyScannerConfiguration.RegistryScannerBaseDir
            (fun fi -> fi |> assemblyScannerConfiguration.RegistryFileNameCondition.Invoke)
            (BuildUp.Operations.writeContentsOfRegistry repositoryBuilder)

    /// <summary>Creates a new instance of LambdaContainerBootstrapper where baseDir is <c>AppDomain.CurrentDomain.BaseDirectory</c> and any dll/exe file is evaluated</summary>
    /// <returns>A new instance of LambdaContainerFactory</returns>
    static member Create() =
        new LambdaContainerBootstrapper(new LambdaContainerConfiguration() , new LambdaContainerAssemblyScannerConfiguration())

    /// <summary>Configures the assembly scanner.</summary>
    /// <param name="configure"> A Configuration function to setup the factory config</param>
    /// <returns>The current instance of LambdaContainerFactory</returns>
    member this.ConfigureAssemblyScanner(configuration : Action<LambdaContainerAssemblyScannerConfiguration>) =
        ParameterGuard.checkForNull configuration
        assemblyScannerConfiguration |> configuration.Invoke

        this

    /// <summary>Configures the container settings.</summary>
    /// <param name="configure"> A Configuration function to setup the factory config</param>
    /// <returns>The current instance of LambdaContainerFactory</returns>
    member this.ConfigureContainerSettings(configuration : Action<LambdaContainerConfiguration>) =
        ParameterGuard.checkForNull configuration
        containerConfig |> configuration.Invoke

        this

    /// <summary>Configures the container with registrations from the provided registrations command. This can either replace or act as a supplement to the assembly scanning strategy.</summary>
    /// <param name="withRecordingFrom"> A registration recorderding script</param>
    /// <returns>The current instance of LambdaContainerFactory</returns>
    member this.WithRegistrationsFrom (withRecordingFrom : Action<ILambdaContainerRegistrationsRecorder>) =
        ParameterGuard.checkForNull withRecordingFrom
        let newRegistrations = new List<Delegate>()
        
        let recorder =  { new ILambdaContainerRegistrationsRecorder with
                            member this.Record<'registrationsType when 'registrationsType :> ILambdaContainerRegistrations> (registrations : LambdaRegistrationsCommand<'registrationsType>) =
                                newRegistrations.Add(registrations)
                                this
                        }

        withRecordingFrom.Invoke(recorder)
        _registrationCommands.AddRange(newRegistrations)

        this

    /// <summary>Executes the bootstrapper, builds and returns the Lambda Container</summary>
    /// <returns>A new instance of ILambdaContainer. The same instance will be available at: LambdaContainerInstance.Current</returns>
    member this.Run() =
        let repositoryBuilder = new ConcurrentFactoryConfigurationRepositoryBuilder(containerConfig.RegistrationCollisionMode, None) :> IFactoryConfigurationRepositoryBuilder
        
        //Make sure the container is available for injection
        repositoryBuilder.Add 
            (fun lc -> lc :> Object) 
            (FactoryIdentity(None, typeof<ILambdaContainer>, Some("Core library"))) 
            OutputLifetime.Singleton 
            DisposalScope.SubScope

        repositoryBuilder.Add 
            (fun lc -> lc :> Object) 
            (FactoryIdentity(None, typeof<ILambdaContainerScoping>, Some("Core library"))) 
            OutputLifetime.Singleton 
            DisposalScope.SubScope
        
        //Apply preconfigured registration commands
        _registrationCommands 
        |> Seq.iter (repositoryBuilder 
        |> BuildUp.Operations.applyRegistrationsCommand)

        //Execute the assembly scanner
        match assemblyScannerConfiguration.Enabled with
        | false -> 
            ()
        | true -> 
            repositoryBuilder |> runAssemblyScanner
        
        //Construct and save the current container
        let repository = repositoryBuilder.Build()
        let typeResolver = new DynamicTypeResolver()
        let scoping =
            {
                CreateSubScope = Scoping.Operations.createSubScope
                CreateResolutionScope = Scoping.Operations.createResolutionScope
            }
        new LambdaContainer(repository, typeResolver, scoping) :> ILambdaContainer