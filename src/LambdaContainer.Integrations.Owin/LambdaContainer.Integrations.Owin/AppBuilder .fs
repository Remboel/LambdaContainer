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

namespace LambdaContainer.Integrations.Owin.AppBuilder
open LambdaContainer.Core.Contracts
open Owin
open Microsoft.Owin
open System.Runtime.CompilerServices
open LambdaContainer.Integrations.Owin.Middleware.Internal
open LambdaContainer.Integrations.Owin.Middleware.External
open System
open System.Threading.Tasks

[<Extension>]
type AppBuilderExtensions =
    
    /// <summary>
    /// Attach an instance of ILambdaContainer to the provided owin app builder.
    /// Make sure this function is called before anything else in the pipeline that may need it.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="container"></param>
    [<Extension>]
    static member WithLambdaContainer(builder : IAppBuilder, container: ILambdaContainer) =
        
        let attachContainerMiddleware (context : IOwinContext) (next : Func<Task>) =
            container 
            |> LambdaContainerAttachment.attachTo context next 
            |> Async.StartAsTask :> Task

        builder.Use(handler = Func<IOwinContext,Func<Task>,Task>(attachContainerMiddleware))

    /// <summary>
    /// Register a middleware type that it will be created and its dependencies injected by the Lambda Container which is currently active in the pipeline.
    /// </summary>
    [<Extension>]
    static member UseLambdaContainerManagedMiddleware<'a when 'a :> OwinMiddleware>(builder : IAppBuilder) =
        builder.Use<LambdaContainerMiddleware<'a>>()


