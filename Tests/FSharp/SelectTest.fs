module Tests.FSharp.SelectTest

open Tests.FSharp.Models

open LinqToDB
open LinqToDB.Mapping
open NUnit.Framework
open System.Linq

let SelectField (db : IDataContext) = 
    let persons = db.GetTable<Person>()
    let q = query {
        for p in persons do
        select p.LastName
    }

    let sql = q.ToString()
    Assert.That(sql.IndexOf("First"), Is.LessThan(0))
    Assert.That(sql.IndexOf("LastName"), Is.GreaterThan(0))

let SelectFieldDeeplyComplexPerson (db : IDataContext) = 
    let persons = db.GetTable<DeeplyComplexPerson>()
    let q = query {
        for p in persons do
        select p.Name.LastName.Value
    }

    let sql = q.ToString()
    Assert.That(sql.IndexOf("First"), Is.LessThan(0))
    Assert.That(sql.IndexOf("LastName"), Is.GreaterThan(0))

let SelectLeftJoin (db : IDataContext) = 
    let children = db.GetTable<Child>()
    let parents = db.GetTable<Parent>()

    let child = query {
        for child in children do
        leftOuterJoin parent in parents
            on (child.ParentID = parent.ParentID) into result
        for parent in result do
        where (parent.Value1 = 6)
        select child
        headOrDefault
    }

    Assert.NotNull(child)