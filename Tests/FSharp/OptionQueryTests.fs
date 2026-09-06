module Tests.FSharp.OptionQueryTests

open System.Linq

open LinqToDB
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

let private seedV (db: IDataContext) =
    db.Insert({ VOptRow.Id = 1; Name = ValueSome "a" }) |> ignore
    db.Insert({ VOptRow.Id = 2; Name = ValueNone })     |> ignore

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
// boolean value, which most providers render as CASE WHEN ... THEN 1 ELSE 0 END.
let IsSomeProjection (db: IDataContext) =
    use _t = db.CreateLocalTable<OptRow>()
    seed db
    db.GetTable<OptRow>().OrderBy(fun x -> x.Id).Select(fun x -> x.Name.IsSome).ToArray()

// .Value over a *value-typed* option. The converter's provider type is Nullable<int> here (unlike the
// string cases, where it is the element type itself), so this exercises the re-type to a non-nullable CLR type.
let IntValue (db: IDataContext) =
    use _t = db.CreateLocalTable<OptRow>()
    seed db
    db.GetTable<OptRow>().Where(fun x -> x.Age.IsSome).OrderBy(fun x -> x.Id).Select(fun x -> x.Age.Value).ToArray()

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
