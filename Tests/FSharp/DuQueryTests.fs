module Tests.FSharp.DuQueryTests

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

// single-case DU column round-trips (stored as its underlying value) and equality translates to SQL (row 1)
let EqualsLiteral (db: IDataContext) =
    use _t = db.CreateLocalTable<DuRow>()
    seed db
    (db.GetTable<DuRow>().Where(fun x -> x.Key = UserId 10).ToArray()).Length
