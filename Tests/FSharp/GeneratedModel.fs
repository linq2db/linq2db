module Tests.FSharp.GeneratedModel

open System.Linq

open LinqToDB
open LinqToDB.Data
open LinqToDB.Mapping

open NUnit.Framework
open Tests

// Mirrors the shape the CLI scaffolder emits for F# (issue #1553): an idiomatic record with
// [<Table>]/[<Column>] attributes on fields (explicit DB column names + DataType), nullable columns as
// 'T option. The test proves linq2db materializes and round-trips that shape on every provider (UseFSharp()
// is applied by the test base); the generated code itself runs in GeneratedCode.fs.

[<Table("GeneratedScaffoldPerson")>]
[<NoComparison>]
type GeneratedPerson =
    {
        [<Column("PersonID", DataType = DataType.Int32, IsPrimaryKey = true)>] Id : int
        [<Column("FirstName", DataType = DataType.NVarChar, Length = 50)>] FirstName : string
        [<Column("MiddleName", DataType = DataType.NVarChar, Length = 50)>] MiddleName : string option
        [<Column("Age", DataType = DataType.Int32)>] Age : int option
    }

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

// The generated function wrappers' shape (DataModelGenerator.Functions): nullable scalar parameters are 'T option and
// reach the query expression unchanged (only stored-procedure wrappers unwrap them). SQLite built-ins stand in for
// database functions.
type GeneratedFunctionRow =
    {
        [<Column("key")>] Key : int64
    }

type GeneratedFunctionsDb (options : DataOptions<GeneratedFunctionsDb>) =
    inherit DataConnection(options.Options)

    [<Sql.TableFunction("json_each")>]
    member this.JsonEach (json : string option) : IQueryable<GeneratedFunctionRow> = this.QueryFromExpression<GeneratedFunctionRow>((fun () -> this.JsonEach(json)))

    [<Sql.Function("abs", ServerSideOnly = true)>]
    static member Abs (value : int option) : int option = raise (System.InvalidOperationException("Scalar function cannot be called outside of query"))

let TestFunctionOptionArguments (db : DataConnection) =
    use ctx = new GeneratedFunctionsDb(new DataOptions<GeneratedFunctionsDb>(db.Options))

    let some : int option = Some -5
    let none : int option = None
    let json : string option = Some "[10,20,30]"
    let noJson : string option = None

    Assert.That(ctx.Select(fun () -> GeneratedFunctionsDb.Abs(some)), Is.EqualTo(Some 5))
    Assert.That(ctx.Select(fun () -> GeneratedFunctionsDb.Abs(none)), Is.EqualTo(None : int option))
    let rows   = ctx.JsonEach(json)
    let noRows = ctx.JsonEach(noJson)
    Assert.That(rows.Count(),   Is.EqualTo 3)
    Assert.That(noRows.Count(), Is.EqualTo 0)
