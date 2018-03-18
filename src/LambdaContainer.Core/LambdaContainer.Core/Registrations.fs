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

[<AutoOpen>]
module Registrations =
    open System
    open LambdaContainer.Core.Contracts
    open LambdaContainer.Core.NetFrameworkEx

    type PerformRegistrationCommand = (ILambdaContainer -> Object) -> Type -> string option -> unit

    let toObject (input : 'a) =
        input :> Object

    let internal registerFactory (register : PerformRegistrationCommand) (factory : Func<ILambdaContainer, 'a>) outputType name =
        ParameterGuard.checkForNull outputType
        ParameterGuard.checkForNull factory

        register (factory.Invoke >> toObject) outputType name

    let internal assertTypeRegistration (output : Type) (implementation : Type) =
                ParameterGuard.checkForNull output
                ParameterGuard.checkForNull implementation
                if output.Equals(implementation) then 
                    raise <| InvalidRegistrationException("Error: Cannot register a type to itself. Consider registering a factory in stead.")

                //Since F# lacks the proper type contraint support ( a','b when 'b:>'a) we have to check it at runtime
                if not <| output.IsAssignableFrom(implementation) then 
                    raise <| InvalidRegistrationException("Error: First argument is not a generalization of second type argument")

    let internal registerTypeMapping (register : PerformRegistrationCommand) outputType implementation name =
        assertTypeRegistration outputType implementation
        register (fun lc -> lc.GetInstanceOfType implementation) outputType name

    type internal CoreRegistrations(doRegister : PerformRegistrationCommand) = 
    
        interface ICoreRegistrations with

            member __.RegisterMapping outputType implementation =
                registerTypeMapping doRegister outputType implementation None
                __ :> ICoreRegistrations
    
            member __.RegisterMappingByName outputType implementation  name =
                ParameterGuard.checkForNull name
            
                registerTypeMapping doRegister outputType implementation (Some name)
                __ :> ICoreRegistrations

            member __.RegisterFactory outputType factory =
                registerFactory doRegister factory outputType None
                __ :> ICoreRegistrations
        
            member __.RegisterFactoryByName outputType factory name =
                ParameterGuard.checkForNull name
            
                registerFactory doRegister factory outputType (Some name)
                __ :> ICoreRegistrations

    type internal FactoryRegistrations(doRegister : PerformRegistrationCommand) = 
    
        interface IFactoryRegistrations with
        
            member __.Register<'a> (factory : Func<ILambdaContainer,'a>) =
                registerFactory doRegister factory typeof<'a> None
                __ :> IFactoryRegistrations
        
            member __.RegisterByName<'a>  ((factory : Func<ILambdaContainer,'a>),  name) =
                ParameterGuard.checkForNull name
            
                registerFactory doRegister factory typeof<'a> (Some name)
                __ :> IFactoryRegistrations

    type internal TypeMappingRegistrations(doRegister : PerformRegistrationCommand) = 
    
        interface ITypeMappingRegistrations with
    
            member __.Register<'a, 'b>() =
                assertTypeRegistration typeof<'a> typeof<'b>
            
                registerTypeMapping doRegister typeof<'a> typeof<'b> None
                __ :> ITypeMappingRegistrations
    
            member __.RegisterByName<'a,'b> name =
                ParameterGuard.checkForNull name
                assertTypeRegistration typeof<'a> typeof<'b>
            
                registerTypeMapping doRegister typeof<'a> typeof<'b> (Some name)
                __ :> ITypeMappingRegistrations

    module internal Conventions =
        open System.Reflection
        open ParameterGuard

        module private Tools =
            
            let isTypeOf (abstraction : Type) =
                Predicate<Type>(fun t2 -> abstraction.IsAssignableFrom(t2))

            let matchExact (t1 : Type) =
                Predicate<Type>(fun t2 -> t1.Equals(t2))
       
        open Tools

        type ConventionFragmentFactory = 
            (RegistrationConvention -> RegistrationConvention)

        
        open LambdaContainer.Core.ReflectionEx.PrimitiveReflection

        type TypeLoader() =
            
            let withTypes t =
                t |> Seq.filter canBeInstantiated

            interface ITypeLoader with
                member __.AssemblyTypes<'a>() = 
                    Func<Type seq>(
                        fun () -> 
                            withTypes <| typeof<'a>.Assembly.GetTypes())
                member __.AssemblyTypesFrom sourceAssembly = 
                    checkForNull sourceAssembly
                    Func<Type seq>(
                        fun () -> 
                            withTypes <| sourceAssembly.GetTypes())
                member __.Types types = 
                    checkForNull types
                    Func<Type seq>(
                        fun () -> 
                            withTypes types)
                member __.Type<'a>() =
                    Func<Type seq>(
                        fun () -> 
                            withTypes <| seq { yield  typeof<'a> })

        type TypeCondition() =
            interface ITypeCondition with
                member __.TypeOf<'a>() = 
                    matchExact typeof<'a>
                member __.TypeOf t = 
                    checkForNull t
                    matchExact t
                member __.ImplementationsOf<'a>() = 
                    isTypeOf typeof<'a>
                member __.ImplementationsOf tAbstraction = 
                    checkForNull tAbstraction
                    isTypeOf tAbstraction
                member __.Match spec = 
                    checkForNull spec
                    spec
        
        type TypeAbstractionSelector() =
            
            let asInstanceOfType tAbstraction =
                Func<Type, Type seq>(
                    fun t -> 
                        match matchExact(t).Invoke(tAbstraction) , isTypeOf(tAbstraction).Invoke(t) with
                        | false, true ->
                            //Do not register a -> a
                            seq { yield tAbstraction }
                        | _ ->
                            Seq.empty)

            let fixedAbstractionFilter t =
                not <| typeof<IDisposable>.Equals(t)

            let asImplementedInterfaces (t : Type) filter =
                t.GetTypeInfo().ImplementedInterfaces
                |> Seq.filter fixedAbstractionFilter
                |> Seq.filter filter

            interface ITypeAbstractionSelector with
                member __.ImplementationsOf<'a>() = 
                   asInstanceOfType typeof<'a>
                member __.ImplementationsOf tAbstraction = 
                    checkForNull tAbstraction
                    asInstanceOfType tAbstraction
                member __.ImplementationsOfTypes types = 
                    checkForNull types
                    let abstractionSelectors =
                        types
                        |> Seq.map asInstanceOfType
                        |> List.ofSeq

                    Func<Type, Type seq>(
                        fun t ->
                            abstractionSelectors
                            |> List.map (fun selector -> selector.Invoke(t) |> List.ofSeq)
                            |> List.collect id
                            |> List.distinct
                            |> Seq.ofList)
                    
                member __.ImplementedInterfaces() = 
                    Func<Type, Type seq> (fun t -> asImplementedInterfaces t (fun _ -> true))

                member __.ImplementedInterfacesFiltered filter =
                    checkForNull filter
                    Func<Type, Type seq> (fun t -> asImplementedInterfaces t filter.Invoke)

        type ConventionSpecification
            (
                typeLoader : ITypeLoader,
                typeCondition : ITypeCondition,
                typeAbstractionSelector : ITypeAbstractionSelector,
                accumulatedConventionSpec : ConventionFragmentFactory list
            ) =
            
            let extendWith spec =
                ConventionSpecification
                    (
                        typeLoader,
                        typeCondition,
                        typeAbstractionSelector,
                        spec :: accumulatedConventionSpec
                    ) :> IConventionSpecification

            let extendWithNaming matchAbstraction getName =
                extendWith 
                    (fun next -> 
                        let naming = matchAbstraction typeCondition
                        RegistrationConvention.ApplyNamingStrategy(
                            naming, 
                            getName >> (fun x -> match String.IsNullOrWhiteSpace x with | true -> None | _ -> Some(x)), 
                            next))
            
            member __.ToRegistrationConvention() =
               let rec buildConvention remainder current =
                   match remainder with
                   | [] ->
                       current
                   | head::tail ->
                       current
                       |> head
                       |> buildConvention tail

               buildConvention 
                   accumulatedConventionSpec 
                        RegistrationConvention.Complete
            
            interface IConventionSpecification with
                member __.Append selectTypes =
                    checkForNull selectTypes
                    extendWith 
                        (fun next -> 
                            RegistrationConvention.Include(selectTypes.Invoke typeLoader , next))

                member __.ScopeTo matchTypes =
                    checkForNull matchTypes
                    extendWith 
                        (fun next -> 
                            RegistrationConvention.ScopeTo(matchTypes.Invoke typeCondition , next))

                member __.Except matchTypes =
                    checkForNull matchTypes
                    extendWith 
                        (fun next -> 
                            RegistrationConvention.Remove(matchTypes.Invoke typeCondition , next))

                member __.UsingNamingStrategy (matchAbstraction,getName) =
                    checkForNull matchAbstraction
                    checkForNull getName
                    extendWithNaming
                        matchAbstraction.Invoke
                        getName.Invoke

                member __.UsingUniqueNamingStrategy pickAbstractions =
                    checkForNull pickAbstractions
                    extendWithNaming
                        pickAbstractions.Invoke
                        (fun context -> 
                            sprintf "%s-->%s" context.Abstraction.FullName context.Implementation.FullName)

                member __.Register provideAbstraction =
                    checkForNull provideAbstraction
                    extendWith 
                        (fun next -> 
                            RegistrationConvention.Register(provideAbstraction.Invoke typeAbstractionSelector, next))

                member __.ClearNamingStrategy matchAbstraction =
                    checkForNull matchAbstraction
                    extendWithNaming
                        matchAbstraction.Invoke
                        (fun _ -> null)

        type internal RegistrationConventionApi
            (
                coreApi : ICoreRegistrations, 
                typeLoader : ITypeLoader, 
                typeCondition : ITypeCondition, 
                typeAbstractionSelector : ITypeAbstractionSelector
            ) =
            interface IRegistrationConvention with
                
                member __.Register buildWith =
                    checkForNull buildWith

                    let rec apply spec naming typeSet =
                        match spec with
                        | Include(loadTypes, next) ->
                            apply 
                                next 
                                naming 
                                (typeSet@(loadTypes.Invoke() |> List.ofSeq))
                        | ScopeTo(matchType, next) ->
                            apply 
                                next 
                                naming 
                                (typeSet |> List.filter matchType.Invoke)
                        | Remove(matchType, next) ->
                            apply 
                                next 
                                naming 
                                (typeSet |> List.filter (not << matchType.Invoke))
                        | ApplyNamingStrategy(matchType, namingStrategy, next) ->
                            apply 
                                next 
                                ((matchType, namingStrategy)::naming) 
                                typeSet
                        | Register(provideAbstractionsOf, next) ->
                            
                            let withProvidedAbstractions t =
                                (t, provideAbstractionsOf.Invoke(t))

                            let byTypesWithAbstractions mapping =
                                snd mapping |> (not << Seq.isEmpty)

                            let withNamingStrategy(t,abstractions) =
                                let getNamingStrategy tAbstraction =
                                        naming 
                                        |> List.filter (fun (condition, _) -> condition.Invoke(tAbstraction))
                                        |> List.map snd
                                        |> List.tryHead
                                    
                                let namedAbstractions =
                                    abstractions
                                    |> Seq.map
                                        (fun tAbstraction -> 
                                            (tAbstraction, getNamingStrategy tAbstraction))
                                    |> List.ofSeq

                                (t, namedAbstractions)
                            
                            let performRegistration (t, abstractions) =
                                abstractions
                                |> List.iter 
                                    (fun (abstraction, namingStrategy) ->
                                        match namingStrategy |> Option.map (fun x -> x {Abstraction = abstraction; Implementation = t}) with 
                                        | None | Some(None) ->
                                            coreApi.RegisterMapping abstraction t |> ignore
                                        | Some(Some(name)) ->
                                            coreApi.RegisterMappingByName abstraction t name |> ignore)

                            typeSet
                            |> List.map withProvidedAbstractions
                            |> List.filter byTypesWithAbstractions
                            |> List.map withNamingStrategy
                            |> List.iter performRegistration

                            apply 
                                next 
                                naming 
                                typeSet

                        | Complete ->
                            ()

                    let specification = 
                        ConventionSpecification(typeLoader,typeCondition,typeAbstractionSelector,[])
                        |> buildWith.Invoke
                        :?> ConventionSpecification

                    let convention = specification.ToRegistrationConvention()

                    apply convention [] []

                    __ :> IRegistrationConvention


        let private typeLoader = TypeLoader()
        let private typeCondition = TypeCondition()
        let private typeAbstractionSelector = TypeAbstractionSelector()

        let createApi coreRegistrations =
            RegistrationConventionApi
                (
                    coreRegistrations, 
                    typeLoader, 
                    typeCondition, 
                    typeAbstractionSelector
                )
            :> IRegistrationConvention