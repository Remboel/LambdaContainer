module LambdaContainer.Core.Tests.LambdaContainerIntegrationTest
open System
open LambdaContainer.Core.Contracts
open LambdaContainer.Core.Container
open LambdaContainer.Core.Setup
open System.Threading
open LambdaContainer.Core.BootTests
open Xunit

let createBootstrapper() = LambdaContainerBootstrapper
                            .Create()
                            .ConfigureAssemblyScanner(
                                fun (config : LambdaContainerAssemblyScannerConfiguration) -> config.RegistryFileNameCondition <- config.RegistryFileNameCondition)

let createSut() =
    createBootstrapper().Run()

[<Fact>]
let ``Can Build Lambda Container``() =
    Assert.NotNull(createSut())

[<Fact>]
let ``Can Get Instance Provided By FsharpFactoryProvider Client``() =
    //Arrange
    let container = createSut()
    let name = typeof<Provider1.Tests.Provider1>.FullName

    //Act + Assert
    Assert.Equal(name,container.GetInstanceByName<string>(name))

[<Fact>]
let ``Can Get Instance Provided By FactoryProvider Client``() =
    //Arrange
    let container = createSut()
    let name = typeof<Provider2.Tests.Provider1>.FullName

    //Act + Assert
    Assert.Equal(name,container.GetInstanceByName<string>(name))

[<Fact>]
let ``Can Get Instance Provided By TypeRegistration Client``() =
    //Arrange
    let container = createSut()

    //Act
    let res = container.GetInstance<Provider2.Tests.IAclass>()

    //Assert
    Assert.NotNull(res.ChildAutoInjected)

[<Fact>]
let ``Can Get Instance Provided By TypeRegistration As Application Singleton Client``() =
    //Arrange
    let container = createSut()

    //Act
    let res1 = container.GetInstance<Provider2.Tests.IAchild2>()
    let res2 = Tasks.Task.Run(fun () -> container.GetInstance<Provider2.Tests.IAchild2>()).GetAwaiter().GetResult()

    //Assert
    Assert.Same(res1,res2)

[<Fact>]
let ``Can Get Instance Provided By TypeRegistration Client And Inject All Of A Type In Ctor``() =
    //Arrange
    let container = createSut()

    //Act
    let res = container.GetInstance<Provider2.Tests.ContructorInjectAllOfType>()

    //Assert
    Assert.NotNull(res.Variants)
    Assert.Equal(2,res.Variants.Length)

[<Fact>]
let ``IsTypeRegistered Returns True``() =
    //Arrange
    let container = createSut()
    
    //Act
    let res1 = container.IsRegistered<Provider2.Tests.IAchild2>()
    let res2 = container.IsTypeRegistered typeof<Provider2.Tests.IAchild2>

    //Assert
    Assert.Equal((true,true),(res1,res2))

[<Fact>]
let ``IsTypeRegistered Returns False``() =
    //Arrange
    let container = createSut()
    
    //Act
    let res1 = container.IsRegistered<Provider2.Tests.AChild>()
    let res2 = container.IsTypeRegistered typeof<Provider2.Tests.AChild>

    //Assert
    Assert.Equal((false,false),(res1,res2))

[<Fact>]
let ``IsTypeRegisteredByName Returns True``() =
    //Arrange
    let name = typeof<Provider1.Tests.Provider1>.FullName
    let container = createSut()
    
    //Act
    let res1 = container.IsRegisteredByName<string>(name)
    let res2 = container.IsTypeRegisteredByName typeof<string> name

    //Assert
    Assert.Equal((true,true),(res1,res2))

[<Fact>]
let ``IsTypeRegisteredByName Returns False``() =
    //Arrange
    let container = createSut()
    
    //Act
    let res1 = container.IsRegisteredByName<string>(Guid.NewGuid().ToString())
    let res2 = container.IsTypeRegisteredByName typeof<string> (Guid.NewGuid().ToString())

    //Assert
    Assert.Equal((false,false),(res1,res2))

[<Fact>]
let ``Can Resolve The Container``() =
    //Arrange
    let container = createSut()
    
    //Act + Assert
    Assert.Same(container,container.GetInstance<ILambdaContainer>())

[<Fact>]
let ``Can Perform Property Injection``() =
    //Arrange
    let container = createSut()

    //Act
    let res = container.GetInstance<Provider2.Tests.PropertyInjectionTestType>()

    //Assert
    Assert.NotNull(res.InjectedChild1)
    Assert.NotNull(res.InjectedChild2)

[<Fact>]
let ``Can Perform Method Injection``() =
    //Arrange
    let container = createSut()

    //Act
    let res = container.GetInstance<Provider2.Tests.MethodInjectionTestType>()

    //Assert
    Assert.NotNull(res.GetInjectedTypeThatHadPropertyInjection())
    Assert.NotNull(res.GetInjectedTypeThatHadPropertyInjection().InjectedChild1)
    Assert.NotNull(res.GetInjectedTypeThatHadPropertyInjection().InjectedChild2)

[<Fact>]
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
    Assert.NotNull(res1)
    Assert.NotNull(res2)
    Assert.Equal((typeof<Provider2.Tests.AChild>, typeof<Provider2.Tests.AClass>),(res1.GetType(), res2.GetType()))

[<Fact>]
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
    Assert.Equal("Hello",res)

[<Fact>]
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
    Assert.Equal(originalValue, original)
    Assert.Equal(scopedValue, scoped)

[<Fact>]
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
    Assert.Equal(valueOfNotOverridden, notOverridden)

[<Fact>]
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
    Assert.Equal(originalValue,original)
    Assert.Equal(scopedValue,scoped)