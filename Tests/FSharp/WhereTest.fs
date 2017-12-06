module Tests.FSharp.WhereTest

open System

open Tests.FSharp.Models

open LinqToDB
open LinqToDB.Mapping
open Tests.Tools

let private TestOnePerson id firstName persons =
    let list = persons :> Person System.Linq.IQueryable |> Seq.toList
    NUnitAssert.AreEqual(1, list |> List.length )

    let person = list |> List.head

    NUnitAssert.AreEqual(id, person.ID)
    NUnitAssert.AreEqual(firstName, person.FirstName)

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


let LoadSinglesWithPatient (db : IDataContext) =
    let persons = db.GetTable<Person>().LoadWith( fun x -> x.Patient :> Object )
    let johnId = 1
    let john = query {
        for p in persons do
        where (p.ID = johnId)
        exactlyOne
    }

    NUnitAssert.AreEqual(johnId, john.ID)
    NUnitAssert.AreEqual("John", john.FirstName)
    NUnitAssert.IsNull  (john.Patient)

    let testerId = 2
    let tester = query {
        for p in persons do
        where (p.ID = testerId)
        exactlyOne
    }

    NUnitAssert.AreEqual(testerId, tester.ID)
    NUnitAssert.AreEqual("Tester", tester.FirstName)
    NUnitAssert.IsNotNull( tester.Patient)
    NUnitAssert.AreEqual( tester.Patient.PersonID, testerId )


let LoadSingleComplexPerson (db : IDataContext) =
    let persons = db.GetTable<ComplexPerson>()
    let john = query {
        for p in persons do
        where (p.ID = TestMethod())
        exactlyOne
    }
    NUnitAssert.AreEqual(
        { ComplexPerson.ID=1
          Name = {FirstName="John"; MiddleName=null; LastName="Pupkin"}
          Gender="M" }
        , john)

let LoadSingleDeeplyComplexPerson (db : IDataContext) =
    let persons = db.GetTable<DeeplyComplexPerson>()
    let john = query {
        for p in persons do
        where (p.ID = TestMethod())
        exactlyOne
    }
    NUnitAssert.AreEqual(
        { DeeplyComplexPerson.ID=1
          Name = {FirstName="John"; MiddleName=null; LastName={Value="Pupkin"}}
          Gender="M" }
        , john)

let LoadColumnOfDeeplyComplexPerson (db : IDataContext) =
    let persons = db.GetTable<DeeplyComplexPerson>()
    let lastName = query {
        for p in persons do
        where (p.ID = TestMethod())
        select p.Name.LastName.Value
        exactlyOne
    }
    NUnitAssert.AreEqual("Pupkin", lastName)

let LoadSingleWithOptions (db : IDataContext) =
    let persons = db.GetTable<PersonWithOptions>()
    let john = query {
        for p in persons do
        where (p.ID = TestMethod())
        exactlyOne
    }
    NUnitAssert.AreEqual(
        { PersonWithOptions.ID=1
          FirstName = "John"
          MiddleName = None
          LastName = Some("Pupkin")
          Gender= Gender.Male
          }
        , john)

    NUnitAssert.IsTrue( match john.MiddleName with |None -> true;  |Some _ -> false );
    NUnitAssert.IsTrue( match john.LastName   with |None -> false; |Some _ -> true );



let LoadSingleCLIMutable (db : IDataContext)  (nullPatient : PatientCLIMutable)  =
    let persons = db.GetTable<PersonCLIMutable>().LoadWith( fun x -> x.Patient :> Object )
    let john = query {
        for p in persons do
        where (p.ID = 1)
        exactlyOne
    }

    NUnitAssert.IsNotNull( john )
    NUnitAssert.AreEqual( john.ID, 1 )
    NUnitAssert.IsNull( john.Patient )

    let tester = query {
        for p in persons do
        where (p.ID = 2)
        exactlyOne
    }

    NUnitAssert.IsNotNull( tester )
    NUnitAssert.AreEqual( tester.ID, 2 )
    NUnitAssert.IsNotNull( tester.Patient )
    NUnitAssert.AreEqual( tester.Patient.PersonID, 2 )
