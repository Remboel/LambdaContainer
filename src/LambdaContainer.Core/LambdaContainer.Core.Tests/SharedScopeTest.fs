module LambdaContainer.Core.Tests.SharedScopeTest
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
    Assert.DoesNotThrow(fun () -> new SharedScope(mock<IInstanceFactory>()) |> ignore)

[<Test>]
let ``CreateSubScope Returns Self``() =
    //Arrange
    let factory = mock<IInstanceFactory>()
    let sut = new SharedScope(factory) :> IInstanceFactory
    let container = mock<ILambdaContainer>()

    //Act
    let clone = sut.CreateSubScope()

    //Assert
    clone |> should be (sameAs sut)
    clone.Invoke container |> ignore
    factory.Received().Invoke(container) |> ignore

[<Test>]
let ``Can Dispose Does Not Dispose Shared Factory``() =
    //Arrange
    let factory = mock<ITestDisposableInstanceFactory>()
    let sut = new SharedScope(factory) :> IDisposable

    //Act
    sut.Dispose()

    //Assert
    factory.DidNotReceive().Dispose()