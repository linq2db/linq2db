module Tests.FSharp.InsertTest

open Tests.FSharp.Models

open LinqToDB
open LinqToDB.Mapping
open NUnit.Framework
     
let Insert1 (db : IDataContext) = 
    let cleanup = fun() ->
        db.GetTable<Child>().Delete(fun c -> c.ChildID > 1000)
    try
        let id = 1001
        cleanup() |> ignore

        let child = {Child.ParentID=1; Child.ChildID=id}
        Assert.AreEqual(1, db.Insert(child))
        //Assert.AreEqual(1, db.GetTable<Child>().Insert(fun () -> {Child.ParentID=1; ChildID=id}))
        Assert.AreEqual(1, 
            query { 
                for c in db.GetTable<Child>() do 
                where (c.ChildID = id)
                count })

    finally
        cleanup() |> ignore


let Insert2 (db : IDataContext) = 
    
    let p = 
        { ComplexPerson.Name = { FirstName = "fn"; MiddleName = ""; LastName = "ln" }
          Gender = "M"
          ID = 0L }

    let id = db.InsertWithIdentity(p) :?> int64

    let inserted = query { 
        for p in db.GetTable<ComplexPerson>() do
        where (p.ID = id)
        exactlyOne }
        
    Assert.AreEqual(p.Name.FirstName, inserted.Name.FirstName)
    Assert.AreEqual(p.Name.LastName, inserted.Name.LastName)
    Assert.AreEqual(p.Gender, inserted.Gender)

let InsertNoneOption (db : IDataContext) = 
    
    let p = 
        { Person.FirstName = "fn"
          MiddleName = None
          LastName = "ln"
          Gender = Gender.Male
          ID = None }

    let id = db.InsertWithIdentity(p) :?> int64

    let p = { p with ID = Some(id) }

    let inserted = query { 
        for p in db.GetTable<Person>() do
        where (p.ID = Some(id))
        exactlyOne }
        
    Assert.AreEqual(p, inserted);

    Assert.AreEqual(None, inserted.MiddleName)

let InsertSomeOption (db : IDataContext) = 
    
    let p = 
        { Person.FirstName = "fn"
          MiddleName = Some("md")
          LastName = "ln"
          Gender = Gender.Male
          ID = None }

    let id = query { 
        for p in db.GetTable<Person>() do
        maxBy p.ID }

    let insertedID = db.InsertWithIdentity(p) :?> int64 |> Some

    let inserted = query { 
        for p in db.GetTable<Person>() do
        where (p.ID > id)
        exactlyOne }

    let shouldBeInserted = {p with ID = insertedID}
        
    Assert.AreEqual(shouldBeInserted, inserted)

//    finally
        //db.GetTable<Person>().Delete(fun t -> t.ID > id) |> ignore
