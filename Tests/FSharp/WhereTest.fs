module Tests.FSharp.WhereTest

open System
open System.Linq

open Tests.FSharp.Models

open LinqToDB
open LinqToDB.Data
open LinqToDB.Mapping
open NUnit.Framework

let private TestOnePerson id firstName persons =
    let list = persons :> Person System.Linq.IQueryable |> Seq.toList
    Assert.That(list |> List.length, Is.EqualTo 1 )

    let person = list |> List.head

    Assert.That(person.ID, Is.EqualTo(id : int))
    Assert.That(person.FirstName, Is.EqualTo(firstName : string))

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
    let persons = db.GetTable<Person>().LoadWith( fun x -> x.Patient)
    let johnId = 1
    let john = query {
        for p in persons do
        where (p.ID = johnId)
        exactlyOne
    }

    Assert.That(john.ID, Is.EqualTo johnId)
    Assert.That(john.FirstName, Is.EqualTo "John")
    Assert.That(john.Patient, Is.Null)

    let testerId = 2
    let tester = query {
        for p in persons do
        where (p.ID = testerId)
        exactlyOne
    }

    Assert.That(tester.ID, Is.EqualTo testerId)
    Assert.That(tester.FirstName, Is.EqualTo "Tester")
    Assert.That(tester.Patient, Is.Not.Null)
    Assert.That(tester.Patient.PersonID, Is.EqualTo testerId)


let LoadSingleComplexPerson (db : IDataContext) =
    let persons = db.GetTable<ComplexPerson>()
    let john = query {
        for p in persons do
        where (p.ID = TestMethod())
        exactlyOne
    }

    Assert.That(john.ID, Is.EqualTo 1)
    Assert.That(john.Name, Is.Not.Null)
    Assert.That(john.Name.FirstName, Is.EqualTo "John")
    Assert.That(john.Name.MiddleName, Is.Null)
    Assert.That(john.Name.LastName, Is.EqualTo "Pupkin")
    Assert.That(john.Gender, Is.EqualTo "M")

let LoadSingleDeeplyComplexPerson (db : IDataContext) =
    let persons = db.GetTable<DeeplyComplexPerson>()
    let john = query {
        for p in persons do
        where (p.ID = TestMethod())
        exactlyOne
    }

    Assert.That(john.ID, Is.EqualTo 1)
    Assert.That(john.Name, Is.Not.Null)
    Assert.That(john.Name.FirstName, Is.EqualTo "John")
    Assert.That(john.Name.MiddleName, Is.Null)
    Assert.That(john.Name.LastName, Is.EqualTo { Value = "Pupkin" })
    Assert.That(john.Gender, Is.EqualTo "M")

let LoadColumnOfDeeplyComplexPerson (db : IDataContext) =
    let persons = db.GetTable<DeeplyComplexPerson>()
    let lastName = query {
        for p in persons do
        where (p.ID = TestMethod())
        select p.Name.LastName.Value
        exactlyOne
    }
    Assert.That(lastName, Is.EqualTo "Pupkin")

let LoadSingleWithOptions (db : IDataContext) =
    let persons = db.GetTable<PersonWithOptions>()
    let john = query {
        for p in persons do
        where (p.ID = TestMethod())
        exactlyOne
    }

    Assert.That(john.ID, Is.EqualTo 1)
    Assert.That(john.FirstName, Is.EqualTo "John")
    Assert.That(john.MiddleName, Is.EqualTo None)
    Assert.That(john.LastName, Is.EqualTo(Some("Pupkin")))
    Assert.That(john.Gender, Is.EqualTo Gender.Male)
    Assert.That( (match john.MiddleName with |None -> true;  |Some _ -> false), Is.True );
    Assert.That( (match john.LastName   with |None -> false; |Some _ -> true), Is.True );



let LoadSingleCLIMutable (db : IDataContext)  =
    let persons = db.GetTable<PersonCLIMutable>().LoadWith( fun x -> x.Patient )
    let john = query {
        for p in persons do
        where (p.ID = 1)
        exactlyOne
    }

    Assert.That( john, Is.Not.Null)
    Assert.That( john.ID, Is.EqualTo 1 )
    Assert.That( john.Patient, Is.Null )

    let tester = query {
        for p in persons do
        where (p.ID = 2)
        exactlyOne
    }

    Assert.That( tester, Is.Not.Null )
    Assert.That( tester.ID, Is.EqualTo 2 )
    Assert.That( tester.Patient, Is.Not.Null )
    Assert.That( tester.Patient.PersonID, Is.EqualTo 2 )

let RecordParametersMapping (db : IDataContext) =
    let persons = db.GetTable<PersonConflictingNamesRecord>()
    let john = query {
        for p in persons do
        where (p.ID = 1)
        exactlyOne
    }

    Assert.That(john, Is.Not.Null)
    Assert.That(john.ID, Is.EqualTo 1)
    Assert.That(john.id, Is.EqualTo "John")
    Assert.That(john.Id, Is.EqualTo "Pupkin")
    Assert.That(john.iD, Is.Null)

