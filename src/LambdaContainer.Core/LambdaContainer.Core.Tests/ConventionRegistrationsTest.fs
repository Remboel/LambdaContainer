module LambdaContainer.Core.Tests.ConventionRegistrationsTest
open System
open NSubstitute
open LambdaContainer.Core.RepositoryConstruction
open LambdaContainer.Core.Contracts
open LambdaContainer.Core.Tests.TestUtilities
open Xunit

 module TestTypes =
        type IT1 = interface end
        type IT2 = inherit IT1
        type IT3 = inherit IT1
        type IT4 = interface end

        type T1() = 
            interface IT2
            interface IT3
            interface IDisposable with
                member __.Dispose() = ()

        type T2() = 
            interface IT1

        type T3() = 
            interface IT2
            interface IT4

        [<AbstractClass>]
        type TAbstract() =
            abstract member X : string -> unit
    
open TestTypes
open LambdaContainer.Core.ReflectionEx.PrimitiveReflection

module TypeLoaderTest =
        
    let private sut = Conventions.TypeLoader() :> ITypeLoader

    [<Fact>]
    let ``AssemblyTypes returns all types concrete types regardless of access level``() =
        let expected = System.Reflection.Assembly.GetExecutingAssembly().GetTypes() |> Seq.filter canBeInstantiated 
        let actual = sut.AssemblyTypes<ITestType>().Invoke()
        Assert.Equal<Type seq>(expected, actual)

    [<Fact>]
    let ``AssemblyTypesFrom returns all concrete types regardless of access level``() =
        let expected = (System.Reflection.Assembly.GetExecutingAssembly().GetTypes() |> Seq.filter canBeInstantiated)
        let actual = (sut.AssemblyTypesFrom typeof<ITestType>.Assembly).Invoke()
        Assert.Equal<Type seq>(expected, actual)

    [<Fact>]
    let ``TypesFrom returns all provided concrete types and removes interfaces and abstracts``() =
        let types = [typeof<IT1>; typeof<T1>; typeof<TAbstract>]

        Assert.Equal([typeof<T1>],(sut.Types types).Invoke())

    [<Fact>]
    let ``Type x returns [x]``() =
        Assert.Equal([typeof<T1>],(sut.Type<T1>()).Invoke())

    [<Fact>]
    let ``Type x returns [] if x is abstract``() =
        Assert.Equal([],(sut.Type<TAbstract>()).Invoke())

    [<Fact>]
    let ``Type x returns [] if x is an interface``() =
        Assert.Equal([],(sut.Type<ITestType>()).Invoke())

module TypeConditionTest =
    let sut = Conventions.TypeCondition() :> ITypeCondition

    [<Fact>]
    let ``TypeOf<'a> will match type 'a ``() =
        Assert.True(sut.TypeOf<ITestType>().Invoke(typeof<ITestType>))

    [<Fact>]
    let ``TypeOf<'a> will not match type 'b ``() =
        Assert.False(sut.TypeOf<ITestType>().Invoke(typeof<TestTypeImpl>))

    [<Fact>]
    let ``TypeOf 'a will match type 'a ``() =
        Assert.True((sut.TypeOf(typeof<ITestType>)).Invoke(typeof<ITestType>))

    [<Fact>]
    let ``TypeOf 'a will not match type 'b ``() =
        Assert.False((sut.TypeOf(typeof<ITestType>)).Invoke(typeof<TestTypeImpl>))

    [<Fact>]
    let ``ImplementationsOf<'a> will match type 'a ``() =
        Assert.True(sut.ImplementationsOf<ITestType>().Invoke(typeof<ITestType>))

    [<Fact>]
    let ``ImplementationsOf<'a> will match type 'b if 'b implements 'a ``() =
        Assert.True(sut.ImplementationsOf<ITestType>().Invoke(typeof<TestTypeImpl>))

    [<Fact>]
    let ``ImplementationsOf<'a> will not match type that is not implementation of 'a ``() =
        Assert.False(sut.ImplementationsOf<ITestType>().Invoke(typeof<ITestAction>))

    [<Fact>]
    let ``ImplementationsOf 'a will match type 'a ``() =
        Assert.True(sut.ImplementationsOf(typeof<ITestType>).Invoke(typeof<ITestType>))

    [<Fact>]
    let ``ImplementationsOf 'a will match type 'b if 'b implements 'a ``() =
        Assert.True(sut.ImplementationsOf(typeof<ITestType>).Invoke(typeof<TestTypeImpl>))

    [<Fact>]
    let ``ImplementationsOf 'a will not match type that is not implementation of 'a ``() =
        Assert.False(sut.ImplementationsOf(typeof<ITestType>).Invoke(typeof<ITestAction>))

    [<Fact>]
    let ``Match will echo provided predicate``() =
        let input = MatchType(fun (t : Type) -> t <> typeof<string>)
        Assert.Same(input, sut.Match(input))

