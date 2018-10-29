module LambdaContainer.Core.Tests.FactoryIdentityTest
open LambdaContainer.Core.FactoryContracts
open Xunit

[<Fact>]
let ``Can Construct Named Identity``() =
    //Arrange
    let name = "the name"
    let thetype = typeof<string>
    let sourceInfo = "source information from registry"

    //Act
    let sut = new FactoryIdentity(Some(name), thetype, Some(sourceInfo))

    //Assert
    Assert.False(sut.IsAnonymous())
    Assert.Equal(name, sut.GetName())
    Assert.Equal(thetype, sut.GetOutputType())
    Assert.Equal(sourceInfo, sut.GetRegistrationSourceInformation())

[<Fact>]
let ``Can Construct Anonymous Identity``() =
    //Arrange
    let thetype = typeof<string>
    let sourceInfo = "source information from registry"

    //Act
    let sut = new FactoryIdentity(None, thetype, Some sourceInfo)

    //Assert
    Assert.True(sut.IsAnonymous())

[<Fact>]
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
    Assert.True(areEqual)
    Assert.Equal((false,false),(areNotEqualDueToDifferentType, areNotEqualDueToDifferentName))
