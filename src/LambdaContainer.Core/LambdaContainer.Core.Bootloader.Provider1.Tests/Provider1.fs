namespace LambdaContainer.Core.BootTests.Provider1.Tests
open LambdaContainer.Core.Contracts

type public Provider1() =
    interface ILambdaContainerRegistry with
        member this.WriteContentsTo recorder =
            let typeName = typeof<Provider1>.FullName
            recorder.Record<IFactoryRegistrations>(fun builder -> builder.Build().RegisterByName( (fun _ -> typeName), typeName) |> ignore) 
            |> ignore
