module Tests.FSharp.Issue4646

open LinqToDB
open LinqToDB.Mapping
open NUnit.Framework
open Tests

[<Table("Issue4646Table", IsColumnAttributeRequired = false)>]
type OptionRow =
    { [<PrimaryKey>] Id : int
      IntValue        : int option
      StrValue        : string option }

// https://github.com/linq2db/linq2db/issues/195
// https://github.com/linq2db/linq2db/issues/4646
// Round-trips 'T option columns with no manual MappingSchema setup (UseFSharp() is applied by the
// test base). Asserts None reads back as None - in particular int option None must NOT become Some 0.
let TestOptionRoundtrip (db : IDataContext) =
    use _t = db.CreateLocalTable<OptionRow>()

    let someRow = { Id = 1; IntValue = Some 42; StrValue = Some "hello" }
    let noneRow = { Id = 2; IntValue = None;    StrValue = None }

    db.Insert(someRow) |> ignore
    db.Insert(noneRow) |> ignore

    let r1 = query { for r in db.GetTable<OptionRow>() do
                     where (r.Id = 1)
                     exactlyOne }
    let r2 = query { for r in db.GetTable<OptionRow>() do
                     where (r.Id = 2)
                     exactlyOne }

    Assert.That(r1.IntValue, Is.EqualTo(Some 42))
    Assert.That(r1.StrValue, Is.EqualTo(Some "hello"))
    Assert.That(r2.IntValue, Is.EqualTo None)
    Assert.That(r2.StrValue, Is.EqualTo None)
