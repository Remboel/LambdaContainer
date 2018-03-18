module LambdaContainer.Core.Tests.DynamicTypeResolverTests
open System
open NSubstitute
open NUnit.Framework
open LambdaContainer.Core.Contracts
open LambdaContainer.Core.TypeResolvers
open LambdaContainer.Core.Tests.TestUtilities
open FsUnit

[<AllowNullLiteral>]
type public TestType(injectedParam : string) =
    member __.GetInjectedParam() = injectedParam

[<AllowNullLiteral>]
type public TestType2(injectedParam : string) =
    member __.GetInjectedParam() = injectedParam
    public new() = TestType2("FromDefaultCtor")


[<AllowNullLiteral>]
type public TestType3 (calledByInjectionCtor : bool) =
    member __.WasCreatedByInjectionCtor() = calledByInjectionCtor
    
    [<LambdaContainerInjectionConstructor>]
    public new() = TestType3(true)


[<AllowNullLiteral>]
type public TestType4 (afriend :string) =
    member __.GetInjectedParam() = afriend
    
    public new(afriend :string, _ : int) = 
        TestType4(afriend)
        then
            failwith "I should not have been chosen as injection ctor because I have a primitive param"

[<AllowNullLiteral>]
type public MethodInjectionTestType() =
    let mutable _t1 = null
    let mutable _t2 = null
    let mutable _t3 = null

    member __.GetInjectedParams() = (_t1,_t2)
    member __.ShouldNotBeInjected() = _t3

    [<LambdaContainerInjection>]
    member __.Inject (t1 : TestType) (t2 : string) =
        _t1 <- t1
        _t2 <- t2

    [<LambdaContainerInjection>]
    member private __.NopeInject1 (t2 : string) = 
        failwith "I should not have been called - I am not public"

    [<LambdaContainerInjection>]
    member internal __.NopeInject2 (t2 : string) = 
        failwith "I should not have been called - I am not public"

    member __.InjectNoNo (t3 : TestType)=
        _t3 <- t3

[<AllowNullLiteral>]
type public PropertyInjectionTestType() =
    let mutable _t1 = null
    let mutable _t2 = null
    
    [<LambdaContainerInjection>]
    member __.Injected 
        with get () = 
            _t1
    member __.Injected 
        with set (newVal : TestType) = 
            _t1 <- newVal

    member __.ShouldNotBeInjected 
        with get () = 
            _t2
    member __.ShouldNotBeInjected 
        with set (newVal : TestType) = 
            _t2 <- newVal

    [<LambdaContainerInjection>]
    member private __.ShouldNotBeInjected1 
        with set (newVal : TestType) = 
            failwith "I should not have been called - I am not public"

    [<LambdaContainerInjection>]
    member internal __.ShouldNotBeInjected2 
        with set (newVal : TestType) = 
            failwith "I should not have been called - I am not public"

let private createSut() = 
    new DynamicTypeResolver() :> ITypeResolver

let private resolveFrom<'a when 'a:null> (sut : ITypeResolver) paramBuilder =
    ((sut.Resolve paramBuilder typeof<'a>) |> Option.get :?> 'a) |> Option.ofObj

[<Test>]
let ``Can Construct``() =
    Assert.DoesNotThrow(fun () -> createSut() |> ignore)

module ConstructorInjectionTests =
    let createSimpleParameterBuilder<'a> (result : 'a) =
        let pb = mock<System.Type -> Object>()
        let injectedType = typeof<'a>
        (pb injectedType).Returns(result) |> ignore
        pb

    [<Test>]
    let ``Can Get Instance Of Specific Type And Dependency Inject Ctor Args``() =
        //Arrange
        let sut = createSut()

        //Act
        let resolvedInstance = createSimpleParameterBuilder "test" |> resolveFrom<TestType> sut

        //Assert
        resolvedInstance|> Option.map (fun x -> x.GetInjectedParam()) |> should equal (Some "test")

    [<Test>]
    let ``Can Get Instance Of Specific Type Always Selects Ctor With Most Args If No Marked Exists``() =
        //Arrange
        let sut = createSut()

        //Act
        let resolvedInstance = createSimpleParameterBuilder "test" |> resolveFrom<TestType2> sut

        //Assert
        resolvedInstance|> Option.map (fun x -> x.GetInjectedParam()) |> should equal (Some "test")

    [<Test>]
    let ``Can Get Instance Of Specific Type Always Skips Ctor With Primitive Args``() =
        //Arrange
        let sut = createSut()

        //Act - should throw exception if incorrect ctor is used
        let resolvedInstance = createSimpleParameterBuilder "test" |> resolveFrom<TestType4> sut

        //Assert
        resolvedInstance |> Option.map (fun x -> x.GetInjectedParam()) |> should equal (Some "test")

    [<Test>]
    let ``Can Get Instance Of Specific Type Always Selects Marked Ctor``() =
        //Arrange
        let sut = createSut()

        //Act
        let resolvedInstance = createSimpleParameterBuilder "" |> resolveFrom<TestType3> sut

        //Assert
        resolvedInstance.IsSome |> should be True
        resolvedInstance.Value.WasCreatedByInjectionCtor() |> should be True

module MethodInjectionTests =

    let parameterBuilder =
        (fun t ->   
            if t = typeof<string> 
                then 
                    "test" :> Object
            elif t = typeof<TestType> 
                then 
                    new TestType("")  :> Object
            else 
                failwith "Unexpected dependency type")

    [<Test>]
    let ``Can Perform Method Injection``() =
        //Arrange
        let sut = createSut()

        //Act
        let resolvedInstance = parameterBuilder |> resolveFrom<MethodInjectionTestType> sut

        //Assert
        resolvedInstance.IsSome |> should be True
        resolvedInstance.Value.GetInjectedParams() |> fst |> should not' (be Null)
        resolvedInstance.Value.GetInjectedParams() |> snd |> should equal "test"

    [<Test>]
    let ``Does Not Method Inject Unless Marked``() =
        //Arrange
        let sut = createSut()

        //Act
        let resolvedInstance = parameterBuilder |> resolveFrom<MethodInjectionTestType> sut

        //Assert
        resolvedInstance.IsSome |> should be True
        resolvedInstance.Value.ShouldNotBeInjected() |> should be Null

module PropertyInjectionTests =
    let parameterBuilder =
        (fun t ->   
            if t = typeof<TestType> 
                then 
                    new TestType("")  :> Object
            else 
                failwith "Unexpected dependency type")

    [<Test>]
    let ``Can Perform Property Injection``() =
        //Arrange
        let sut = createSut()

        //Act
        let resolvedInstance = parameterBuilder |> resolveFrom<PropertyInjectionTestType> sut
        
        //Assert
        resolvedInstance.IsSome |> should be True
        resolvedInstance.Value.Injected |> should not' (be Null)


    [<Test>]
    let ``Does Not Property Inject Unless Marked``() =
        //Arrange
        let sut = createSut()

        //Act
        let resolvedInstance = parameterBuilder |> resolveFrom<PropertyInjectionTestType> sut

        //Assert
        resolvedInstance.IsSome |> should be True
        resolvedInstance.Value.ShouldNotBeInjected |> should be Null