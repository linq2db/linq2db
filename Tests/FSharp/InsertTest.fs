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
          ID = 0 }

    let id = query { 
        for p in db.GetTable<Person>() do
        maxBy p.ID }
  //  try
    db.Insert(p) |> ignore

    let inserted = query { 
        for p in db.GetTable<ComplexPerson>() do
        where (p.ID > id)
        exactlyOne }
        
    Assert.AreEqual(p.Name.FirstName, inserted.Name.FirstName)
    Assert.AreEqual(p.Name.LastName, inserted.Name.LastName)
    Assert.AreEqual(p.Gender, inserted.Gender)

//    finally
        //db.GetTable<Person>().Delete(fun t -> t.ID > id) |> ignore
