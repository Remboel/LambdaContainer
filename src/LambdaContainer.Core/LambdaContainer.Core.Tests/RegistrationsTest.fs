module LambdaContainer.Core.Tests.RegistrationsTest
open System
open NSubstitute
open NUnit.Framework
open LambdaContainer.Core.RepositoryConstruction
open LambdaContainer.Core.Contracts
open LambdaContainer.Core.Tests.TestUtilities
open FsUnit

type RegistrationArguments = { f:ILambdaContainer -> Object ; t:Type ; n : string option }

let mutable lcMock = mock<ILambdaContainer>()
let mutable receivedArgs = None

let injector factory t n = 
    receivedArgs <- Some({f = factory; t = t ; n = n})

let assertHasRegistered () = 
    receivedArgs |> Option.isSome |> should be True

let assertRegisteredType t = 
    receivedArgs.Value.t |> should equal t

let assertRegisteredName n = 
    receivedArgs.Value.n |> should equal n

let assertRegisteredFactoryOutput r = 
    receivedArgs.Value.f(lcMock) |> should equal r

let assertTypeRegistration<'a when 'a: not struct> ()= 
    let r = mock<'a>()
    (lcMock.GetInstanceOfType typeof<'a>).Returns(r) |> ignore
    assertRegisteredFactoryOutput r

let assertRegistrationPerformed<'a> name =
    assertHasRegistered()
    assertRegisteredType typeof<'a>
    assertRegisteredName name

[<SetUp>]
let Setup() =
    receivedArgs <- None
    lcMock <- mock<ILambdaContainer>()

[<Test>]
let ``Can Construct A FactoryRegistrations``() =
    Assert.DoesNotThrow(fun () ->new FactoryRegistrations(injector) |> ignore)

[<Test>]
let ``FactoryRegistrations Can Register``() =
    //Arrange
    let sut = FactoryRegistrations(injector) :> IFactoryRegistrations
    let factoryOutput = mock<ITestType>()

    //Act
    sut.Register<ITestType>(fun _ -> factoryOutput) |> ignore

    //Assert
    assertRegistrationPerformed<ITestType> None
    assertRegisteredFactoryOutput factoryOutput

[<Test>]
let ``FactoryRegistrations Can Register By Name``() =
    //Arrange
    let sut = FactoryRegistrations(injector) :> IFactoryRegistrations
    let factoryOutput = mock<ITestType>()
    let name = "a name"

    //Act
    sut.RegisterByName<ITestType>((fun _ -> factoryOutput), name) |> ignore

    //Assert
    assertRegistrationPerformed<ITestType> (Some name)
    assertRegisteredFactoryOutput factoryOutput

[<Test>]
let ``Can Construct A TypeMappingRegistrations``() =
    Assert.DoesNotThrow(fun () ->new TypeMappingRegistrations(injector) |> ignore)

[<Test>]
let ``TypeMappingRegistrations Can Register``() =
    //Arrange
    let sut = TypeMappingRegistrations(injector) :> ITypeMappingRegistrations

    //Act
    sut.Register<ITestType, TestTypeImpl>() |> ignore

    //Assert
    assertRegistrationPerformed<ITestType> None
    assertTypeRegistration<TestTypeImpl>()

[<Test>]
let ``TypeMappingRegistrations Can Register By Name``() =
    //Arrange
    let sut = TypeMappingRegistrations(injector) :> ITypeMappingRegistrations
    let name = Guid.NewGuid().ToString()

    //Act
    sut.RegisterByName<ITestType, TestTypeImpl>(name) |> ignore

    //Assert
    assertRegistrationPerformed<ITestType> (Some name)
    assertTypeRegistration<TestTypeImpl>()

[<Test>]
let ``TypeMappingRegistrations Can Not Register T to T``() =
    //Arrange
    let sut = TypeMappingRegistrations(injector) :> ITypeMappingRegistrations

    //Act + Assert
    Assert.Throws<InvalidRegistrationException>(fun () -> sut.Register<TestTypeImpl, TestTypeImpl>() |> ignore) |> ignore

[<Test>]
let ``TypeMappingRegistrations Can Not Register T to T By Name``() =
    //Arrange
    let sut = TypeMappingRegistrations(injector) :> ITypeMappingRegistrations
    let name = Guid.NewGuid().ToString()

    //Act + Assert
    Assert.Throws<InvalidRegistrationException>(fun () -> sut.RegisterByName<TestTypeImpl, TestTypeImpl>(name) |> ignore) |> ignore

[<Test>]
let ``TypeMappingRegistrations Can Not Register T1 to T2 If T2 Is Not Implementation Of T1``() =
    //Arrange
    let sut = TypeMappingRegistrations(injector) :> ITypeMappingRegistrations

    //Act + Assert
    Assert.Throws<InvalidRegistrationException>(fun () -> sut.Register<ITestType, TestClosedType>() |> ignore) |> ignore

[<Test>]
let ``TypeMappingRegistrations Can Not Register T1 to T2 By Name If T2 Is Not Implementation Of T1``() =
    //Arrange
    let sut = TypeMappingRegistrations(injector) :> ITypeMappingRegistrations

    //Act + Assert
    Assert.Throws<InvalidRegistrationException>(fun () -> sut.RegisterByName<ITestType, TestClosedType>("a name") |> ignore) |> ignore

[<Test>]
let ``Can Construct A CoreRegistrations``() =
    Assert.DoesNotThrow(fun () ->new CoreRegistrations(injector) |> ignore)

let toFactoryFunc x = (Func<ILambdaContainer,Object>(fun _ -> x :> Object))

[<Test>]
let ``CoreRegistrations Can RegisterFactory``() =
    //Arrange
    let sut = CoreRegistrations(injector) :> ICoreRegistrations
    let factoryOutput = mock<ITestType>()

    //Act
    sut.RegisterFactory typeof<ITestType> (toFactoryFunc factoryOutput) |> ignore

    //Assert
    assertRegistrationPerformed<ITestType> None
    assertRegisteredFactoryOutput factoryOutput

[<Test>]
let ``CoreRegistrations Can Register Factory By Name``() =
    //Arrange
    let sut = CoreRegistrations(injector) :> ICoreRegistrations
    let factoryOutput = mock<ITestType>()
    let name = "a name"

    //Act
    sut.RegisterFactoryByName typeof<ITestType> (toFactoryFunc factoryOutput) name |> ignore

    //Assert
    assertRegistrationPerformed<ITestType> (Some name)
    assertRegisteredFactoryOutput factoryOutput

[<Test>]
let ``CoreRegistrations Can Register Mapping``() =
    //Arrange
    let sut = CoreRegistrations(injector) :> ICoreRegistrations

    //Act
    sut.RegisterMapping typeof<ITestType> typeof<TestTypeImpl> |> ignore

    //Assert
    assertRegistrationPerformed<ITestType> None
    assertTypeRegistration<TestTypeImpl>()

[<Test>]
let ``CoreRegistrations Can Register Mapping By Name``() =
    //Arrange
    let sut = CoreRegistrations(injector) :> ICoreRegistrations
    let name = Guid.NewGuid().ToString()

    //Act
    sut.RegisterMappingByName typeof<ITestType> typeof<TestTypeImpl> name |> ignore

    //Assert
    assertRegistrationPerformed<ITestType> (Some name)
    assertTypeRegistration<TestTypeImpl>()

[<Test>]
let ``CoreRegistrations Can Not Register T to T``() =
    //Arrange
    let sut = CoreRegistrations(injector) :> ICoreRegistrations

    //Act + Assert
    Assert.Throws<InvalidRegistrationException>(fun () -> sut.RegisterMapping typeof<TestTypeImpl> typeof<TestTypeImpl> |> ignore) |> ignore

[<Test>]
let ``CoreRegistrations Can Not Register T to T By Name``() =
    //Arrange
    let sut = CoreRegistrations(injector) :> ICoreRegistrations
    let name = Guid.NewGuid().ToString()

    //Act + Assert
    Assert.Throws<InvalidRegistrationException>(fun () -> sut.RegisterMappingByName typeof<TestTypeImpl> typeof<TestTypeImpl> name |> ignore) |> ignore

[<Test>]
let ``CoreRegistrations Can Not Register T1 to T2 If T2 Is Not Implementation Of T1``() =
    //Arrange
    let sut = CoreRegistrations(injector) :> ICoreRegistrations

    //Act + Assert
    Assert.Throws<InvalidRegistrationException>(fun () -> sut.RegisterMapping typeof<ITestType> typeof<TestClosedType> |> ignore) |> ignore

[<Test>]
let ``CoreRegistrations Can Not Register T1 to T2 By Name If T2 Is Not Implementation Of T1``() =
    //Arrange
    let sut = CoreRegistrations(injector) :> ICoreRegistrations

    //Act + Assert
    Assert.Throws<InvalidRegistrationException>(fun () -> sut.RegisterMappingByName typeof<ITestType> typeof<TestClosedType> "a name" |> ignore) |> ignore