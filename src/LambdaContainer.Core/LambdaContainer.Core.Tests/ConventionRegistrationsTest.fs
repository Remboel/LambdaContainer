module LambdaContainer.Core.Tests.ConventionRegistrationsTest
open System
open NSubstitute
open NUnit.Framework
open LambdaContainer.Core.RepositoryConstruction
open LambdaContainer.Core.Contracts
open LambdaContainer.Core.Tests.TestUtilities
open FsUnit

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

    [<Test>]
    let ``AssemblyTypes returns all types concrete types regardless of access level``() =
        sut.AssemblyTypes<ITestType>().Invoke()
        |> should equal 
            (System.Reflection.Assembly.GetExecutingAssembly().GetTypes() |> Seq.filter canBeInstantiated)

    [<Test>]
    let ``AssemblyTypesFrom returns all concrete types regardless of access level``() =
        (sut.AssemblyTypesFrom typeof<ITestType>.Assembly).Invoke()
        |> should equal 
            (System.Reflection.Assembly.GetExecutingAssembly().GetTypes() |> Seq.filter canBeInstantiated)

    [<Test>]
    let ``TypesFrom returns all provided concrete types and removes interfaces and abstracts``() =
        let types = [typeof<IT1>; typeof<T1>; typeof<TAbstract>]

        (sut.Types types).Invoke()
        |> should equal [typeof<T1>]

    [<Test>]
    let ``Type x returns [x]``() =
        (sut.Type<T1>()).Invoke()
        |> should equal [typeof<T1>]

    [<Test>]
    let ``Type x returns [] if x is abstract``() =
        (sut.Type<TAbstract>()).Invoke()
        |> should equal []

    [<Test>]
    let ``Type x returns [] if x is an interface``() =
        (sut.Type<ITestType>()).Invoke()
        |> should equal []

module TypeConditionTest =
    let sut = Conventions.TypeCondition() :> ITypeCondition

    [<Test>]
    let ``TypeOf<'a> will match type 'a ``() =
        sut.TypeOf<ITestType>().Invoke(typeof<ITestType>) |> should equal true

    [<Test>]
    let ``TypeOf<'a> will not match type 'b ``() =
        sut.TypeOf<ITestType>().Invoke(typeof<TestTypeImpl>) |> should equal false

    [<Test>]
    let ``TypeOf 'a will match type 'a ``() =
        (sut.TypeOf(typeof<ITestType>)).Invoke(typeof<ITestType>) |> should equal true

    [<Test>]
    let ``TypeOf 'a will not match type 'b ``() =
        (sut.TypeOf(typeof<ITestType>)).Invoke(typeof<TestTypeImpl>) |> should equal false

    [<Test>]
    let ``ImplementationsOf<'a> will match type 'a ``() =
        sut.ImplementationsOf<ITestType>().Invoke(typeof<ITestType>) |> should equal true

    [<Test>]
    let ``ImplementationsOf<'a> will match type 'b if 'b implements 'a ``() =
        sut.ImplementationsOf<ITestType>().Invoke(typeof<TestTypeImpl>) |> should equal true

    [<Test>]
    let ``ImplementationsOf<'a> will not match type that is not implementation of 'a ``() =
        sut.ImplementationsOf<ITestType>().Invoke(typeof<ITestAction>) |> should equal false

    [<Test>]
    let ``ImplementationsOf 'a will match type 'a ``() =
        sut.ImplementationsOf(typeof<ITestType>).Invoke(typeof<ITestType>) |> should equal true

    [<Test>]
    let ``ImplementationsOf 'a will match type 'b if 'b implements 'a ``() =
        sut.ImplementationsOf(typeof<ITestType>).Invoke(typeof<TestTypeImpl>) |> should equal true

    [<Test>]
    let ``ImplementationsOf 'a will not match type that is not implementation of 'a ``() =
        sut.ImplementationsOf(typeof<ITestType>).Invoke(typeof<ITestAction>) |> should equal false

    [<Test>]
    let ``Match will echo provided predicate``() =
        let input = MatchType(fun (t : Type) -> t <> typeof<string>)
        sut.Match(input) |> should be (sameAs input)

