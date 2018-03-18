module LambdaContainer.Core.Tests.FactoryConfigurationRepositoryTest
open System
open System.Linq
open System.Collections.Generic
open NSubstitute
open NUnit.Framework
open LambdaContainer.Core.FactoryContracts
open LambdaContainer.Core.InstanceFactories
open LambdaContainer.Core.RepositoryConstruction
open LambdaContainer.Core.Contracts
open LambdaContainer.Core.Tests.TestUtilities
open LambdaContainer.Core.Tests.TestUtilities.Identity
open FsUnit

type internal ITestDisposableInstanceFactory =
    inherit IInstanceFactory
    inherit IDisposable

let private constructFactoryConfig(name : string option) =
    let theType = typeof<string>
    let factory = (fun (_ : ILambdaContainer) -> System.Guid.NewGuid().ToString() :> System.Object)
    let identity = new FactoryIdentity(name,theType,None)
    new InstanceFactoryForTransientProducts(factory, identity) :> IInstanceFactory

let private createReadWriteInput() = new Dictionary<Type,IReadOnlyDictionary<FactoryIdentity,IInstanceFactory>>()

let private createRepositoryInput identToFactMap=
    let input = createReadWriteInput()
    let factoryMap = identToFactMap |> dict |> (fun x -> x.ToDictionary(keySelector = (fun kvp -> kvp.Key), elementSelector = (fun kvp -> kvp.Value)))
    input.Add(typeof<string>, factoryMap)
    input :> IReadOnlyDictionary<Type,IReadOnlyDictionary<FactoryIdentity,IInstanceFactory>>

let private createSutWith identToFactMap = 
    identToFactMap
    |> createRepositoryInput 
    |> (fun x -> new FactoryConfigurationRepository(x) :> IFactoryConfigurationRepository)

[<Test>]
let ``Can Construct``() =
    //Act
    let sut = createSutWith []

    //Assert
    sut.Retrieve typeof<string> None |> should equal None

[<Test>]
let ``Can Retrieve``() =
    //Arrange
    let factoryConfig = constructFactoryConfig(None)
    let sut = createSutWith [factoryConfig.GetIdentity(), factoryConfig]

    //Act
    let retrievedConfig = sut.Retrieve (factoryConfig.GetIdentity().GetOutputType()) None

    //Assert
    retrievedConfig |> should equal (Some factoryConfig)

[<Test>]
let ``Can Retrieve By Name``() =
    //Arrange
    let factoryConfig1 = constructFactoryConfig( Some "1")
    let factoryConfig2 = constructFactoryConfig( Some "2")
    let sut = createSutWith [factoryConfig1.GetIdentity(), factoryConfig1; factoryConfig2.GetIdentity(), factoryConfig2]

    //Act
    let retrievedConfig = sut.Retrieve (factoryConfig2.GetIdentity().GetOutputType()) (Some "2")

    //Assert
    retrievedConfig |> should equal (Some factoryConfig2)

[<Test>]
let ``Can Retrieve All``() =
    //Arrange
    let factoryConfig1 = constructFactoryConfig( Some "1")
    let factoryConfig2 = constructFactoryConfig( Some "2")
    let sut = createSutWith [factoryConfig1.GetIdentity(), factoryConfig1; factoryConfig2.GetIdentity(), factoryConfig2]

    //Act
    let retrievedConfigs = sut.RetrieveAll (factoryConfig2.GetIdentity().GetOutputType())

    //Assert
    retrievedConfigs.IsSome |> should be True
    retrievedConfigs.Value |> List.ofSeq |> should equal [factoryConfig1; factoryConfig2]

[<Test>]
let ``Retrieve All For Anonymous Registration Throws``() =
    //Arrange
    let factoryConfig1 = constructFactoryConfig(None)
    let sut = createSutWith [factoryConfig1.GetIdentity(), factoryConfig1]

    //Act - Assert
    Assert.Throws<InvalidOperationException>(fun () -> sut.RetrieveAll (factoryConfig1.GetIdentity().GetOutputType()) |> ignore) |> ignore


[<Test>]
let ``Can CreateSubScope``() =
    //Arrange
    let factoryConfig1 = constructFactoryConfig(None)
    let sut = createSutWith [factoryConfig1.GetIdentity(), factoryConfig1]

    //Act
    let clone = sut.CreateSubScope()

    //Assert
    clone |> should not' (be sameAs sut)
    clone |> should not' (equal null)

[<Test>]
let ``CreateSubScope Clones Inner Content``() =
    //Arrange
    let factoryConfig1 = mock<IInstanceFactory>()
    let factoryConfig2 = mock<IInstanceFactory>()
    let factoryConfig1Clone = mock<IInstanceFactory>()
    let factoryConfig2Clone = mock<IInstanceFactory>()
    let sut = createSutWith [namedIdentity<string> "1", factoryConfig1; namedIdentity<string> "2", factoryConfig2]

    factoryConfig1.CreateSubScope().Returns(factoryConfig1Clone) |> ignore
    factoryConfig2.CreateSubScope().Returns(factoryConfig2Clone) |> ignore

    //Act
    let clone = sut.CreateSubScope()

    //Assert
    let cloneConfig1 = clone.Retrieve typeof<string> (Some "1")
    let cloneConfig2 = clone.Retrieve typeof<string> (Some "2")

    cloneConfig1.Value |> should be (sameAs factoryConfig1Clone)
    cloneConfig2.Value |> should be (sameAs factoryConfig2Clone)

[<Test>]
let ``Can Dispose``() =
    //Arrange
    let factoryExpectedToBeDisposed = mock<ITestDisposableInstanceFactory>()
    let sut = createSutWith [namedIdentity<string> "1", factoryExpectedToBeDisposed :> IInstanceFactory; namedIdentity<string> "2", mock<IInstanceFactory>()]

    //Act
    (sut :> IDisposable).Dispose()

    //Assert
    factoryExpectedToBeDisposed.Received().Dispose()