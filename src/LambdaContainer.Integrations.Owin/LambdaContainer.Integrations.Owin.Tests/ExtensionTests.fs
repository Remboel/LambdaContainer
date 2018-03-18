module LambdaContainer.Integrations.Owin.Tests.ExtensionTests
open NUnit.Framework
open FsUnit
open System.Threading.Tasks
open NSubstitute
open System
open Microsoft.Owin
open Owin
open LambdaContainer.Core.Contracts
open LambdaContainer.Integrations.Owin.OwinContext
open LambdaContainer.Integrations.Owin.AppBuilder

[<TestFixture>]
type public OwinContextTests() =
    
    [<Test>]
    member __.GetLambdaContainer_Returns_Instance_From_OwinContext() =
        //Arrange
        let context = Substitute.For<IOwinContext>()
        let container = Substitute.For<ILambdaContainer>()

        context.Get<ILambdaContainer>(OwinDataKeys.LambdaContainerScope).Returns(container) |> ignore

        //Act + Assert
        context.GetLambdaContainer() |> should equal container

    [<Test>]
    member __.GetLambdaContainer_When_No_Container_In_Context_Returns_Null() =
        //Arrange
        let context = Substitute.For<IOwinContext>()
        context.Get<ILambdaContainer>(OwinDataKeys.LambdaContainerScope).Returns(Unchecked.defaultof<ILambdaContainer>) |> ignore

        //Act + Assert
        context.GetLambdaContainer() |> should equal null

[<TestFixture>]
type public AppBuilderExtensionsTests() =
    
    [<Test>]
    member __.WithLambdaContainer_Extends_Pipeline_And_Returns_AppBuilder() =
        //Arrange
        let appBuilder = Substitute.For<IAppBuilder>()
        let container = Substitute.For<ILambdaContainer>()
        appBuilder.Use(Arg.Any<Func<IOwinContext,OwinMiddleware,Task>>()).ReturnsForAnyArgs(appBuilder) |> ignore

        //Act + assert
        container |> appBuilder.WithLambdaContainer |> should equal appBuilder
