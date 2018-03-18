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

namespace LambdaContainer.Core.Container
open System
open System.Diagnostics
open System.Collections.Generic
open System.Collections.Concurrent
open System.Threading
open System.Reflection
open System.Linq.Expressions
open LambdaContainer.Core.Contracts
open LambdaContainer.Core.FactoryContracts
open LambdaContainer.Core.InstanceFactories
open LambdaContainer.Core.RepositoryConstruction
open LambdaContainer.Core.TypeResolvers
open LambdaContainer.Core.NetFrameworkEx
open LambdaContainer.Core.NetFrameworkEx.DataStructures
open LambdaContainer.Core.NetFrameworkEx.ObjectTransforms

type internal BuildProcedure = BuildProcedure of BuildStep

and internal BuildStep =
    | ResolveFromRepository of BuildStep
    | ResolveAllIfArrayIsRequested of BuildStep
    | TryDynamicResolution of BuildStep
    | Terminate of BuildTermination

and internal BuildTermination =
    | GiveUp
    | MissingRegistration

module internal BuildPlanner =
    let private buildProcedures = 
        [[ ResolveFromRepository(             //[0,0] Name: None, acceptNone: false
                ResolveAllIfArrayIsRequested(
                    TryDynamicResolution(
                        Terminate(
                            MissingRegistration)))) |> BuildProcedure;
        
             ResolveFromRepository(             //[0,1] Name: None, acceptNone: true
                ResolveAllIfArrayIsRequested(
                    TryDynamicResolution(
                        Terminate(
                            GiveUp)))) |> BuildProcedure]; 
        
          [ ResolveFromRepository(              //[1,0] Name: Some, acceptNone: false
                Terminate(
                    MissingRegistration)) |> BuildProcedure;
        
            ResolveFromRepository(              //[1,1] Name: None, acceptNone: true
                Terminate(
        
                    GiveUp)) |> BuildProcedure]]

    let createBuildProcedure (hasName : bool) (acceptNone : bool) = 
        buildProcedures.[Convert.ToInt32(hasName)].[Convert.ToInt32(acceptNone)]

type internal RepositoryScopingParams = 
    {
        CreateSubScope : IFactoryConfigurationRepository option -> (ILambdaContainerRegistrationsRecorder -> unit) option -> IFactoryConfigurationRepository
        CreateResolutionScope : IFactoryConfigurationRepository -> (ILambdaContainerRegistrationsRecorder -> unit) -> IFactoryConfigurationRepository
    }

