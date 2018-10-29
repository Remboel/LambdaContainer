module LambdaContainer.Core.Tests.SubScopeTest
open System
open NSubstitute
open LambdaContainer.Core.Contracts
open LambdaContainer.Core.FactoryContracts
open LambdaContainer.Core.DisposalScopes
open LambdaContainer.Core.Tests.TestUtilities
open Xunit
    
[<Fact>]
let ``Can Construct``() =
    Assert.NotNull(new SubScope(mock<IInstanceFactory>()))

[<Fact>]
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
    Assert.NotSame(sut, clone)
    Assert.Equal(typeof<SubScope>, clone.GetType())

    sut.Invoke container |> ignore
    clone.Invoke container |> ignore
    factory.Received().Invoke(container) |> ignore
    factoryClone.Received().Invoke(container) |> ignore

[<Fact>]
let ``Can Dispose``() =
    //Arrange
    let factory = mock<ITestDisposableInstanceFactory>()
    let sut = new SubScope(factory) :> IDisposable

    //Act
    sut.Dispose()

    //Assert
    factory.Received().Dispose()