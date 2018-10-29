module LambdaContainer.Core.Tests.SharedScopeTest
open System
open NSubstitute
open LambdaContainer.Core.Contracts
open LambdaContainer.Core.FactoryContracts
open LambdaContainer.Core.DisposalScopes
open LambdaContainer.Core.Tests.TestUtilities
open Xunit
    
[<Fact>]
let ``Can Construct``() =
    Assert.NotNull(new SharedScope(mock<IInstanceFactory>()))

[<Fact>]
let ``CreateSubScope Returns Self``() =
    //Arrange
    let factory = mock<IInstanceFactory>()
    let sut = new SharedScope(factory) :> IInstanceFactory
    let container = mock<ILambdaContainer>()

    //Act
    let clone = sut.CreateSubScope()

    //Assert
    Assert.Same(sut, clone)
    clone.Invoke container |> ignore
    factory.Received().Invoke(container) |> ignore

[<Fact>]
let ``Can Dispose Does Not Dispose Shared Factory``() =
    //Arrange
    let factory = mock<ITestDisposableInstanceFactory>()
    let sut = new SharedScope(factory) :> IDisposable

    //Act
    sut.Dispose()

    //Assert
    factory.DidNotReceive().Dispose()