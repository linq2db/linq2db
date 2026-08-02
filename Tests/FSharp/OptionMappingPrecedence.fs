module Tests.FSharp.OptionMappingPrecedence

open LinqToDB
open LinqToDB.Mapping
open NUnit.Framework

// Note carries an explicit [<Column>] (with no DataType) alongside the fluent DataType override below.
// That is the exact shape the lower-priority combine protects: the option schema's embedded attribute
// reader also sees the plain [<Column>], and at higher priority its DataType-less column would shadow
// the fluent VarChar (EntityDescriptor keeps the first ColumnAttribute per member).
[<Table("OptionPrecedenceTable")>]
type PrecedenceRow =
    { [<PrimaryKey; Column>] Id   : int
      [<Column>]             Note : string option }

// Builds a schema that explicitly maps the option column Note to DataType.VarChar via fluent mapping.
let BuildExplicitSchema () =
    let ms = MappingSchema()
    let mb = FluentMappingBuilder(ms)
    mb.Entity<PrecedenceRow>().Property(fun e -> e.Note).HasDataType(DataType.VarChar) |> ignore
    mb.Build() |> ignore
    ms

// Verifies UseFSharp()'s automatic 'T option schema - combined as a lower-priority fallback - does NOT
// override a user's explicit fluent DataType on an option column. Regression guard for the lower-priority
// combine in UseFSharp() (issue #195 follow-up): without it the option schema's derived NVarChar would win.
let VerifyExplicitDataTypePreserved (db : IDataContext) =
    let ed  = db.MappingSchema.GetEntityDescriptor(typeof<PrecedenceRow>)
    let col = ed.Columns |> Seq.find (fun c -> c.MemberName = "Note")
    Assert.That(col.DataType, Is.EqualTo DataType.VarChar)
