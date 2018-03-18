module LambdaContainer.Core.Tests.BuildPlannerTest
open NUnit.Framework
open LambdaContainer.Core.Container
open FsUnit

let ``Should be the selected plan given inputs for acceptNone and withName`` acceptNone withName expectedPlan =
    BuildPlanner.createBuildProcedure withName acceptNone |> should equal expectedPlan

[<Test>]
let ``Validate Plan For NoName And Do Not Accept None``() =
    ResolveFromRepository(
        ResolveAllIfArrayIsRequested(
            TryDynamicResolution(
                Terminate(
                    MissingRegistration)))) |> BuildProcedure
    |>
    ``Should be the selected plan given inputs for acceptNone and withName`` false false

[<Test>]
let ``Validate Plan For NoName And Accept None``() =
    ResolveFromRepository(
        ResolveAllIfArrayIsRequested(
            TryDynamicResolution(
                Terminate(
                    GiveUp)))) |> BuildProcedure
    |>
    ``Should be the selected plan given inputs for acceptNone and withName`` true false

[<Test>]
let ``Validate Plan For WithName And Do Not Accept None``() =
    ResolveFromRepository(
        Terminate(
            MissingRegistration)) |> BuildProcedure
    |>
    ``Should be the selected plan given inputs for acceptNone and withName`` false true

[<Test>]
let ``Validate Plan For WithName And Accept None``() =
    ResolveFromRepository(
        Terminate(
            GiveUp)) |> BuildProcedure
    |>
    ``Should be the selected plan given inputs for acceptNone and withName`` true true