type internal LambdaContainer(repository : IFactoryConfigurationRepository, typeResolver : ITypeResolver, scoping : RepositoryScopingParams) as this =
    let resolutionStack = new ThreadLocal<StackSet<string>>(fun () -> new StackSet<string>())

    let createResolutionKey (theType : Type) theName =
        let namePart = 
            match theName with 
            | None -> 
                "" 
            | Some(x) -> 
                x

        sprintf "%s_%s" namePart theType.FullName

    let beginResolution t n =
        let key = createResolutionKey t n
        resolutionStack.Value.Push key

    let endResolution() =
        resolutionStack.Value.Pop() |> ignore

    let printResolutionStack forType name =
        let key = createResolutionKey forType name
        sprintf "%s-->%s" (System.String.Join("-->",resolutionStack.Value.AsSeq)) key
    
    let terminateProcedure (instanceType : Type) (name : string option) termination : Object option =
        match termination with
        | GiveUp -> 
            None
        | MissingRegistration -> 
            raise <| MissingRegistrationException(sprintf "Error: No registration and failed to dynamically build for Name[%s] Type[%s]" instanceType.FullName (match name with |None -> String.Empty |Some(t)->t))
    
    let innerInvokeFactory (factory : IInstanceFactory) =
        try
            (this :> ILambdaContainer) |> factory.Invoke
        with
            | CyclicDependencyException _ -> 
                reraise()
            | ex -> 
                raise <| FactoryInvocationException((sprintf "Error invoking factory on %s" (factory.ToString())), ex)
    
    let isRegisteredInRepository (instanceType : Type) (name : string option) =
        Option.isSome <| repository.Retrieve instanceType name
        
    
    let rec followProcedure (instanceType : Type) (name : string option) currentStep : Object option =
        let executeStep = followProcedure instanceType name
        
        match currentStep with
        | ResolveFromRepository(ns) -> 
            match repository.Retrieve instanceType name with
            | None -> 
                ns |> executeStep
            | Some(aConfig) -> 
                aConfig 
                |> innerInvokeFactory 
                |> Some

        | ResolveAllIfArrayIsRequested(ns) -> 
            match instanceType.IsArray with
            | false -> 
                ns |> executeStep
            | true ->  
                let elementType = instanceType.GetElementType()
                (innerGetAllInstances (elementType) |> Array.ofSeq)
                |> ArrayTransforms.ToArrayOfType elementType |> Some

        | TryDynamicResolution(ns) -> 
            let paramBuilder = (fun t -> innerGetInstance t None false)
            match instanceType |> typeResolver.Resolve paramBuilder with
            | None-> 
                ns |> executeStep
            | Some(instance) -> 
                instance |> Some

        | Terminate(bt) -> 
            bt |> terminateProcedure instanceType name
    
    and innerGetInstance (instanceType : Type) (name : string option) (acceptNone : bool) =
        match beginResolution instanceType name with
        | false -> 
            raise <| CyclicDependencyException (sprintf "Error: Encountered dependency cycle:%s" (printResolutionStack instanceType name))
        | _ ->  
            try
                try
                    match BuildPlanner.createBuildProcedure name.IsSome acceptNone with
                    | BuildProcedure(firstStep) -> 
                        match firstStep |> followProcedure instanceType name with
                        | Some(result) -> 
                            result
                        | None -> 
                            null
                with
                    | MissingRegistrationException msg as ex -> 
                        Trace.TraceError(sprintf "Error during instance resolution: %s. Resolution stack:%s" (ex.ToString()) (printResolutionStack instanceType name))
                        match acceptNone with
                        |true -> 
                            null
                        |false -> 
                            reraise()
                    | _ -> 
                        reraise()
            finally
                endResolution()

    and innerGetAllInstances (instanceType : Type) =
        ParameterGuard.checkForNull instanceType
        match repository.RetrieveAll instanceType with
        | None -> 
            Seq.empty<Object>
        | Some(configs) -> 
            configs |> Seq.map (fun config -> config |> innerInvokeFactory)

    interface IDisposable with
        member __.Dispose() = 
            repository.Dispose()

    interface ILambdaContainer with
        member __.GetInstanceOfType instanceType =
            ParameterGuard.checkForNull instanceType
            innerGetInstance instanceType None false

        member __.GetInstanceOfTypeOrNull instanceType =
            ParameterGuard.checkForNull instanceType
            innerGetInstance instanceType None true
        
        member t__his.GetInstanceOfTypeByName instanceType name =
            ParameterGuard.checkForNull instanceType
            ParameterGuard.checkForNull name
            innerGetInstance instanceType (Some(name)) false

        member __.GetInstanceOfTypeByNameOrNull instanceType name =
            ParameterGuard.checkForNull instanceType
            ParameterGuard.checkForNull name
            innerGetInstance instanceType (Some(name)) true
        
        member __.GetAllInstancesOfType instanceType =
            ParameterGuard.checkForNull instanceType
            innerGetAllInstances instanceType

        member __.IsTypeRegistered instanceType =
            ParameterGuard.checkForNull instanceType
            isRegisteredInRepository instanceType None
        
        member __.IsTypeRegisteredByName instanceType name =
            ParameterGuard.checkForNull instanceType
            ParameterGuard.checkForNull name
            isRegisteredInRepository instanceType (Some(name))
        
        member __.GetInstance<'a>() =
            innerGetInstance typeof<'a> None false :?> 'a

        member __.GetInstanceOrNull<'a when 'a : null>() =
            innerGetInstance typeof<'a> None true :?> 'a
        
        member __.GetInstanceByName<'a> (name : string) =
            ParameterGuard.checkForNull name
            innerGetInstance typeof<'a> (Some(name)) false :?> 'a

        member __.GetInstanceByNameOrNull<'a when 'a:null> (name : string) =
            ParameterGuard.checkForNull name
            innerGetInstance typeof<'a> (Some(name)) true :?> 'a
        
        member __.GetAllInstances<'a>() =
            innerGetAllInstances typeof<'a>
            |> Seq.where (isNull >> not)
            |> Seq.cast<'a>

        member __.IsRegistered<'a>() = 
            isRegisteredInRepository typeof<'a> None

        member __.IsRegisteredByName<'a> name = 
            ParameterGuard.checkForNull name
            isRegisteredInRepository typeof<'a> (Some(name))

    interface ILambdaContainerScoping with
        member __.CreateSubScope() =
            let scopedRepository = scoping.CreateSubScope (repository |> Option.Some) None

            new LambdaContainer(scopedRepository, typeResolver, scoping) :> ILambdaContainer

        member __.CreateSubScopeWith recordAdditionalRegistrations =
            
            let scopedRepository = scoping.CreateSubScope (repository |> Option.Some) (Some(recordAdditionalRegistrations.Invoke))

            new LambdaContainer(scopedRepository, typeResolver, scoping) :> ILambdaContainer

        member __.WithCustomizedResolution<'a when 'a :> ILambdaContainerRegistrations> (applyTransientsRegistrations : Action<'a>) =
            let scopedRepository = 
                scoping.CreateResolutionScope 
                    repository 
                        (fun r -> 
                            r.Record<'a>(
                                fun b -> 
                                    b
                                     .WithDisposalScope(DisposalScope.SubScope)
                                     .WithOutputLifetime(OutputLifetime.Transient)
                                     .Build()
                                     |> applyTransientsRegistrations.Invoke
                            ) |> ignore)

            new LambdaContainer(scopedRepository, typeResolver, scoping) :> IResolutionScope
