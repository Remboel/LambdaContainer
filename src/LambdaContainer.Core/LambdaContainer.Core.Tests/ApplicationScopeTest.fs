module LambdaContainer.Core.Tests.ApplicationScopeTest
open System
open NSubstitute
open LambdaContainer.Core.Contracts
open LambdaContainer.Core.FactoryContracts
open LambdaContainer.Core.DisposalScopes
open LambdaContainer.Core.Tests.TestUtilities
open Xunit

let container = mock<ILambdaContainer>()

[<Fact>]
let ``Can Construct``() =
    Assert.NotNull(new ApplicationScope(mock<IInstanceFactory>(), DisposalScope.Container))

[<Fact>]
let ``CreateSubScope With Shared Scope``() =
    //Arrange
    let factory = mock<IInstanceFactory>()
    let sut = new ApplicationScope(factory, DisposalScope.Container) :> IInstanceFactory

    //Act
    let clone = sut.CreateSubScope()

    //Assert
    Assert.NotNull(clone)
    Assert.Equal(typeof<SharedScope>,clone.GetType())
    [sut ; clone] |> List.iter(fun x -> x.Invoke container |> ignore)
    factory.Received(2).Invoke(container) |> ignore

[<Fact>]
let ``CreateSubScope With SubScope``() =
    //Arrange
    let factory = mock<IInstanceFactory>()
    let factoryClone = mock<IInstanceFactory>()
    let sut = new ApplicationScope(factory, DisposalScope.SubScope) :> IInstanceFactory

    factory.CreateSubScope().Returns(factoryClone) |> ignore

    //Act
    let clone = sut.CreateSubScope()

    //Assert
    Assert.NotSame(sut,clone)
    Assert.Equal(typeof<SubScope>, clone.GetType())
    [sut ; clone] |> List.iter(fun x -> x.Invoke container |> ignore)
    factory.Received().Invoke(container) |> ignore
    factoryClone.Received().Invoke(container) |> ignore

[<Fact>]
let ``Can Dispose``() =
    //Arrange
    let factory = mock<ITestDisposableInstanceFactory>()
    let sut = new ApplicationScope(factory, DisposalScope.SubScope) :> IDisposable

    //Act
    sut.Dispose()

    //Assert
    factory.Received().Dispose()