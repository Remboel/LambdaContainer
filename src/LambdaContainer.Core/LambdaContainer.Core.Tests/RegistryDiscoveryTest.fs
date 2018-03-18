module LambdaContainer.Core.Tests.RegistryDiscovery
open System
open NUnit.Framework
open LambdaContainer.Core.Contracts
open LambdaContainer.Core.Setup
open LambdaContainer.Core.BootTests
open FsUnit
open System.Collections.Generic

let assertRegistryTypeLoaded (registries : Object seq) t=
    registries |> Seq.exists (fun r -> r.GetType().Equals(t)) |> should be True

[<Test>]
let ``Can identify all registries found in resources projects``() =
    //Arrange
    let results = List<Object>()
    
    //Act
    RegistryDiscovery.discoverRegistries<ILambdaContainerRegistry>
        AppDomain.CurrentDomain.BaseDirectory
        (fun info -> info.Name.StartsWith "LambdaContainer.Core.BootTests.Provider")
        (results.Add >> ignore)

    //Assert
    [   typeof<Provider2.Tests.Provider1>
        typeof<Provider2.Tests.Provider2>
        typeof<Provider1.Tests.Provider1>
        typeof<Provider2.Tests.Provider3> ]
    |> Seq.iter (assertRegistryTypeLoaded results)
