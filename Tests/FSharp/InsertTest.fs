module Tests.FSharp.InsertTest

open Tests.FSharp.Models

open LinqToDB
open LinqToDB.Mapping
open NUnit.Framework
open Tests.Tools

let Insert1 (db : IDataContext) =
    let cleanup = fun() ->
        db.GetTable<Child>().Delete(fun c -> c.ChildID > 1000)
    try
        let id = 1001
        cleanup() |> ignore

        let child = {Child.ParentID=1; Child.ChildID=id}
        Assert.That(db.Insert(child), Is.EqualTo 1)
        //Assert.That(db.GetTable<Child>().Insert(fun () -> {Child.ParentID=1; ChildID=id}), Is.EqualTo 1)
        Assert.That(query {
                for c in db.GetTable<Child>() do
                where (c.ChildID = id)
                count }, Is.EqualTo 1)

    finally
        cleanup() |> ignore


let Insert2 (db : IDataContext, personId : int) =

    let p =
        { ComplexPerson.Name = { FirstName = "fn"; MiddleName = ""; LastName = "ln" }
          Gender = "M"
          ID = personId }

    let id = query {
        for p in db.GetTable<Person>() do
        maxBy p.ID }
    try
    db.Insert(p) |> ignore

    let inserted = query {
        for p in db.GetTable<ComplexPerson>() do
        where (p.ID > id)
        exactlyOne }

    Assert.That(inserted.Name.FirstName, Is.EqualTo p.Name.FirstName)
    Assert.That(inserted.Name.LastName, Is.EqualTo p.Name.LastName)
    Assert.That(inserted.Gender, Is.EqualTo p.Gender)

    finally
        db.GetTable<Person>().Delete(fun t -> t.ID > id) |> ignore
