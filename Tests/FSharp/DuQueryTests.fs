module Tests.FSharp.DuQueryTests

// A left-joined row is typed 'DuRow | null', but linq2db translates the projection to SQL rather than
// dereferencing it at runtime, so selecting a column off it is safe; suppress the nullability warning.
#nowarn "3261"

open System.Linq

open LinqToDB
open LinqToDB.Mapping

open Tests

// single-case discriminated union wrapping a scalar
type UserId = UserId of int

[<Table>]
type DuRow =
    { [<PrimaryKey>] Id:  int
      [<Column>]     Key: UserId }

let private seed (db: IDataContext) =
    db.Insert({ DuRow.Id = 1; Key = UserId 10 }) |> ignore
    db.Insert({ DuRow.Id = 2; Key = UserId 20 }) |> ignore

[<Table>]
type DuOuter =
    { [<PrimaryKey>] Oid:   int
      [<Column>]     RefId: int }

// single-case DU column round-trips (stored as its underlying value) and equality translates to SQL (row 1)
let EqualsLiteral (db: IDataContext) =
    use _t = db.CreateLocalTable<DuRow>()
    seed db
    (db.GetTable<DuRow>().Where(fun x -> x.Key = UserId 10).ToArray()).Length

// A null-producing read of a single-case-DU column must materialize as null, not a fabricated UserId 0.
// The LEFT JOIN leaves DuRow.Key NULL for the unmatched outer row (Oid=2, RefId=99), so the converter
// reads a SQL NULL for that row. Returns the count of null keys (expected 1).
let NullReadKey (db: IDataContext) =
    use _t1 = db.CreateLocalTable<DuRow>()
    use _t2 = db.CreateLocalTable<DuOuter>()
    db.Insert({ DuRow.Id = 1; Key = UserId 10 }) |> ignore
    db.Insert({ DuOuter.Oid = 1; RefId = 1 })    |> ignore
    db.Insert({ DuOuter.Oid = 2; RefId = 99 })   |> ignore
    let keys =
        query {
            for o in db.GetTable<DuOuter>() do
            for d in db.GetTable<DuRow>().Where(fun x -> o.RefId = x.Id).DefaultIfEmpty() do
            sortBy o.Oid
            select d.Key
        } |> Seq.toArray
    keys |> Array.filter (fun k -> obj.ReferenceEquals(k, null)) |> Array.length
