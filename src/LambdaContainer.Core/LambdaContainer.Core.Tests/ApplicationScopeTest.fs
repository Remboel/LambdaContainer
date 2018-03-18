module LambdaContainer.Core.Tests.ApplicationScopeTest
open System
open NSubstitute
open LambdaContainer.Core.Contracts
open NUnit.Framework
open LambdaContainer.Core.FactoryContracts
open LambdaContainer.Core.DisposalScopes
open LambdaContainer.Core.Tests.TestUtilities
open FsUnit

let container = mock<ILambdaContainer>()

[<Test>]
let ``Can Construct``() =
    Assert.DoesNotThrow(fun () -> new ApplicationScope(mock<IInstanceFactory>(), DisposalScope.Container) |> ignore)

[<Test>]
let ``CreateSubScope With Shared Scope``() =
    //Arrange
    let factory = mock<IInstanceFactory>()
    let sut = new ApplicationScope(factory, DisposalScope.Container) :> IInstanceFactory

    //Act
    let clone = sut.CreateSubScope()

    //Assert
    clone |> should not' (be Null)
    clone.GetType() |> should equal typeof<SharedScope>
    [sut ; clone] |> List.iter(fun x -> x.Invoke container |> ignore)
    factory.Received(2).Invoke(container) |> ignore

[<Test>]
let ``CreateSubScope With SubScope``() =
    //Arrange
    let factory = mock<IInstanceFactory>()
    let factoryClone = mock<IInstanceFactory>()
    let sut = new ApplicationScope(factory, DisposalScope.SubScope) :> IInstanceFactory

    factory.CreateSubScope().Returns(factoryClone) |> ignore

    //Act
    let clone = sut.CreateSubScope()

    //Assert
    clone |> should not' (be sameAs sut)
    clone.GetType() |> should equal typeof<SubScope>
    [sut ; clone] |> List.iter(fun x -> x.Invoke container |> ignore)
    factory.Received().Invoke(container) |> ignore
    factoryClone.Received().Invoke(container) |> ignore

[<Test>]
let ``Can Dispose``() =
    //Arrange
    let factory = mock<ITestDisposableInstanceFactory>()
    let sut = new ApplicationScope(factory, DisposalScope.SubScope) :> IDisposable

    //Act
    sut.Dispose()

    //Assert
    factory.Received().Dispose()