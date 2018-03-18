module LambdaContainer.Core.Tests.FactoryIdentityTest
open NUnit.Framework
open LambdaContainer.Core.FactoryContracts
open FsUnit

[<Test>]
let ``Can Construct Named Identity``() =
    //Arrange
    let name = "the name"
    let thetype = typeof<string>
    let sourceInfo = "source information from registry"

    //Act
    let sut = new FactoryIdentity(Some(name), thetype, Some(sourceInfo))

    //Assert
    sut.IsAnonymous() |> should equal false
    sut.GetName() |> should equal name
    sut.GetOutputType() |> should equal thetype
    sut.GetRegistrationSourceInformation() |> should equal sourceInfo

[<Test>]
let ``Can Construct Anonymous Identity``() =
    //Arrange
    let thetype = typeof<string>
    let sourceInfo = "source information from registry"

    //Act
    let sut = new FactoryIdentity(None, thetype, Some sourceInfo)

    //Assert
    sut.IsAnonymous() |> should equal true

[<Test>]
let ``Two Identities Are Equal When Type And Name Equals``() =
    //Arrange
    let type1 = typeof<string>
    let type2 = typeof<int>
    let name = Some("the name")
    let sourceInfo = "source information from registry"
    let candidate1 = new FactoryIdentity(name, type1, Some sourceInfo)
    let candidate2 = new FactoryIdentity(name, type1, None)
    let candidate3 = new FactoryIdentity(name, type2, Some sourceInfo)
    let candidate4 = new FactoryIdentity(None, type1, Some sourceInfo)

    //Act
    let areEqual = candidate1.Equals(candidate2)
    let areNotEqualDueToDifferentType = candidate1.Equals(candidate3)
    let areNotEqualDueToDifferentName = candidate1.Equals(candidate4)

    //Assert
    areEqual |> should equal true
    [areNotEqualDueToDifferentType; areNotEqualDueToDifferentName] |> should equal [false;false]