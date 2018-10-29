module LambdaContainer.Core.Tests.RegistrationsTest
open System
open NSubstitute
open LambdaContainer.Core.RepositoryConstruction
open LambdaContainer.Core.Contracts
open LambdaContainer.Core.Tests.TestUtilities
open Xunit

type RegistrationArguments = { f:ILambdaContainer -> Object ; t:Type ; n : string option }

let mutable lcMock = mock<ILambdaContainer>()
let mutable receivedArgs = None

let injector factory t n = 
    receivedArgs <- Some({f = factory; t = t ; n = n})

let assertHasRegistered () = 
    Assert.True(receivedArgs.IsSome)

let assertRegisteredType t = 
    Assert.Equal(t, receivedArgs.Value.t)

let assertRegisteredName n = 
    Assert.Equal(n, receivedArgs.Value.n)

let assertRegisteredFactoryOutput r = 
    Assert.Equal(r, receivedArgs.Value.f(lcMock))

let assertTypeRegistration<'a when 'a: not struct> ()= 
    let r = mock<'a>()
    (lcMock.GetInstanceOfType typeof<'a>).Returns(r) |> ignore
    assertRegisteredFactoryOutput r

let assertRegistrationPerformed<'a> name =
    assertHasRegistered()
    assertRegisteredType typeof<'a>
    assertRegisteredName name

let setup() =
    receivedArgs <- None
    lcMock <- mock<ILambdaContainer>()

[<Fact>]
let ``Can Construct A FactoryRegistrations``() =
    setup()
    Assert.NotNull(new FactoryRegistrations(injector))

[<Fact>]
let ``FactoryRegistrations Can Register``() =
    //Arrange
    setup()
    let sut = FactoryRegistrations(injector) :> IFactoryRegistrations
    let factoryOutput = mock<ITestType>()

    //Act
    sut.Register<ITestType>(fun _ -> factoryOutput) |> ignore

    //Assert
    assertRegistrationPerformed<ITestType> None
    assertRegisteredFactoryOutput factoryOutput

[<Fact>]
let ``FactoryRegistrations Can Register By Name``() =
    //Arrange
    setup()
    let sut = FactoryRegistrations(injector) :> IFactoryRegistrations
    let factoryOutput = mock<ITestType>()
    let name = "a name"

    //Act
    sut.RegisterByName<ITestType>((fun _ -> factoryOutput), name) |> ignore

    //Assert
    assertRegistrationPerformed<ITestType> (Some name)
    assertRegisteredFactoryOutput factoryOutput

[<Fact>]
let ``Can Construct A TypeMappingRegistrations``() =
    setup()
    Assert.NotNull(new TypeMappingRegistrations(injector))

[<Fact>]
let ``TypeMappingRegistrations Can Register``() =
    //Arrange
    setup()
    let sut = TypeMappingRegistrations(injector) :> ITypeMappingRegistrations

    //Act
    sut.Register<ITestType, TestTypeImpl>() |> ignore

    //Assert
    assertRegistrationPerformed<ITestType> None
    assertTypeRegistration<TestTypeImpl>()

[<Fact>]
let ``TypeMappingRegistrations Can Register By Name``() =
    //Arrange
    setup()
    let sut = TypeMappingRegistrations(injector) :> ITypeMappingRegistrations
    let name = Guid.NewGuid().ToString()

    //Act
    sut.RegisterByName<ITestType, TestTypeImpl>(name) |> ignore

    //Assert
    assertRegistrationPerformed<ITestType> (Some name)
    assertTypeRegistration<TestTypeImpl>()

[<Fact>]
let ``TypeMappingRegistrations Can Not Register T to T``() =
    //Arrange
    setup()
    let sut = TypeMappingRegistrations(injector) :> ITypeMappingRegistrations

    //Act + Assert
    Assert.Throws<InvalidRegistrationException>(fun () -> sut.Register<TestTypeImpl, TestTypeImpl>() |> ignore) |> ignore

[<Fact>]
let ``TypeMappingRegistrations Can Not Register T to T By Name``() =
    //Arrange
    setup()
    let sut = TypeMappingRegistrations(injector) :> ITypeMappingRegistrations
    let name = Guid.NewGuid().ToString()

    //Act + Assert
    Assert.Throws<InvalidRegistrationException>(fun () -> sut.RegisterByName<TestTypeImpl, TestTypeImpl>(name) |> ignore) |> ignore

[<Fact>]
let ``TypeMappingRegistrations Can Not Register T1 to T2 If T2 Is Not Implementation Of T1``() =
    //Arrange
    setup()
    let sut = TypeMappingRegistrations(injector) :> ITypeMappingRegistrations

    //Act + Assert
    Assert.Throws<InvalidRegistrationException>(fun () -> sut.Register<ITestType, TestClosedType>() |> ignore) |> ignore

[<Fact>]
let ``TypeMappingRegistrations Can Not Register T1 to T2 By Name If T2 Is Not Implementation Of T1``() =
    //Arrange
    setup()
    let sut = TypeMappingRegistrations(injector) :> ITypeMappingRegistrations

    //Act + Assert
    Assert.Throws<InvalidRegistrationException>(fun () -> sut.RegisterByName<ITestType, TestClosedType>("a name") |> ignore) |> ignore

[<Fact>]
let ``Can Construct A CoreRegistrations``() =
    setup()
    Assert.NotNull(new CoreRegistrations(injector))

let toFactoryFunc x = (Func<ILambdaContainer,Object>(fun _ -> x :> Object))

[<Fact>]
let ``CoreRegistrations Can RegisterFactory``() =
    //Arrange
    setup()
    let sut = CoreRegistrations(injector) :> ICoreRegistrations
    let factoryOutput = mock<ITestType>()

    //Act
    sut.RegisterFactory typeof<ITestType> (toFactoryFunc factoryOutput) |> ignore

    //Assert
    assertRegistrationPerformed<ITestType> None
    assertRegisteredFactoryOutput factoryOutput

[<Fact>]
let ``CoreRegistrations Can Register Factory By Name``() =
    //Arrange
    setup()
    let sut = CoreRegistrations(injector) :> ICoreRegistrations
    let factoryOutput = mock<ITestType>()
    let name = "a name"

    //Act
    sut.RegisterFactoryByName typeof<ITestType> (toFactoryFunc factoryOutput) name |> ignore

    //Assert
    assertRegistrationPerformed<ITestType> (Some name)
    assertRegisteredFactoryOutput factoryOutput

[<Fact>]
let ``CoreRegistrations Can Register Mapping``() =
    //Arrange
    setup()
    let sut = CoreRegistrations(injector) :> ICoreRegistrations

    //Act
    sut.RegisterMapping typeof<ITestType> typeof<TestTypeImpl> |> ignore

    //Assert
    assertRegistrationPerformed<ITestType> None
    assertTypeRegistration<TestTypeImpl>()

[<Fact>]
let ``CoreRegistrations Can Register Mapping By Name``() =
    //Arrange
    setup()
    let sut = CoreRegistrations(injector) :> ICoreRegistrations
    let name = Guid.NewGuid().ToString()

    //Act
    sut.RegisterMappingByName typeof<ITestType> typeof<TestTypeImpl> name |> ignore

    //Assert
    assertRegistrationPerformed<ITestType> (Some name)
    assertTypeRegistration<TestTypeImpl>()

[<Fact>]
let ``CoreRegistrations Can Not Register T to T``() =
    //Arrange
    setup()
    let sut = CoreRegistrations(injector) :> ICoreRegistrations

    //Act + Assert
    Assert.Throws<InvalidRegistrationException>(fun () -> sut.RegisterMapping typeof<TestTypeImpl> typeof<TestTypeImpl> |> ignore) |> ignore

[<Fact>]
let ``CoreRegistrations Can Not Register T to T By Name``() =
    //Arrange
    setup()
    let sut = CoreRegistrations(injector) :> ICoreRegistrations
    let name = Guid.NewGuid().ToString()

    //Act + Assert
    Assert.Throws<InvalidRegistrationException>(fun () -> sut.RegisterMappingByName typeof<TestTypeImpl> typeof<TestTypeImpl> name |> ignore) |> ignore

[<Fact>]
let ``CoreRegistrations Can Not Register T1 to T2 If T2 Is Not Implementation Of T1``() =
    //Arrange
    setup()
    let sut = CoreRegistrations(injector) :> ICoreRegistrations

    //Act + Assert
    Assert.Throws<InvalidRegistrationException>(fun () -> sut.RegisterMapping typeof<ITestType> typeof<TestClosedType> |> ignore) |> ignore

[<Fact>]
let ``CoreRegistrations Can Not Register T1 to T2 By Name If T2 Is Not Implementation Of T1``() =
    //Arrange
    setup()
    let sut = CoreRegistrations(injector) :> ICoreRegistrations

    //Act + Assert
    Assert.Throws<InvalidRegistrationException>(fun () -> sut.RegisterMappingByName typeof<ITestType> typeof<TestClosedType> "a name" |> ignore) |> ignore