module Tests.FSharp.Issue4132

open Tests.FSharp.Models

open LinqToDB
open LinqToDB.Mapping
open Tests
open Tests.Tools
open NUnit.Framework

[<Table>]
type Issue4132Table =
    { [<PrimaryKey>]
      Id: int
      [<Column>]
      Number: int
      [<Column>]
      Text: string }

let Issue4132Test1(db : IDataContext) =
    use table = db.CreateLocalTable<Issue4132Table>()
    table.Insert(fun() ->{Issue4132Table.Id = 0; Number = 1; Text = "freshly inserted"}) |> ignore

let Issue4132Test2(db : IDataContext) =
    use table = db.CreateLocalTable<Issue4132Table>()
    table.Insert(fun () -> {Issue4132Table.Id = 1; Number = 1; Text = "before"}) |> ignore

    table.Update((fun row -> row.Number = 1), (fun row -> {row with Text = "updated recently"})) |> ignore

    let row = query { for r in db.GetTable<Issue4132Table>() do
                      where (r.Id = 1)
                      exactlyOne }
    Assert.That(row.Text,   Is.EqualTo "updated recently")
    Assert.That(row.Number, Is.EqualTo 1)
    Assert.That(row.Id,     Is.EqualTo 1)
