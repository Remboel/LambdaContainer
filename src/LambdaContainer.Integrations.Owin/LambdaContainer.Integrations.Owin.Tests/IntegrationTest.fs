module LambdaContainer.Integrations.Owin.Tests.IntegrationTest
open Microsoft.Owin
open Owin
open LambdaContainer.Core.Contracts
open LambdaContainer.Core.Setup
open Microsoft.Owin.Testing
open NUnit.Framework
open FsUnit
open System.Net
open System.Net.Http
open LambdaContainer.Integrations.Owin.AppBuilder
open LambdaContainer.Integrations.Owin.OwinContext
open System.Threading.Tasks

module LambdaContainerAttachmentTests =
    
    [<Test>]
    let ``OwinApp With LambdaContainer Will Have Scoped OwinVars``() =
        //Arrange
        use server = TestServer.Create(
                        fun app -> 
                            let container =
                                LambdaContainerBootstrapper
                                    .Create()
                                    .ConfigureAssemblyScanner(fun x -> x.Enabled <- false)
                                    .Run()
                            app
                                .WithLambdaContainer(container)
                                .Run(fun context -> 
                                        let lc = context.GetLambdaContainer()
                                        lc |> should not' (equal null)
                            
                                        let context = lc.GetInstance<IOwinContext>()
                                        context |> should not' (equal null)

                                        let request = lc.GetInstance<IOwinRequest>()
                                        request |> should not' (equal null)

                                        let response = lc.GetInstance<IOwinResponse>()
                                        response |> should not' (equal null)
                            
                                        response.StatusCode <- 200
                                        response.WriteAsync("You are awesome!")))
        
        //Act
        let response = server.HttpClient.GetAsync("/") |> Async.AwaitTask |> Async.RunSynchronously
        
        //Assert
        response.StatusCode |> should equal HttpStatusCode.OK
        response.Content.ReadAsStringAsync() |> Async.AwaitTask |> Async.RunSynchronously |> should equal "You are awesome!"

    type ISimpleDependency =
        abstract member GetVal : unit -> string

    type SimpleImplementation(injectedValue) =
        interface ISimpleDependency with
            member __.GetVal() = 
                injectedValue

    type SimpleMiddleware(next, dep : ISimpleDependency, response : IOwinResponse) =
        inherit OwinMiddleware(next)

        override __.Invoke c = 
            async {
                do! dep.GetVal() 
                    |> response.WriteAsync 
                    |> Async.AwaitTask
                return! next.Invoke(c) |> Async.AwaitTask
            } |> Async.StartAsTask :> Task
            
    
    [<Test>]
    let ``If LambdaContainerMiddleware is used then the middlewares dependencies are injected by the active lambda container``() =
        //Arrange
        let injectedValue = "A simple value :-)"
        let textFromLeafMiddleware = "I come from the leaf :-)"
        use server = TestServer.Create(
                        fun app -> 
                            let container =
                                LambdaContainerBootstrapper
                                    .Create()
                                    .WithRegistrationsFrom(
                                        fun x -> x.Record<ITypeMappingRegistrations>(
                                                    fun b -> b.Build().Register<ISimpleDependency,SimpleImplementation>() |> ignore)
                                                   .Record<IFactoryRegistrations>(fun b -> b.Build().Register<string>(fun _ -> injectedValue ) |> ignore) 
                                                   |> ignore)
                                    .ConfigureAssemblyScanner(fun x -> x.Enabled <- false)
                                    .Run()
                            app
                                .WithLambdaContainer(container)
                                .UseLambdaContainerManagedMiddleware<SimpleMiddleware>()
                                .Run(fun c -> 
                                        c.Response.Write textFromLeafMiddleware
                                        Task.Delay(1)))
        
        //Act
        let response = server.HttpClient.GetAsync("/") |> Async.AwaitTask |> Async.RunSynchronously
        
        //Assert
        response.StatusCode |> should equal HttpStatusCode.OK
        response.Content.ReadAsStringAsync() |> Async.AwaitTask |> Async.RunSynchronously |> should equal (sprintf "%s%s" injectedValue textFromLeafMiddleware) 
