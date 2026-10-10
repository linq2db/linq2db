module Tests.FSharp.OptionQueryTests

open System.Linq

open LinqToDB
open LinqToDB.Data
open LinqToDB.Mapping

open Tests

[<Table(IsColumnAttributeRequired = false)>]
type OptRow =
    { [<PrimaryKey>] Id:   int
      Name:                string option
      Age:                 int option }

[<Table(IsColumnAttributeRequired = false)>]
type VOptRow =
    { [<PrimaryKey>] Id:   int
      Name:                string voption }

let private seed (db: IDataContext) =
    db.Insert({ OptRow.Id = 1; Name = Some "a"; Age = Some 5 }) |> ignore
    db.Insert({ OptRow.Id = 2; Name = None;     Age = None })   |> ignore
    db.Insert({ OptRow.Id = 3; Name = Some "b"; Age = Some 7 }) |> ignore

// The local table is dropped as each function returns, so by the time the caller could read LastQuery it
// holds the DROP - the SELECT has to be captured here. Null for a remote context, which is not a
// DataConnection.
let private lastQuery (db: IDataContext) : string | null =
    match db with
    | :? DataConnection as dc -> dc.LastQuery
    | _                       -> null

let private seedV (db: IDataContext) =
    db.Insert({ VOptRow.Id = 1; Name = ValueSome "a" }) |> ignore
    db.Insert({ VOptRow.Id = 2; Name = ValueNone })     |> ignore

// The translator must decline an operand that is a parameter rather than a column. Sql.AsSql forces the
// captured voption into SQL, and the emitted `IS NULL` would then test the *parameter's* nullness: a boxed
// ValueNone is a non-null struct, so the predicate folds to false and the query silently returns no rows
// instead of all of them. Declining turns that into a translation error.
let ParameterOperandIsRefused (db: IDataContext) =
    use _t = db.CreateLocalTable<OptRow>()
    seed db
    let vopt : int voption = ValueNone
    (db.GetTable<OptRow>().Where(fun x -> ValueOption.isNone (Sql.AsSql vopt) && x.Id > 0).ToArray()).Length

// option .IsSome in a Where predicate -> IS NOT NULL (rows 1 and 3)
let IsSome (db: IDataContext) =
    use _t = db.CreateLocalTable<OptRow>()
    seed db
    (db.GetTable<OptRow>().Where(fun x -> x.Name.IsSome).ToArray()).Length

// option .IsNone -> IS NULL (row 2)
let IsNone (db: IDataContext) =
    use _t = db.CreateLocalTable<OptRow>()
    seed db
    (db.GetTable<OptRow>().Where(fun x -> x.Name.IsNone).ToArray()).Length

// option .Value in a comparison -> underlying-column comparison (row 1)
let Value (db: IDataContext) =
    use _t = db.CreateLocalTable<OptRow>()
    seed db
    (db.GetTable<OptRow>().Where(fun x -> x.Name.Value = "a").ToArray()).Length

// standalone option .Value (projection) -> underlying column value (rows 1 and 3 -> "a","b")
let ValueProjection (db: IDataContext) =
    use _t = db.CreateLocalTable<OptRow>()
    seed db
    db.GetTable<OptRow>().Where(fun x -> x.Name.IsSome).OrderBy(fun x -> x.Id).Select(fun x -> x.Name.Value).ToArray()

// option .IsSome in a *projection* (not a predicate) -> the search condition has to materialize as a
// boolean value, which most providers render as CASE WHEN ... THEN 1 ELSE 0 END. Unlike a predicate, a
// declined translation does not throw here - it selects the bare column and evaluates .IsSome in memory,
// returning the same values - so the SQL is returned as well and is the only thing that tells them apart.
let IsSomeProjection (db: IDataContext) =
    use _t = db.CreateLocalTable<OptRow>()
    seed db
    let r = db.GetTable<OptRow>().OrderBy(fun x -> x.Id).Select(fun x -> x.Name.IsSome).ToArray()
    struct (r, lastQuery db)

