module LambdaContainer.Core.Tests.RegistryDiscovery
open System
open LambdaContainer.Core.Contracts
open LambdaContainer.Core.Setup
open LambdaContainer.Core.BootTests
open System.Collections.Generic
open Xunit

let assertRegistryTypeLoaded (registries : Object seq) t=
    Assert.True(registries |> Seq.exists (fun r -> r.GetType().Equals(t)) )

[<Fact>]
let ``Can identify all registries found in resources projects``() =
    //Arrange
    let results = List<Object>()
    
    //Act
    RegistryDiscovery.discoverRegistries<ILambdaContainerRegistry>
        AppDomain.CurrentDomain.BaseDirectory
        (fun info -> info.Name.StartsWith "LambdaContainer.Core.Bootloader")
        (results.Add >> ignore)

    //Assert
    [   typeof<Provider2.Tests.Provider1>
        typeof<Provider2.Tests.Provider2>
        typeof<Provider1.Tests.Provider1>
        typeof<Provider2.Tests.Provider3> ]
    |> Seq.iter (assertRegistryTypeLoaded results)
