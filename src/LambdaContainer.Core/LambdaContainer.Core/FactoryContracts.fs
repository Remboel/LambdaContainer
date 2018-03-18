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

namespace LambdaContainer.Core.FactoryContracts
open LambdaContainer.Core.Contracts
open LambdaContainer.Core.NetFrameworkEx
open System
open System.Collections.Concurrent
open System.Threading

type internal FactoryIdentity(name : string option, outputType : Type, registrationSourceInfo : string option) =
    
    let nameForHash =
                match name with
                | Some(name) -> 
                    name
                | None -> 
                    System.String.Empty
    
    let hashCode = (nameForHash.GetHashCode() * 397) ^^^ (outputType.GetHashCode())

    //Public
    member __.GetName() = 
        nameForHash
    
    member __.GetOutputType() = 
        outputType

    member __.GetRegistrationSourceInformation() = 
        match registrationSourceInfo with 
        | None -> 
            System.String.Empty 
        | Some(sourceInfo) -> 
            sourceInfo

    member __.IsAnonymous() = 
        match name with 
        | Some(_) -> 
            false 
        | None -> 
            true
    
    //Public overrides
    override __.GetHashCode() = 
        hashCode

    override __.Equals(other) =
        match Object.ReferenceEquals(null,other) with
            | true -> 
                false
            | false -> 
                match other.GetType() with
                | a when other.GetType().Equals(__.GetType()) ->
                    nameForHash.Equals((other :?> FactoryIdentity).GetName()) 
                    && 
                    outputType.Equals((other :?> FactoryIdentity).GetOutputType())
                | _ -> 
                    false

    override __.ToString() =
        sprintf """["%s"] : %s . Registered by: %s""" 
            (if __.IsAnonymous() then "-" else __.GetName()) 
            (__.GetOutputType().FullName) 
            (__.GetRegistrationSourceInformation())
                        
type internal IDisposalScope<'a> =
    inherit IDisposable
    abstract member CreateSubScope : unit -> 'a

type internal IInstanceFactory =
    inherit IDisposalScope<IInstanceFactory>
    abstract member Invoke : ILambdaContainer -> Object
    abstract member GetIdentity : unit -> FactoryIdentity