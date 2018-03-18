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

namespace LambdaContainer.Integrations.Owin.OwinContext
open System.Runtime.CompilerServices
open Microsoft.Owin
open LambdaContainer.Core.Contracts

[<AbstractClass; Sealed>]
type internal OwinDataKeys() =
    static member LambdaContainerScope = "lambda-container_owin_scope"

[<Extension>]
type OwinContextExtensions =
    
    /// <summary>
    /// Retrieve the lambda container scope attached to the provided context.
    /// </summary>
    /// <param name="context"></param>
    [<Extension>]
    static member GetLambdaContainer(context : IOwinContext) =
        context.Get<ILambdaContainer>(OwinDataKeys.LambdaContainerScope)

[<Extension>]
type internal InternalOwinContextExtensions =

    [<Extension>]
    static member MaybeGetLambdaContainer(context : IOwinContext) =
        let container = context.GetLambdaContainer()
        if container = Unchecked.defaultof<ILambdaContainer> then
            None
        else
            container |> Option.Some