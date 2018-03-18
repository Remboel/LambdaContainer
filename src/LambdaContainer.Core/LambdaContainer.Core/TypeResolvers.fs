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

namespace LambdaContainer.Core.TypeResolvers
open System
open System.Collections.Generic
open System.Collections.Concurrent
open System.Threading
open LambdaContainer.Core.NetFrameworkEx
open LambdaContainer.Core.ReflectionEx
open System.Linq.Expressions

type BuildInstanceFunc = Type -> Object

type internal ITypeResolver =
    abstract member Resolve : paramBuilder : BuildInstanceFunc -> t : Type -> Object option

open InjectionTargetReflector

type internal DynamicTypeResolver() =
    let factoryFuncMap = ConcurrentDictionary<Type,(BuildInstanceFunc) -> Object>()

    let compileFactoryFunc (targetType : Type) (ctor : TypeCtorCache) =
        //Parameters for innner func
        let parameterTypes = 
            ctor.Parameters 
            |> Array.map (fun p -> p.ParameterType)
        
        let injectionMethods = 
            targetType.GetMethods() 
            |> selectInjectionMethods 
            |> Array.ofSeq
        
        let injectionMethodTypes = 
            injectionMethods 
            |> Array.map (fun m -> m.GetParameters() |> Array.map(fun p -> p.ParameterType))

        let injectionProperties = 
            targetType.GetProperties() 
            |> selectInjectionProperties 
            |> Array.ofSeq

        let propertyTypes = 
            injectionProperties 
            |> Array.map (fun p -> p.PropertyType)

        //Parameters definitions
        let typeToCreateParam = Expression.Parameter(typeof<Type> , "typeToCreate")
        let ctorParamsParam = Expression.Parameter(typeof<Type[]> , "ctorParamTypes")
        let paramBuilderParam = Expression.Parameter(typeof<BuildInstanceFunc> , "paramBuilder")
        let targetTypePropertyParams = Expression.Parameter(typeof<Type[]>, "targetTypePropertyParams");
        let targetTypeMethodParams = Expression.Parameter(typeof<Type[][]>, "targetTypeMethodParams");

        //Inner variables
        let buildUpResult = Expression.Variable(targetType, "buildUpResult");

        //------------Factory implementation------------------
        let blockBody = List<Expression>();
        
        //Helper functions
        let invokeMethod = TypeExplorer.getMethod<FSharpFunc<Type,Object>, Object> <@ fun x -> x.Invoke(typeof<string>) @>

        let convertToExpr toType rightHandExpression = 
            Expression.Convert(rightHandExpression, toType) :> Expression

        let buildObjectOfTypeExpr (typeToBuildExpression : Expression) = 
            Expression.Call(paramBuilderParam, invokeMethod, typeToBuildExpression)

        let injectExpr finalType typeParamExpression = 
            typeParamExpression
            |> buildObjectOfTypeExpr
            |> convertToExpr finalType
        
        let accessArrayExpr index sourceArrayExpr = 
            Expression.ArrayAccess(sourceArrayExpr, Expression.Constant(index)) 

        //Create object and assign it to result variable
        let invokeCtorExpr ctorInfo parameterExpr = 
            Expression.New(ctorInfo, parameterExpr)
        
        let createObject = 
            parameterTypes
            |> Seq.mapi (
                fun i t -> 
                    ctorParamsParam
                    |> accessArrayExpr i
                    |> injectExpr t)
            |> Array.ofSeq
            |> invokeCtorExpr ctor.Ctor

        let append = blockBody.Add
        let appendMany = blockBody.AddRange

        append <| Expression.Assign(buildUpResult, createObject)

        //Perform method injection
        injectionMethods
        |> Seq.mapi (fun i m -> Expression.Call( buildUpResult,
                                                 m,
                                                 injectionMethodTypes.[i]
                                                 |> Seq.mapi(fun ii t -> targetTypeMethodParams
                                                                         |> accessArrayExpr i
                                                                         |> accessArrayExpr ii
                                                                         |> injectExpr t)) :> Expression)
        |> appendMany

        //Perform property injection
        injectionProperties
        |> Seq.mapi (fun i p -> Expression.Assign( Expression.Property(buildUpResult, p),
                                                   targetTypePropertyParams
                                                   |> accessArrayExpr i
                                                   |> injectExpr p.PropertyType) :> Expression)
        |> appendMany

        //Add the return value
        append <| buildUpResult

        //Create full body
        let buildUpBody = Expression.Block(typeof<Object>, [|buildUpResult|], blockBody)

        //Create the final function and partially apply it with the type parameters.
        let innerFuncExpr = 
            Expression.Lambda<Func<Type, Type[],Type[],Type[][], BuildInstanceFunc, Object>>( 
                buildUpBody, 
                typeToCreateParam, 
                ctorParamsParam, 
                targetTypePropertyParams,
                targetTypeMethodParams,
                paramBuilderParam)

        let innerFunc = innerFuncExpr.Compile()
        (fun (paramBuilder : BuildInstanceFunc) -> innerFunc.Invoke(targetType, parameterTypes,propertyTypes, injectionMethodTypes ,paramBuilder))
    
    let tryGetFactoryFunc (targetType : Type) =
        
        match factoryFuncMap.TryGetValue(targetType) with
        | true,  f -> 
            Some(f)
        | false, _ ->
            match tryGetSuitableConstructor targetType with
            | Some(ctorCache) -> 
                factoryFuncMap.GetOrAdd(
                    targetType, 
                    Func<Type,(BuildInstanceFunc)->Object>(fun t -> ctorCache |> compileFactoryFunc t)) 
                |> Some
            | None -> 
                None
    
    interface ITypeResolver with
        member __.Resolve paramBuilder t =
            match tryGetFactoryFunc t with
            | Some(f) -> 
                paramBuilder 
                |> f 
                |> Option.Some 
            | None -> 
                None