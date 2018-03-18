﻿module LambdaContainer.Core.Tests.LambdaContainerTest
open Fasterflect
open System
open NSubstitute
open NUnit.Framework
open LambdaContainer.Core.FactoryContracts
open LambdaContainer.Core.RepositoryConstruction
open LambdaContainer.Core.Contracts
open LambdaContainer.Core.Container
open LambdaContainer.Core.TypeResolvers
open LambdaContainer.Core.Tests.TestUtilities
open FsUnit

type public TestType(injectedParam : string) =
    member __.GetInjectedParam() = injectedParam

type public TestType2(injectedParam : string) =
    member __.GetInjectedParam() = injectedParam
    public new() = TestType2("FromDefaultCtor")


type public TestType3 (calledByInjectionCtor : bool) =
    member __.GetInjectedParam() = calledByInjectionCtor
    
    [<LambdaContainerInjectionConstructor>]
    public new() = TestType3(true)


type public TestType4 (afriend :string) =
    member __.GetInjectedParam() = afriend
    
    public new(afriend :string, _ : int) = 
        TestType4(afriend)
        then
            failwith "I should not have been chosen as injection ctor"


type public CyclicType(injectedParam : CyclicType) =
    member __.GetInjectedParam() = injectedParam

let private echoRepoScopingConfig =
    {
        CreateSubScope = (fun r _ -> r |> Option.get)
        CreateResolutionScope = (fun r _ -> r)
    }

let private echoSpecificRepo repo =
    {
        CreateSubScope = (fun _ _ -> repo)
        CreateResolutionScope = (fun r _ -> r)
    }

let mutable private repository : IFactoryConfigurationRepository = Unchecked.defaultof<IFactoryConfigurationRepository>
let mutable private typeResolver : ITypeResolver = Unchecked.defaultof<ITypeResolver>

let private createSutWithScoping scoping =
    new LambdaContainer(repository, typeResolver, scoping) :> ILambdaContainer

let private createSut() =
    createSutWithScoping echoRepoScopingConfig

