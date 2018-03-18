
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

namespace LambdaContainer.Core.DisposalScopes
open LambdaContainer.Core.Contracts
open LambdaContainer.Core.FactoryContracts
open LambdaContainer.Core.NetFrameworkEx
open System
open System.Collections.Concurrent
open System.Threading

type internal SharedScope(managedFactory : IInstanceFactory) =
    
    override __.ToString() = 
        managedFactory.ToString()

    interface IDisposalScope<IInstanceFactory> with

        //A shared scope does not create sub scopes - so just return the current scope
        member this.CreateSubScope() =  
            this :> IInstanceFactory
        
        //Instance is teared down with the application, not in the shared scope
        member __.Dispose() = 
            () 

    interface IInstanceFactory with
        member __.GetIdentity() = 
            managedFactory.GetIdentity()

        member __.Invoke container = 
            container |> managedFactory.Invoke

type internal SubScope(managedFactory : IInstanceFactory) =
    
    override __.ToString() = 
        managedFactory.ToString()

    interface IDisposalScope<IInstanceFactory> with

        member __.CreateSubScope() = 
            new SubScope(managedFactory.CreateSubScope()) :> IInstanceFactory
        
        member __.Dispose() = 
            managedFactory |> Disposer.disposeIfPossible

    interface IInstanceFactory with
        
        member __.GetIdentity() = 
            managedFactory.GetIdentity()

        member __.Invoke container = 
            container |> managedFactory.Invoke

type internal ApplicationScope(managedFactory : IInstanceFactory, subScopeMode : DisposalScope) =
    
    override __.ToString() = 
        managedFactory.ToString()

    interface IDisposalScope<IInstanceFactory> with

        //Reuse any pre-created instance across scopes
        member __.CreateSubScope() =
            match subScopeMode with
            | DisposalScope.Container -> 
                new SharedScope(managedFactory) :> IInstanceFactory
            | DisposalScope.SubScope -> 
                new SubScope(managedFactory.CreateSubScope()) :> IInstanceFactory
            | _ -> failwith "Invalid subscrope mode"
        
        member __.Dispose() = 
            managedFactory |> Disposer.disposeIfPossible

    interface IInstanceFactory with
        member __.GetIdentity() = 
            managedFactory.GetIdentity()

        member __.Invoke container = 
            container |> managedFactory.Invoke