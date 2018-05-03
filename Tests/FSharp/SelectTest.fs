module Tests.FSharp.SelectTest

open Tests.FSharp.Models

open LinqToDB
open LinqToDB.Mapping
open Tests.Tools

let SelectField (db : IDataContext) =
    let persons = db.GetTable<Person>()
    let q = query {
        for p in persons do
        select p.LastName
    }

    let sql = q.ToString()
    NUnitAssert.ThatIsLessThan(sql.IndexOf("First"), 0)
    NUnitAssert.ThatIsGreaterThan(sql.IndexOf("LastName"), 0)

let SelectFieldDeeplyComplexPerson (db : IDataContext) =
    let persons = db.GetTable<DeeplyComplexPerson>()
    let q = query {
        for p in persons do
        select p.Name.LastName.Value
    }

    let sql = q.ToString()
    NUnitAssert.ThatIsLessThan(sql.IndexOf("First"), 0)
    NUnitAssert.ThatIsGreaterThan(sql.IndexOf("LastName"), 0)