let private expectAsRegistrationResult<'a> sut (name : string option) (result : 'a option) =
    
    let factory =
        match result with
        | Some(x) ->
            let factory = mock<IInstanceFactory>()
            (factory.Invoke sut).Returns(x) |> ignore
            Some factory
        | None ->
            None
    
    (repository.Retrieve typeof<'a> name).Returns(factory) |> ignore

let private expectAsManyRegistrationResult<'a> sut (results : 'a seq option) =
    
    let factories =
        match results with
        | Some(x) ->
            x
            |> Seq.map(
                fun result ->
                    let factory = mock<IInstanceFactory>()
                    (factory.Invoke sut).Returns(result) |> ignore        
                    factory
                    )
            |> Option.Some
        | None ->
            None
    
    (repository.RetrieveAll typeof<'a>).Returns(factories) |> ignore

let private expectFromTypeResolver<'a> (result : 'a option) =
    let result = 
        match result with 
        | Some(x) -> 
            x :> obj |> Option.Some 
        | None -> 
            None
    
    (typeResolver.Resolve (Arg.Any<System.Type -> Object>()) typeof<'a>).Returns(result) |> ignore

[<SetUp>]
let Setup() =
    repository <- mock<IFactoryConfigurationRepository>()
    typeResolver <- mock<ITypeResolver>()

[<Test>]
let ``Can Construct``() =
    Assert.DoesNotThrow(fun () -> createSut() |> ignore)

[<Test>]
let ``Get Instance With Missing Registration Throws``() =
    //Arrange
    let sut = createSut()
    None |> expectAsRegistrationResult<string> sut None

    //Act - Assert
    Assert.Throws<MissingRegistrationException>(fun () ->  sut.GetInstanceOfType typeof<string> |> ignore) |> ignore

[<Test>]
let ``GetInstanceOrNull With Missing Registration Returns Null``() =
    //Arrange
    let sut = createSut()
    None |> expectAsRegistrationResult<string> sut None

    //Act + Assert
    sut.GetInstanceOfTypeOrNull typeof<string> |> should be Null

[<Test>]
let ``Exceptions During Factory Invocation Are Wrapped``() =
    //Arrange
    let sut = createSut()
    let theType = typeof<string>
    let instanceFactory = mock<IInstanceFactory>()
    (repository.Retrieve theType None).Returns(Some(instanceFactory)) |> ignore
    instanceFactory
        .When(fun c -> c.Invoke sut |> ignore)
        .Do(fun x -> raise <| ArgumentException("error"))

    //Act - Assert
    Assert.Throws<FactoryInvocationException>(fun () ->  sut.GetInstanceOfType theType |> ignore) |> ignore

[<Test>]
let ``Can Get Instance Of Type``() =
    //Arrange
    let sut = createSut()
    let expecedResult = "Hello lambda container"
    Some expecedResult |> expectAsRegistrationResult<string> sut None

    //Act + Assert
    sut.GetInstanceOfType typeof<string> |> should equal expecedResult

[<Test>]
let ``Can Get Instance Of Type By Name``() =
    //Arrange
    let sut = createSut()
    let expecedResult = "Hello lambda container"
    Some expecedResult |> expectAsRegistrationResult<string> sut (Some("a name"))

    //Act + Assert
    sut.GetInstanceOfTypeByName typeof<string> "a name" |> should equal expecedResult

[<Test>]
let ``Can Get Instance Generic``() =
    //Arrange
    let sut = createSut()
    let expecedResult = "Hello lambda container"
    Some expecedResult |> expectAsRegistrationResult<string> sut None

    //Act + Assert
    sut.GetInstance<string>() |> should equal expecedResult

[<Test>]
let ``Can Get Instance Or Null Generic``() =
    //Arrange
    let sut = createSut()
    let expecedResult = "Hello lambda container"
    Some expecedResult |> expectAsRegistrationResult<string> sut None

    //Act + Assert
    sut.GetInstanceOrNull<string>() |> should equal expecedResult

[<Test>]
let ``Can Get Instance Or Null Generic With Missing Registration Returns Null``() =
    //Arrange
    let sut = createSut()
    None |> expectAsRegistrationResult<string> sut None

    //Act + Assert
    sut.GetInstanceOrNull<string>() |> should be Null

[<Test>]
let ``Can Get Instance Generic By Name``() =
    //Arrange
    let sut = createSut()
    let expecedResult = "Hello lambda container"
    Some expecedResult |> expectAsRegistrationResult<string> sut (Some("a name"))

    //Act + Assert
    sut.GetInstanceByName<string> "a name" |> should equal expecedResult

[<Test>]
let ``Can Get Instances Generic``() =
    //Arrange
    let sut = createSut()
    let expectedResults = ["Hello lambda container1" ; "Hello lambda container2"]
    expectedResults |> Seq.ofList |> Option.Some |> expectAsManyRegistrationResult<string> sut

    //Act + Assert
    sut.GetAllInstances<string>() |> should equal expectedResults

[<Test>]
let ``Can Get Instances Of Type``() =
    //Arrange
    let sut = createSut()
    let expectedResults = ["Hello lambda container1" ; "Hello lambda container2"]
    expectedResults |> Seq.ofList |> Option.Some |> expectAsManyRegistrationResult<string> sut

    //Act + Assert
    sut.GetAllInstancesOfType typeof<string> |> should equal expectedResults

[<Test>]
let ``Can Get Instance Of Unregistered Type Through Type Resolver``() =
    //Arrange
    let sut = createSut()
    let expecedResult = "Hello lambda container1"
    Some(expecedResult) |> expectFromTypeResolver<string>
    
    //Act + Assert
    sut.GetInstance<string>() |> should equal expecedResult

[<Test>]
let ``Fails If Unregistered And Resolved By Name``() =
    //Arrange
    let sut = createSut()
    
    //Act - Assert
    Assert.Throws<MissingRegistrationException>(fun () -> sut.GetInstanceByName<string>("the name") |> ignore) |> ignore

[<Test>]
let ``Fails If Unregistered And Type Resolver Returns None``() =
    //Arrange
    let sut = createSut()
    None |> expectFromTypeResolver<string>
    
    //Act - Assert
    Assert.Throws<MissingRegistrationException>(fun () -> sut.GetInstance<string>() |> ignore) |> ignore

[<Test>]
let ``Can Inject Use Registered Factory Method For Specific Type``() =
    //Arrange
    let sut = createSut()
    let theResult = new TestType3(false)
    Some theResult |> expectAsRegistrationResult<TestType3> sut None

    //Act + Assert
    sut.GetInstance<TestType3>() |> should be (sameAs theResult)

[<Test>]
let ``Can Detect Cyclic Dependency``() =
    //Arrange
    let sut = createSut()
    let instanceFactory = mock<IInstanceFactory>()
    let theType = typeof<TestType3>
    (repository.Retrieve theType None).Returns(Some(instanceFactory)) |> ignore
    instanceFactory
                .When(
                    fun x->x.Invoke sut |> ignore)
                .Do(
                    fun x -> x.Arg<ILambdaContainer>().GetInstance<TestType3>() |> ignore)

    //Act - assert
    Assert.Throws<CyclicDependencyException>(fun () -> sut.GetInstance<TestType3>() |> ignore) |> ignore

[<Test>]
let ``Can Dispose``() =
    //Arrange
    let sut = createSut()

    //Act
    sut.Dispose()

    //Assert
    repository.Received().Dispose()

[<Test>]
let ``Can CreateSubScope``() =
    //Arrange
    let scopedRepository = mock<IFactoryConfigurationRepository>()
    let sut = createSutWithScoping (echoSpecificRepo scopedRepository)

    //Act
    let subScope = sut.CreateSubScope()

    //Assert
    subScope |> should not' (equal null)
    subScope.GetFieldValue("repository") :?> IFactoryConfigurationRepository |> should equal scopedRepository

[<Test>]
let ``Can CreateSubScope With Additional Registrations``() =
    //Arrange
    let scopedRepository = mock<IFactoryConfigurationRepository>()
    let recFunc = Action<ILambdaContainerRegistrationsRecorder>(fun _ -> ())
    let cloneFunc = fun (r : IFactoryConfigurationRepository option) (rr : (ILambdaContainerRegistrationsRecorder -> unit) option) -> 
                                Assert.That(r.Value, Is.EqualTo repository)
                                Assert.That(rr.IsSome, Is.True)
                                scopedRepository
    let scoping =
        {
            CreateSubScope = cloneFunc
            CreateResolutionScope = (fun r f -> r)
        }

    let sut = createSutWithScoping scoping

    //Act
    let subScope = sut.CreateSubScopeWith(recFunc)

    //Assert
    subScope |> should not' (equal null)
    subScope.GetFieldValue("repository") :?> IFactoryConfigurationRepository |> should equal scopedRepository