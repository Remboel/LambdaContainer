module LambdaContainer.Core.Tests.StackSetTest
open LambdaContainer.Core.Tests.TestUtilities
open System
open NUnit.Framework
open System.Collections.Generic
open LambdaContainer.Core.NetFrameworkEx
open LambdaContainer.Core.NetFrameworkEx.DataStructures
open FsUnit

[<Test>]
let ``Can Construct``() =
    Assert.DoesNotThrow(fun () -> new StackSet<string>() |> ignore)

[<Test>]
let ``Can Push``() =
    //Arrange
    let newItem = "hello there"
    let ss = new StackSet<string>()

    //Act
    let pushed = ss.Push newItem

    //Assert
    Assert.That(pushed, Is.True)
    FieldSpy.getField<HashSet<string>> ss "set" |> Seq.tryHead |> Option.get |> should be (sameAs newItem)
    FieldSpy.getField<Stack<string>> ss "stack" |> Seq.tryHead |> Option.get |> should be (sameAs newItem)

[<Test>]
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
    [pushedItemOne; pushedItemTwo; pushedItemOneSecondTime] |> should equal [true; true; false]

[<Test>]
let ``Peek Returns Some``() =
    //Arrange
    let item = "hello there"
    let ss = new StackSet<string>()
    ss.Push(item) |> ignore

    //Act + Assert
    ss.Peek() |> should equal (Some item)

[<Test>]
let ``Peek Returns Top Of Stack``() =
    //Arrange
    let item1 = "hello there1"
    let item2 = "hello there2"
    let ss = new StackSet<string>()
    ss.Push(item1) |> ignore
    ss.Push(item2) |> ignore

    //Act + Assert
    ss.Peek() |> should equal (Some item2)

[<Test>]
let ``Peek Returns None``() =
    //Arrange
    let ss = new StackSet<string>()

   //Act + Assert
    ss.Peek() |> should equal None

[<Test>]
let ``Pop Fails If Empty``() =
    //Arrange
    let ss = new StackSet<string>()

    //Act - Assert
    Assert.Throws<InvalidOperationException>(fun () -> ss.Pop() |> ignore) |> ignore

[<Test>]
let ``Can Pop``() =
    //Arrange
    let ss = new StackSet<string>()
    let item = "hello there"
    ss.Push(item) |> ignore

   //Act + Assert
    ss.Pop() |> should equal item

[<Test>]
let ``Pop Removes Top Of Stack``() =
    //Arrange
    let ss = new StackSet<string>()
    let item1 = "hello there1"
    let item2 = "hello there2"
    ss.Push(item1) |> ignore
    ss.Push(item2) |> ignore

    //Act + Assert
    ss.Pop() |> should equal item2
    ss.Peek() |> should equal (Some item1)

[<Test>]
let ``Count When Empty Returns 0``() =
    //Arrange
    let ss = new StackSet<string>()

    //Act + Assert
    ss.Count() |> should equal 0

[<Test>]
let ``Count Returns 2``() =
    //Arrange
    let ss = new StackSet<string>()
    ss.Push "1" |> ignore
    ss.Push "2" |> ignore

    //Act + Assert
    ss.Count() |> should equal 2
