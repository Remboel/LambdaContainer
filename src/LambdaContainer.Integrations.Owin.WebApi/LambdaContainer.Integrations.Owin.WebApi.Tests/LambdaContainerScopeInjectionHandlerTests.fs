module LambdaContainer.Integrations.Owin.WebApi.Tests.LambdaContainerScopeInjectionHandlerTests
open FsUnit
open NUnit.Framework
open LambdaContainer.Integrations.Owin.WebApi.ContainerInjection
open System.Net.Http
open NSubstitute
open System.Threading
open Microsoft.Owin
open LambdaContainer.Core.Contracts
open System.Web.Http.Hosting

let withOwinContext (context : IOwinContext) (request : HttpRequestMessage) =
    request.Properties.Add("MS_OwinContext", context :> obj)
    request

let withLambdaContainer (container : ILambdaContainer) (owinContext : IOwinContext) =
    owinContext.Get<ILambdaContainer>("lambda-container_owin_scope").Returns(container) |> ignore
    owinContext

[<Test>]
let ``If lambdacontainer is present in owin pipeline it will be used for WebApi``() =
    //Arrange
    let sut = new LambdaContainerScopeInjectionHandler(InnerHandler = Substitute.For<HttpMessageHandler>())
    let container = Substitute.For<ILambdaContainer>()
    let owinContext = 
        Substitute.For<IOwinContext>() 
        |> withLambdaContainer container
    let request = 
        new HttpRequestMessage() 
        |> withOwinContext owinContext

    let invoker = new HttpMessageInvoker(sut)

    //Act
    invoker.SendAsync(request,CancellationToken.None) |> Async.AwaitTask |> Async.RunSynchronously |> ignore

    //Assert
    request.Properties.[HttpPropertyKeys.DependencyScope] |> (fun x -> x.GetType()) |> should equal typeof<SharedContainerScope>

[<Test>]
let ``If no lambdacontainer is present in owin pipeline the dependency scope will remain unchanged``() =
    //Arrange
    let sut = new LambdaContainerScopeInjectionHandler(InnerHandler = Substitute.For<HttpMessageHandler>())
    let owinContext = 
        Substitute.For<IOwinContext>() 
        |> withLambdaContainer Unchecked.defaultof<ILambdaContainer>
    let request = 
        new HttpRequestMessage() 
        |> withOwinContext owinContext

    let invoker = new HttpMessageInvoker(sut)

    //Act
    invoker.SendAsync(request,CancellationToken.None) |> Async.AwaitTask |> Async.RunSynchronously |> ignore

    //Assert
    request.Properties.ContainsKey(HttpPropertyKeys.DependencyScope) |> should equal false

[<Test>]
let ``If no owin context is present in the request the dependency scope will remain unchanged``() =
    //Arrange
    let sut = new LambdaContainerScopeInjectionHandler(InnerHandler = Substitute.For<HttpMessageHandler>())
    let request = 
        new HttpRequestMessage() 
        |> withOwinContext null

    let invoker = new HttpMessageInvoker(sut)

    //Act
    invoker.SendAsync(request,CancellationToken.None) |> Async.AwaitTask |> Async.RunSynchronously |> ignore

    //Assert
    request.Properties.ContainsKey(HttpPropertyKeys.DependencyScope) |> should equal false