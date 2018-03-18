module LambdaContainer.Integrations.Owin.WebApi.Tests.AppBuilderExtensionsTests
open FsUnit
open NUnit.Framework
open LambdaContainer.Integrations.Owin.WebApi.AppBuilderEx
open LambdaContainer.Integrations.Owin.WebApi.ContainerInjection
open System.Web.Http
open System.Net.Http
open NSubstitute

[<Test>]
let ``WithLambdaContainerForWebApi adds the container injecting handler at position 0``() =
    let config = new HttpConfiguration()
    let app = Substitute.For<Owin.IAppBuilder>()

    app.WithLambdaContainerForWebApi(config) |> ignore

    config.MessageHandlers.[0].GetType() |> should equal typeof<LambdaContainerScopeInjectionHandler>

[<Test>]
let ``WithLambdaContainerForWebApi will not add handler if already present``() =
    let config = new HttpConfiguration()
    let app = Substitute.For<Owin.IAppBuilder>()

    app.WithLambdaContainerForWebApi(config) |> ignore
    app.WithLambdaContainerForWebApi(config) |> ignore

    config.MessageHandlers.[0].GetType() |> should equal typeof<LambdaContainerScopeInjectionHandler>
    config.MessageHandlers.Count |> should equal 1
    