module TypeAbstractionSelectorTest =
    let sut = Conventions.TypeAbstractionSelector() :> ITypeAbstractionSelector

    [<Fact>]
    let  ``ImplementationsOf<'a> creates function that echoes [] if input is 'a``() =
        Assert.Equal([],sut.ImplementationsOf<ITestType>().Invoke(typeof<ITestType>))

    [<Fact>]
    let  ``ImplementationsOf<'a> creates function that echoes ['a] if 'b implements 'a``() =
        Assert.Equal([typeof<ITestType>],sut.ImplementationsOf<ITestType>().Invoke(typeof<TestTypeImpl>))

    [<Fact>]
    let  ``ImplementationsOf<'a> creates function that echoes [] if 'b does not implement 'a``() =
        Assert.Equal([],sut.ImplementationsOf<ITestType>().Invoke(typeof<ITestAction>))

    [<Fact>]
    let  ``ImplementationsOf 'a creates function that echoes [] if input is 'a``() =
        Assert.Equal([],(sut.ImplementationsOf typeof<ITestType>).Invoke(typeof<ITestType>))

    [<Fact>]
    let  ``ImplementationsOf 'a creates function that echoes ['a] if 'b implements 'a``() =
        Assert.Equal([typeof<ITestType>],(sut.ImplementationsOf typeof<ITestType>).Invoke(typeof<TestTypeImpl>))

    [<Fact>]
    let  ``ImplementationsOf 'a creates function that echoes [] if 'b does not implement 'a``() =
        Assert.Equal([],(sut.ImplementationsOf typeof<ITestType>).Invoke(typeof<ITestAction>))

    [<Fact>]
    let  ``ImplementationsOfTypes 'a and 'b creates function that echoes types that are implemented but not equal to input``() =
        Assert.Equal([typeof<ITestType>],(sut.ImplementationsOfTypes [typeof<ITestType>; typeof<ITestAction>; typeof<TestTypeImpl>]).Invoke(typeof<TestTypeImpl>))

    [<Fact>]
    let ``ImplementedInterfaces creates a function that returns all interfaces except IDisposable implemented by the input``() =
        Assert.Equal([typeof<IT1>; typeof<IT2>; typeof<IT3>],sut.ImplementedInterfaces().Invoke(typeof<T1>)|> Seq.sortBy(fun x -> x.Name) )


    [<Fact>]
    let ``ImplementedInterfacesFiltered creates a function that returns all interfaces except IDisposable implemented by the input and accepted by the filter``() =
        Assert.Equal( [typeof<IT2>; typeof<IT3>], sut.ImplementedInterfacesFiltered(fun t -> t <> typeof<IT1>).Invoke(typeof<T1>) |> Seq.sortBy(fun x -> x.Name))

