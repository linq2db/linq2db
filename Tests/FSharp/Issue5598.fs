module Tests.FSharp.Issue5598

open Tests.FSharp.Issue4132

open LinqToDB
open LinqToDB.Data
open Tests
open NUnit.Framework

// https://github.com/linq2db/linq2db/issues/5598
// An F# record-copy update `{ row with X = v }` must set only the changed column, not every column
// (including the PK). The insert -> update-one-column -> read-back round-trip is asserted on every
// provider; the SQL-shape check (only [Text] in SET, no [Number]) is gated by `assertSql` because it
// relies on SQLite's bracket identifier quoting. This lets the same scenario also run on YDB - the
// provider that originally rejected the all-column UPDATE - as a data-correctness round-trip.
let UpdateSetsOnlyChangedColumn (db : DataConnection) (assertSql : bool) =
    use table = db.CreateLocalTable<Issue4132Table>()
    table.Insert(fun () -> { Id = 1; Number = 5; Text = "before" }) |> ignore

    table.Update((fun row -> row.Id = 1), (fun row -> { row with Text = "after" })) |> ignore

    if assertSql then
        let updateSql = db.LastQuery
        Assert.That(updateSql, Does.Contain "[Text]")
        Assert.That(updateSql, Does.Not.Contain "[Number]")

    let row = query { for r in db.GetTable<Issue4132Table>() do
                      where (r.Id = 1)
                      exactlyOne }
    Assert.That(row.Text,   Is.EqualTo "after")
    Assert.That(row.Number, Is.EqualTo 5)
    Assert.That(row.Id,     Is.EqualTo 1)
