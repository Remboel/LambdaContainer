module LambdaContainer.Core.Tests.BuildPlannerTest
open LambdaContainer.Core.Container
open Xunit

let private ``Should be the selected plan given inputs for acceptNone and withName`` acceptNone withName expectedPlan =
    Assert.Equal(expectedPlan, BuildPlanner.createBuildProcedure withName acceptNone)

[<Fact>]
let ``Validate Plan For NoName And Do Not Accept None``() =
    ResolveFromRepository(
        ResolveAllIfArrayIsRequested(
            TryDynamicResolution(
                Terminate(
                    MissingRegistration)))) |> BuildProcedure
    |>
    ``Should be the selected plan given inputs for acceptNone and withName`` false false

[<Fact>]
let ``Validate Plan For NoName And Accept None``() =
    ResolveFromRepository(
        ResolveAllIfArrayIsRequested(
            TryDynamicResolution(
                Terminate(
                    GiveUp)))) |> BuildProcedure
    |>
    ``Should be the selected plan given inputs for acceptNone and withName`` true false

[<Fact>]
let ``Validate Plan For WithName And Do Not Accept None``() =
    ResolveFromRepository(
        Terminate(
            MissingRegistration)) |> BuildProcedure
    |>
    ``Should be the selected plan given inputs for acceptNone and withName`` false true

[<Fact>]
let ``Validate Plan For WithName And Accept None``() =
    ResolveFromRepository(
        Terminate(
            GiveUp)) |> BuildProcedure
    |>
    ``Should be the selected plan given inputs for acceptNone and withName`` true true

