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

// https://github.com/linq2db/linq2db/issues/195
// Guards precision/scale preservation for an auto-mapped 'decimal option' column. Its DB type is resolved
// from the converter's provider type against the provider schema (not MappingSchema.Default), so a non-zero
// scale must survive the round-trip: a bare decimal(18,0) on a strict provider would truncate 12.34 to 12.
[<NoComparison>]
[<Table("OptionDecimalTable", IsColumnAttributeRequired = false)>]
type DecimalOptionRow =
    { [<PrimaryKey>] Id    : int
      Value               : decimal option }

let TestDecimalOptionRoundtrip (db : IDataContext) =
    use _t = db.CreateLocalTable<DecimalOptionRow>()

    db.Insert({ Id = 1; Value = Some 12.34m }) |> ignore
    db.Insert({ Id = 2; Value = None         }) |> ignore

    let r1 = query { for r in db.GetTable<DecimalOptionRow>() do
                     where (r.Id = 1)
                     exactlyOne }
    let r2 = query { for r in db.GetTable<DecimalOptionRow>() do
                     where (r.Id = 2)
                     exactlyOne }

    Assert.That(r1.Value, Is.EqualTo(Some 12.34m))
    Assert.That(r2.Value, Is.EqualTo None)

// A complex (record) element type - not a scalar in MappingSchema.Default.
[<NoComparison; NoEquality>]
type ComplexElem = { A : int; B : string }

// https://github.com/linq2db/linq2db/issues/195
// Negative branch of the scalar gate: an option over a complex/entity element is NOT auto-scalarized.
// IsScalarOption gates on MappingSchema.Default.IsScalarType, so only a scalar-element option gets the
// F# ValueConverter. The scalar-element option ('int option') here must carry the converter; the
// complex-element option ('ComplexElem option') must be left untouched (no auto-mapped value converter).
[<NoComparison; NoEquality>]
[<Table("ComplexOptionTable", IsColumnAttributeRequired = false)>]
type ComplexOptionRow =
    { [<PrimaryKey>] Id    : int
      ScalarOpt           : int option
      ComplexOpt          : ComplexElem option }

let VerifyComplexElementOptionNotScalarized (db : IDataContext) =
    let ed = db.MappingSchema.GetEntityDescriptor(typeof<ComplexOptionRow>)

    // The scalar-element option is recognised as a column carrying the F# value converter...
    let scalar = ed.Columns |> Seq.find (fun c -> c.MemberName = "ScalarOpt")
    Assert.That(scalar.ValueConverter, Is.Not.Null)

    // ...but the complex-element option is left untouched: no auto-mapped value converter.
    let complexConverter =
        ed.Columns
        |> Seq.tryFind (fun c -> c.MemberName = "ComplexOpt")
        |> Option.bind (fun c -> Option.ofObj c.ValueConverter)
    Assert.That(complexConverter, Is.EqualTo None)
