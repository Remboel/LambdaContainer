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

namespace LambdaContainer.Core.InstanceFactories
open LambdaContainer.Core.Contracts
open LambdaContainer.Core.FactoryContracts
open LambdaContainer.Core.NetFrameworkEx
open System
open System.Collections.Concurrent
open System.Threading

(*
    Factories
*)
type internal InstanceFactoryForTransientProducts(instanceFactory : (ILambdaContainer -> Object), identity : FactoryIdentity) =
    
    override __.ToString() = 
        identity.ToString()
    
    interface IInstanceFactory with
    
        member __.GetIdentity() = 
            identity

        member __.Invoke container = 
            container |> instanceFactory

    interface IDisposalScope<IInstanceFactory> with
        member __.Dispose() = 
            ()
        
        member __.CreateSubScope() = 
            __ :> IInstanceFactory

type internal InstanceFactoryThreadSingletonProducts(instanceFactory : (ILambdaContainer -> Object), identity : FactoryIdentity) =
    let rwLock = new ReaderWriterLockSlim()
    let mutable instance = new ThreadLocal<Object>(true)

    interface IDisposalScope<IInstanceFactory> with
        member __.Dispose() =
            try
                rwLock.EnterWriteLock()
                
                //Cache existing instances and dispose the thread local map
                let existingInstances = instance.Values |> Seq.toArray
                instance |> Disposer.disposeIfPossible

                //Replace the threadlocal instance and dispose the cached instances.
                instance <- new ThreadLocal<Object>()
                existingInstances |> Seq.iter Disposer.disposeIfPossible
                
            finally
                if(rwLock.IsWriteLockHeld) then 
                    rwLock.ExitWriteLock()

        member __.CreateSubScope() =
            new InstanceFactoryThreadSingletonProducts(instanceFactory,identity) :> IInstanceFactory

    interface IInstanceFactory with

        member __.GetIdentity() = 
                identity

        member __.Invoke container =
                try
                    rwLock.EnterReadLock()
                    
                    match instance.IsValueCreated with
                    | true -> 
                        ()
                    | false ->  
                        let theInstance = container |> instanceFactory
                        instance.Value <- theInstance

                    instance.Value

                finally
                    rwLock.ExitReadLock()
    
    member private __.GetIdentity() =
        identity
    
    override __.ToString() =
        identity.ToString()

type internal InstanceFactorySingletonProducts(instanceFactory : (ILambdaContainer -> Object), identity : FactoryIdentity) =
    let mutable instance = Option<Object>.None
    let rwLock = new ReaderWriterLockSlim()

    interface IDisposalScope<IInstanceFactory> with
        member __.Dispose() =
            try
                rwLock.EnterWriteLock()
                
                //Dispose if needed
                match instance with
                | None -> 
                    ()
                | Some(ins) -> 
                    ins |> Disposer.disposeIfPossible
                    instance <- None
                
            finally
                if(rwLock.IsWriteLockHeld) then 
                    rwLock.ExitWriteLock()
        
        member __.CreateSubScope() =
            new InstanceFactorySingletonProducts(instanceFactory,identity) :> IInstanceFactory

    interface IInstanceFactory with

        member __.GetIdentity() = 
                identity

        member __.Invoke container =
                try
                    rwLock.EnterReadLock()
                    match instance with
                    | None ->
                        rwLock.ExitReadLock()
                        try
                            rwLock.EnterWriteLock()
                            if instance.IsNone then
                                instance <- (Some(container |> instanceFactory))
                        finally
                            rwLock.ExitWriteLock()
                                
                    | _ -> 
                        ()

                    instance.Value
                finally
                    if rwLock.IsReadLockHeld then 
                        rwLock.ExitReadLock()
    
    member private __.GetIdentity() =
        identity
    
    override __.ToString() =
        identity.ToString()