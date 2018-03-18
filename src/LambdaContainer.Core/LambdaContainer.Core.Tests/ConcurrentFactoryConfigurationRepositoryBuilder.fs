module LambdaContainer.Core.Tests.ConcurrentFactoryConfigurationRepositoryBuilder
open System
open System.Collections.Generic
open NSubstitute
open NUnit.Framework
open LambdaContainer.Core.Contracts
open LambdaContainer.Core.FactoryContracts
open LambdaContainer.Core.DisposalScopes
open LambdaContainer.Core.RepositoryConstruction
open LambdaContainer.Core.Tests.TestUtilities
open LambdaContainer.Core.Tests.TestUtilities.Identity
open Fasterflect
open FsUnit

let private fakeFactory<'a> (value : 'a) =
    (fun (_ : ILambdaContainer) -> value :> Object)

let simpleResult = "simple"

let simpleFactory = fakeFactory<string> simpleResult

let private simpleId = simpleIdentity<string>()

let private addTo (sut : IFactoryConfigurationRepositoryBuilder) factory = 
    sut.Add factory simpleId OutputLifetime.Transient DisposalScope.Container

let private createSut mode =
    new ConcurrentFactoryConfigurationRepositoryBuilder(mode, None) :> IFactoryConfigurationRepositoryBuilder

[<Test>]
let ``Can construct``() =
    (createSut RegistrationCollisionModes.Fail).Build() |> should not' (equal null)

[<Test>]
let ``Can Construct With Reference Repository``() =
    //Arrange
    let referenceRepository = mock<IFactoryConfigurationRepository>()
    let referenceRegistrations = Dictionary<Type,IReadOnlyDictionary<FactoryIdentity,IInstanceFactory>>()
    let inner1 = Dictionary<FactoryIdentity,IInstanceFactory>()
    let inner2 = Dictionary<FactoryIdentity,IInstanceFactory>()
    inner1.Add(simpleIdentity<string>() , mock<IInstanceFactory>())
    inner2.Add(simpleIdentity<int>() ,mock<IInstanceFactory>())
    referenceRegistrations.Add(typeof<string>,inner1)
    referenceRegistrations.Add(typeof<int>,inner2)

    referenceRepository.GetRegistrations().Returns(referenceRegistrations) |> ignore

    let sut = new ConcurrentFactoryConfigurationRepositoryBuilder(RegistrationCollisionModes.Fail, Some referenceRepository ) :> IFactoryConfigurationRepositoryBuilder
    
    //Act
    let enrichedMap = sut.Build()

    //Assert
    enrichedMap |> should not' (equal null)
    Assert.That(enrichedMap.GetRegistrations(), Is.EquivalentTo referenceRegistrations)

[<Test>]
let ``Can Add``() =
    //Arrange
    let sut = createSut(RegistrationCollisionModes.Fail)
    
    //Act - no exceptions
    simpleFactory |> addTo sut

let private Test_Build_Repository_And_Verify_Result expectedMode =
    //Arrange
    let sut = createSut(RegistrationCollisionModes.Fail)
    sut.Add simpleFactory simpleId OutputLifetime.Transient expectedMode

    //Act
    let result = sut.Build()

    //Assert
    let constructed = result.Retrieve (simpleId.GetOutputType()) None
    constructed |> Option.isSome |> should equal true
    constructed.Value.GetIdentity() |> should equal simpleId
    constructed.Value.GetType() |> should equal typeof<ApplicationScope>
    constructed.Value.GetFieldValue("subScopeMode") :?> DisposalScope |> should equal expectedMode

[<Test>]
let ``Can Add With SubscopeMode Container And Build Repository``() = 
    Test_Build_Repository_And_Verify_Result DisposalScope.Container

[<Test>]
let ``Can Add With SubscopeMode SubScope Build Repository``() = 
    Test_Build_Repository_And_Verify_Result DisposalScope.SubScope

[<Test>]
let ``Colliding Registration Throws``() =
    //Arrange
    let sut = createSut(RegistrationCollisionModes.Fail)
    
    //Act - Assert
    simpleFactory |> addTo sut
    Assert.Throws<OverlappingRegistrationException>(fun () -> simpleFactory |> addTo sut) |> ignore

[<Test>]
let ``Colliding Registration Ignores``() =
    //Arrange
    let sut = createSut(RegistrationCollisionModes.Ignore)

    //Act
    fakeFactory<string> "1" |> addTo sut
    fakeFactory<string> "2" |> addTo sut

    //Assert
    let resFunc = sut.Build().Retrieve (simpleId.GetOutputType()) None
    let instance = resFunc.Value.Invoke (mock<ILambdaContainer>())
    instance |> should equal "1"

[<Test>]
let ``Colliding Registration Overrrides``() =
    //Arrange
    let sut = createSut(RegistrationCollisionModes.Override)

    //Act
    fakeFactory<string> "1" |> addTo sut
    fakeFactory<string> "2" |> addTo sut

    //Assert
    let resFunc = sut.Build().Retrieve (simpleId.GetOutputType()) None
    let instance = resFunc.Value.Invoke (mock<ILambdaContainer>())
    instance |> should equal "2"

[<Test>]
let ``Register Anonymous If Named Exists Throws``() =
    //Arrange
    let sut = createSut(RegistrationCollisionModes.Fail)
    let namedId = namedIdentity<string> "a name"
    
    sut.Add simpleFactory namedId OutputLifetime.Transient DisposalScope.Container

    //Act - Assert
    Assert.Throws<IncompatibleRegistrationException>(fun () -> simpleFactory |> addTo sut) |> ignore