module TypeAbstractionSelectorTest =
    let sut = Conventions.TypeAbstractionSelector() :> ITypeAbstractionSelector

    [<Test>]
    let  ``ImplementationsOf<'a> creates function that echoes [] if input is 'a``() =
        sut.ImplementationsOf<ITestType>().Invoke(typeof<ITestType>) |> should equal []

    [<Test>]
    let  ``ImplementationsOf<'a> creates function that echoes ['a] if 'b implements 'a``() =
        sut.ImplementationsOf<ITestType>().Invoke(typeof<TestTypeImpl>) |> should equal [typeof<ITestType>]

    [<Test>]
    let  ``ImplementationsOf<'a> creates function that echoes [] if 'b does not implement 'a``() =
        sut.ImplementationsOf<ITestType>().Invoke(typeof<ITestAction>) |> should equal []

    [<Test>]
    let  ``ImplementationsOf 'a creates function that echoes [] if input is 'a``() =
        (sut.ImplementationsOf typeof<ITestType>).Invoke(typeof<ITestType>) |> should equal []

    [<Test>]
    let  ``ImplementationsOf 'a creates function that echoes ['a] if 'b implements 'a``() =
        (sut.ImplementationsOf typeof<ITestType>).Invoke(typeof<TestTypeImpl>) |> should equal [typeof<ITestType>]

    [<Test>]
    let  ``ImplementationsOf 'a creates function that echoes [] if 'b does not implement 'a``() =
        (sut.ImplementationsOf typeof<ITestType>).Invoke(typeof<ITestAction>) |> should equal []

    [<Test>]
    let  ``ImplementationsOfTypes 'a and 'b creates function that echoes types that are implemented but not equal to input``() =
        (sut.ImplementationsOfTypes [typeof<ITestType>; typeof<ITestAction>; typeof<TestTypeImpl>]).Invoke(typeof<TestTypeImpl>) |> should equal [typeof<ITestType>]

    [<Test>]
    let ``ImplementedInterfaces creates a function that returns all interfaces except IDisposable implemented by the input``() =
        sut.ImplementedInterfaces().Invoke(typeof<T1>) 
        |> Seq.sortBy(fun x -> x.Name) 
        |> should equal [typeof<IT1>; typeof<IT2>; typeof<IT3>]


    [<Test>]
    let ``ImplementedInterfacesFiltered creates a function that returns all interfaces except IDisposable implemented by the input and accepted by the filter``() =
        sut.ImplementedInterfacesFiltered(fun t -> t <> typeof<IT1>).Invoke(typeof<T1>) 
        |> Seq.sortBy(fun x -> x.Name) 
        |> should equal [typeof<IT2>; typeof<IT3>]

module ConventionSpecificationTest =
    let private sut = Conventions.ConventionSpecification(mock<ITypeLoader>(), mock<ITypeCondition>(), mock<ITypeAbstractionSelector>(),[]) :> IConventionSpecification

    let private build (builder : IConventionSpecification) =
        (builder :?> Conventions.ConventionSpecification).ToRegistrationConvention()

    [<Test>]
    let ``Append applies type inclusion element with provided typeloader``() =
        match build <| sut.Append(fun tl -> tl.Type<ITestType>()) with
        | Include(_ ,Complete) ->
            Assert.Pass "Ok"
        | _ ->
            Assert.Fail "Expected different result"

    [<Test>]
    let ``ScopeTo applies type scope reduction element with provided typeloader``() =
        match build <| sut.ScopeTo(fun tl -> tl.ImplementationsOf<ITestType>()) with
        | ScopeTo(_ ,Complete) ->
            Assert.Pass "Ok"
        | _ ->
            Assert.Fail "Expected different result"

    [<Test>]
    let ``Except applies type filter element with provided typeloader``() =
        match build <| sut.Except(fun tl -> tl.ImplementationsOf<ITestType>()) with
        | Remove(_ ,Complete) ->
            Assert.Pass "Ok"
        | _ ->
            Assert.Fail "Expected different result"

    [<Test>]
    let ``UsingNamingStrategy applies naming strategy to selected types``() =
        match build <| sut.UsingNamingStrategy((fun tl -> tl.ImplementationsOf<ITestType>()),(fun _ -> "x")) with
        | ApplyNamingStrategy(_ ,_,Complete) ->
            Assert.Pass "Ok"
        | _ ->
            Assert.Fail "Expected different result"

    [<Test>]
    let ``UsingUniqueNamingStrategy applies naming strategy to selected types``() =
        match build <| sut.UsingUniqueNamingStrategy((fun tl -> tl.ImplementationsOf<ITestType>())) with
        | ApplyNamingStrategy(_ ,_,Complete) ->
            Assert.Pass "Ok"
        | _ ->
            Assert.Fail "Expected different result"

    [<Test>]
    let ``Register creates registration action``() =
        match build <| sut.Register(fun tas -> tas.ImplementedInterfaces()) with
        | Register(_,Complete) ->
            Assert.Pass "Ok"
        | _ ->
            Assert.Fail "Expected different result"

    [<Test>]
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
            Assert.Pass "Ok"
        | _ ->
            Assert.Fail "Expected different result"

module RegistrationConventionApiTest =

    [<Test>]
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


        coreRegistrations.ReceivedCalls() |> Seq.length |> should equal 4
        coreRegistrations.Received().RegisterMapping typeof<IT3> typeof<T1> |> ignore
        coreRegistrations.Received().RegisterMappingByName typeof<IT2> typeof<T1> (sprintf "%s==>%s" typeof<IT2>.Name typeof<T1>.Name) |> ignore
        coreRegistrations.Received().RegisterMappingByName typeof<IT2> typeof<T3> (sprintf "%s==>%s" typeof<IT2>.Name typeof<T3>.Name) |> ignore        
        coreRegistrations.Received().RegisterMappingByName typeof<IT4> typeof<T3> (sprintf "%s-->%s" typeof<IT4>.FullName typeof<T3>.FullName) |> ignore