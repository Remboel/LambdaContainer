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

namespace LambdaContainer.Core.ReflectionEx
open System
open System.Reflection
open LambdaContainer.Core.Contracts
open LambdaContainer.Core.NetFrameworkEx

module internal PrimitiveReflection =
    let internal canBeInstantiated (targetType : Type) =
        (not targetType.IsAbstract && targetType.IsClass && not targetType.IsArray)
    
    let internal isPublic (m : MethodBase) = 
        m.IsPublic
    
    let internal isWritable (p : PropertyInfo) = 
        p.CanWrite
    
    let internal extractSetter (p : PropertyInfo) = 
        p.GetSetMethod(true)

    let internal isPrimitiveType (p : ParameterInfo) = 
        p.ParameterType.IsPrimitive

    let internal hasNoPrimitiveParameters (m : MethodBase) =
        m.GetParameters()
        |> Seq.ofArray
        |> Seq.where isPrimitiveType
        |> Seq.length
        |> (=) 0

module internal InjectionTargetReflector =
    open PrimitiveReflection

    type internal TypeCtorCache = { Ctor : ConstructorInfo; Parameters : ParameterInfo array}

    let private isMarkedAsInjectionConstructor (ctor:ConstructorInfo) =
        ctor.GetCustomAttributes() |> Seq.exists (fun attr -> attr :? LambdaContainerInjectionConstructorAttribute)

    let private isMarkedAsInjectionMethod (m:MethodInfo) =
        m.GetCustomAttributes(true) |> Seq.exists (fun attr -> attr :? LambdaContainerInjectionAttribute)

    let private isMarkedAsInjectionProperty (p:PropertyInfo) =
        p.GetCustomAttributes(true) |> Seq.exists (fun attr -> attr :? LambdaContainerInjectionAttribute)

    let private isAcceptableForInjection m = (isPublic m) && (hasNoPrimitiveParameters m)

    let selectInjectionMethods :(MethodInfo seq -> MethodInfo seq) = 
        Seq.where isMarkedAsInjectionMethod 
        >> Seq.where isAcceptableForInjection

    let selectInjectionProperties : (PropertyInfo seq -> PropertyInfo seq)=
        Seq.where isMarkedAsInjectionProperty
        >> Seq.where isWritable 
        >> Seq.where (extractSetter >> isAcceptableForInjection)

    let tryGetSuitableConstructor (targetType : Type) =
        match targetType |> canBeInstantiated with
        | false -> 
            None
        | true -> 
            match (targetType.GetConstructors()
                    |> Seq.where isPublic
                    |> Seq.where hasNoPrimitiveParameters
                    |> Seq.sortBy (fun (ctor : ConstructorInfo) -> 
                                    match ctor |> isMarkedAsInjectionConstructor with
                                    | true -> 
                                        System.Int32.MaxValue
                                    | false -> 
                                        ctor.GetParameters().Length)
                   |> Seq.tryLast) with
            | None -> 
                 None
            | Some(ctor) -> 
                 Some({Ctor = ctor; Parameters = ctor.GetParameters()})
    
