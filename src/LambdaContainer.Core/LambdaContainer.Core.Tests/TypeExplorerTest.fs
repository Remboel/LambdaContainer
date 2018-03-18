module LambdaContainer.Core.Tests.TypeExplorerTest
open LambdaContainer.Core.NetFrameworkEx
open System.Collections.Generic
open NUnit.Framework
open FsUnit

[<Test>]
let ``TypeExplorer Can Get Property From Expression``() =
    TypeExplorer.getProperty<List<string>,int> <@ fun x -> x.Capacity @> |> (fun x -> x.Name) |> should equal "Capacity"


[<Test>]
let ``TypeExplorer Can Get Method From Expression``() =
    TypeExplorer.getMethod<List<string>,unit> <@ fun x -> x.Add("") @> |> (fun x -> x.Name) |> should equal "Add"

[<Test>]
let ``TypeExplorer Can Get StaticMethod From Expression``() =
    TypeExplorer.getStaticMethod <@ Assert.That(1, Is.EqualTo 1) @> |> (fun x -> x.Name) |> should equal "That"
