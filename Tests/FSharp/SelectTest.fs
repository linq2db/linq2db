module Tests.FSharp.SelectTest

open System.Linq

open Tests.FSharp.Models

open LinqToDB
open LinqToDB.Mapping
open NUnit.Framework

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

let LoadDeeplyComplexPersonTable (db : IDataContext) = 
    let persons = db.GetTable<DeeplyComplexPerson>()
    let list = query {
        for p in persons do
        select p
    }
    Assert.AreNotEqual(0, list.ToList().Count)
