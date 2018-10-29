module internal LambdaContainer.Core.Tests.TestUtilities
open System
open Fasterflect
open LambdaContainer.Core.Contracts
open LambdaContainer.Core.FactoryContracts
open LambdaContainer.Core.InstanceFactories
open NSubstitute

type ITestDisposableInstanceFactory =
    inherit IInstanceFactory
    inherit IDisposable

type ITestType = interface end

type ITestAction = interface end

type TestTypeImpl() = 
    class
        interface ITestType
    end

type TestClosedType = class end

[<RequireQualifiedAccess>]
module FieldSpy =
    let getField<'a> target fieldName =
        match target.GetFieldValue fieldName with
        | :? 'a as v -> v
        | null -> failwith "No field with that name"
        | _ -> failwith "Invalid field type"

let mock<'a when 'a:not struct>() = 
    Substitute.For<'a>()

module Identity =
    let simpleIdentity<'a> ()=
        new FactoryIdentity(None,typeof<string>,None)

    let namedIdentity<'a> name =
        new FactoryIdentity(Some name,typeof<string>,None)