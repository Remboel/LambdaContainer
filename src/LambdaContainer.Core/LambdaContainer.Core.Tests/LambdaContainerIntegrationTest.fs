module LambdaContainer.Core.Tests.LambdaContainerIntegrationTest
open System
open NUnit.Framework
open LambdaContainer.Core.Contracts
open LambdaContainer.Core.Container
open LambdaContainer.Core.Setup
open System.Threading
open LambdaContainer.Core.BootTests
open FsUnit

let createBootstrapper() = LambdaContainerBootstrapper
                            .Create()
                            .ConfigureAssemblyScanner(
                                fun (config : LambdaContainerAssemblyScannerConfiguration) -> config.RegistryFileNameCondition <- config.RegistryFileNameCondition)

let createSut() =
    createBootstrapper().Run()

[<Test>]
let ``Can Build Lambda Container``() =
    Assert.DoesNotThrow(fun () -> createSut() |> ignore)

[<Test>]
let ``Can Get Instance Provided By FsharpFactoryProvider Client``() =
    //Arrange
    let container = createSut()
    let name = typeof<Provider1.Tests.Provider1>.FullName

    //Act + Assert
    container.GetInstanceByName<string>(name) |> should equal name

[<Test>]
let ``Can Get Instance Provided By FactoryProvider Client``() =
    //Arrange
    let container = createSut()
    let name = typeof<Provider2.Tests.Provider1>.FullName

    //Act + Assert
    container.GetInstanceByName<string>(name) |> should equal name

[<Test>]
let ``Can Get Instance Provided By TypeRegistration Client``() =
    //Arrange
    let container = createSut()

    //Act
    let res = container.GetInstance<Provider2.Tests.IAclass>()

    //Assert
    res.ChildAutoInjected |> should not' (be Null)

[<Test>]
let ``Can Get Instance Provided By TypeRegistration As Application Singleton Client``() =
    //Arrange
    let container = createSut()

    //Act
    let res1 = container.GetInstance<Provider2.Tests.IAchild2>()
    let res2 = Tasks.Task.Run(fun () -> container.GetInstance<Provider2.Tests.IAchild2>()).GetAwaiter().GetResult()

    //Assert
    res1 |> should be (sameAs res2)

[<Test>]
let ``Can Get Instance Provided By TypeRegistration Client And Inject All Of A Type In Ctor``() =
    //Arrange
    let container = createSut()

    //Act
    let res = container.GetInstance<Provider2.Tests.ContructorInjectAllOfType>()

    //Assert
    res.Variants |> should not' (be Null)
    res.Variants.Length |> should equal 2

[<Test>]
let ``IsTypeRegistered Returns True``() =
    //Arrange
    let container = createSut()
    
    //Act
    let res1 = container.IsRegistered<Provider2.Tests.IAchild2>()
    let res2 = container.IsTypeRegistered typeof<Provider2.Tests.IAchild2>

    //Assert
    [res1; res2] |> should equal [true; true]

[<Test>]
let ``IsTypeRegistered Returns False``() =
    //Arrange
    let container = createSut()
    
    //Act
    let res1 = container.IsRegistered<Provider2.Tests.AChild>()
    let res2 = container.IsTypeRegistered typeof<Provider2.Tests.AChild>

    //Assert
    [res1; res2] |> should equal [false; false]

[<Test>]
let ``IsTypeRegisteredByName Returns True``() =
    //Arrange
    let name = typeof<Provider1.Tests.Provider1>.FullName
    let container = createSut()
    
    //Act
    let res1 = container.IsRegisteredByName<string>(name)
    let res2 = container.IsTypeRegisteredByName typeof<string> name

    //Assert
    [res1; res2] |> should equal [true; true]

[<Test>]
let ``IsTypeRegisteredByName Returns False``() =
    //Arrange
    let container = createSut()
    
    //Act
    let res1 = container.IsRegisteredByName<string>(Guid.NewGuid().ToString())
    let res2 = container.IsTypeRegisteredByName typeof<string> (Guid.NewGuid().ToString())

    //Assert
    [res1; res2] |> should equal [false; false]

[<Test>]
let ``Can Resolve The Container``() =
    //Arrange
    let container = createSut()
    
    //Act + Assert
    container.GetInstance<ILambdaContainer>() |> should be (sameAs container)

[<Test>]
let ``Can Perform Property Injection``() =
    //Arrange
    let container = createSut()

    //Act
    let res = container.GetInstance<Provider2.Tests.PropertyInjectionTestType>()

    //Assert
    res.InjectedChild1 |> should not' (be Null)
    res.InjectedChild2 |> should not' (be Null)

