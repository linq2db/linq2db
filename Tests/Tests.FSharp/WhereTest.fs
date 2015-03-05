module Tests.FSharp.WhereTest

open LinqToDB
open LinqToDB.Mapping
open NUnit.Framework

type Gender = 
    | [<MapValue("M")>] Male = 0
    | [<MapValue("F")>] Female = 1
    | [<MapValue("U")>] Unknown = 2 
    | [<MapValue("O")>] Other = 3
type PersonID = int

type Person = 
    { [<SequenceName(ProviderName.Firebird, "PersonID")>]
      [<Column("PersonID"); Identity; PrimaryKey>]
      ID : int 
      [<NotNull>] 
      FirstName : string
      [<NotNull>]
      LastName : string
      [<Nullable>]
      MiddleName : string 
      Gender : Gender }
//      [<Association(ThisKey = "ID", OtherKey = "PersonID", CanBeNull=true)>]
//      Patient : Patient }

//and Patient =
//    { [<PrimaryKey>]
//      PersonID : PersonID
//      Diagnosis : string
//      [<Association(ThisKey = "PersonID", OtherKey = "ID", CanBeNull = false)>]
//      Person : Person }

let private TestOnePerson id firstName persons = //(int id, string firstName, IQueryable<Person> persons)
    let list = persons :> Person System.Linq.IQueryable |> Seq.toList
    Assert.AreEqual(1, list |> List.length )

    let person = list |> List.head

    Assert.AreEqual(id, person.ID)
    Assert.AreEqual(firstName, person.FirstName)

let TestOneJohn = TestOnePerson 1 "John"

let TestMethod() = 
    1
     
let LoadSingle (db : IDataContext) = 
    let persons = db.GetTable<Person>()
    TestOneJohn(query {
        for p in persons do
        where (p.ID = TestMethod())
        select p
    })
    //TestOneJohn(from p in db.Person where p.ID == TestMethod() select p);