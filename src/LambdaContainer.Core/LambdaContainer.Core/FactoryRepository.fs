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
open System.Collections.Concurrent
open System.Collections.Generic
open System.Collections.ObjectModel
open LambdaContainer.Core.NetFrameworkEx
open LambdaContainer.Core.FactoryContracts
open LambdaContainer.Core.InstanceFactories
open LambdaContainer.Core.Contracts

(*
    Factory configuration Repository
*)

type internal IFactoryConfigurationRepository =
    inherit IDisposalScope<IFactoryConfigurationRepository>
    abstract member Retrieve : outputType : Type -> name : string option -> IInstanceFactory option
    abstract member RetrieveAll : outputType : Type -> IInstanceFactory seq option
    abstract member GetRegistrations : unit -> IReadOnlyDictionary<Type,IReadOnlyDictionary<FactoryIdentity,IInstanceFactory>>

type internal FactoryConfigurationRepository(referenceMap : IReadOnlyDictionary<Type,IReadOnlyDictionary<FactoryIdentity,IInstanceFactory>>) =
    
    interface IFactoryConfigurationRepository with

        member __.Retrieve outputType registrationName =
            ParameterGuard.checkForNull outputType
            match referenceMap.TryGetValue(outputType) with
            | (false, _) -> 
                None
            | (true, factories) ->
                let identity = new FactoryIdentity(registrationName, outputType, None)
                let hasFactory, factory = factories.TryGetValue(identity)
                match hasFactory with 
                | false -> 
                    None 
                | true -> 
                    Some(factory)

        member __.RetrieveAll outputType =
            ParameterGuard.checkForNull outputType

            match referenceMap.TryGetValue(outputType) with
            | (false, _) -> 
                None
            | (true, registrationsForOutputType) -> 
                match registrationsForOutputType.Count with
                | invalid when invalid = 1 && (registrationsForOutputType.Keys |> Seq.exists (fun x -> x.IsAnonymous())) -> 
                    raise <| InvalidOperationException("Error: RetrieveAll is only compatible with type factories registered by name")
                | _ -> 
                    registrationsForOutputType.Values |> Seq.where (fun _ -> true) |> Some

        member __.GetRegistrations() = 
            referenceMap        

    interface IDisposalScope<IFactoryConfigurationRepository> with
            
            member __.CreateSubScope() =
                let cloneIdentityMap (originalMap : KeyValuePair<Type,IReadOnlyDictionary<FactoryIdentity,IInstanceFactory>>) =
                    let mapped = new Dictionary<FactoryIdentity,IInstanceFactory>()
                
                    originalMap.Value
                    |> Seq.iter (fun entry -> mapped.Add(entry.Key,entry.Value.CreateSubScope()))

                    new KeyValuePair<Type,IReadOnlyDictionary<FactoryIdentity,IInstanceFactory>>(originalMap.Key, mapped |> DictionaryEx.asReadOnly)

                let newMap = new Dictionary<Type,IReadOnlyDictionary<FactoryIdentity,IInstanceFactory>>()
                referenceMap
                |> Seq.map cloneIdentityMap
                |> Seq.iter (fun mapCopy -> newMap.Add(mapCopy.Key,mapCopy.Value))
            
                new FactoryConfigurationRepository(newMap |> DictionaryEx.asReadOnly) :> IFactoryConfigurationRepository

            member __.Dispose() =
                referenceMap
                |> Seq.collect (fun map -> map.Value.Values)
                |> Seq.iter Disposer.disposeIfPossible
                |> ignore

type internal RepositoryCompositionKind =
    | Owns of IFactoryConfigurationRepository
    | References of IFactoryConfigurationRepository


module internal RepositorySequences =
    let rec matchFirstResult<'a> (current : IFactoryConfigurationRepository option)  (remainder : IFactoryConfigurationRepository seq) (f : IFactoryConfigurationRepository -> 'a option ) =
        match current with
        | None ->
            None
        | Some(r) ->
            match r |> f with
            | None ->
                matchFirstResult<'a> (remainder |> Seq.tryHead) (remainder |> Seq.skip 1) f
            | Some(result) ->
                result |> Option.Some

    let rec matchFirstResultFrom<'a> (repositories : IFactoryConfigurationRepository seq) (f : IFactoryConfigurationRepository -> 'a option ) =
        f |> matchFirstResult (repositories |> Seq.tryHead) (repositories |> Seq.skip 1)

type internal CompositeConfigurationRepository(repositories : RepositoryCompositionKind array) =
    
    let unwrappedRepositories = 
        repositories
        |> Array.map (
            fun x -> 
                match x with
                | Owns(r) -> 
                    r
                | References(r) ->
                    r)

    interface IFactoryConfigurationRepository with
        member __.Retrieve outputType name =
            ParameterGuard.checkForNull outputType
            RepositorySequences.matchFirstResultFrom unwrappedRepositories (fun x -> x.Retrieve outputType name)

        member __.RetrieveAll outputType =
            ParameterGuard.checkForNull outputType
            let set = HashSet<IInstanceFactory>()
            unwrappedRepositories
            |> Seq.map (
                fun x ->
                    match x.RetrieveAll outputType with
                    | None ->
                        Seq.empty
                    | Some(factories) ->
                        factories)
            |> Seq.iter 
                (Seq.iter (set.Add >> ignore))

            set :> (IInstanceFactory seq) |> Option.Some
        
        member __.GetRegistrations() = 
            raise <| NotSupportedException("Implement if relevant")
            
    interface IDisposalScope<IFactoryConfigurationRepository> with
            
            member __.CreateSubScope() =
                raise <| NotSupportedException("Implement if relevant")

            member __.Dispose() =
                repositories
                |> Seq.iter (
                    fun x -> 
                        match x with
                        | Owns(r) -> 
                            r.Dispose()
                        | _ ->
                            ())