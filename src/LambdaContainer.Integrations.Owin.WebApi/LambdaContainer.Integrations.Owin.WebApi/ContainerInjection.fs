// The MIT License(MIT)
// Copyright(c) 2015 Morten Rembøl Jacobsen

// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and 
// associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, 
// and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE 
// WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR 
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, 
// ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
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

module LambdaContainer.Integrations.Owin.WebApi.ContainerInjection
open System.Net.Http
open System.Runtime.CompilerServices
open System.Web.Http.Hosting
open LambdaContainer.Integrations.Owin.OwinContext
open Microsoft.Owin
open LambdaContainer.Core.Contracts
open System.Web.Http.Dependencies

[<Extension>]
type internal InternalOwinContextExtensions =

    [<Extension>]
    static member MaybeGetLambdaContainer(context : IOwinContext) =
        let container = context.GetLambdaContainer()
        if container = Unchecked.defaultof<ILambdaContainer> then
            None
        else
            container |> Option.Some

type SharedContainerScope(c : ILambdaContainer) = 
    interface IDependencyScope with
        member __.Dispose() = 
            () //Shared scope is disposed by someone else

        member __.GetService t =
            c.GetInstanceOfTypeOrNull(t)

        member __.GetServices t =
            c.GetAllInstancesOfType(t)
        

type LambdaContainerScopeInjectionHandler() =
    inherit DelegatingHandler()

    member private __._SendAsync(r,c) =
        base.SendAsync(r, c)

    override __.SendAsync(request, ct) = 
        async {
            match request.GetOwinContext() with
            | null ->
                ()
            | context ->
                match context.MaybeGetLambdaContainer() with
                | None ->
                    ()
                | Some(container) ->
                    request.Properties.[HttpPropertyKeys.DependencyScope] <- new SharedContainerScope(container)
                    
            return! __._SendAsync(request, ct) |> Async.AwaitTask
        } |> Async.StartAsTask

