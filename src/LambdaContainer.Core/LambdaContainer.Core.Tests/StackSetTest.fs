module LambdaContainer.Core.Tests.StackSetTest
open LambdaContainer.Core.Tests.TestUtilities
open System
open System.Collections.Generic
open LambdaContainer.Core.NetFrameworkEx
open LambdaContainer.Core.NetFrameworkEx.DataStructures
open Xunit

[<Fact>]
let ``Can Construct``() =
    Assert.NotNull(new StackSet<string>())

[<Fact>]
let ``Can Push``() =
    //Arrange
    let newItem = "hello there"
    let ss = new StackSet<string>()

    //Act
    let pushed = ss.Push newItem

    //Assert¨'
    Assert.True(pushed)
    Assert.Same(newItem, FieldSpy.getField<HashSet<string>> ss "set" |> Seq.tryHead |> Option.get)
    Assert.Same(newItem, FieldSpy.getField<Stack<string>> ss "stack" |> Seq.tryHead |> Option.get)

[<Fact>]
let ``Cannot Push Same Item Twice``() =
    //Arrange
    let item1 = "hello there1"
    let item2 = "hello there2"
    let ss = new StackSet<string>()

    //Act
    let pushedItemOne = ss.Push item1
    let pushedItemTwo = ss.Push item2
    let pushedItemOneSecondTime = ss.Push item1

    //Assert
    Assert.Equal((true, true, false),(pushedItemOne, pushedItemTwo, pushedItemOneSecondTime))

[<Fact>]
let ``Peek Returns Some``() =
    //Arrange
    let item = "hello there"
    let ss = new StackSet<string>()
    ss.Push(item) |> ignore

    //Act + Assert
    Assert.Equal(Some item, ss.Peek())

[<Fact>]
let ``Peek Returns Top Of Stack``() =
    //Arrange
    let item1 = "hello there1"
    let item2 = "hello there2"
    let ss = new StackSet<string>()
    ss.Push(item1) |> ignore
    ss.Push(item2) |> ignore

    //Act + Assert
    Assert.Equal(Some item2, ss.Peek())

[<Fact>]
let ``Peek Returns None``() =
    //Arrange
    let ss = new StackSet<string>()

   //Act + Assert
    Assert.Equal(None, ss.Peek())

[<Fact>]
let ``Pop Fails If Empty``() =
    //Arrange
    let ss = new StackSet<string>()

    //Act - Assert
    Assert.Throws<InvalidOperationException>(fun () -> ss.Pop() |> ignore) |> ignore

[<Fact>]
let ``Can Pop``() =
    //Arrange
    let ss = new StackSet<string>()
    let item = "hello there"
    ss.Push(item) |> ignore

    //Act + Assert
    Assert.Equal(item, ss.Pop())

[<Fact>]
let ``Pop Removes Top Of Stack``() =
    //Arrange
    let ss = new StackSet<string>()
    let item1 = "hello there1"
    let item2 = "hello there2"
    ss.Push(item1) |> ignore
    ss.Push(item2) |> ignore

    //Act + Assert
    Assert.Equal(item2, ss.Pop())
    Assert.Equal(Some item1, ss.Peek())

[<Fact>]
let ``Count When Empty Returns 0``() =
    //Arrange
    let ss = new StackSet<string>()

    //Act + Assert
    Assert.Equal(0, ss.Count())

[<Fact>]
let ``Count Returns 2``() =
    //Arrange
    let ss = new StackSet<string>()
    ss.Push "1" |> ignore
    ss.Push "2" |> ignore

    //Act + Assert
    Assert.Equal(2, ss.Count())