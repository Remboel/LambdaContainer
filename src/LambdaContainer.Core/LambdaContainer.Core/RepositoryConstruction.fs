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

namespace LambdaContainer.Core.RepositoryConstruction
open System
open System.Diagnostics
open System.Collections.Concurrent
open System.Collections.Generic
open System.Collections.ObjectModel
open LambdaContainer.Core.NetFrameworkEx
open LambdaContainer.Core.InstanceFactories
open LambdaContainer.Core.Contracts
open LambdaContainer.Core.FactoryContracts
open LambdaContainer.Core.DisposalScopes

(*
    Factory configuration Repository builder
*)

type internal IFactoryConfigurationRepositoryBuilder =
    abstract member Add : (ILambdaContainer -> Object) -> FactoryIdentity -> OutputLifetime -> DisposalScope -> unit
    abstract member Build : unit -> IFactoryConfigurationRepository

type internal ConcurrentFactoryConfigurationRepositoryBuilder(collissionHandling, referenceRepository : IFactoryConfigurationRepository option) =
    let registrations = new ConcurrentDictionary<Type,ConcurrentDictionary<FactoryIdentity,IInstanceFactory>>
                            (
                                match referenceRepository with
                                | None -> 
                                    []
                                | Some(repo) -> 
                                    repo.GetRegistrations()
                                    |> Seq.map (fun item -> KeyValuePair<Type,ConcurrentDictionary<FactoryIdentity,IInstanceFactory>>
                                                                (   item.Key,
                                                                    ConcurrentDictionary<FactoryIdentity,IInstanceFactory>(item.Value)
                                                                )
                                                )
                                    |> List.ofSeq
                            )   

    interface IFactoryConfigurationRepositoryBuilder with
        member __.Add factory identity lifetime subScopeMode =
            ParameterGuard.checkForNull identity
            ParameterGuard.checkForNull factory

            let factoryLookup = 
                registrations.AddOrUpdate 
                                       (identity.GetOutputType(),
                                       (fun _ -> new ConcurrentDictionary<FactoryIdentity,IInstanceFactory>()),
                                       (fun _ existing -> existing))
            
            match factoryLookup.Values |> Seq.tryHead with
            | None -> 
                ()
            | Some(head) -> 
                if head.GetIdentity().IsAnonymous() <> identity.IsAnonymous() then 
                    raise <| IncompatibleRegistrationException (sprintf "Error:Registration %s cannot be added since it conflicts with existing registration %s" (identity.ToString()) (head.ToString()))
            
            
            let factoryWithLifetimeManagement = 
                match lifetime with
                | OutputLifetime.Transient ->
                    new InstanceFactoryForTransientProducts(factory,identity) :> IInstanceFactory
                | OutputLifetime.ThreadSingleton ->
                    new InstanceFactoryThreadSingletonProducts(factory,identity) :> IInstanceFactory
                | OutputLifetime.Singleton -> 
                    new InstanceFactorySingletonProducts(factory,identity) :> IInstanceFactory
                | _ -> failwith "unknown lifetime"

            let factoryWithScopedLifetime = new ApplicationScope(factoryWithLifetimeManagement, subScopeMode) :> IInstanceFactory

            match factoryLookup.TryAdd(identity, (factoryWithScopedLifetime)) with
            | true -> 
                ()
            | false -> 
                match collissionHandling with
                | RegistrationCollisionModes.Fail -> 
                    raise <| OverlappingRegistrationException (sprintf "Error:Attempt to override existing registration %s with %s" (identity.ToString()) (factoryLookup.[identity].ToString())) 
                | RegistrationCollisionModes.Ignore -> 
                    ()
                | RegistrationCollisionModes.Override -> 
                    factoryLookup.AddOrUpdate(identity,factoryWithScopedLifetime,(fun _ _ -> factoryWithScopedLifetime)) |> ignore
                | _ -> failwith "unknown collision handling"

        member __.Build() =
            let result = new Dictionary<Type,IReadOnlyDictionary<FactoryIdentity,IInstanceFactory>>() :> IDictionary<Type,IReadOnlyDictionary<FactoryIdentity,IInstanceFactory>>

            registrations |> Seq.iter (fun mapping -> result.Add(mapping.Key, mapping.Value |> DictionaryEx.asReadOnly))
            
            new FactoryConfigurationRepository(result |> DictionaryEx.asReadOnly) :> IFactoryConfigurationRepository

