module Tests.FSharp.Issue5598

open Tests.FSharp.Issue4132

open LinqToDB
open LinqToDB.Data
open Tests
open NUnit.Framework

// https://github.com/linq2db/linq2db/issues/5598
// An F# record-copy update `{ row with X = v }` must set only the changed column, not every column
// (including the PK). [Number] is neither changed nor in the predicate, so it appears in the SQL only
// if the update wrongly sets all columns.
let UpdateSetsOnlyChangedColumn (db : DataConnection) =
    use table = db.CreateLocalTable<Issue4132Table>()
    table.Insert(fun () -> { Id = 1; Number = 5; Text = "before" }) |> ignore

    table.Update((fun row -> row.Id = 1), (fun row -> { row with Text = "after" })) |> ignore
    let updateSql = db.LastQuery

    Assert.That(updateSql, Does.Contain "Text")
    Assert.That(updateSql, Does.Not.Contain "[Number]")

    let row = query { for r in db.GetTable<Issue4132Table>() do
                      where (r.Id = 1)
                      exactlyOne }
    Assert.That(row.Text,   Is.EqualTo "after")
    Assert.That(row.Number, Is.EqualTo 5)
    Assert.That(row.Id,     Is.EqualTo 1)
