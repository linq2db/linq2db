module Tests.FSharp.RecordSetterTest

open Tests.FSharp.Models

open LinqToDB
open LinqToDB.Mapping
open Tests
open Tests.Tools
open NUnit.Framework

[<Table>]
type RecordSetterTable =
    { [<PrimaryKey>]
      Id: int
      [<Column>]
      Number: int
      [<Column>]
      Text: string }

let InsertRecordFromSetterLambda(db : IDataContext) =
    use table = db.CreateLocalTable<RecordSetterTable>()
    table.Insert(fun() ->{RecordSetterTable.Id = 0; Number = 1; Text = "freshly inserted"}) |> ignore

let UpdateRecordCopyWithPredicate(db : IDataContext) =
    use table = db.CreateLocalTable<RecordSetterTable>()
    table.Insert(fun () -> {RecordSetterTable.Id = 1; Number = 1; Text = "before"}) |> ignore

    table.Update((fun row -> row.Number = 1), (fun row -> {row with Text = "updated recently"})) |> ignore

    let row = query { for r in db.GetTable<RecordSetterTable>() do
                      where (r.Id = 1)
                      exactlyOne }
    Assert.That(row.Text,   Is.EqualTo "updated recently")
    Assert.That(row.Number, Is.EqualTo 1)
    Assert.That(row.Id,     Is.EqualTo 1)
