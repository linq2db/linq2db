module Tests.FSharp.Issue3357

open System
open System.Linq

open Tests.FSharp.Models

open LinqToDB
open LinqToDB.Mapping
open Tests.Tools

type Record2 = {
    Id: int
    Name: string
}

let Union1(db : IDataContext) =
    let persons = db.GetTable<Person>()
    let query1 = query {
        for p in persons do
        where (p.ID = 1)
        select (p.ID, p.FirstName)
    }

    let query2 = query {
        for p in persons do
        where (p.ID = 1)
        select (p.ID, p.FirstName)
    }

    let result = query1.Concat(query2).ToArray()
    
    NUnitAssert.IsTrue(result[0] = (1, "John"));

let Union2(db : IDataContext) =
    let persons = db.GetTable<Person>()
    let query1 = query {
        for p in persons do
        where (p.ID = 1)
        select { Id = p.ID; Name = p.FirstName }
    }

    let query2 = query {
        for p in persons do
        where (p.ID = 1)
        select { Id = p.ID; Name = p.FirstName }
    }

    let result = query1.Concat(query2).ToArray()
    
    NUnitAssert.IsTrue(result[0] = {Id = 1 ; Name = "John" });

let Union3(db : IDataContext) =
    let persons = db.GetTable<Person>()
    let query1 = query {
        for p in persons do
        where (p.ID = 1)
        select {| Id = p.ID; Name = p.FirstName |}
    }

    let query2 = query {
        for p in persons do
        where (p.ID = 1)
        select {| Id = p.ID; Name = p.FirstName |}
    }

    let result = query1.Concat(query2).ToArray()
    
    NUnitAssert.IsTrue(result[0] = {| Id = 1 ; Name = "John" |});
