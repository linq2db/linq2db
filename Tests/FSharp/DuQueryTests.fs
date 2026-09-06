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

// Nullable single-case-DU column. Uses the attribute-free recognition path (no explicit [<Column>]),
// which is what an idiomatic F# record looks like.
[<Table(IsColumnAttributeRequired = false)>]
type DuOptRow =
    { [<PrimaryKey>] Id:  int
      Key:                UserId option }

// `UserId option` stores the union's wrapped scalar and NULL for None. Returns the round-tripped keys
// with None encoded as -1, so a silently-dropped column (which reads back as all-None) is visible.
let OptionRoundTrip (db: IDataContext) =
    use _t = db.CreateLocalTable<DuOptRow>()
    db.Insert({ DuOptRow.Id = 1; Key = Some (UserId 10) }) |> ignore
    db.Insert({ DuOptRow.Id = 2; Key = None })             |> ignore
    db.GetTable<DuOptRow>().OrderBy(fun x -> x.Id).Select(fun x -> x.Key).ToArray()
    |> Array.map (function Some (UserId v) -> v | None -> -1)

// A *struct* single-case union is auto-mapped the same way, but being a value type it cannot hold null:
// a NULL read yields default(StructUserId) - the union wrapping 0 - exactly as a plain `int` member reads
// NULL as 0. `StructUserId option` is the way to express a nullable column of this type.
[<Struct>]
type StructUserId = StructUserId of int

[<Table(IsColumnAttributeRequired = false)>]
type StructDuRow =
    { [<PrimaryKey>] Id: int
      Key:                StructUserId }

[<Table(IsColumnAttributeRequired = false)>]
type StructDuOptRow =
    { [<PrimaryKey>] Id: int
      Key:                StructUserId option }

// Declared behaviour, not an aspiration: the unmatched LEFT JOIN row materializes as StructUserId 0.
let StructNullRead (db: IDataContext) =
    use _t1 = db.CreateLocalTable<StructDuRow>()
    use _t2 = db.CreateLocalTable<DuOuter>()
    db.Insert({ StructDuRow.Id = 1; Key = StructUserId 10 }) |> ignore
    db.Insert({ DuOuter.Oid = 1; RefId = 1 })  |> ignore
    db.Insert({ DuOuter.Oid = 2; RefId = 99 }) |> ignore
    query {
        for o in db.GetTable<DuOuter>() do
        for d in db.GetTable<StructDuRow>().Where(fun x -> o.RefId = x.Id).DefaultIfEmpty() do
        sortBy o.Oid
        select d.Key
    }
    |> Seq.toArray
    |> Array.map (fun (StructUserId v) -> v)

// The nullable spelling for a struct union round-trips properly, including None.
let StructOptionRoundTrip (db: IDataContext) =
    use _t = db.CreateLocalTable<StructDuOptRow>()
    db.Insert({ StructDuOptRow.Id = 1; Key = Some (StructUserId 10) }) |> ignore
    db.Insert({ StructDuOptRow.Id = 2; Key = None })                   |> ignore
    db.GetTable<StructDuOptRow>().OrderBy(fun x -> x.Id).Select(fun x -> x.Key).ToArray()
    |> Array.map (function Some (StructUserId v) -> v | None -> -1)

// Shapes the auto-mapping must leave alone: a multi-case DU, an F# list, and a single-case union whose
// wrapped field is not a scalar. All three are unions or union-like, so they sit right next to the shapes
// the readers do claim.
type Status = Active | Inactive
type Inner  = { A: int }
type Wrap   = Wrap of Inner

[<Table(IsColumnAttributeRequired = false)>]
type BoundaryRow =
    { [<PrimaryKey>] Id: int
      Key:                UserId          // claimed: single-case scalar union
      OptKey:             UserId option   // claimed: option over a single-case scalar union
      Status:             Status          // not claimed: two cases
      Items:              int list        // not claimed: two cases
      Wrapped:            Wrap }          // not claimed: wrapped field is not a scalar

// Mapping-only (no table, no round-trip): which members become columns, and which carry a value converter.
let MappingBoundary (db: IDataContext) =
    db.MappingSchema.GetEntityDescriptor(typeof<BoundaryRow>).Columns
    |> Seq.map (fun c -> c.MemberName + (if isNull (box c.ValueConverter) then "" else ":conv"))
    |> Seq.sort
    |> String.concat ","

// single-case DU column round-trips (stored as its underlying value) and equality translates to SQL (row 1)
let EqualsLiteral (db: IDataContext) =
    use _t = db.CreateLocalTable<DuRow>()
    seed db
    (db.GetTable<DuRow>().Where(fun x -> x.Key = UserId 10).ToArray()).Length

// single-case DU column reads back as the reconstructed union (from-provider converter direction).
// The (UserId v) pattern proves each element really is a reconstructed union - it throws on a null.
let ReadBack (db: IDataContext) =
    use _t = db.CreateLocalTable<DuRow>()
    seed db
    db.GetTable<DuRow>().OrderBy(fun x -> x.Id).Select(fun x -> x.Key).ToArray()
    |> Array.map (fun (UserId v) -> v)

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
