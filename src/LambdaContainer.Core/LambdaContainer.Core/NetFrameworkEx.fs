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

module internal LambdaContainer.Core.NetFrameworkEx
open System
open System.Reflection
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Quotations.Patterns
open System.Collections.Generic
open System.Linq.Expressions
open System.Collections.Concurrent
open System.Collections.ObjectModel

    module ParameterGuard =
        exception NullArgumentConventionBrokenException of string

        let checkForNull (candidate : System.Object) =
            match candidate with
            | null -> 
                raise <| NullArgumentConventionBrokenException("Error: Candidate was null")
            | _ -> 
                ()

    module Disposer =
        let disposeIfPossible<'a when 'a :> Object> (x : 'a) =
            match (x :> Object) with
            | :? IDisposable as d -> 
                d.Dispose()
            | _ -> 
                ()

    module DictionaryEx =
        let asReadOnly<'dic,'key,'value when 'dic :> IDictionary<'key,'value>> (candidate : 'dic) =
            ParameterGuard.checkForNull candidate
            new ReadOnlyDictionary<'key,'value>(candidate) :> IReadOnlyDictionary<'key,'value>

    module DataStructures =
        type StackSet<'a>() =
            let set = new HashSet<'a>()
            let stack = new Stack<'a>()

            member public __.AsSeq() = 
                stack.ToArray() |> Seq.ofArray

            member public __.Push item =
                match set.Add(item) with
                | false -> 
                    false
                | true ->   
                    stack.Push(item)
                    true

            member public __.Peek() =
                match stack.Count with
                | 0 -> 
                    None
                | x -> 
                    stack.Peek() |> Some

            member public __.Pop() =
                match set.Count with
                | 0 ->
                    raise <| InvalidOperationException "Error: Cannot pop on an empty stack"
                | _ ->
                    let x = stack.Pop()
                    set.Remove(x) |> ignore
                    x

            member public __.Count() = 
                set.Count

    module internal TypeExplorer =
        let getProperty<'a, 'propValType> (memberAccessExpression : Expr<'a -> 'propValType>) =
            match memberAccessExpression with
            | Lambda(_, body) -> 
                match body with
                | PropertyGet(_, propInfo, _) -> propInfo
                | _ -> failwith "Error. Please provide a property 'get' expression."
            | _ -> 
                failwith "Invalid parameter provided. Expected a lambda expression"

        let getMethod<'a, 'returnType> (methodCallExpression : Expr<'a->'returnType>) =
            match methodCallExpression with
            | Lambda(_, body) -> 
                match body with
                | Call(_, mi, _) -> mi
                | _ -> failwith "Error. Please provide a method call expression."
            | _ -> 
                failwith "Invalid parameter provided. Expected a lambda expression"

        let getStaticMethod<'a, 'returnType> (methodCallExpression : Expr) =
            match methodCallExpression with
            | Call(_, mi, _) -> 
                mi
            | _ -> 
                failwith "Error. Please provide a method call expression."
    
    module ObjectTransforms =
        
        [<AbstractClass; Sealed>]
        type internal ArrayTransforms() =
            static let arrayCastFunctions = new ConcurrentDictionary<Type, Func<Object[], Object>>()

            static member private TransformArray<'elementType>(input : Object array) = input |> Array.map (fun element -> element :?> 'elementType)

            static member private CreateDynamicCastFunc (elementType : Type) =
                let inputParam = Expression.Parameter(typeof<Object[]>,"inputParam")

                let castMethod = TypeExplorer.getStaticMethod <@ ArrayTransforms.TransformArray([||]) @>
                let genericCastMethod = castMethod.GetGenericMethodDefinition().MakeGenericMethod elementType
                let executeCastExpr = Expression.Call( genericCastMethod, inputParam )
                let conversionLambda = Expression.Lambda<Func<Object[], Object>>( executeCastExpr, inputParam)
                conversionLambda.Compile()

            static member internal ToArrayOfType (targetElementType : Type) inputArray = 
                let f = arrayCastFunctions.GetOrAdd(targetElementType, Func<Type,Func<Object[], Object>>(fun t -> t |> ArrayTransforms.CreateDynamicCastFunc))
                f.Invoke(inputArray)