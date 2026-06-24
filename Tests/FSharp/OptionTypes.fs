module Tests.FSharp.OptionTypes

open System
open LinqToDB
open LinqToDB.Mapping
open NUnit.Framework
open Tests

// https://github.com/linq2db/linq2db/issues/195
// Guards the Nullable<_> element edge case: the provider type must not become Nullable<Nullable<_>>
// (which MakeGenericType rejects). Round-trips a System.Nullable<int> option column.
[<NoComparison>]
[<Table("OptionNullableElemTable", IsColumnAttributeRequired = false)>]
type NullableElemRow =
    { [<PrimaryKey>] Id    : int
      Value               : Nullable<int> option }

let TestNullableElementOptionRoundtrip (db : IDataContext) =
    use _t = db.CreateLocalTable<NullableElemRow>()

    db.Insert({ Id = 1; Value = Some (Nullable<int>(42)) }) |> ignore
    db.Insert({ Id = 2; Value = None                     }) |> ignore

    let r1 = query { for r in db.GetTable<NullableElemRow>() do
                     where (r.Id = 1)
                     exactlyOne }
    let r2 = query { for r in db.GetTable<NullableElemRow>() do
                     where (r.Id = 2)
                     exactlyOne }

    Assert.That(r1.Value, Is.EqualTo(Some (Nullable<int>(42))))
    Assert.That(r2.Value, Is.EqualTo None)

// https://github.com/linq2db/linq2db/issues/195
// Auto-maps F# struct value-options ('T voption) the same way as reference options: ValueSome v stores
// the value, ValueNone stores NULL; int voption ValueNone must store NULL (not 0).
[<Table("VOptionTable", IsColumnAttributeRequired = false)>]
type VOptionRow =
    { [<PrimaryKey>] Id    : int
      IntValue            : int voption
      StrValue            : string voption }

let TestValueOptionRoundtrip (db : IDataContext) =
    use _t = db.CreateLocalTable<VOptionRow>()

    db.Insert({ Id = 1; IntValue = ValueSome 42; StrValue = ValueSome "hello" }) |> ignore
    db.Insert({ Id = 2; IntValue = ValueNone;    StrValue = ValueNone         }) |> ignore

    let r1 = query { for r in db.GetTable<VOptionRow>() do
                     where (r.Id = 1)
                     exactlyOne }
    let r2 = query { for r in db.GetTable<VOptionRow>() do
                     where (r.Id = 2)
                     exactlyOne }

    Assert.That(r1.IntValue, Is.EqualTo(ValueSome 42))
    Assert.That(r1.StrValue, Is.EqualTo(ValueSome "hello"))
    Assert.That(r2.IntValue, Is.EqualTo (ValueNone : int voption))
    Assert.That(r2.StrValue, Is.EqualTo (ValueNone : string voption))
