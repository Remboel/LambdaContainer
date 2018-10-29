module LambdaContainer.Core.Tests.TypeExplorerTest
open LambdaContainer.Core.NetFrameworkEx
open System.Collections.Generic
open Xunit

[<Fact>]
let ``TypeExplorer Can Get Property From Expression``() =
    Assert.Equal("Capacity", TypeExplorer.getProperty<List<string>,int> <@ fun x -> x.Capacity @> |> (fun x -> x.Name))


[<Fact>]
let ``TypeExplorer Can Get Method From Expression``() =
    Assert.Equal("Add", TypeExplorer.getMethod<List<string>,unit> <@ fun x -> x.Add("") @> |> (fun x -> x.Name))

[<Fact>]
let ``TypeExplorer Can Get StaticMethod From Expression``() =
    Assert.Equal("Equal",TypeExplorer.getStaticMethod <@ Assert.Equal(1, 1) @> |> (fun x -> x.Name))