module internal BuildUp =
    module private Types =
        type internal LambdaContainerRegistrationsBuilder<'registrationsType when 'registrationsType :> ILambdaContainerRegistrations>(repositoryBuilder : IFactoryConfigurationRepositoryBuilder, registrationSourceInfo : string ,configuredLifetime : OutputLifetime option, scope : DisposalScope option) =
    
            member private __.AssertLambdaContainerRegistration (instanceType : Type) =
                    //Do not allow manual registration of the active container or subscoping interface
                    if (instanceType.Equals(typeof<ILambdaContainer>)) then 
                        raise <| InvalidRegistrationException("Error: Registering the lambda container interface is not allowed since it is available by default")
                    if (instanceType.Equals(typeof<ILambdaContainerScoping>)) then 
                        raise <| InvalidRegistrationException("Error: Registering the lambda container scoping interface is not allowed since it is available by default")

            member private this.InnerRegister (lifetime : OutputLifetime) (scopeMode) (factory :  ILambdaContainer -> Object) (outputType : Type) (name : string option) =
                    this.AssertLambdaContainerRegistration outputType
                    repositoryBuilder.Add 
                        factory 
                        (FactoryIdentity(name,outputType,Some(registrationSourceInfo))) 
                        lifetime 
                        scopeMode

            interface ILambdaContainerRegistrationsBuilder<'registrationsType> with
    
                member __.WithOutputLifetime newLifetime =
                    new LambdaContainerRegistrationsBuilder<'registrationsType>(repositoryBuilder,registrationSourceInfo,Some(newLifetime), scope) :> ILambdaContainerRegistrationsBuilder<'registrationsType>
    
                member __.WithDisposalScope newScope =
                    new LambdaContainerRegistrationsBuilder<'registrationsType>(repositoryBuilder,registrationSourceInfo,configuredLifetime, Some(newScope)) :> ILambdaContainerRegistrationsBuilder<'registrationsType>

                member this.Build() =
                    let activeLifetime = 
                        match configuredLifetime with 
                        | None -> 
                            OutputLifetime.Transient 
                        | Some(aLifetime) -> 
                            aLifetime
            
                    let subScopeMode =  
                        match scope with 
                        | None -> 
                            DisposalScope.SubScope 
                        | Some(mode) -> 
                            mode

                    let writeRegistration = this.InnerRegister activeLifetime subScopeMode

                    (match typeof<'registrationsType> with
                        | t when t.Equals(typeof<IFactoryRegistrations>) -> 
                            writeRegistration |> FactoryRegistrations :> Object
                        | t when t.Equals(typeof<ITypeMappingRegistrations>) -> 
                            writeRegistration |> TypeMappingRegistrations :> Object
                        | t when t.Equals(typeof<ICoreRegistrations>) -> 
                            writeRegistration |> CoreRegistrations :> Object
                        | t when t.Equals(typeof<IRegistrationConvention>) -> 
                            writeRegistration |> CoreRegistrations |>  Conventions.createApi :> Object
                        | t -> 
                            raise <| InvalidRegistrationsException(sprintf "Error: Invalid registrations type selected: %s." t.FullName))
                    :?> 'registrationsType
    
    module Operations =
        open Types
        let build (builder : IFactoryConfigurationRepositoryBuilder) = 
            builder.Build()

        let recordFrom (source : ILambdaContainerRegistry) (destination : ILambdaContainerRegistrationsRecorder) = 
            source.WriteContentsTo destination

        let applyRegistrationsCommand repositoryBuilder (registrationsCommand : Delegate) =
                match registrationsCommand with
                | :? LambdaRegistrationsCommand<IFactoryRegistrations> as command -> 
                    command.Invoke (LambdaContainerRegistrationsBuilder<IFactoryRegistrations>(repositoryBuilder, registrationsCommand.GetType().FullName, None, None))
                | :? LambdaRegistrationsCommand<ITypeMappingRegistrations> as command -> 
                    command.Invoke (LambdaContainerRegistrationsBuilder<ITypeMappingRegistrations>(repositoryBuilder, registrationsCommand.GetType().FullName, None, None))
                | :? LambdaRegistrationsCommand<ICoreRegistrations> as command -> 
                    command.Invoke (LambdaContainerRegistrationsBuilder<ICoreRegistrations>(repositoryBuilder, registrationsCommand.GetType().FullName, None, None))
                | :? LambdaRegistrationsCommand<IRegistrationConvention> as command -> 
                    command.Invoke (LambdaContainerRegistrationsBuilder<IRegistrationConvention>(repositoryBuilder, registrationsCommand.GetType().FullName, None, None))
                | _ -> 
                    Trace.TraceError "Unknown command encountered"

        let createRegistrationsRecorder repositoryBuilder =
            {   new ILambdaContainerRegistrationsRecorder with
                    member this.Record<'registrationsType when 'registrationsType :> ILambdaContainerRegistrations> (registrations : LambdaRegistrationsCommand<'registrationsType>) =
                        applyRegistrationsCommand repositoryBuilder registrations
                        this
             }
        
        let applyRecordedRegistrationCommands (extendWith : (ILambdaContainerRegistrationsRecorder -> unit)) repositoryBuilder =
             
             repositoryBuilder
             |> createRegistrationsRecorder
             |> extendWith
             
             repositoryBuilder

        let writeContentsOfRegistry repositoryBuilder (registry : ILambdaContainerRegistry) =
            repositoryBuilder
            |> createRegistrationsRecorder
            |> recordFrom registry

module internal Scoping =
    module internal Operations =
        
        let createSubScope (repository : IFactoryConfigurationRepository option) (extendWith : (ILambdaContainerRegistrationsRecorder -> unit) option) =
            let repoClone = 
                match repository with
                | Some(r) ->
                    r.CreateSubScope()
                | None ->
                    let emptyRepo = Dictionary<Type,IReadOnlyDictionary<FactoryIdentity,IInstanceFactory>>()
                    new FactoryConfigurationRepository(emptyRepo) :> IFactoryConfigurationRepository

            match extendWith with
            | None -> 
                repoClone
            | Some(extendWith) ->
                ConcurrentFactoryConfigurationRepositoryBuilder(RegistrationCollisionModes.Override, Some(repoClone)) :> IFactoryConfigurationRepositoryBuilder
                |> BuildUp.Operations.applyRecordedRegistrationCommands extendWith
                |> BuildUp.Operations.build

        let createResolutionScope (repository : IFactoryConfigurationRepository) (extendWith : (ILambdaContainerRegistrationsRecorder -> unit)) =
            let resolutionScopedRepository = createSubScope None (extendWith |> Option.Some)
            let repoCollection =
                [|
                    Owns(resolutionScopedRepository)
                    References(repository)
                |]
            new CompositeConfigurationRepository(repoCollection) :> IFactoryConfigurationRepository