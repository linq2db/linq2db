module Tests.FSharp.OptionTypes

open System
open System.Net
open LinqToDB
open LinqToDB.Mapping
open NUnit.Framework
open Shouldly
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

// A custom value type that is a scalar ONLY when registered on a schema - never in MappingSchema.Default.
[<Struct>]
type MyId = { IdValue : int }

// Registers MyId as a scalar type (Int32) on a fresh schema, plus round-trip converters. MyId is unknown
// to MappingSchema.Default, so it is scalar only via this registration.
let BuildCustomScalarSchema () =
    let ms = MappingSchema()
    ms.SetConverter<MyId, int>(fun id -> id.IdValue)  |> ignore
    ms.SetConverter<int, MyId>(fun v -> { IdValue = v }) |> ignore
    ms.AddScalarType(typeof<MyId>, DataType.Int32)
    ms

// https://github.com/linq2db/linq2db/issues/195
// A 'T option whose element is scalar ONLY in the active/user schema (MyId, made scalar via AddScalarType
// on the schema passed to GetDataContext) must still auto-map. Currently it does NOT: IsScalarOption
// consults MappingSchema.Default, which doesn't know MyId, so no ColumnAttribute/ValueConverter is emitted
// and the option member is not mapped as a column. Gated [ActiveIssue] until the scalar gate is fixed.
[<NoComparison>]
[<Table("CustomScalarOptionTable", IsColumnAttributeRequired = false)>]
type CustomScalarOptionRow =
    { [<PrimaryKey>] Id    : int
      Value               : MyId option }

let VerifyCustomScalarOptionMapped (db : IDataContext) =
    let ed  = db.MappingSchema.GetEntityDescriptor(typeof<CustomScalarOptionRow>)
    match ed.Columns |> Seq.tryFind (fun c -> c.MemberName = "Value") with
    | Some c -> Assert.That(c.ValueConverter, Is.Not.Null)
    | None   -> Assert.Fail("MyId option column was not mapped - auto-option-mapping did not recognise the user-registered scalar type")

// Negative counterpart used for the cache-isolation test: in a schema that does NOT register MyId as a scalar,
// the schema-aware reader must leave the MyId option member unmapped (no auto value converter) - either no
// "Value" column, or one carrying no ValueConverter.
let VerifyCustomScalarOptionNotMapped (db : IDataContext) =
    let ed  = db.MappingSchema.GetEntityDescriptor(typeof<CustomScalarOptionRow>)
    let converter =
        ed.Columns
        |> Seq.tryFind (fun c -> c.MemberName = "Value")
        |> Option.bind (fun c -> Option.ofObj c.ValueConverter)
    converter.ShouldBe(None)

// https://github.com/linq2db/linq2db/issues/5675 (Tests #7)
// Precedence for the newly-broadened case: OptionMappingPrecedence pins that an explicit fluent DataType
// survives for a MappingSchema.Default-scalar element ('string option'), but the schema-aware gate now also
// auto-maps options whose element is scalar ONLY via schema registration (MyId). This pins that an explicit
// fluent mapping still wins for THAT element too: the option carries a plain [<Column>] (whose DataType-less
// attribute the lower-priority auto reader also supplies), plus a fluent VarChar override - the explicit
// DataType must be preserved rather than shadowed by the auto-scalarization.
[<NoComparison>]
[<Table("CustomScalarPrecedenceTable")>]
type CustomScalarPrecedenceRow =
    { [<PrimaryKey; Column>] Id    : int
      [<Column>]             Value : MyId option }

// Registers MyId as a scalar (so 'MyId option' is auto-map eligible) and explicitly maps Value to VarChar.
let BuildCustomScalarExplicitColumnSchema () =
    let ms = BuildCustomScalarSchema ()
    let mb = FluentMappingBuilder(ms)
    mb.Entity<CustomScalarPrecedenceRow>().Property(fun e -> e.Value).HasDataType(DataType.VarChar) |> ignore
    mb.Build() |> ignore
    ms

let VerifyCustomScalarExplicitDataTypePreserved (db : IDataContext) =
    let ed  = db.MappingSchema.GetEntityDescriptor(typeof<CustomScalarPrecedenceRow>)
    let col = ed.Columns |> Seq.find (fun c -> c.MemberName = "Value")
    // Explicit fluent DataType wins over the broadened auto-scalarization's DataType-less ColumnAttribute...
    col.DataType.ShouldBe(DataType.VarChar)
    // ...and the option is still recognised as a scalar column (auto value converter supplies None <-> NULL).
    col.ValueConverter.ShouldNotBeNull() |> ignore

// https://github.com/linq2db/linq2db/issues/5675 (Tests #2)
// Provider-native scalar element: IPAddress is scalar only because the PostgreSQL provider layer registers it
// (PostgreSQLMappingSchema.AddScalarType(typeof<IPAddress>, ...)), never in MappingSchema.Default. This asserts
// metadata mapping (not a DDL round-trip): the schema-aware gate resolves scalar-ness against the active
// provider-inclusive schema, so under a PostgreSQL context 'IPAddress option' must be recognised as a scalar
// column carrying the auto value converter - the case no contained fix could reach, since the element's
// scalar-ness comes from the provider layer and no schema is passed to GetDataContext.
[<NoComparison>]
[<Table("OptionIPAddressTable", IsColumnAttributeRequired = false)>]
type IPAddressOptionRow =
    { [<PrimaryKey>] Id    : int
      Value               : IPAddress option }

let VerifyProviderNativeScalarOptionMapped (db : IDataContext) =
    let ed = db.MappingSchema.GetEntityDescriptor(typeof<IPAddressOptionRow>)
    match ed.Columns |> Seq.tryFind (fun c -> c.MemberName = "Value") with
    | Some c -> c.ValueConverter.ShouldNotBeNull() |> ignore
    | None   -> Assert.Fail("IPAddress option column was not mapped - the provider-native scalar was not recognised by the schema-aware gate")