[<Test>]
let ``Can Perform Method Injection``() =
    //Arrange
    let container = createSut()

    //Act
    let res = container.GetInstance<Provider2.Tests.MethodInjectionTestType>()

    //Assert
    res.GetInjectedTypeThatHadPropertyInjection() |> should not' (be Null)
    res.GetInjectedTypeThatHadPropertyInjection().InjectedChild1 |> should not' (be Null)
    res.GetInjectedTypeThatHadPropertyInjection().InjectedChild2 |> should not' (be Null)

[<Test>]
let ``Can Configure Container With Registration Commands For Type Mappings``() =
    //Arrange
    let bs = createBootstrapper()
                .ConfigureAssemblyScanner(fun c -> c.Enabled <- false)
                .WithRegistrationsFrom(
                    fun r -> r.Record<ITypeMappingRegistrations>(
                                fun c -> c.Build()
                                          .Register<Provider2.Tests.IAchild, Provider2.Tests.AChild>()
                                          .Register<Provider2.Tests.IAclass, Provider2.Tests.AClass>() |> ignore) |> ignore)

    let container = bs.Run()

    //Act
    let res1 = container.GetInstance<Provider2.Tests.IAchild>()
    let res2 = container.GetInstance<Provider2.Tests.IAclass>()

    //Assert
    res1 |> should not' (be Null)
    res2 |> should not' (be Null)
    [res1.GetType() ; res2.GetType()] |> should equal [typeof<Provider2.Tests.AChild> ; typeof<Provider2.Tests.AClass>]

[<Test>]
let ``Can Configure Container With Registration Commands For Factory Mappings``() =
    //Arrange
    let bs = createBootstrapper()
                .ConfigureAssemblyScanner(fun c -> c.Enabled <- false)
                .WithRegistrationsFrom(
                    fun r -> r.Record<IFactoryRegistrations>(
                                fun c -> c.Build().RegisterByName( ((fun _ -> "Hello")), "theNAme") |> ignore) |> ignore)

    let container = bs.Run()

    //Act
    let res = container.GetInstanceByName<string>("theNAme")

    //Assert
    res |> should equal "Hello"

[<Test>]
let ``Can Override Registration In Subscope``() =
    //Arrange
    let name = Guid.NewGuid().ToString()
    let originalValue = "Hello root"
    let scopedValue = "Hello leaf"
    let bs = createBootstrapper()
                .ConfigureAssemblyScanner(fun c -> c.Enabled <- false)
                .WithRegistrationsFrom(
                    fun r -> r.Record<IFactoryRegistrations>(
                                fun c -> c.Build().RegisterByName((fun _ -> originalValue), name) |> ignore) |> ignore)

    let container = bs.Run()
    let subscopedContainer = container.CreateSubScopeWith(
                                        fun r -> r.Record<IFactoryRegistrations>(
                                                    fun fr -> fr.Build().RegisterByName(
                                                                (fun _ -> scopedValue), name) |> ignore) |> ignore)
    
    //Act
    let original = container.GetInstanceByName<string>(name)
    let scoped = subscopedContainer.GetInstanceByName<string>(name)

    //Assert
    original |> should equal originalValue
    scoped |> should equal scopedValue

[<Test>]
let ``GetInstance In Subscope When Not Overridden Resolves In Original Scope``() =
    //Arrange
    let nameOfNotOverridden = Guid.NewGuid().ToString()
    let valueOfNotOverridden = "Hello root 2"
    let bs = createBootstrapper()
                .ConfigureAssemblyScanner(fun c -> c.Enabled <- false)
                .WithRegistrationsFrom(
                    fun r -> r.Record<IFactoryRegistrations>(
                                fun c -> c.Build().RegisterByName((fun _ -> valueOfNotOverridden), nameOfNotOverridden) |> ignore) |> ignore)

    let container = bs.Run()
    let subscopedContainer = container.CreateSubScopeWith(fun _ -> ())
    
    //Act
    let notOverridden = subscopedContainer.GetInstanceByName<string>(nameOfNotOverridden)

    //Assert
    notOverridden |> should equal valueOfNotOverridden

[<Test>]
let ``Can Override Registration In CustomizedResolutionScope``() =
    //Arrange
    let name = Guid.NewGuid().ToString()
    let originalValue = "Hello root"
    let scopedValue = "Hello leaf"
    let bs = createBootstrapper()
                .ConfigureAssemblyScanner(fun c -> c.Enabled <- false)
                .WithRegistrationsFrom(
                    fun r -> r.Record<IFactoryRegistrations>(
                                fun c -> c.Build().RegisterByName((fun _ -> originalValue), name) |> ignore) |> ignore)

    let container = bs.Run()
    let customizedResolution = 
        container.WithCustomizedResolution<IFactoryRegistrations>(
            fun x -> x.RegisterByName((fun _ -> scopedValue), name) |> ignore)
    
    //Act
    let original = container.GetInstanceByName<string>(name)
    let scoped = customizedResolution.GetInstanceByName<string>(name)

    //Assert
    original |> should equal originalValue
    scoped |> should equal scopedValue