module ConventionSpecificationTest =
    let private sut = Conventions.ConventionSpecification(mock<ITypeLoader>(), mock<ITypeCondition>(), mock<ITypeAbstractionSelector>(),[]) :> IConventionSpecification

    let private build (builder : IConventionSpecification) =
        (builder :?> Conventions.ConventionSpecification).ToRegistrationConvention()

    [<Fact>]
    let ``Append applies type inclusion element with provided typeloader``() =
        match build <| sut.Append(fun tl -> tl.Type<ITestType>()) with
        | Include(_ ,Complete) ->
            ()
        | _ ->
            failwith "Expected different result"

    [<Fact>]
    let ``ScopeTo applies type scope reduction element with provided typeloader``() =
        match build <| sut.ScopeTo(fun tl -> tl.ImplementationsOf<ITestType>()) with
        | ScopeTo(_ ,Complete) ->
            ()
        | _ ->
            failwith "Expected different result"

    [<Fact>]
    let ``Except applies type filter element with provided typeloader``() =
        match build <| sut.Except(fun tl -> tl.ImplementationsOf<ITestType>()) with
        | Remove(_ ,Complete) ->
            ()
        | _ ->
            failwith "Expected different result"

    [<Fact>]
    let ``UsingNamingStrategy applies naming strategy to selected types``() =
        match build <| sut.UsingNamingStrategy((fun tl -> tl.ImplementationsOf<ITestType>()),(fun _ -> "x")) with
        | ApplyNamingStrategy(_ ,_,Complete) ->
            ()
        | _ ->
            failwith "Expected different result"

    [<Fact>]
    let ``UsingUniqueNamingStrategy applies naming strategy to selected types``() =
        match build <| sut.UsingUniqueNamingStrategy((fun tl -> tl.ImplementationsOf<ITestType>())) with
        | ApplyNamingStrategy(_ ,_,Complete) ->
            ()
        | _ ->
            failwith "Expected different result"

    [<Fact>]
    let ``Register creates registration action``() =
        match build <| sut.Register(fun tas -> tas.ImplementedInterfaces()) with
        | Register(_,Complete) ->
            ()
        | _ ->
            failwith "Expected different result"

    [<Fact>]
    let ``Order of created registration expression is correct``() =
        let specification = 
            build <|
                sut
                    .Append(fun tl -> tl.AssemblyTypes<ITestAction>())
                    .ScopeTo(fun tc -> tc.ImplementationsOf<ITestAction>())
                    .Except(fun c -> c.ImplementationsOf<string>())
                    .UsingNamingStrategy((fun tc -> tc.ImplementationsOf<ITestAction>()), (fun _ -> ""))
                    .Register(fun x -> x.ImplementedInterfaces())
                    .ClearNamingStrategy(fun x -> x.ImplementationsOf<ITestType>())
                    .Register(fun x -> x.ImplementedInterfaces())
        
        match specification with
        | Include
            (_,ScopeTo
                (_,Remove
                    (_,ApplyNamingStrategy
                        (_,_,Register
                            (_,ApplyNamingStrategy
                                (_,_,Register
                                    (_,Complete))))))) ->
            ()
        | _ ->
            failwith "Expected different result"

module RegistrationConventionApiTest =

    [<Fact>]
    let ``Register applies recorded registrations to inbound core api``() =
        let coreRegistrations = mock<ICoreRegistrations>()
        let sut = Conventions.createApi(coreRegistrations)
        sut.Register(
            fun convention -> 
                convention
                    .Append(fun t -> t.AssemblyTypes<IT1>())
                    .ScopeTo(fun t-> t.Match(fun x -> x.FullName.Contains("LambdaContainer.Core.Tests.ConventionRegistrationsTest")))
                    .Except(fun t -> t.TypeOf<T2>())
                    .UsingUniqueNamingStrategy(fun t -> t.TypeOf<IT1>())
                    .ClearNamingStrategy(fun t -> t.TypeOf<IT1>()) //Ensure clearing the naming strategy works
                    .UsingNamingStrategy((fun t -> t.TypeOf<IT2>()), (fun x -> sprintf "%s==>%s" x.Abstraction.Name x.Implementation.Name))
                    .UsingUniqueNamingStrategy(fun t -> t.TypeOf<IT4>())
                    .Register(fun r -> r.ImplementedInterfacesFiltered(fun x -> x <> typeof<IT1>))) 
            |> ignore

        Assert.Equal(4,coreRegistrations.ReceivedCalls() |> Seq.length)
        coreRegistrations.Received().RegisterMapping typeof<IT3> typeof<T1> |> ignore
        coreRegistrations.Received().RegisterMappingByName typeof<IT2> typeof<T1> (sprintf "%s==>%s" typeof<IT2>.Name typeof<T1>.Name) |> ignore
        coreRegistrations.Received().RegisterMappingByName typeof<IT2> typeof<T3> (sprintf "%s==>%s" typeof<IT2>.Name typeof<T3>.Name) |> ignore        
        coreRegistrations.Received().RegisterMappingByName typeof<IT4> typeof<T3> (sprintf "%s-->%s" typeof<IT4>.FullName typeof<T3>.FullName) |> ignore