﻿module Tests.FSharp.Issue2678

open LinqToDB
open LinqToDB.Mapping
open Tests
open Tests.Tools

[<Table("R")>]
[<Sealed>]
type RowClass() =
    [<PrimaryKey>]
    member val MetadataVersion = 0 with get, set
    [<Column>]
    member val DictionaryKey = 0 with get, set

[<Table("R")>]
[<CLIMutable>]
[<NoComparison; NoEquality>]
type RowRecord =
    { [<PrimaryKey>]
      MetadataVersion: int
      [<Column>]
      DictionaryKey: int }

let InsertAndSelectObject(db : IDataContext) =
    use table = db.CreateLocalTable<RowClass>()
    let row = RowClass(MetadataVersion = 2, DictionaryKey = 5)
    db.Insert row |> ignore
    let selectedRow = query {
        for row in table do
        select row
        exactlyOne
    }
    NUnitAssert.AreEqual(row.MetadataVersion, selectedRow.MetadataVersion)
    NUnitAssert.AreEqual(row.DictionaryKey, selectedRow.DictionaryKey)

let InsertAndSelectRecord(db : IDataContext) =
    use table = db.CreateLocalTable<RowRecord>()
    let row = { MetadataVersion = 2; DictionaryKey = 5 }
    db.Insert row |> ignore
    let selectedRow = query {
        for row in table do
        select row
        exactlyOne
    }
    NUnitAssert.AreEqual(row.MetadataVersion, selectedRow.MetadataVersion)
    NUnitAssert.AreEqual(row.DictionaryKey, selectedRow.DictionaryKey)
