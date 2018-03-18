module LambdaContainer.Core.Tests.InstanceFactoriesTest
open System
open LambdaContainer.Core.Contracts
open NUnit.Framework
open LambdaContainer.Core.FactoryContracts
open LambdaContainer.Core.InstanceFactories
open LambdaContainer.Core.Tests.TestUtilities
open LambdaContainer.Core.Tests.TestUtilities.Identity
open FsUnit
open System.Threading.Tasks

type DisposableTestObject() =
    let mutable isDisposed = false

    member __.IsDisposed() = 
        isDisposed

    interface IDisposable with
        member __.Dispose() = 
            isDisposed <- true

let container = mock<ILambdaContainer>()

let private invokeAsync (factory : IInstanceFactory) = 
    Task.Run(fun () -> factory.Invoke container) |> Async.AwaitTask

let private invokeManyAsync howManyTimes (factory : IInstanceFactory) = 
        Task.Run(
            fun () ->
                [1 .. howManyTimes]
                |> List.map (fun _ -> factory.Invoke container)) 
        |> Async.AwaitTask
    

let private invokeInParallelAsync factories =
    factories
    |> List.map invokeAsync
    |> Async.Parallel
    |> Async.RunSynchronously

let private constructSutWithRandomStringFactory(lifetime : OutputLifetime) =
    let factory = 
        (fun (_ : ILambdaContainer) -> 
            System.Guid.NewGuid().ToString() :> System.Object)
    
    let identity = simpleIdentity<string>()

    match lifetime with
    | OutputLifetime.Transient -> 
        new InstanceFactoryForTransientProducts(factory,identity) :> IInstanceFactory
    | OutputLifetime.ThreadSingleton -> 
        new InstanceFactoryThreadSingletonProducts(factory,identity) :> IInstanceFactory
    | OutputLifetime.Singleton -> 
        new InstanceFactorySingletonProducts(factory,identity) :> IInstanceFactory
    | _ -> failwith "unknown lifetume"

module InstanceCreationTests =
    let shouldAllBeEqual (inputList : obj list) =
        inputList |> List.forall (fun x -> x.Equals(inputList.[0])) |> should be True

    [<Test>]
    let ``Can Construct Instance Factory ForTransient Products``() =
        //Arrange
        let factory = (fun (_ : ILambdaContainer) -> "test" :> System.Object)
        let identity = simpleIdentity<string>()

        //Act
        let sut = new InstanceFactoryForTransientProducts(factory, identity) :> IInstanceFactory

        //Assert
        sut.GetIdentity() |> should equal identity
        sut.Invoke container |> should equal "test"

    [<Test>]
    let ``Can Get InstanceFactory For Transient Outputs``() =
        //Arrange
        let sut = constructSutWithRandomStringFactory(OutputLifetime.Transient)

        //Act
        let res1 = sut.Invoke container
        let res2 = sut.Invoke container

        //Assert
        res1 |> should not' (equal res2)
    
    [<Test>]
    let ``Can Get InstanceFactory For Application Singletons``() =
        //Arrange
        let sut = constructSutWithRandomStringFactory(OutputLifetime.Singleton)

        //Act
        let results = sut.Invoke container :: ([sut ; sut] |> invokeInParallelAsync |> List.ofArray)

        //Assert
        results |> shouldAllBeEqual

    [<Test>]
    let ``Can Get InstanceFactory For Thread Singletons``() =
        //Arrange
        let sut = constructSutWithRandomStringFactory(OutputLifetime.ThreadSingleton)

        //Act
        let results = 
            [sut; sut]
            |> List.map (invokeManyAsync 2)
            |> Async.Parallel
            |> Async.RunSynchronously

        //Assert
        System.Linq.Enumerable.Intersect(first = results.[0], second = results.[1]) |> Seq.length |> should equal 0
        results |> Array.iter (fun x -> x |> shouldAllBeEqual)

module CloneTests =
    let private doTestClone<'a when 'a :> IInstanceFactory> lifetime returnsSelf =
        //Arrange
        let sut = constructSutWithRandomStringFactory(lifetime)
        let originalResult = sut.Invoke container

        //Act
        let clone = sut.CreateSubScope()

        //Assert
        clone |> should not' (be Null)
        clone = sut |> should equal returnsSelf
        clone.GetType() |> should equal typeof<'a>
        clone.Invoke container |> should not' (equal originalResult)

    [<Test>]
    let ``Can Clone TransientOutputFactory``() = 
        doTestClone<InstanceFactoryForTransientProducts> OutputLifetime.Transient true

    [<Test>]
    let ``Can Clone ThreadSingletonOutputFactory``() = 
        doTestClone<InstanceFactoryThreadSingletonProducts> OutputLifetime.ThreadSingleton false

    [<Test>]
    let ``Can Clone SingletonOutputFactory``() = 
        doTestClone<InstanceFactorySingletonProducts> OutputLifetime.Singleton false

module DisposalTests =
    let shouldHaveBeenDisposed (candidate : obj) =
        (candidate :?> DisposableTestObject).IsDisposed() |> should be True

    let disposableTestObjectFactory = (fun _ -> new DisposableTestObject() :> Object)

    [<Test>]
    let ``Can Dispose ThreadSingletonOutputFactory``() =
        //Arrange
        let id = simpleIdentity<DisposableTestObject>()
        let fac = new InstanceFactoryThreadSingletonProducts(disposableTestObjectFactory, id) :> IInstanceFactory

        let createdResults = [fac; fac] |> invokeInParallelAsync

        //Act
        fac.Dispose()

        //Assert
        createdResults |> Array.iter shouldHaveBeenDisposed

    [<Test>]
    let ``Can Dispose SingletonOutputFactory``() =
        //Arrange
        let id = simpleIdentity<DisposableTestObject>()
        let fac = new InstanceFactorySingletonProducts(disposableTestObjectFactory, id) :> IInstanceFactory

        let res = fac.Invoke container

        //Act
        fac.Dispose()

        //Assert
        res |> shouldHaveBeenDisposed
