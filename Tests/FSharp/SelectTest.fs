module Tests.FSharp.SelectTest

open Tests.FSharp.Models

open System.Linq
open LinqToDB
open NUnit.Framework

let SelectField (db : IDataContext) =
    let persons = db.GetTable<Person>()
    let q = query {
        for p in persons do
        select p.LastName
    }

    let sql = "" + q.ToString()
    Assert.That(sql.IndexOf("First"), Is.LessThan 0)
    Assert.That(sql.IndexOf("LastName"), Is.GreaterThan 0)

let SelectFieldDeeplyComplexPerson (db : IDataContext) =
    let persons = db.GetTable<DeeplyComplexPerson>()
    let q = query {
        for p in persons do
        select p.Name.LastName.Value
    }

    let sql = "" + q.ToString()
    Assert.That(sql.IndexOf("First"), Is.LessThan 0)
    Assert.That(sql.IndexOf("LastName"), Is.GreaterThan 0)


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

    Assert.That(child, Is.Not.Null)

let Issue3699Test (db : IDataContext) =
    let children = db.GetTable<Parent>()
    let parents = db.GetTable<Parent>()
    let pets = db.GetTable<Parent>()

    let q =
        parents
            .Join(children,
                (fun p -> p.ParentID),
                (fun c -> c.ParentID),
                (fun p c -> {| p = p; c = c |})
            )
            .GroupJoin(pets,
                (fun o -> o.p.ParentID),
                (fun pet -> pet.ParentID),
                (fun o pets -> {| p = o.p; c = o.c; pets = pets |})
            )
            .SelectMany((fun o -> o.pets.DefaultIfEmpty()),
                (fun o pet -> {| p = o.p; c = o.c; pet = pet |})
            )

    Assert.That(q.ToList(), Is.Not.Null)
