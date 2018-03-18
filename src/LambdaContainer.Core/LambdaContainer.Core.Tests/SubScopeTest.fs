module LambdaContainer.Core.Tests.SubScopeTest
open System
open NSubstitute
open LambdaContainer.Core.Contracts
open NUnit.Framework
open LambdaContainer.Core.FactoryContracts
open LambdaContainer.Core.DisposalScopes
open LambdaContainer.Core.Tests.TestUtilities
open FsUnit
    
[<Test>]
let ``Can Construct``() =
    Assert.DoesNotThrow(fun () -> new SubScope(mock<IInstanceFactory>()) |> ignore)

[<Test>]
let ``CreateSubScope Clones Factory``() =
    //Arrange
    let factory = mock<IInstanceFactory>()
    let factoryClone = mock<IInstanceFactory>()
    let sut = new SubScope(factory) :> IInstanceFactory
    let container = mock<ILambdaContainer>()

    factory.CreateSubScope().Returns(factoryClone) |> ignore

    //Act
    let clone = sut.CreateSubScope()

    //Assert
    clone |> should not' (be sameAs sut)
    clone.GetType() |> should equal typeof<SubScope>

    sut.Invoke container |> ignore
    clone.Invoke container |> ignore
    factory.Received().Invoke(container) |> ignore
    factoryClone.Received().Invoke(container) |> ignore

[<Test>]
let ``Can Dispose``() =
    //Arrange
    let factory = mock<ITestDisposableInstanceFactory>()
    let sut = new SubScope(factory) :> IDisposable

    //Act
    sut.Dispose()

    //Assert
    factory.Received().Dispose()