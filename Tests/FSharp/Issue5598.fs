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
        // assert against the SET clause only: [Id] (PK) and [Number] must not be written, while
        // [Id] legitimately appears in the WHERE clause, so the check is scoped to text before WHERE.
        let updateSql = nonNull db.LastQuery
        let setClause = updateSql.Substring(0, updateSql.IndexOf "WHERE")
        Assert.That(setClause, Does.Contain "[Text]")
        Assert.That(setClause, Does.Not.Contain "[Number]")
        Assert.That(setClause, Does.Not.Contain "[Id]")

    let row = query { for r in db.GetTable<Issue4132Table>() do
                      where (r.Id = 1)
                      exactlyOne }
    Assert.That(row.Text,   Is.EqualTo "after")
    Assert.That(row.Number, Is.EqualTo 5)
    Assert.That(row.Id,     Is.EqualTo 1)

// Async counterpart: UpdateAsync builds its expression from the same synchronous UpdatePredicateSetter
// method info as Update (async-ness is handled at execution), so the F# record-copy rewrite must fire on
// this path too. Locks in that the async update emits only the changed column (PK was rejected by YDB).
let UpdateSetsOnlyChangedColumnAsync (db : DataConnection) (assertSql : bool) : System.Threading.Tasks.Task =
    task {
        use table = db.CreateLocalTable<Issue4132Table>()
        table.Insert(fun () -> { Id = 1; Number = 5; Text = "before" }) |> ignore

        let! _ = table.UpdateAsync((fun row -> row.Id = 1), (fun row -> { row with Text = "after" }))

        if assertSql then
            let updateSql = nonNull db.LastQuery
            let setClause = updateSql.Substring(0, updateSql.IndexOf "WHERE")
            Assert.That(setClause, Does.Contain "[Text]")
            Assert.That(setClause, Does.Not.Contain "[Number]")
            Assert.That(setClause, Does.Not.Contain "[Id]")

        let row = query { for r in db.GetTable<Issue4132Table>() do
                          where (r.Id = 1)
                          exactlyOne }
        Assert.That(row.Text,   Is.EqualTo "after")
        Assert.That(row.Number, Is.EqualTo 5)
        Assert.That(row.Id,     Is.EqualTo 1)
    } :> System.Threading.Tasks.Task
