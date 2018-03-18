// The MIT License(MIT)
// Copyright(c) 2017 Morten Rembøl Jacobsen

// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and 
// associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, 
// and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE 
// WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR 
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, 
// ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

namespace LambdaContainer.Integrations.Owin.Middleware
open LambdaContainer.Core.Contracts
open Microsoft.Owin
open System
open System.Threading.Tasks
open LambdaContainer.Integrations.Owin.OwinContext
    
module Internal =
    module LambdaContainerAttachment = 

        let internal withScope container (context : IOwinContext) =
            context.Set<ILambdaContainer>(OwinDataKeys.LambdaContainerScope, container)

        let attachTo (context : IOwinContext) (next : Func<Task>) (container : ILambdaContainer) = async { 
                use scope = 
                    container
                        .CreateSubScopeWith(
                            fun r -> r.Record<IFactoryRegistrations>(
                                        fun fr ->  fr
                                                    .Build()
                                                    .Register(fun _ -> context)
                                                    .Register(fun _ -> context.Request)
                                                    .Register(fun _ -> context.Response) |> ignore) 
                                        |> ignore)
                
                context 
                |> withScope scope 
                |> ignore

                return! next.Invoke() |> Async.AwaitTask
            }

namespace LambdaContainer.Integrations.Owin.Middleware.External
open LambdaContainer.Core.Contracts
open Microsoft.Owin
open System
open System.Threading.Tasks
open LambdaContainer.Integrations.Owin.OwinContext

type LambdaContainerMiddleware<'a when 'a :> OwinMiddleware>(next) =
    inherit OwinMiddleware(next)
        override __.Invoke context =
            match context.MaybeGetLambdaContainer() with
            | None ->
                raise <| InvalidOperationException("""No instance of the Lambda Container is available at this stage of the Owin pipeline. 
                                                      Please make sure you register the lambda container before anything that depends on it""")
            | Some(container) ->
                container.WithCustomizedResolution<IFactoryRegistrations>(
                    fun x -> x.Register<OwinMiddleware>(
                                fun _ -> next) |> ignore)
                    .GetInstance<'a>()
                    .Invoke(context)