// .Value over a *value-typed* option. The converter's provider type is Nullable<int> here (unlike the
// string cases, where it is the element type itself), so this exercises the re-type to a non-nullable CLR type.
let IntValue (db: IDataContext) =
    use _t = db.CreateLocalTable<OptRow>()
    seed db
    db.GetTable<OptRow>().Where(fun x -> x.Age.IsSome).OrderBy(fun x -> x.Id).Select(fun x -> x.Age.Value).ToArray()

// `.Value` re-types the column placeholder, so a row whose column is NULL materializes the element's
// default rather than raising the way `Option.get None` does - the same as the core's Nullable<T>.Value.
// Row 2 has no Age, so the middle element is 0 rather than an exception.
let ValueOverNoneRow (db: IDataContext) =
    use _t = db.CreateLocalTable<OptRow>()
    seed db
    db.GetTable<OptRow>().OrderBy(fun x -> x.Id).Select(fun x -> x.Age.Value).ToArray()

// The FSharp.Core module-function spellings (Option.isSome / isNone / get and the ValueOption
// equivalents) must translate the same as the member-access forms.
let ModuleIsSome (db: IDataContext) =
    use _t = db.CreateLocalTable<OptRow>()
    seed db
    (db.GetTable<OptRow>().Where(fun x -> Option.isSome x.Name).ToArray()).Length

let ModuleIsNone (db: IDataContext) =
    use _t = db.CreateLocalTable<OptRow>()
    seed db
    (db.GetTable<OptRow>().Where(fun x -> Option.isNone x.Name).ToArray()).Length

let ModuleGet (db: IDataContext) =
    use _t = db.CreateLocalTable<OptRow>()
    seed db
    (db.GetTable<OptRow>().Where(fun x -> Option.get x.Name = "a").ToArray()).Length

let VOptionModuleIsSome (db: IDataContext) =
    use _t = db.CreateLocalTable<VOptRow>()
    seedV db
    (db.GetTable<VOptRow>().Where(fun x -> ValueOption.isSome x.Name).ToArray()).Length

let VOptionModuleIsNone (db: IDataContext) =
    use _t = db.CreateLocalTable<VOptRow>()
    seedV db
    (db.GetTable<VOptRow>().Where(fun x -> ValueOption.isNone x.Name).ToArray()).Length

let VOptionModuleGet (db: IDataContext) =
    use _t = db.CreateLocalTable<VOptRow>()
    seedV db
    (db.GetTable<VOptRow>().Where(fun x -> ValueOption.get x.Name = "a").ToArray()).Length

// struct voption .IsSome -> IS NOT NULL (row 1)
let VOptionIsSome (db: IDataContext) =
    use _t = db.CreateLocalTable<VOptRow>()
    seedV db
    (db.GetTable<VOptRow>().Where(fun x -> x.Name.IsSome).ToArray()).Length

// struct voption .IsNone -> IS NULL (row 2)
let VOptionIsNone (db: IDataContext) =
    use _t = db.CreateLocalTable<VOptRow>()
    seedV db
    (db.GetTable<VOptRow>().Where(fun x -> x.Name.IsNone).ToArray()).Length

// struct voption .IsValueSome (the generated case-tester spelling) -> IS NOT NULL (row 1)
let VOptionIsValueSome (db: IDataContext) =
    use _t = db.CreateLocalTable<VOptRow>()
    seedV db
    (db.GetTable<VOptRow>().Where(fun x -> x.Name.IsValueSome).ToArray()).Length

// struct voption .IsValueNone (the generated case-tester spelling) -> IS NULL (row 2)
let VOptionIsValueNone (db: IDataContext) =
    use _t = db.CreateLocalTable<VOptRow>()
    seedV db
    (db.GetTable<VOptRow>().Where(fun x -> x.Name.IsValueNone).ToArray()).Length

// struct voption .Value in a comparison -> underlying-column comparison (row 1)
let VOptionValue (db: IDataContext) =
    use _t = db.CreateLocalTable<VOptRow>()
    seedV db
    (db.GetTable<VOptRow>().Where(fun x -> x.Name.Value = "a").ToArray()).Length
