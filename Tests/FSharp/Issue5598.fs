module Tests.FSharp.Issue5598

open Tests.FSharp.Issue4132

open LinqToDB
open LinqToDB.Data
open Tests
open NUnit.Framework

// The SET clause is the statement text before WHERE; the no-predicate Update overload emits a WHERE-less
// UPDATE, so fall back to the whole statement instead of throwing on IndexOf returning -1.
let private setClauseOf (updateSql : string) =
    let whereIdx = updateSql.IndexOf "WHERE"
    if whereIdx < 0 then updateSql else updateSql.Substring(0, whereIdx)

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
        let setClause = setClauseOf updateSql
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
            let setClause = setClauseOf updateSql
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

// No-predicate overload `table.Update(fun row -> {...})` (UpdateSetter) - a full-table single-column
// update with no Where. Exercises the RewriteUpdate(mc, false) branch the predicate tests don't reach.
// The emitted UPDATE has no WHERE, so the SET-clause check relies on setClauseOf tolerating its absence.
let UpdateSetsOnlyChangedColumnNoPredicate (db : DataConnection) (assertSql : bool) =
    use table = db.CreateLocalTable<Issue4132Table>()
    table.Insert(fun () -> { Id = 1; Number = 5; Text = "before" }) |> ignore

    table.Update(fun row -> { row with Text = "after" }) |> ignore

    if assertSql then
        let updateSql = nonNull db.LastQuery
        let setClause = setClauseOf updateSql
        Assert.That(setClause, Does.Contain "[Text]")
        Assert.That(setClause, Does.Not.Contain "[Number]")
        Assert.That(setClause, Does.Not.Contain "[Id]")

    let row = query { for r in db.GetTable<Issue4132Table>() do
                      where (r.Id = 1)
                      exactlyOne }
    Assert.That(row.Text,   Is.EqualTo "after")
    Assert.That(row.Number, Is.EqualTo 5)
    Assert.That(row.Id,     Is.EqualTo 1)

// No-op record copy `{ r with Id = r.Id }` (every ctor argument a self-copy, empty change set): the PK
// must still be kept out of SET (YDB rejects assigning a PK column) by assigning only the non-PK columns
// to their own values, rather than re-emitting the full all-column UPDATE.
let UpdateNoOpExcludesPrimaryKey (db : DataConnection) (assertSql : bool) =
    use table = db.CreateLocalTable<Issue4132Table>()
    table.Insert(fun () -> { Id = 1; Number = 5; Text = "before" }) |> ignore

    table.Update((fun row -> row.Id = 1), (fun row -> { row with Id = row.Id })) |> ignore

    if assertSql then
        let updateSql = nonNull db.LastQuery
        let setClause = setClauseOf updateSql
        Assert.That(setClause, Does.Not.Contain "[Id]")
        Assert.That(setClause, Does.Contain "[Number]")
        Assert.That(setClause, Does.Contain "[Text]")

    let row = query { for r in db.GetTable<Issue4132Table>() do
                      where (r.Id = 1)
                      exactlyOne }
    Assert.That(row.Text,   Is.EqualTo "before")
    Assert.That(row.Number, Is.EqualTo 5)
    Assert.That(row.Id,     Is.EqualTo 1)