let RecordProjectionColumnsOnly (db : IDataContext) =
    let persons = query {
        for p in db.GetTable<Person>() do
        select {
            ID = p.ID
            id = p.FirstName
            Id = p.LastName
            iD = "ибн Алёша"
        }
    }

    let john = query {
        for p in persons do
        where (p.ID = 1 && p.iD = "ибн Алёша")
        exactlyOne
    }

    Assert.That(john, Is.Not.Null)
    Assert.That(john.ID, Is.EqualTo 1)
    Assert.That(john.id, Is.EqualTo "John")
    Assert.That(john.Id, Is.EqualTo "Pupkin")
    Assert.That(john.iD, Is.EqualTo "ибн Алёша")

let RecordProjectionAll (db : IDataContext) =
    let persons = query {
        for p in db.GetTable<Person>() do
        select {
            ID = p.ID
            FirstName = p.FirstName
            LastName = p.LastName
            MiddleName = "ибн Алёша"
            Gender = p.Gender
            Patient = Unchecked.defaultof<PatientCLIMutable>
        }
    }

    let john = query {
        for p in persons do
        where (p.ID = 1 && p.MiddleName = "ибн Алёша")
        exactlyOne
    }

    Assert.That(john, Is.Not.Null)
    Assert.That(john.ID, Is.EqualTo 1)
    Assert.That(john.FirstName, Is.EqualTo "John")
    Assert.That(john.LastName, Is.EqualTo "Pupkin")
    Assert.That(john.MiddleName, Is.EqualTo "ибн Алёша")

let RecordComplexProjection (db : IDataContext) =
    let persons = query {
        for p in db.GetTable<Person>() do
        select {
            ID = p.ID
            Name = {
                id = p.FirstName
                Id = p.LastName
                iD = "ибн Алёша"
                unused = -1
            }
        }
    }

    let john = query {
        for p in persons do
        where (p.ID = 1 && p.Name.unused = -1 && p.Name.id = "John" && p.Name.Id = "Pupkin")
        exactlyOne
    }

    Assert.That(john, Is.Not.Null)
    Assert.That(john.ID, Is.EqualTo 1)
    Assert.That(john.Name.id, Is.EqualTo "John")
    Assert.That(john.Name.Id, Is.EqualTo "Pupkin")
    Assert.That(john.Name.iD, Is.EqualTo "ибн Алёша")

let ComplexRecordParametersMapping (db : IDataContext) =
    let persons = db.GetTable<ComplexPersonRecord>()
    let john = query {
        for p in persons do
        where (p.ID = 1)
        exactlyOne
    }

    Assert.That(john, Is.Not.Null)
    Assert.That(john.ID, Is.EqualTo 1)
    Assert.That(john.Name, Is.Not.Null)
    Assert.That(john.Name.id, Is.EqualTo "John")
    Assert.That(john.Name.Id, Is.EqualTo "Pupkin")
    Assert.That(john.Name.iD, Is.Null)

let ComplexRecordParametersMappingUsingRecordReaderBuilder (db : DataConnection) =
    let john = db.Execute<ComplexPersonRecord>("select LastName, PersonID, FirstName, MiddleName FROM Person WHERE PersonID = 1")

    Assert.That(john, Is.Not.Null)
    Assert.That(john.ID, Is.EqualTo 1)
    Assert.That(john.Name, Is.Not.Null)
    Assert.That(john.Name.id, Is.EqualTo "John")
    Assert.That(john.Name.Id, Is.EqualTo "Pupkin")
    Assert.That(john.Name.iD, Is.Null)
    Assert.That(john.Name.unused, Is.EqualTo(0))

let UnionRecord1 (db : IDataContext) =
    let persons = db.GetTable<ComplexPersonRecord>()
    let query = query {
        for p in persons do
        where (p.ID = 1)
    }

    let john = query.Union(query).ToList().Single()

    Assert.That(john, Is.Not.Null)
    Assert.That(john.ID, Is.EqualTo 1)
    Assert.That(john.Name, Is.Not.Null)
    Assert.That(john.Name.id, Is.EqualTo "John")
    Assert.That(john.Name.Id, Is.EqualTo "Pupkin")
    Assert.That(john.Name.iD, Is.Null)

let UnionRecord2 (db : IDataContext) =
    let persons = db.GetTable<PersonConflictingNamesRecord>()
    let query = query {
        for p in persons do
        where (p.ID = 1)
    }

    let john = query.Union(query).ToList().Single()

    Assert.That(john, Is.Not.Null)
    Assert.That(john.ID, Is.EqualTo 1)
    Assert.That(john.id, Is.EqualTo "John")
    Assert.That(john.Id, Is.EqualTo "Pupkin")
    Assert.That(john.iD, Is.Null)
