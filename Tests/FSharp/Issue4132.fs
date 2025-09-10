module Tests.FSharp.Issue4132

open Tests.FSharp.Models

open LinqToDB
open LinqToDB.Mapping
open Tests
open Tests.Tools

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
    table.Update((fun row -> row.Number = 1), (fun row -> {row with Text = "updated recently"})) |> ignore
