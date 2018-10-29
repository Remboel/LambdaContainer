module LambdaContainer.Core.Tests.DynamicTypeResolverTests
open System
open NSubstitute
open LambdaContainer.Core.Contracts
open LambdaContainer.Core.TypeResolvers
open LambdaContainer.Core.Tests.TestUtilities
open Xunit

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

[<Fact>]
let ``Can Construct``() =
    Assert.NotNull(createSut())

module ConstructorInjectionTests =
    let createSimpleParameterBuilder<'a> (result : 'a) =
        let pb = mock<System.Type -> Object>()
        let injectedType = typeof<'a>
        (pb injectedType).Returns(result) |> ignore
        pb

    [<Fact>]
    let ``Can Get Instance Of Specific Type And Dependency Inject Ctor Args``() =
        //Arrange
        let sut = createSut()

        //Act
        let resolvedInstance = createSimpleParameterBuilder "test" |> resolveFrom<TestType> sut

        //Assert
        Assert.Equal(Some "test", resolvedInstance|> Option.map (fun x -> x.GetInjectedParam()))

    [<Fact>]
    let ``Can Get Instance Of Specific Type Always Selects Ctor With Most Args If No Marked Exists``() =
        //Arrange
        let sut = createSut()

        //Act
        let resolvedInstance = createSimpleParameterBuilder "test" |> resolveFrom<TestType2> sut

        //Assert
        Assert.Equal(Some "test", resolvedInstance|> Option.map (fun x -> x.GetInjectedParam()))

    [<Fact>]
    let ``Can Get Instance Of Specific Type Always Skips Ctor With Primitive Args``() =
        //Arrange
        let sut = createSut()

        //Act - should throw exception if incorrect ctor is used
        let resolvedInstance = createSimpleParameterBuilder "test" |> resolveFrom<TestType4> sut

        //Assert
        Assert.Equal(Some "test", resolvedInstance|> Option.map (fun x -> x.GetInjectedParam()))

    [<Fact>]
    let ``Can Get Instance Of Specific Type Always Selects Marked Ctor``() =
        //Arrange
        let sut = createSut()

        //Act
        let resolvedInstance = createSimpleParameterBuilder "" |> resolveFrom<TestType3> sut

        //Assert
        Assert.True(resolvedInstance.IsSome)
        Assert.True(resolvedInstance.Value.WasCreatedByInjectionCtor())

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

    [<Fact>]
    let ``Can Perform Method Injection``() =
        //Arrange
        let sut = createSut()

        //Act
        let resolvedInstance = parameterBuilder |> resolveFrom<MethodInjectionTestType> sut

        //Assert
        Assert.True(resolvedInstance.IsSome)
        Assert.NotEqual(null, resolvedInstance.Value.GetInjectedParams() |> fst)
        Assert.Equal("test", resolvedInstance.Value.GetInjectedParams() |> snd)

    [<Fact>]
    let ``Does Not Method Inject Unless Marked``() =
        //Arrange
        let sut = createSut()

        //Act
        let resolvedInstance = parameterBuilder |> resolveFrom<MethodInjectionTestType> sut

        //Assert
        Assert.True(resolvedInstance.IsSome)
        Assert.Null(resolvedInstance.Value.ShouldNotBeInjected())

module PropertyInjectionTests =
    let parameterBuilder =
        (fun t ->   
            if t = typeof<TestType> 
                then 
                    new TestType("")  :> Object
            else 
                failwith "Unexpected dependency type")

    [<Fact>]
    let ``Can Perform Property Injection``() =
        //Arrange
        let sut = createSut()

        //Act
        let resolvedInstance = parameterBuilder |> resolveFrom<PropertyInjectionTestType> sut
        
        //Assert
        Assert.True(resolvedInstance.IsSome)
        Assert.NotNull(resolvedInstance.Value.Injected)


    [<Fact>]
    let ``Does Not Property Inject Unless Marked``() =
        //Arrange
        let sut = createSut()

        //Act
        let resolvedInstance = parameterBuilder |> resolveFrom<PropertyInjectionTestType> sut

        //Assert
        Assert.True(resolvedInstance.IsSome)
        Assert.Null(resolvedInstance.Value.ShouldNotBeInjected)