module Tests.FSharp.GeneratedModel

open LinqToDB
open LinqToDB.Data
open LinqToDB.Mapping

open NUnit.Framework
open Tests

// Mirrors the shape the CLI scaffolder emits for F# (issue #1553): an idiomatic record with
// [<Table>]/[<Column>] attributes on fields (explicit DB column names + DataType), nullable columns as
// 'T option, and a DataConnection-derived context with a DataOptions<T> primary constructor and GetTable
// member accessors. These tests prove linq2db actually materializes and round-trips the generated shape at
// runtime (UseFSharp() is applied by the test base), complementing the compile-only Tests.FSharp.Scaffold
// baseline gate.

[<Table("GeneratedScaffoldPerson")>]
[<NoComparison>]
type GeneratedPerson =
    {
        [<Column("PersonID", DataType = DataType.Int32, IsPrimaryKey = true)>] Id : int
        [<Column("FirstName", DataType = DataType.NVarChar, Length = 50)>] FirstName : string
        [<Column("MiddleName", DataType = DataType.NVarChar, Length = 50)>] MiddleName : string option
        [<Column("Age", DataType = DataType.Int32)>] Age : int option
    }

type GeneratedScaffoldDb (options : DataOptions<GeneratedScaffoldDb>) =
    inherit DataConnection(options.Options)
    member this.People = this.GetTable<GeneratedPerson>()

// Round-trips the generated record shape: explicitly [<Column>]-attributed fields with DB-name/property
// divergence (PersonID -> Id), Some/None option columns, a non-null PK. None must read back as None (in
// particular int option None must NOT become Some 0).
let TestGeneratedEntityRoundtrip (db : IDataContext) =
    use _t = db.CreateLocalTable<GeneratedPerson>()

    let someRow = { Id = 1; FirstName = "John"; MiddleName = Some "Q"; Age = Some 42 }
    let noneRow = { Id = 2; FirstName = "Jane"; MiddleName = None;     Age = None }

    db.Insert(someRow) |> ignore
    db.Insert(noneRow) |> ignore

    let r1 = query { for p in db.GetTable<GeneratedPerson>() do
                     where (p.Id = 1)
                     exactlyOne }
    let r2 = query { for p in db.GetTable<GeneratedPerson>() do
                     where (p.Id = 2)
                     exactlyOne }

    Assert.That(r1.FirstName, Is.EqualTo "John")
    Assert.That(r1.MiddleName, Is.EqualTo(Some "Q"))
    Assert.That(r1.Age, Is.EqualTo(Some 42))
    Assert.That(r2.MiddleName, Is.EqualTo None)
    Assert.That(r2.Age, Is.EqualTo None)

// Exercises the generated context type: constructed via its DataOptions<T> primary constructor and read
// through the generated GetTable member accessor (create + read on the context's own connection).
let TestGeneratedContextTableAccess (db : DataConnection) =
    use ctx = new GeneratedScaffoldDb(new DataOptions<GeneratedScaffoldDb>(db.Options))
    use _t = ctx.CreateLocalTable<GeneratedPerson>()

    ctx.Insert({ Id = 1; FirstName = "John"; MiddleName = Some "Q"; Age = Some 42 }) |> ignore

    let people = ctx.People |> Seq.toList
    Assert.That(people.Length, Is.EqualTo 1)
    Assert.That(people.[0].MiddleName, Is.EqualTo(Some "Q"))
